using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectCalendarReminderWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ConnectSettings> settings,
    ILogger<ConnectCalendarReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Max(1, settings.Value.CalendarReminderCheckMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no worker de lembretes de agenda Connect");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IConnectNotificationService>();

        var now = DateTime.UtcNow;
        var horizon = now.AddDays(2);

        var candidates = await db.ConnectCalendarEvents
            .Include(e => e.Participants)
            .Where(e => e.IsActive && e.DeletedAt == null)
            .Where(e => e.ReminderMinutes != null && e.ReminderMinutes > 0)
            .Where(e => e.Inicio < horizon)
            .OrderBy(e => e.Inicio)
            .Take(100)
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var entity in candidates)
        {
            var reminderMinutes = entity.ReminderMinutes!.Value;
            var occurrences = ConnectCalendarRecurrenceExpander
                .Expand(entity, now.AddMinutes(-reminderMinutes), horizon)
                .ToList();

            foreach (var (occurrenceStart, _, _) in occurrences)
            {
                if (occurrenceStart <= now)
                {
                    continue;
                }

                var reminderAt = occurrenceStart.AddMinutes(-reminderMinutes);
                if (now < reminderAt)
                {
                    continue;
                }

                if (entity.LastReminderOccurrenceStart == occurrenceStart)
                {
                    continue;
                }

                var recipientIds = new HashSet<Guid> { entity.OrganizadorId };
                foreach (var p in entity.Participants.Where(p => p.IsActive))
                {
                    recipientIds.Add(p.UserId);
                }

                var startLocal = occurrenceStart.ToLocalTime();
                var title = $"Lembrete — {entity.Titulo}";
                var message = entity.AllDay
                    ? $"O evento \"{entity.Titulo}\" é hoje ({startLocal:dd/MM/yyyy})."
                    : $"O evento \"{entity.Titulo}\" começa em {reminderMinutes} min ({startLocal:dd/MM/yyyy HH:mm}).";

                if (!string.IsNullOrWhiteSpace(entity.Local))
                {
                    message += $" Local: {entity.Local}.";
                }

                foreach (var userId in recipientIds)
                {
                    await notificationService.CreateAsync(new CreateConnectNotificationRequest(
                        userId,
                        title,
                        message,
                        ConnectNotificationCategory.Info,
                        nameof(ConnectCalendarEvent),
                        entity.Id), cancellationToken);
                }

                entity.LastReminderSentAt = now;
                entity.LastReminderOccurrenceStart = occurrenceStart;
                entity.UpdatedAt = now;
                sent++;
                break;
            }
        }

        if (sent > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Lembretes de agenda: {Count} notificação(ões) enviada(s)", sent);
        }
    }
}
