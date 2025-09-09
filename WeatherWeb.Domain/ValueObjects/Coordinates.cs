namespace WeatherWeb.Domain.ValueObjects;

public readonly struct Coordinates(double latitude, double longitude)
{
    public double Latitude { get; } = latitude;
    public double Longitude { get; } = longitude;

    public bool IsValid =>
        Latitude is >= -90 and <= 90 &&
        Longitude is >= -180 and <= 180;

    public override string ToString() => $"({Latitude}, {Longitude})";
}
