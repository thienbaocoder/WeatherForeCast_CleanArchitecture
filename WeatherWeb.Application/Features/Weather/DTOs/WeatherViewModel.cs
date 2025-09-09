using System;
using System.Collections.Generic;

namespace WeatherWeb.Application.Features.Weather.DTOs;

public sealed class WeatherViewModel
{
    public string? LocationName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double TemperatureC { get; set; }
    public double? FeelsLikeC { get; set; }
    public double WindSpeed { get; set; }         // m/s
    public int? CloudCoverPercent { get; set; }
    public double? PressureHpa { get; set; }
    public double? VisibilityKm { get; set; }
    public string? ObservedAt { get; set; }

    public int? WeatherCode { get; set; }
    public string? Condition { get; set; }
    public List<HourlyForecastItem> Hourly { get; set; } = new();
    public List<DailyForecastItem> Daily { get; set; } = new();
}

public sealed class HourlyForecastItem
{
    public DateTimeOffset Time { get; set; }
    public double TempC { get; set; }
    public int WeatherCode { get; set; }
    public int? PrecipProb { get; set; } // %
}

public sealed class DailyForecastItem
{
    public DateTimeOffset Date { get; set; }
    public double MaxC { get; set; }
    public double MinC { get; set; }
    public int WeatherCode { get; set; }
    public int? PrecipProbMax { get; set; } // %
}
