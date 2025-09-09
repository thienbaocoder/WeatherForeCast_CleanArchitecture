namespace WeatherWeb.Domain.ValueObjects;

public readonly struct WindSpeed(double metersPerSecond)
{
    public double Mps { get; } = metersPerSecond;
    public double KmPerHour => Mps * 3.6;
    public override string ToString() => $"{Mps:0.#} m/s";
}
