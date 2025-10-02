using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using WeatherWeb.Application.Abstractions;
using WeatherWeb.Application.Features.Chat.DTOs;
using WeatherWeb.Application.Features.Weather.DTOs;

namespace WeatherWeb.Infrastructure.Chat;

public sealed class RuleBasedChatBot : IChatBot
{
    private readonly IWeatherService _weather;

    public RuleBasedChatBot(IWeatherService weather) => _weather = weather;

    // --- Regex tiếng Việt đơn giản ---
    private static readonly Regex ReCity =
        new(@"(?:\bở\b|\btại\b)\s+(?<city>[A-Za-zÀ-ỹ\s\.]+)|\b(?<city>Đà Lạt|Hà Nội|Ho Chi Minh|TP\.? HCM|Đà Nẵng|Cần Thơ|Hải Phòng)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ReNow =
        new(@"(bây giờ|lúc này|hiện tại)", RegexOptions.IgnoreCase);

    private static readonly Regex ReToday =
        new(@"(h(ô|o)m nay|hnay|hôm nay|nay)", RegexOptions.IgnoreCase);

    private static readonly Regex ReTomorrow =
        new(@"(ng(à|a)y mai|mai)", RegexOptions.IgnoreCase);

    private static readonly Regex ReRain =
        new(@"(mưa|mua)", RegexOptions.IgnoreCase); // xử lý gõ không dấu đơn giản

    private static readonly Regex ReGoOut =
        new(@"(ra ngoài|đi ra ngoài|có nên .* ra ngoài)", RegexOptions.IgnoreCase);

    public async Task<ChatReply> AskAsync(string message, ChatContext context, CancellationToken ct = default)
    {
        // 1) Hiểu ý người dùng
        var intent = DetectIntent(message);
        var timeScope = DetectTimeScope(message);
        var city = ExtractCity(message) ?? context.LastCity;

        // Nếu không có city mà có GPS: dùng GPS
        bool useGps = false;
        WeatherViewModel? vm = null;

        if (city is null && context.Latitude is double lat && context.Longitude is double lon)
        {
            useGps = true;
            vm = await _weather.GetCurrentByCoordinatesAsync(lat, lon, ct);
        }
        else
        {
            city ??= "Ho Chi Minh"; // fallback mềm
            vm = await _weather.GetCurrentAsync(city, ct);
        }

        if (vm is null)
        {
            return new ChatReply
            {
                Text = $"Mình không tìm thấy dữ liệu thời tiết cho “{city ?? "vị trí hiện tại"}”. Bạn có thể thử tên khác (vd: “Đà Lạt”).",
                CityUsed = city,
                UsedGps = useGps
            };
        }

        // 2) Trả lời theo intent + thời gian
        string answer = intent switch
        {
            Intent.Rain => AnswerRain(vm, timeScope),
            Intent.GoOut => AnswerGoOut(vm, timeScope),
            _ => AnswerWeather(vm, timeScope)
        };

        return new ChatReply
        {
            Text = answer,
            CityUsed = useGps ? "Vị trí hiện tại" : vm.LocationName,
            UsedGps = useGps
        };
    }

    // --- Intent đơn giản ---
    private enum Intent { Weather, Rain, GoOut }

    private static Intent DetectIntent(string msg)
    {
        if (ReGoOut.IsMatch(msg)) return Intent.GoOut;
        if (ReRain.IsMatch(msg)) return Intent.Rain;
        return Intent.Weather;
    }

    private enum TimeScope { Now, Today, Tomorrow }

    private static TimeScope DetectTimeScope(string msg)
    {
        if (ReTomorrow.IsMatch(msg)) return TimeScope.Tomorrow;
        if (ReToday.IsMatch(msg)) return TimeScope.Today;
        if (ReNow.IsMatch(msg)) return TimeScope.Now;
        // Nếu không nói rõ: ưu tiên Now (trừ khi hỏi mưa thì Today được xử lý trong AnswerRain)
        return TimeScope.Now;
    }

    private static string? ExtractCity(string msg)
    {
        var m = ReCity.Match(msg);
        if (m.Success)
        {
            var city = m.Groups["city"].Value.Trim();
            return string.IsNullOrWhiteSpace(city) ? null : city;
        }
        return null;
    }

    // --- Helpers & generator câu trả lời ---
    private static string ConditionLabel(int? code) => code switch
    {
        0 => "trời quang mây",
        1 or 2 => "ít mây",
        3 => "nhiều mây",
        45 or 48 => "sương mù",
        51 or 53 or 55 or 56 or 57 => "mưa phùn",
        61 or 63 or 65 => "mưa",
        71 or 73 or 75 => "tuyết",
        80 or 81 or 82 => "mưa rào",
        95 or 96 or 99 => "dông",
        _ => "không xác định"
    };

    private static bool IsRainCode(int? code) =>
        code is 51 or 53 or 55 or 56 or 57 or 61 or 63 or 65 or 80 or 81 or 82 or 95 or 96 or 99;

    private static int? NextHoursMaxRainProb(WeatherViewModel vm, int hours = 3)
    {
        if (vm.Hourly is null || vm.Hourly.Count == 0) return null;
        var now = DateTimeOffset.Now;
        var next = vm.Hourly
                     .Where(h => h.Time >= now)
                     .OrderBy(h => h.Time)
                     .Take(hours);
        return next.Any() ? next.Max(h => h.PrecipProb ?? 0) : null;
    }

    private static string DayLabel(DateTimeOffset d)
    {
        var local = d.ToLocalTime().Date;
        var today = DateTime.Today;
        if (local == today) return "hôm nay";
        if (local == today.AddDays(1)) return "ngày mai";
        return local.DayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ 2",
            DayOfWeek.Tuesday => "Thứ 3",
            DayOfWeek.Wednesday => "Thứ 4",
            DayOfWeek.Thursday => "Thứ 5",
            DayOfWeek.Friday => "Thứ 6",
            DayOfWeek.Saturday => "Thứ 7",
            DayOfWeek.Sunday => "Chủ nhật",
            _ => local.ToString("dd/MM")
        };
    }

