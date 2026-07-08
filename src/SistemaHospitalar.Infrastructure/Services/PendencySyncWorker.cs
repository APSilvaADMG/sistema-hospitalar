using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class PendencySyncWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PendencySyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllUsersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Erro na sincronização de pendências");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task SyncAllUsersAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pendencyService = scope.ServiceProvider.GetRequiredService<IPendencyService>();
        var hubService = scope.ServiceProvider.GetRequiredService<IUnifiedNotificationHubService>();

        var userIds = await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .Take(200)
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            await pendencyService.SyncForUserAsync(userId, cancellationToken);
            await hubService.NotifyHubUpdatedAsync(userId, cancellationToken);
        }
    }
}
