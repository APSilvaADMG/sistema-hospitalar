using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectSlaMonitorWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ConnectSettings> settings,
    ILogger<ConnectSlaMonitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Max(5, settings.Value.SlaMonitorIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOverdueTicketsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no monitor de SLA do Connect");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    private async Task ProcessOverdueTicketsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IConnectNotificationService>();
        var mailService = scope.ServiceProvider.GetRequiredService<IConnectMailService>();
        var realtimeNotifier = scope.ServiceProvider.GetRequiredService<IConnectRealtimeNotifier>();

        var now = DateTime.UtcNow;
        var alertCooldown = TimeSpan.FromHours(24);

        var overdue = await db.ConnectTickets
            .Include(t => t.Responsavel)
            .Include(t => t.Solicitante)
            .Where(t => t.IsActive && t.DeletedAt == null)
            .Where(t => t.DueAt != null && t.DueAt < now)
            .Where(t => t.Status != ConnectTicketStatus.Resolvido && t.Status != ConnectTicketStatus.Cancelado)
            .Where(t => t.LastSlaAlertAt == null || t.LastSlaAlertAt < now - alertCooldown)
            .OrderBy(t => t.DueAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var managerUserIds = await ResolveConnectAdminUserIdsAsync(db, cancellationToken);

        foreach (var ticket in overdue)
        {
            var isSecondAlert = ticket.LastSlaAlertAt != null;
            var targetUserId = ticket.ResponsavelId ?? ticket.SolicitanteId;
            var dueLocal = ticket.DueAt!.Value.ToLocalTime();

            await notificationService.CreateAsync(new CreateConnectNotificationRequest(
                targetUserId,
                $"SLA vencido — {ticket.Protocolo}",
                $"O chamado \"{ticket.Titulo}\" ultrapassou o prazo ({dueLocal:dd/MM/yyyy HH:mm}).",
                ConnectNotificationCategory.Alert,
                "ConnectTicket",
                ticket.Id), cancellationToken);

            await realtimeNotifier.NotifySlaAlertAsync(ticket.Id, targetUserId, cancellationToken);
            await realtimeNotifier.NotifyTicketUpdatedAsync(ticket.Id, cancellationToken);

            if (ticket.ResponsavelId is Guid assigneeId && assigneeId != ticket.SolicitanteId)
            {
                try
                {
                    await mailService.CreateAsync(assigneeId, new CreateMailRequest(
                        $"[SLA] Chamado {ticket.Protocolo} vencido",
                        $"O chamado \"{ticket.Titulo}\" está com SLA vencido desde {dueLocal:dd/MM/yyyy HH:mm}.\n\nAcesse APSMed Connect › Chamados para atualizar o status.",
                        MessagePriority.Alta,
                        [new MailRecipientInputDto(assigneeId, MessageRecipientType.To)],
                        null,
                        true), cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Falha ao enviar e-mail de SLA para ticket {TicketId}", ticket.Id);
                }
            }

            if (isSecondAlert && managerUserIds.Count > 0)
            {
                foreach (var managerId in managerUserIds.Where(id => id != targetUserId))
                {
                    await notificationService.CreateAsync(new CreateConnectNotificationRequest(
                        managerId,
                        $"Escalação SLA — {ticket.Protocolo}",
                        $"Segundo alerta de SLA vencido no chamado \"{ticket.Titulo}\" (responsável: {ticket.Responsavel?.FullName ?? "não atribuído"}).",
                        ConnectNotificationCategory.Alert,
                        "ConnectTicket",
                        ticket.Id), cancellationToken);

                    await realtimeNotifier.NotifySlaAlertAsync(ticket.Id, managerId, cancellationToken);
                }
            }

            ticket.LastSlaAlertAt = now;
            ticket.UpdatedAt = now;
        }

        if (overdue.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("SLA monitor: {Count} alerta(s) emitido(s)", overdue.Count);
        }
    }

    private static async Task<IReadOnlyList<Guid>> ResolveConnectAdminUserIdsAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var adminRoles = await db.RolePermissions.AsNoTracking()
            .Where(r => r.PermissionCode == PermissionCodes.ConnectAdmin)
            .Select(r => r.Role)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (adminRoles.Count == 0) return [];

        return await db.Users.AsNoTracking()
            .Where(u => u.IsActive && adminRoles.Contains(u.Role))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
    }
}
