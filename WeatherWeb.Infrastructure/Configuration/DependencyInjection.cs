using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherWeb.Application.Abstractions;
using WeatherWeb.Infrastructure.Weather;

namespace WeatherWeb.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var openMeteoBase = config["ApiClients:OpenMeteo:BaseUrl"] ?? "https://api.open-meteo.com/";
        var geocodingBase = config["ApiClients:OpenMeteoGeocoding:BaseUrl"] ?? "https://geocoding-api.open-meteo.com/";

        services.AddHttpClient("OpenMeteo.Api", c => c.BaseAddress = new Uri(openMeteoBase));
        services.AddHttpClient("OpenMeteo.Geocoding", c => c.BaseAddress = new Uri(geocodingBase));

        services.AddScoped<IWeatherService, OpenMeteoService>(); 
        services.AddScoped<IChatBot, Chat.RuleBasedChatBot>();
        return services;
    }
}
