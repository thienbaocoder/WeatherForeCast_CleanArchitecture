using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WeatherWeb.Infrastructure.Weather;

public sealed class GeocodingResult
{
    public List<GeocodingItem>? Results { get; set; }
}

public sealed class GeocodingItem
{
    public string? Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Country { get; set; }
}

// Model cho /v1/forecast?current=...
public sealed class OpenMeteoForecastResponse
{
    [JsonPropertyName("current")] public CurrentBlock? Current { get; set; }
    [JsonPropertyName("hourly")] public HourlyBlock? Hourly { get; set; }
    [JsonPropertyName("daily")] public DailyBlock? Daily { get; set; }

    public sealed class CurrentBlock
    {
        [JsonPropertyName("time")] public string? Time { get; set; }
        [JsonPropertyName("temperature_2m")] public double Temperature2m { get; set; }
        [JsonPropertyName("apparent_temperature")] public double ApparentTemperature { get; set; }
        [JsonPropertyName("wind_speed_10m")] public double WindSpeed10m { get; set; } // km/h or m/s (tuỳ query)
        [JsonPropertyName("cloud_cover")] public int CloudCover { get; set; }      // %
        [JsonPropertyName("pressure_msl")] public double? PressureMsl { get; set; }
        [JsonPropertyName("surface_pressure")] public double? SurfacePressure { get; set; }
        [JsonPropertyName("visibility")] public double? Visibility { get; set; }  // m
        [JsonPropertyName("weather_code")] public int WeatherCode { get; set; }
    }

    public sealed class HourlyBlock
    {
        [JsonPropertyName("time")] public string[]? Time { get; set; }
        [JsonPropertyName("temperature_2m")] public double[]? Temperature2m { get; set; }
        [JsonPropertyName("weather_code")] public int[]? WeatherCode { get; set; }
        [JsonPropertyName("precipitation_probability")] public int[]? PrecipProb { get; set; }
    }

    public sealed class DailyBlock
    {
        [JsonPropertyName("time")] public string[]? Time { get; set; }
        [JsonPropertyName("temperature_2m_max")] public double[]? Temperature2mMax { get; set; }
        [JsonPropertyName("temperature_2m_min")] public double[]? Temperature2mMin { get; set; }
        [JsonPropertyName("weather_code")] public int[]? WeatherCode { get; set; }
        [JsonPropertyName("precipitation_probability_max")] public int[]? PrecipProbMax { get; set; }
    }
}
