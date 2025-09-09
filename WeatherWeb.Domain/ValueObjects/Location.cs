using WeatherWeb.Domain.ValueObjects;

namespace WeatherWeb.Domain.Entities;

public sealed class Location(string name, string? country, Coordinates coordinates)
{
    public string Name { get; } = name;
    public string? Country { get; } = country;
    public Coordinates Coordinates { get; } = coordinates;

    public override string ToString() => Country is null ? Name : $"{Name}, {Country}";
}