    // Tìm khung giờ khô ráo sớm nhất trong N giờ tới
    private static (DateTimeOffset time, int prob)? FindNextDryHour(WeatherViewModel vm, int withinHours = 12, int threshold = 30)
    {
        if (vm.Hourly is null || vm.Hourly.Count == 0) return null;
        var now = DateTimeOffset.Now;
        var cand = vm.Hourly
            .Where(h => h.Time > now && h.Time <= now.AddHours(withinHours))
            .Select(h => new { h.Time, Prob = h.PrecipProb ?? 0, h.WeatherCode })
            .Where(x => x.Prob <= threshold && !IsRainCode(x.WeatherCode))
            .OrderBy(x => x.Time)
            .FirstOrDefault();

        return cand is null ? null : (cand.Time, cand.Prob);
    }

    // Chấm điểm ngày đẹp trong vài ngày tới (ít mưa, nhiệt độ dễ chịu ~27°C, không dông)
    private static (DailyForecastItem day, double score)? BestDay(WeatherViewModel vm, int days = 5)
    {
        if (vm.Daily is null || vm.Daily.Count == 0) return null;

        var today = DateTimeOffset.Now.Date;
        var best = vm.Daily
            .Where(d => d.Date.Date >= today && d.Date.Date <= today.AddDays(days))
            .Select(d => new
            {
                Day = d,
                Precip = d.PrecipProbMax ?? 0,
                TempMid = (d.MaxC + d.MinC) / 2.0,
                RainPenalty = IsRainCode(d.WeatherCode) ? 15 : 0
            })
            .Select(x => new
            {
                x.Day,
                Score = 100 - x.Precip
                            - Math.Min(25, Math.Abs(x.TempMid - 27) * 3)
                            - x.RainPenalty
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return best is null ? null : (best.Day, best.Score);
    }

    // --- Trả lời theo intent ---

    private static string AnswerRain(WeatherViewModel vm, TimeScope scope)
    {
        if (scope == TimeScope.Today && vm.Daily?.Count > 0)
        {
            var today = vm.Daily.First();
            var prob = today.PrecipProbMax ?? 0;
            var label = ConditionLabel(today.WeatherCode);

            if (prob >= 60)
            {
                var nextDry = FindNextDryHour(vm, withinHours: 12, threshold: 30);
                if (nextDry is { } g)
                {
                    var hh = g.time.ToLocalTime().ToString("HH:mm");
                    return $"Hôm nay khả năng mưa **cao** (~{prob}%), {label}. **Nên đợi đến {hh}** khi mưa giảm (~{g.prob}%) rồi hãy di chuyển.";
                }
                return $"Hôm nay khả năng mưa **cao** (~{prob}%), {label}. Nên hạn chế di chuyển ngoài trời.";
            }

            return $"Hôm nay mưa khoảng **{prob}%**, {label}. Bạn vẫn có thể sắp xếp ra ngoài; kiểm tra lại sát giờ để chắc chắn.";
        }

        if (scope == TimeScope.Tomorrow && vm.Daily?.Count > 1)
        {
            var d = vm.Daily[1];
            var prob = d.PrecipProbMax ?? 0;
            var label = ConditionLabel(d.WeatherCode);
            return prob >= 60
                ? $"Ngày mai khả năng mưa **cao** (~{prob}%), {label}. Cân nhắc dời lịch."
                : $"Ngày mai mưa khoảng **{prob}%**, {label}. Khá ổn để đi nếu bạn linh hoạt thời gian.";
        }

        var nowLabel = ConditionLabel(vm.WeatherCode);
        var next3 = NextHoursMaxRainProb(vm, 3) ?? 0;
        if (IsRainCode(vm.WeatherCode) || next3 >= 60)
        {
            var nextDry = FindNextDryHour(vm, withinHours: 12, threshold: 30);
            if (nextDry is { } g)
            {
                var hh = g.time.ToLocalTime().ToString("HH:mm");
                return $"Hiện tại: {nowLabel}. **Nên đợi tới {hh}** khi xác suất mưa còn ~{g.prob}% rồi hãy ra ngoài.";
            }
            return $"Hiện tại: {nowLabel}. Mưa có thể kéo dài vài giờ tới (≈{next3}%). Cân nhắc ở trong nhà.";
        }

        return $"Hiện tại: {nowLabel}. Khả năng mưa thấp trong vài giờ tới (≈{next3}%). Bạn có thể ra ngoài.";
    }

    private static string AnswerGoOut(WeatherViewModel vm, TimeScope scope)
    {
        if (scope == TimeScope.Tomorrow && vm.Daily?.Count > 1)
        {
            var d = vm.Daily[1];
            var ok = (d.PrecipProbMax ?? 0) < 40 && !IsRainCode(d.WeatherCode);
            var label = ConditionLabel(d.WeatherCode);
            return ok
                ? $"Ngày mai **đi được**: {label}, {d.MinC:0.#}–{d.MaxC:0.#}°C, mưa ~{(d.PrecipProbMax ?? 0)}%. Mang nước/áo khoác nhẹ là ổn."
                : $"Ngày mai **không lý tưởng** (mưa ~{(d.PrecipProbMax ?? 0)}%, {label}). Bạn cân nhắc lùi lịch sang ngày khác ít mưa hơn.";
        }

        var nowLabel = ConditionLabel(vm.WeatherCode);
        var next3 = NextHoursMaxRainProb(vm, 3) ?? 0;
        var rainyNow = IsRainCode(vm.WeatherCode) || next3 >= 60;

        if (!rainyNow)
        {
            var tip = vm.TemperatureC >= 33 ? "Trời khá nóng, nhớ chống nắng và uống đủ nước."
                    : vm.TemperatureC <= 20 ? "Hơi lạnh, mang áo khoác mỏng."
                    : "Thời tiết dễ chịu.";
            return $"Bạn **có thể ra ngoài ngay**. Hiện tại: {nowLabel}, {vm.TemperatureC:0.#}°C, gió {vm.WindSpeed:0.#} m/s. {tip}";
        }

        var nextDry = FindNextDryHour(vm, withinHours: 12, threshold: 30);
        if (nextDry is { } g)
        {
            var hh = g.time.ToLocalTime().ToString("HH:mm");
            return $"Hiện **không thuận lợi** (mưa/khả năng mưa cao). **Nên đợi tới {hh}** khi xác suất mưa còn khoảng {g.prob}% rồi hãy đi.";
        }

        var best = BestDay(vm, days: 5);
        if (best is { } b)
        {
            var dayLbl = DayLabel(b.day.Date);
            var cond = ConditionLabel(b.day.WeatherCode);
            var prob = b.day.PrecipProbMax ?? 0;
            return $"Hôm nay xấu, mưa có thể kéo dài. **Gợi ý**: chờ **{dayLbl}** – {cond}, {b.day.MinC:0.#}–{b.day.MaxC:0.#}°C, mưa ~{prob}%, đi sẽ đẹp hơn.";
        }

        return "Thời tiết hiện chưa thuận lợi để ra ngoài và chưa có khung giờ khô ráo rõ ràng trong hôm nay. Bạn cân nhắc dời lịch.";
    }

    private static string AnswerWeather(WeatherViewModel vm, TimeScope scope)
    {
        if (scope == TimeScope.Tomorrow && vm.Daily?.Count > 1)
        {
            var d = vm.Daily[1];
            var label = ConditionLabel(d.WeatherCode);
            return $"Dự báo **ngày mai**: {label}. Nhiệt độ {d.MinC:0.#}–{d.MaxC:0.#}°C. Khả năng mưa tối đa ~{(d.PrecipProbMax ?? 0)}%.";
        }
        if (scope == TimeScope.Today && vm.Daily?.Count > 0)
        {
            var d = vm.Daily[0];
            var label = ConditionLabel(d.WeatherCode);
            return $"Dự báo **hôm nay**: {label}. Nhiệt độ {d.MinC:0.#}–{d.MaxC:0.#}°C. Khả năng mưa tối đa ~{(d.PrecipProbMax ?? 0)}%.";
        }

        var nowLabel = ConditionLabel(vm.WeatherCode);
        return $"**Hiện tại**: {nowLabel}, {vm.TemperatureC:0.#}°C, gió {vm.WindSpeed:0.#} m/s. Tầm nhìn {(vm.VisibilityKm is double v ? $"{v:0.#} km" : "—")}, áp suất {(vm.PressureHpa is double p ? $"{p:0} hPa" : "—")}.";
    }
}
