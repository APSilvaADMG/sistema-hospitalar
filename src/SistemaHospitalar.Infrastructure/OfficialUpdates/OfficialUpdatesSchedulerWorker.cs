using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.OfficialUpdates;

public class OfficialUpdatesSchedulerWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OfficialUpdatesSettings> settings,
    ILogger<OfficialUpdatesSchedulerWorker> logger) : BackgroundService
{
    private DateOnly? _lastRunDate;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (settings.Value.Enabled && IsDueNow(settings.Value.DailyRunTimeUtc))
                {
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    if (_lastRunDate != today)
                    {
                        _lastRunDate = today;
                        await RunScheduledCheckAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no agendador de atualizações oficiais");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private static bool IsDueNow(string dailyRunTimeUtc)
    {
        if (!TimeSpan.TryParse(dailyRunTimeUtc, out var scheduled))
            scheduled = new TimeSpan(2, 0, 0);

        var now = DateTime.UtcNow;
        return now.Hour == scheduled.Hours && now.Minute == scheduled.Minutes;
    }

    private async Task RunScheduledCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOfficialUpdatesService>();
        logger.LogInformation("Iniciando verificação agendada de atualizações oficiais");
        await service.CheckAllAsync("scheduler", cancellationToken);
    }
}
