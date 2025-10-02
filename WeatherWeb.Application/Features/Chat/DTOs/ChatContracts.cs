namespace WeatherWeb.Application.Features.Chat.DTOs;

public sealed class ChatContext
{
    public string? LastCity { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public sealed class ChatReply
{
    public string Text { get; init; } = "";
    public string? CityUsed { get; init; }
    public bool UsedGps { get; init; }
}
