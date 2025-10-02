using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WeatherWeb.Application.Abstractions;
using WeatherWeb.Application.Features.Weather.DTOs;
using WeatherWeb.Domain.Entities;
using WeatherWeb.Domain.ValueObjects;

namespace WeatherWeb.Infrastructure.Weather;

public sealed class OpenMeteoService : IWeatherService
{
    private readonly IHttpClientFactory _factory;

    private static string ConditionFromWmo(int code) => code switch
    {
        0 => "Trời quang",
        1 or 2 => "Ít mây",
        3 => "Nhiều mây",
        45 or 48 => "Sương mù",
        51 or 53 or 55 or 56 or 57 => "Mưa phùn",
        61 or 63 or 65 => "Mưa",
        71 or 73 or 75 => "Tuyết",
        80 or 81 or 82 => "Mưa rào",
        95 or 96 or 99 => "Dông",
        _ => "Không xác định"
    };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public OpenMeteoService(IHttpClientFactory factory) => _factory = factory;

    // helper chung để gọi /v1/forecast lấy current+hourly+daily
    private async Task<OpenMeteoForecastResponse?> FetchForecastAsync(double lat, double lon, CancellationToken ct)
    {
        var api = _factory.CreateClient("OpenMeteo.Api");
        var url =
            $"v1/forecast?latitude={lat}&longitude={lon}" +
            "&current=temperature_2m,apparent_temperature,wind_speed_10m,cloud_cover,pressure_msl,visibility,weather_code" +
            "&hourly=temperature_2m,weather_code,precipitation_probability" +
            "&daily=temperature_2m_max,temperature_2m_min,weather_code,precipitation_probability_max" +
            "&temperature_unit=celsius&wind_speed_unit=ms&precipitation_unit=mm&timeformat=iso8601&timezone=auto";

        return await api.GetFromJsonAsync<OpenMeteoForecastResponse>(url, JsonOpts, ct);
    }

    // Map response -> ViewModel 
    private WeatherViewModel MapToVm(OpenMeteoForecastResponse resp, double lat, double lon, string placeLabel)
    {
        var cur = resp.Current!;
        var loc = new Location(placeLabel, null, new Coordinates(lat, lon));
        var snapshot = new WeatherSnapshot(
            loc,
            new Temperature(cur.Temperature2m),
            new WindSpeed(cur.WindSpeed10m),
            DateTimeOffset.TryParse(cur.Time, out var t) ? t : DateTimeOffset.UtcNow
        );

        var vm = new WeatherViewModel
        {
            LocationName = placeLabel,
            Latitude = lat,
            Longitude = lon,
            TemperatureC = snapshot.Temperature.Celsius,
            FeelsLikeC = cur.ApparentTemperature,
            WindSpeed = snapshot.Wind.Mps,
            CloudCoverPercent = cur.CloudCover,
            PressureHpa = cur.PressureMsl ?? cur.SurfacePressure,
            VisibilityKm = cur.Visibility is double vis ? vis / 1000.0 : null,
            ObservedAt = snapshot.ObservedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            WeatherCode = cur.WeatherCode,
            Condition = ConditionFromWmo(cur.WeatherCode)
        };

        // Hourly
        if (resp.Hourly?.Time is { Length: > 0 } ht && resp.Hourly.Temperature2m is { Length: > 0 } ht2)
        {
            var n = new[] {
            ht.Length, ht2.Length,
            resp.Hourly.WeatherCode?.Length ?? ht.Length,
            resp.Hourly.PrecipProb?.Length  ?? ht.Length
        }.Min();

            var now = DateTimeOffset.Now.AddMinutes(-30);
            for (int i = 0; i < n && vm.Hourly.Count < 7; i++)
            {
                if (!DateTimeOffset.TryParse(ht[i], out var tt)) continue;
                if (tt < now) continue;
                vm.Hourly.Add(new HourlyForecastItem
                {
                    Time = tt,
                    TempC = ht2[i],
                    WeatherCode = resp.Hourly.WeatherCode is { } wc ? wc[i] : 0,
                    PrecipProb = resp.Hourly.PrecipProb is { } pp ? pp[i] : null
                });
            }
        }

        // Daily
        if (resp.Daily?.Time is { Length: > 0 } dt &&
            resp.Daily.Temperature2mMax is { Length: > 0 } dmax &&
            resp.Daily.Temperature2mMin is { Length: > 0 } dmin)
        {
            var n = new[] {
            dt.Length, dmax.Length, dmin.Length,
            resp.Daily.WeatherCode?.Length ?? dt.Length,
            resp.Daily.PrecipProbMax?.Length ?? dt.Length
        }.Min();

            for (int i = 0; i < n && i < 7; i++)
            {
                if (!DateTimeOffset.TryParse(dt[i], out var dd)) continue;
                vm.Daily.Add(new DailyForecastItem
                {
                    Date = dd,
                    MaxC = dmax[i],
                    MinC = dmin[i],
                    WeatherCode = resp.Daily.WeatherCode is { } wc ? wc[i] : 0,
                    PrecipProbMax = resp.Daily.PrecipProbMax is { } pp ? pp[i] : null
                });
            }
        }

        return vm;
    }

    public async Task<WeatherViewModel?> GetCurrentAsync(string location, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(location)) return null;

        var geo = _factory.CreateClient("OpenMeteo.Geocoding");
        var g = await geo.GetFromJsonAsync<GeocodingResult>(
            $"v1/search?name={Uri.EscapeDataString(location)}&count=1&language=vi&format=json",
            JsonOpts, ct);
        var first = g?.Results?.FirstOrDefault();
        if (first is null) return null;

        var resp = await FetchForecastAsync(first.Latitude, first.Longitude, ct);
        if (resp?.Current is null) return null;

        var label = string.IsNullOrWhiteSpace(first.Country) ? first.Name ?? "—" : $"{first.Name}, {first.Country}";
        return MapToVm(resp, first.Latitude, first.Longitude, label);
    }

    public async Task<WeatherViewModel?> GetCurrentByCoordinatesAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var resp = await FetchForecastAsync(latitude, longitude, ct);
        if (resp?.Current is null) return null;

        string label = "Vị trí hiện tại";
        try
        {
            var geo = _factory.CreateClient("OpenMeteo.Geocoding");
            var rev = await geo.GetFromJsonAsync<GeocodingResult>(
                $"v1/reverse?latitude={latitude}&longitude={longitude}&language=vi&count=1&format=json",
                JsonOpts, ct);
            var p = rev?.Results?.FirstOrDefault();
            if (p is not null)
                label = string.IsNullOrWhiteSpace(p.Country) ? p.Name ?? label : $"{p.Name}, {p.Country}";
        }
        catch { /* bỏ qua lỗi reverse */ }

        return MapToVm(resp, latitude, longitude, label);
    }
}