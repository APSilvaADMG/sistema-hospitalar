using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectContextService(AppDbContext db) : IConnectContextService
{
    public async Task<IReadOnlyList<ConnectContextMessageDto>> ListPatientMessagesAsync(
        Guid userId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var direct = await db.InternalMessages.AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => m.IsActive && m.DeletedAt == null && m.PatientId == patientId)
            .Where(m => m.Status == InternalMessageStatus.Sent)
            .Select(m => new ConnectContextMessageDto(
                m.Id,
                m.Subject,
                m.Content,
                m.Priority,
                m.Sender.FullName,
                m.CreatedAt,
                ConnectContextType.Patient,
                patientId,
                null))
            .ToListAsync(cancellationToken);

        var linked = await db.ConnectContextLinks.AsNoTracking()
            .Include(l => l.Message!).ThenInclude(m => m.Sender)
            .Where(l => l.IsActive && l.ContextType == ConnectContextType.Patient && l.ContextId == patientId)
            .Where(l => l.MessageId != null && l.Message!.IsActive && l.Message.DeletedAt == null)
            .Select(l => new ConnectContextMessageDto(
                l.Message!.Id,
                l.Message.Subject,
                l.Message.Content,
                l.Message.Priority,
                l.Message.Sender.FullName,
                l.Message.CreatedAt,
                ConnectContextType.Patient,
                patientId,
                null))
            .ToListAsync(cancellationToken);

        return direct
            .Concat(linked)
            .GroupBy(m => m.Id)
            .Select(g => g.First())
            .OrderByDescending(m => m.CreatedAt)
            .Take(100)
            .ToList();
    }

    public async Task<IReadOnlyList<ConnectContextMessageDto>> ListGuideMessagesAsync(
        Guid userId,
        Guid guideId,
        string guideType,
        CancellationToken cancellationToken = default)
    {
        var isTiss = guideType.Equals("tiss", StringComparison.OrdinalIgnoreCase);
        var guideLabel = await ResolveGuideLabelAsync(guideId, isTiss, cancellationToken);

        IQueryable<InternalMessage> query = db.InternalMessages.AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => m.IsActive && m.DeletedAt == null && m.Status == InternalMessageStatus.Sent);

        query = isTiss
            ? query.Where(m => m.TissGuideId == guideId)
            : query.Where(m => m.SusGuideId == guideId);

        var direct = await query
            .Select(m => new ConnectContextMessageDto(
                m.Id,
                m.Subject,
                m.Content,
                m.Priority,
                m.Sender.FullName,
                m.CreatedAt,
                ConnectContextType.Guide,
                guideId,
                guideLabel))
            .ToListAsync(cancellationToken);

        var linked = await db.ConnectContextLinks.AsNoTracking()
            .Include(l => l.Message!).ThenInclude(m => m.Sender)
            .Where(l => l.IsActive && l.ContextType == ConnectContextType.Guide && l.ContextId == guideId)
            .Where(l => l.MessageId != null && l.Message!.IsActive && l.Message.DeletedAt == null)
            .Select(l => new ConnectContextMessageDto(
                l.Message!.Id,
                l.Message.Subject,
                l.Message.Content,
                l.Message.Priority,
                l.Message.Sender.FullName,
                l.Message.CreatedAt,
                ConnectContextType.Guide,
                guideId,
                guideLabel))
            .ToListAsync(cancellationToken);

        return direct
            .Concat(linked)
            .GroupBy(m => m.Id)
            .Select(g => g.First())
            .OrderByDescending(m => m.CreatedAt)
            .Take(100)
            .ToList();
    }

    public async Task LinkMessageContextAsync(
        Guid messageId,
        MailContextInputDto context,
        CancellationToken cancellationToken = default)
    {
        var message = await db.InternalMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (message is null) return;

        if (context.PatientId is Guid patientId)
        {
            message.PatientId = patientId;
            await EnsureLinkAsync(messageId, ConnectContextType.Patient, patientId, cancellationToken);
        }

        if (context.TissGuideId is Guid tissGuideId)
        {
            message.TissGuideId = tissGuideId;
            await EnsureLinkAsync(messageId, ConnectContextType.Guide, tissGuideId, cancellationToken);
        }

        if (context.SusGuideId is Guid susGuideId)
        {
            message.SusGuideId = susGuideId;
            await EnsureLinkAsync(messageId, ConnectContextType.Guide, susGuideId, cancellationToken);
        }

        if (context.AppointmentId is Guid appointmentId)
        {
            message.AppointmentId = appointmentId;
            await EnsureLinkAsync(messageId, ConnectContextType.Appointment, appointmentId, cancellationToken);
        }

        if (context.TicketId is Guid ticketId)
        {
            await EnsureTicketLinkAsync(messageId, ticketId, cancellationToken);
        }

        message.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureLinkAsync(
        Guid messageId,
        ConnectContextType contextType,
        Guid contextId,
        CancellationToken cancellationToken)
    {
        var exists = await db.ConnectContextLinks.AnyAsync(
            l => l.MessageId == messageId && l.ContextType == contextType && l.ContextId == contextId && l.IsActive,
            cancellationToken);

        if (exists) return;

        db.ConnectContextLinks.Add(new ConnectContextLink
        {
            MessageId = messageId,
            ContextType = contextType,
            ContextId = contextId,
        });
    }

    private async Task EnsureTicketLinkAsync(
        Guid messageId,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var exists = await db.ConnectContextLinks.AnyAsync(
            l => l.MessageId == messageId && l.TicketId == ticketId && l.IsActive,
            cancellationToken);

        if (exists) return;

        db.ConnectContextLinks.Add(new ConnectContextLink
        {
            MessageId = messageId,
            TicketId = ticketId,
            ContextType = ConnectContextType.Ticket,
            ContextId = ticketId,
        });
    }

    private async Task<string?> ResolveGuideLabelAsync(Guid guideId, bool isTiss, CancellationToken cancellationToken)
    {
        if (isTiss)
        {
            var guide = await db.TissGuides.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == guideId, cancellationToken);
            return guide is null ? null : $"TISS {guide.GuideNumber}";
        }

        var sus = await db.SusGuides.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guideId, cancellationToken);
        return sus is null ? null : $"SUS {sus.GuideNumber}";
    }
}
