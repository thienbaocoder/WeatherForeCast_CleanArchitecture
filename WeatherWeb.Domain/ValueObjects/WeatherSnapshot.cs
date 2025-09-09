using System;
using WeatherWeb.Domain.ValueObjects;

namespace WeatherWeb.Domain.Entities;

public sealed class WeatherSnapshot(
    Location location,
    Temperature temperature,
    WindSpeed wind,
    DateTimeOffset observedAt)
{
    public Location Location { get; } = location;
    public Temperature Temperature { get; } = temperature;
    public WindSpeed Wind { get; } = wind;
    public DateTimeOffset ObservedAt { get; } = observedAt;
}
