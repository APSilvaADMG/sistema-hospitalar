using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SistemaHospitalar.Infrastructure.TvSignage;

public class TvSignageBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<TvSignageSettings> options,
    ILogger<TvSignageBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshWeatherAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no worker TV Corporativa");
            }

            var minutes = Math.Max(15, options.Value.Weather.RefreshMinutes);
            await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
        }
    }

    private async Task RefreshWeatherAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Persistence.AppDbContext>();
        var weather = scope.ServiceProvider.GetRequiredService<ITvWeatherService>();

        var cities = await db.TvDisplays
            .Where(d => d.IsActive && d.WeatherCity != null)
            .Select(d => d.WeatherCity!)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.Value.Weather.DefaultCity))
        {
            cities.Add(options.Value.Weather.DefaultCity);
        }

        foreach (var city in cities.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await weather.EnsureFreshAsync(city, cancellationToken);
        }
    }
}
