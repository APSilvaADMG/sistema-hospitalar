using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.TvSignage;

public interface ITvWeatherService
{
    Task<TvWeatherSnapshot?> EnsureFreshAsync(string city, CancellationToken cancellationToken = default);
}

public class TvWeatherService(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IOptions<TvSignageSettings> options,
    ILogger<TvWeatherService> logger) : ITvWeatherService
{
    private readonly TvWeatherSettings _settings = options.Value.Weather;

    public async Task<TvWeatherSnapshot?> EnsureFreshAsync(string city, CancellationToken cancellationToken = default)
    {
        var normalizedCity = city.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCity)) return null;

        var snapshot = await dbContext.TvWeatherSnapshots
            .FirstOrDefaultAsync(w => w.City == normalizedCity && w.IsActive, cancellationToken);

        var staleMinutes = Math.Max(15, _settings.RefreshMinutes);
        if (snapshot is not null && snapshot.UpdatedAt > DateTime.UtcNow.AddMinutes(-staleMinutes))
        {
            return snapshot;
        }

        if (string.IsNullOrWhiteSpace(_settings.OpenWeatherApiKey))
        {
            return snapshot;
        }

        try
        {
            var client = httpClientFactory.CreateClient("TvOpenWeather");
            var query = BuildQuery(normalizedCity);
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(query)}&appid={_settings.OpenWeatherApiKey}&units=metric&lang=pt_br";
            var response = await client.GetFromJsonAsync<OpenWeatherResponse>(url, cancellationToken);
            if (response is null) return snapshot;

            if (snapshot is null)
            {
                snapshot = new TvWeatherSnapshot { City = normalizedCity };
                dbContext.TvWeatherSnapshots.Add(snapshot);
            }

            snapshot.TemperatureC = (decimal)response.Main.Temp;
            snapshot.HumidityPercent = response.Main.Humidity;
            snapshot.Condition = response.Weather.FirstOrDefault()?.Description ?? "—";
            snapshot.Icon = MapIcon(response.Weather.FirstOrDefault()?.Main);
            snapshot.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return snapshot;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao atualizar clima OpenWeather para {City}", normalizedCity);
            return snapshot;
        }
    }

    private string BuildQuery(string city)
    {
        if (city.Contains(',', StringComparison.Ordinal)) return city;
        return $"{city},{_settings.DefaultCountryCode}";
    }

    private static string? MapIcon(string? main) => main?.ToUpperInvariant() switch
    {
        "CLEAR" => "☀️",
        "CLOUDS" => "☁️",
        "RAIN" => "🌧️",
        "DRIZZLE" => "🌦️",
        "THUNDERSTORM" => "⛈️",
        "SNOW" => "❄️",
        "MIST" or "FOG" => "🌫️",
        _ => "🌤️",
    };

    private sealed class OpenWeatherResponse
    {
        [JsonPropertyName("main")]
        public OpenWeatherMain Main { get; set; } = new();

        [JsonPropertyName("weather")]
        public List<OpenWeatherCondition> Weather { get; set; } = [];
    }

    private sealed class OpenWeatherMain
    {
        [JsonPropertyName("temp")]
        public double Temp { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }
    }

    private sealed class OpenWeatherCondition
    {
        [JsonPropertyName("main")]
        public string? Main { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
