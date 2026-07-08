using Microsoft.EntityFrameworkCore;

using SistemaHospitalar.Application.DTOs.Connect;

using SistemaHospitalar.Application.Interfaces;

using SistemaHospitalar.Domain.Entities;

using SistemaHospitalar.Domain.Enums;

using SistemaHospitalar.Infrastructure.Persistence;



namespace SistemaHospitalar.Infrastructure.Connect;



public class ConnectTicketService(

    AppDbContext db,

    IConnectNotificationService notificationService,

    IConnectMailService mailService,

    IConnectRealtimeNotifier realtimeNotifier) : IConnectTicketService

{

    public async Task<ConnectTicketSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)

    {

        var now = DateTime.UtcNow;

        var query = db.ConnectTickets.AsNoTracking()

            .Where(t => t.IsActive && t.DeletedAt == null);



        var abertos = await query.CountAsync(t => t.Status == ConnectTicketStatus.Aberto, cancellationToken);

        var emAndamento = await query.CountAsync(t => t.Status == ConnectTicketStatus.EmAndamento, cancellationToken);

        var aguardando = await query.CountAsync(t => t.Status == ConnectTicketStatus.Aguardando, cancellationToken);

        var vencidos = await query.CountAsync(

            t => t.DueAt != null && t.DueAt < now

                 && t.Status != ConnectTicketStatus.Resolvido

                 && t.Status != ConnectTicketStatus.Cancelado,

            cancellationToken);



        return new ConnectTicketSummaryDto(abertos, emAndamento, aguardando, vencidos);

    }



    public async Task<IReadOnlyList<ConnectTicketListItemDto>> ListAsync(

        Guid userId,

        ConnectTicketStatus? status,

        ConnectTicketCategory? category,

        MessagePriority? priority,

        bool? assignedToMe,

        bool? myRequests,

        string? search,

        CancellationToken cancellationToken = default)

    {

        var query = db.ConnectTickets.AsNoTracking()

            .Include(t => t.Solicitante)

            .Include(t => t.Responsavel)

            .Where(t => t.IsActive && t.DeletedAt == null);



        if (status.HasValue) query = query.Where(t => t.Status == status.Value);

        if (category.HasValue) query = query.Where(t => t.Categoria == category.Value);

        if (priority.HasValue) query = query.Where(t => t.Prioridade == priority.Value);

        if (assignedToMe == true) query = query.Where(t => t.ResponsavelId == userId);

        if (myRequests == true) query = query.Where(t => t.SolicitanteId == userId);

        if (!string.IsNullOrWhiteSpace(search))

        {

            var term = search.Trim();

            query = query.Where(t => t.Titulo.Contains(term) || t.Protocolo.Contains(term) || t.Descricao.Contains(term));

        }



        var now = DateTime.UtcNow;

        var items = await query

            .OrderByDescending(t => t.CreatedAt)

            .Take(200)

            .ToListAsync(cancellationToken);



        return items.Select(t => MapListItem(t, now)).ToList();

    }



    public async Task<ConnectTicketDetailDto?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)

    {

        var ticket = await LoadTicketAsync(id, cancellationToken);

        return ticket is null ? null : MapDetail(ticket);

    }



    public async Task<ConnectTicketDetailDto> CreateAsync(

        Guid userId, CreateConnectTicketRequest request, CancellationToken cancellationToken = default)

    {

        var now = DateTime.UtcNow;

        var ticket = new ConnectTicket

        {

            Protocolo = await GenerateProtocoloAsync(cancellationToken),

            Titulo = request.Titulo.Trim(),

            Descricao = request.Descricao.Trim(),

            Categoria = request.Categoria,

            SolicitanteId = userId,

            ResponsavelId = request.ResponsavelId,

            Prioridade = request.Prioridade,

            Status = ConnectTicketStatus.Aberto,

            DueAt = ConnectTicketSlaCalculator.CalculateDueAt(request.Categoria, request.Prioridade, now),

        };



        db.ConnectTickets.Add(ticket);

        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = "ticket.create",

            EntityType = nameof(ConnectTicket),

            EntityId = ticket.Id,

            Details = ticket.Protocolo,

        });



        await db.SaveChangesAsync(cancellationToken);



        await NotifyTicketOpenedAsync(userId, ticket, cancellationToken);

        await realtimeNotifier.NotifyTicketUpdatedAsync(ticket.Id, cancellationToken);



        return MapDetail(await LoadTicketAsync(ticket.Id, cancellationToken) ?? ticket);

    }



    public async Task<ConnectTicketDetailDto?> UpdateAsync(

        Guid userId, Guid id, UpdateConnectTicketRequest request, CancellationToken cancellationToken = default)

    {

        var ticket = await db.ConnectTickets

            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.DeletedAt == null, cancellationToken);



        if (ticket is null) return null;



        ticket.Titulo = request.Titulo.Trim();

        ticket.Descricao = request.Descricao.Trim();

        ticket.Categoria = request.Categoria;

        ticket.Prioridade = request.Prioridade;

        ticket.DueAt = ConnectTicketSlaCalculator.CalculateDueAt(request.Categoria, request.Prioridade, ticket.CreatedAt);

        ticket.UpdatedAt = DateTime.UtcNow;



        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = "ticket.update",

            EntityType = nameof(ConnectTicket),

            EntityId = ticket.Id,

        });



        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyTicketUpdatedAsync(ticket.Id, cancellationToken);

        return MapDetail(await LoadTicketAsync(ticket.Id, cancellationToken) ?? ticket);

    }



    public async Task<ConnectTicketDetailDto?> AssignAsync(

        Guid userId, Guid id, AssignConnectTicketRequest request, CancellationToken cancellationToken = default)

    {

        var ticket = await db.ConnectTickets

            .Include(t => t.Solicitante)

            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.DeletedAt == null, cancellationToken);



        if (ticket is null) return null;



        ticket.ResponsavelId = request.ResponsavelId;

        if (ticket.Status == ConnectTicketStatus.Aberto)

            ticket.Status = ConnectTicketStatus.EmAndamento;

        ticket.UpdatedAt = DateTime.UtcNow;



        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = "ticket.assign",

            EntityType = nameof(ConnectTicket),

            EntityId = ticket.Id,

            Details = request.ResponsavelId.ToString(),

        });



        await db.SaveChangesAsync(cancellationToken);



        await notificationService.CreateAsync(new CreateConnectNotificationRequest(

            request.ResponsavelId,

            $"Chamado atribuído: {ticket.Protocolo}",

            $"Você foi designado para o chamado \"{ticket.Titulo}\".",

            ConnectNotificationCategory.Alert,

            nameof(ConnectTicket),

            ticket.Id), cancellationToken);



        await realtimeNotifier.NotifyTicketUpdatedAsync(ticket.Id, cancellationToken);

        return MapDetail(await LoadTicketAsync(ticket.Id, cancellationToken) ?? ticket);

    }



    public async Task<ConnectTicketDetailDto?> ChangeStatusAsync(

        Guid userId, Guid id, ChangeConnectTicketStatusRequest request, CancellationToken cancellationToken = default)

    {

        var ticket = await db.ConnectTickets

            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.DeletedAt == null, cancellationToken);



        if (ticket is null) return null;



        ticket.Status = request.Status;

        if (request.Status == ConnectTicketStatus.Resolvido)

            ticket.ResolvedAt = DateTime.UtcNow;

        ticket.UpdatedAt = DateTime.UtcNow;



        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = "ticket.status",

            EntityType = nameof(ConnectTicket),

            EntityId = ticket.Id,

            Details = request.Status.ToString(),

        });



        await db.SaveChangesAsync(cancellationToken);



        if (ticket.SolicitanteId != userId)

        {

            await notificationService.CreateAsync(new CreateConnectNotificationRequest(

                ticket.SolicitanteId,

                $"Chamado {ticket.Protocolo} atualizado",

                $"Status alterado para {request.Status}.",

                ConnectNotificationCategory.Info,

                nameof(ConnectTicket),

                ticket.Id), cancellationToken);

        }



        await realtimeNotifier.NotifyTicketUpdatedAsync(ticket.Id, cancellationToken);

        return MapDetail(await LoadTicketAsync(ticket.Id, cancellationToken) ?? ticket);

    }



    public async Task<ConnectTicketCommentDto?> AddCommentAsync(

        Guid userId, Guid id, AddConnectTicketCommentRequest request, CancellationToken cancellationToken = default)

    {

        var ticket = await db.ConnectTickets

            .AnyAsync(t => t.Id == id && t.IsActive && t.DeletedAt == null, cancellationToken);



        if (!ticket) return null;



        var comment = new ConnectTicketComment

        {

            TicketId = id,

            UserId = userId,

            Content = request.Content.Trim(),

        };



        db.ConnectTicketComments.Add(comment);

        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = "ticket.comment",

            EntityType = nameof(ConnectTicket),

            EntityId = id,

        });



        await db.SaveChangesAsync(cancellationToken);



        await realtimeNotifier.NotifyTicketUpdatedAsync(id, cancellationToken);

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);

        return new ConnectTicketCommentDto(comment.Id, userId, user.FullName, comment.Content, comment.CreatedAt);

    }



    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)

    {

        var ticket = await db.ConnectTickets

            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.DeletedAt == null, cancellationToken);



        if (ticket is null) return false;



        ticket.DeletedAt = DateTime.UtcNow;

        ticket.UpdatedAt = DateTime.UtcNow;



        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = "ticket.delete",

            EntityType = nameof(ConnectTicket),

            EntityId = ticket.Id,

        });



        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyTicketUpdatedAsync(id, cancellationToken);

        return true;

    }



    private async Task NotifyTicketOpenedAsync(Guid userId, ConnectTicket ticket, CancellationToken cancellationToken)

    {

        if (ticket.ResponsavelId is Guid assigneeId && assigneeId != userId)

        {

            await notificationService.CreateAsync(new CreateConnectNotificationRequest(

                assigneeId,

                $"Novo chamado: {ticket.Protocolo}",

                ticket.Titulo,

                ConnectNotificationCategory.Alert,

                nameof(ConnectTicket),

                ticket.Id), cancellationToken);



            await mailService.CreateAsync(userId, new CreateMailRequest(

                $"[Chamado {ticket.Protocolo}] {ticket.Titulo}",

                ticket.Descricao,

                ticket.Prioridade,

                [new MailRecipientInputDto(assigneeId, MessageRecipientType.To)],

                null,

                true), cancellationToken);

        }

    }



    private async Task<string> GenerateProtocoloAsync(CancellationToken cancellationToken)

    {

        var prefix = $"CHM-{DateTime.UtcNow:yyyyMMdd}";

        var count = await db.ConnectTickets.CountAsync(

            t => t.Protocolo.StartsWith(prefix), cancellationToken);

        return $"{prefix}-{(count + 1):D4}";

    }



    private async Task<ConnectTicket?> LoadTicketAsync(Guid id, CancellationToken cancellationToken)

        => await db.ConnectTickets

            .Include(t => t.Solicitante)

            .Include(t => t.Responsavel)

            .Include(t => t.Comments).ThenInclude(c => c.User)

            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.DeletedAt == null, cancellationToken);



    private static ConnectTicketListItemDto MapListItem(ConnectTicket t, DateTime now)

    {

        var overdue = t.DueAt != null && t.DueAt < now

                      && t.Status is not ConnectTicketStatus.Resolvido and not ConnectTicketStatus.Cancelado;

        return new ConnectTicketListItemDto(

            t.Id, t.Protocolo, t.Titulo, t.Categoria, t.Status, t.Prioridade,

            t.Solicitante.FullName, t.Responsavel?.FullName, t.DueAt, overdue, t.CreatedAt);

    }



    private static ConnectTicketDetailDto MapDetail(ConnectTicket t)

    {

        var now = DateTime.UtcNow;

        var overdue = t.DueAt != null && t.DueAt < now

                      && t.Status is not ConnectTicketStatus.Resolvido and not ConnectTicketStatus.Cancelado;

        var comments = t.Comments

            .Where(c => c.IsActive)

            .OrderBy(c => c.CreatedAt)

            .Select(c => new ConnectTicketCommentDto(c.Id, c.UserId, c.User.FullName, c.Content, c.CreatedAt))

            .ToList();



        return new ConnectTicketDetailDto(

            t.Id, t.Protocolo, t.Titulo, t.Descricao, t.Categoria, t.Status, t.Prioridade,

            t.SolicitanteId, t.Solicitante.FullName, t.ResponsavelId, t.Responsavel?.FullName,

            t.DueAt, overdue, t.ResolvedAt, t.CreatedAt, comments);

    }

}

