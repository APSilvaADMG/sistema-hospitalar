using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectCollectionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ConnectSettings> settings,
    ILogger<ConnectCollectionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (settings.Value.Collection.Enabled)
                {
                    await ProcessOverdueAccountsAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no worker de cobrança Connect");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task ProcessOverdueAccountsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var minInterval = TimeSpan.FromDays(settings.Value.Collection.MinDaysBetweenReminders);

        var overdue = await db.FinancialAccounts
            .Include(f => f.Patient)
            .Where(f => f.IsActive
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid)
                && f.DueDate != null
                && f.DueDate < now)
            .Take(30)
            .ToListAsync(cancellationToken);

        foreach (var account in overdue)
        {
            var payload = JsonSerializer.Serialize(new { financialAccountId = account.Id });
            var recentlySent = await db.ConnectScheduledMessages.AnyAsync(
                m => m.IsActive && m.IsSent
                    && m.ReminderType == ConnectReminderType.BillingOverdue
                    && m.PayloadJson == payload
                    && m.SentAt >= now - minInterval,
                cancellationToken);

            if (recentlySent)
            {
                continue;
            }

            db.ConnectScheduledMessages.Add(new Domain.Entities.ConnectScheduledMessage
            {
                PatientId = account.PatientId,
                ReminderType = ConnectReminderType.BillingOverdue,
                ScheduledFor = now,
                PayloadJson = payload,
            });
        }

        if (overdue.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
