using Microsoft.EntityFrameworkCore;

using SistemaHospitalar.Application.DTOs.Connect;

using SistemaHospitalar.Application.Interfaces;

using SistemaHospitalar.Domain.Entities;

using SistemaHospitalar.Domain.Enums;

using SistemaHospitalar.Infrastructure.Persistence;



namespace SistemaHospitalar.Infrastructure.Connect;



public class ConnectCalendarService(

    AppDbContext db,

    IConnectRealtimeNotifier realtimeNotifier) : IConnectCalendarService

{

    public async Task<IReadOnlyList<ConnectCalendarEventListItemDto>> ListByRangeAsync(

        Guid userId,

        DateTime from,

        DateTime to,

        string scope,

        CancellationToken cancellationToken = default)

    {

        var query = db.ConnectCalendarEvents.AsNoTracking()

            .Include(e => e.Organizador)

            .Include(e => e.Setor)

            .Include(e => e.Participants)

            .Where(e => e.IsActive && e.DeletedAt == null)

            .Where(e =>

                (e.RecurrenceRule == ConnectCalendarRecurrenceRule.None && e.Inicio < to && e.Fim > from) ||

                (e.RecurrenceRule != ConnectCalendarRecurrenceRule.None && e.Inicio < to));



        query = scope.ToLowerInvariant() switch

        {

            "mine" => query.Where(e =>

                e.OrganizadorId == userId ||

                e.Participants.Any(p => p.UserId == userId && p.IsActive)),

            "team" => query.Where(e => e.SetorId != null),

            _ => query,

        };



        var events = await query.OrderBy(e => e.Inicio).ToListAsync(cancellationToken);



        var items = new List<ConnectCalendarEventListItemDto>();



        foreach (var e in events)

        {

            var myParticipant = e.Participants.FirstOrDefault(p => p.UserId == userId && p.IsActive);

            var myResponse = myParticipant?.Response;



            foreach (var (occurrenceStart, occurrenceEnd, isInstance) in ConnectCalendarRecurrenceExpander.Expand(e, from, to))

            {

                items.Add(MapListItem(e, userId, myResponse, occurrenceStart, occurrenceEnd, isInstance));

            }

        }



        return items.OrderBy(i => i.Inicio).ToList();

    }



    public async Task<ConnectCalendarEventDetailDto?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)

    {

        var entity = await LoadEventAsync(id, cancellationToken);

        if (entity is null || !CanAccess(userId, entity)) return null;

        return MapDetail(entity, userId);

    }



    public async Task<ConnectCalendarEventDetailDto> CreateAsync(

        Guid userId,

        CreateConnectCalendarEventRequest request,

        CancellationToken cancellationToken = default)

    {

        var entity = new ConnectCalendarEvent

        {

            Titulo = request.Titulo.Trim(),

            Descricao = request.Descricao?.Trim(),

            Inicio = request.Inicio,

            Fim = request.Fim,

            Local = request.Local?.Trim(),

            OrganizadorId = userId,

            Tipo = request.Tipo,

            AllDay = request.AllDay,

            RecurrenceRule = request.RecurrenceRule,

            Color = NormalizeColor(request.Color),

            ReminderMinutes = request.ReminderMinutes,

            SetorId = request.SetorId,

        };



        db.ConnectCalendarEvents.Add(entity);

        await db.SaveChangesAsync(cancellationToken);

        await SyncParticipantsAsync(entity, request.ParticipantUserIds, cancellationToken);



        await LogAsync(userId, "calendar.create", entity.Id, entity.Titulo, cancellationToken);

        await realtimeNotifier.NotifyCalendarUpdatedAsync(entity.Id, cancellationToken);

        return (await GetAsync(userId, entity.Id, cancellationToken))!;

    }



    public async Task<ConnectCalendarEventDetailDto?> UpdateAsync(

        Guid userId,

        Guid id,

        UpdateConnectCalendarEventRequest request,

        CancellationToken cancellationToken = default)

    {

        var entity = await db.ConnectCalendarEvents

            .Include(e => e.Participants)

            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive && e.DeletedAt == null, cancellationToken);



        if (entity is null || entity.OrganizadorId != userId) return null;



        entity.Titulo = request.Titulo.Trim();

        entity.Descricao = request.Descricao?.Trim();

        entity.Inicio = request.Inicio;

        entity.Fim = request.Fim;

        entity.Local = request.Local?.Trim();

        entity.Tipo = request.Tipo;

        entity.AllDay = request.AllDay;

        entity.RecurrenceRule = request.RecurrenceRule;

        entity.Color = NormalizeColor(request.Color);

        entity.ReminderMinutes = request.ReminderMinutes;

        entity.SetorId = request.SetorId;

        entity.LastReminderSentAt = null;

        entity.LastReminderOccurrenceStart = null;

        entity.UpdatedAt = DateTime.UtcNow;



        await SyncParticipantsAsync(entity, request.ParticipantUserIds, cancellationToken);

        await LogAsync(userId, "calendar.update", entity.Id, entity.Titulo, cancellationToken);

        await realtimeNotifier.NotifyCalendarUpdatedAsync(entity.Id, cancellationToken);

        return await GetAsync(userId, id, cancellationToken);

    }



    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)

    {

        var entity = await db.ConnectCalendarEvents

            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive && e.DeletedAt == null, cancellationToken);



        if (entity is null || entity.OrganizadorId != userId) return false;



        entity.DeletedAt = DateTime.UtcNow;

        entity.UpdatedAt = DateTime.UtcNow;

        await LogAsync(userId, "calendar.delete", entity.Id, entity.Titulo, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyCalendarUpdatedAsync(id, cancellationToken);

        return true;

    }



    public async Task<ConnectCalendarEventDetailDto?> RespondAsync(

        Guid userId,

        Guid id,

        RespondCalendarEventRequest request,

        CancellationToken cancellationToken = default)

    {

        var participant = await db.ConnectCalendarParticipants

            .FirstOrDefaultAsync(p => p.EventId == id && p.UserId == userId && p.IsActive, cancellationToken);



        if (participant is null) return null;



        participant.Response = request.Response;

        participant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyCalendarUpdatedAsync(id, cancellationToken);

        return await GetAsync(userId, id, cancellationToken);

    }



    private async Task<ConnectCalendarEvent?> LoadEventAsync(Guid id, CancellationToken cancellationToken)

    {

        return await db.ConnectCalendarEvents.AsNoTracking()

            .Include(e => e.Organizador)

            .Include(e => e.Setor)

            .Include(e => e.Participants).ThenInclude(p => p.User)

            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive && e.DeletedAt == null, cancellationToken);

    }



    private static bool CanAccess(Guid userId, ConnectCalendarEvent entity) =>

        entity.OrganizadorId == userId ||

        entity.Participants.Any(p => p.UserId == userId && p.IsActive) ||

        entity.SetorId != null;



    private static ConnectCalendarEventListItemDto MapListItem(

        ConnectCalendarEvent entity,

        Guid userId,

        ConnectCalendarParticipantResponse? myResponse,

        DateTime occurrenceStart,

        DateTime occurrenceEnd,

        bool isRecurrenceInstance) =>

        new(

            entity.Id,

            entity.Titulo,

            entity.Descricao,

            occurrenceStart,

            occurrenceEnd,

            entity.Local,

            entity.Tipo,

            entity.AllDay,

            entity.RecurrenceRule,

            entity.Color,

            entity.ReminderMinutes,

            entity.Organizador.FullName,

            entity.SetorId,

            entity.Setor?.Name,

            entity.Participants.Count(p => p.IsActive),

            entity.OrganizadorId == userId,

            myResponse,

            isRecurrenceInstance);



    private static ConnectCalendarEventDetailDto MapDetail(ConnectCalendarEvent entity, Guid userId)

    {

        var myParticipant = entity.Participants.FirstOrDefault(p => p.UserId == userId && p.IsActive);

        return new(

            entity.Id,

            entity.Titulo,

            entity.Descricao,

            entity.Inicio,

            entity.Fim,

            entity.Local,

            entity.Tipo,

            entity.AllDay,

            entity.RecurrenceRule,

            entity.Color,

            entity.ReminderMinutes,

            entity.OrganizadorId,

            entity.Organizador.FullName,

            entity.SetorId,

            entity.Setor?.Name,

            entity.Participants

                .Where(p => p.IsActive)

                .Select(p => new ConnectCalendarParticipantDto(p.UserId, p.User.FullName, p.Response))

                .ToList(),

            entity.OrganizadorId == userId,

            myParticipant?.Response,

            entity.CreatedAt);

    }



    private static string? NormalizeColor(string? color)

    {

        if (string.IsNullOrWhiteSpace(color)) return null;

        var trimmed = color.Trim();

        return trimmed.Length <= 20 ? trimmed : trimmed[..20];

    }



    private async Task SyncParticipantsAsync(

        ConnectCalendarEvent entity,

        IReadOnlyList<Guid>? participantUserIds,

        CancellationToken cancellationToken)

    {

        var ids = (participantUserIds ?? [])

            .Where(id => id != entity.OrganizadorId)

            .Distinct()

            .ToHashSet();



        var existing = entity.Participants.Where(p => p.IsActive).ToList();

        foreach (var p in existing.Where(p => !ids.Contains(p.UserId)))

        {

            p.IsActive = false;

            p.UpdatedAt = DateTime.UtcNow;

        }



        var existingIds = existing.Select(p => p.UserId).ToHashSet();

        foreach (var uid in ids.Where(id => !existingIds.Contains(id)))

        {

            db.ConnectCalendarParticipants.Add(new ConnectCalendarParticipant

            {

                EventId = entity.Id,

                UserId = uid,

            });

        }



        await db.SaveChangesAsync(cancellationToken);

    }



    private async Task LogAsync(Guid userId, string action, Guid entityId, string details, CancellationToken cancellationToken)

    {

        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = action,

            EntityType = nameof(ConnectCalendarEvent),

            EntityId = entityId,

            Details = details,

        });

        await db.SaveChangesAsync(cancellationToken);

    }

}

