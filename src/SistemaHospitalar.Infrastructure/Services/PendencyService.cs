using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Notifications;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class PendencyService(AppDbContext db) : IPendencyService
{
    public async Task SyncForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var syncedKeys = new HashSet<string>();

        await SyncOverdueTicketsAsync(userId, now, syncedKeys, cancellationToken);
        await SyncOverdueTasksAsync(userId, now, syncedKeys, cancellationToken);
        await SyncUnreadMailAsync(userId, syncedKeys, cancellationToken);
        await SyncGuideDraftsAsync(userId, syncedKeys, cancellationToken);
        await SyncLowStockAsync(userId, syncedKeys, cancellationToken);

        var stale = await db.PendingItems
            .Where(p => p.UsuarioResponsavelId == userId
                        && p.IsActive
                        && p.Status == PendingItemStatus.Aberta
                        && p.SourceEntityType != null)
            .ToListAsync(cancellationToken);

        foreach (var item in stale)
        {
            var key = $"{item.SourceEntityType}:{item.SourceEntityId}";
            if (!syncedKeys.Contains(key))
            {
                item.Status = PendingItemStatus.Concluida;
                item.UpdatedAt = now;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendencyDto>> ListForUserAsync(
        Guid userId,
        string? modulo = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.PendingItems
            .AsNoTracking()
            .Where(p => p.UsuarioResponsavelId == userId && p.IsActive);

        if (!string.IsNullOrWhiteSpace(modulo) && Enum.TryParse<PendingModule>(modulo, true, out var mod))
            query = query.Where(p => p.Modulo == mod);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PendingItemStatus>(status, true, out var st))
            query = query.Where(p => p.Status == st);
        else
            query = query.Where(p => p.Status == PendingItemStatus.Aberta || p.Status == PendingItemStatus.EmAndamento);

        return await query
            .OrderByDescending(p => p.Prioridade)
            .ThenBy(p => p.DataLimite)
            .Take(100)
            .Select(p => ToDto(p))
            .ToListAsync(cancellationToken);
    }

    public async Task<PendencySummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await db.PendingItems
            .AsNoTracking()
            .Where(p => p.UsuarioResponsavelId == userId && p.IsActive
                        && (p.Status == PendingItemStatus.Aberta || p.Status == PendingItemStatus.EmAndamento))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var porModulo = items
            .GroupBy(p => p.Modulo.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new PendencySummaryDto(
            items.Count,
            items.Count(p => p.Status == PendingItemStatus.Aberta),
            items.Count(p => p.DataLimite != null && p.DataLimite < now),
            items.Count(p => p.Prioridade == PendingItemPriority.Critica || p.Prioridade == PendingItemPriority.Alta),
            porModulo);
    }

    private async Task SyncOverdueTicketsAsync(
        Guid userId, DateTime now, HashSet<string> syncedKeys, CancellationToken cancellationToken)
    {
        var tickets = await db.ConnectTickets
            .AsNoTracking()
            .Include(t => t.Responsavel)
            .Where(t => t.IsActive && t.DeletedAt == null
                        && t.DueAt != null && t.DueAt < now
                        && t.Status != ConnectTicketStatus.Resolvido
                        && t.Status != ConnectTicketStatus.Cancelado
                        && (t.ResponsavelId == userId || t.SolicitanteId == userId))
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var ticket in tickets)
        {
            var key = $"ConnectTicket:{ticket.Id}";
            syncedKeys.Add(key);
            await UpsertAsync(
                userId,
                key,
                "ConnectTicket",
                ticket.Id,
                $"Chamado vencido: {ticket.Protocolo}",
                ticket.Titulo,
                PendingModule.Tickets,
                PendingItemType.TicketOverdue,
                MapPriority(ticket.Prioridade),
                ticket.Responsavel?.FullName,
                null,
                ticket.CreatedAt,
                ticket.DueAt,
                $"/connect/tickets/{ticket.Id}",
                cancellationToken);
        }
    }

    private async Task SyncOverdueTasksAsync(
        Guid userId, DateTime now, HashSet<string> syncedKeys, CancellationToken cancellationToken)
    {
        var tasks = await db.ConnectTasks
            .AsNoTracking()
            .Include(t => t.Responsavel)
            .Where(t => t.IsActive && t.DeletedAt == null
                        && t.Prazo != null && t.Prazo < now
                        && t.Status != ConnectTaskStatus.Concluida
                        && t.Status != ConnectTaskStatus.Cancelada
                        && t.ResponsavelId == userId)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            var key = $"ConnectTask:{task.Id}";
            syncedKeys.Add(key);
            await UpsertAsync(
                userId,
                key,
                "ConnectTask",
                task.Id,
                $"Tarefa vencida: {task.Titulo}",
                task.Descricao,
                PendingModule.Tasks,
                PendingItemType.TaskOverdue,
                MapPriority(task.Prioridade),
                task.Responsavel?.FullName,
                null,
                task.CreatedAt,
                task.Prazo,
                "/connect/tarefas",
                cancellationToken);
        }
    }

    private async Task SyncUnreadMailAsync(
        Guid userId, HashSet<string> syncedKeys, CancellationToken cancellationToken)
    {
        var unread = await db.InternalMessageRecipients
            .AsNoTracking()
            .Include(r => r.Message).ThenInclude(m => m.Sender)
            .Where(r => r.UserId == userId && r.IsActive && !r.IsRead
                        && r.Folder == MailFolder.Inbox
                        && r.Message.IsActive && r.Message.Status == InternalMessageStatus.Sent)
            .OrderByDescending(r => r.Message.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var recipient in unread)
        {
            var msg = recipient.Message;
            var key = $"InternalMessage:{msg.Id}";
            syncedKeys.Add(key);
            var priority = msg.Priority == MessagePriority.Urgente || msg.Priority == MessagePriority.Critica
                ? PendingItemPriority.Critica
                : msg.Priority == MessagePriority.Alta
                    ? PendingItemPriority.Alta
                    : PendingItemPriority.Normal;

            await UpsertAsync(
                userId,
                key,
                "InternalMessage",
                msg.Id,
                $"E-mail não lido: {msg.Subject}",
                msg.Content.Length > 200 ? msg.Content[..200] : msg.Content,
                PendingModule.Mail,
                PendingItemType.UnreadMail,
                priority,
                msg.Sender?.FullName,
                null,
                msg.CreatedAt,
                null,
                $"/connect/mail/{msg.Id}",
                cancellationToken);
        }
    }

    private async Task SyncGuideDraftsAsync(
        Guid userId, HashSet<string> syncedKeys, CancellationToken cancellationToken)
    {
        var drafts = await db.TissGuides
            .AsNoTracking()
            .Where(g => g.IsActive && g.Status == TissGuideStatus.Draft)
            .OrderByDescending(g => g.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var guide in drafts)
        {
            var key = $"TissGuide:{guide.Id}";
            syncedKeys.Add(key);
            await UpsertAsync(
                userId,
                key,
                "TissGuide",
                guide.Id,
                $"Guia TISS em rascunho",
                $"Guia #{guide.Id.ToString()[..8]} aguardando finalização",
                PendingModule.Guides,
                PendingItemType.GuideDraft,
                PendingItemPriority.Normal,
                null,
                null,
                guide.CreatedAt,
                null,
                $"/guias-tiss/{guide.Id}",
                cancellationToken);
        }
    }

    private async Task SyncLowStockAsync(
        Guid userId, HashSet<string> syncedKeys, CancellationToken cancellationToken)
    {
        var lowStock = await db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.QuantityOnHand < p.MinimumStock && p.MinimumStock > 0)
            .OrderBy(p => p.QuantityOnHand / p.MinimumStock)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var product in lowStock)
        {
            var key = $"Product:{product.Id}";
            syncedKeys.Add(key);
            await UpsertAsync(
                userId,
                key,
                "Product",
                product.Id,
                $"Estoque baixo: {product.Name}",
                $"Saldo {product.QuantityOnHand} {product.Unit} (mín. {product.MinimumStock})",
                PendingModule.Inventory,
                PendingItemType.LowStock,
                product.QuantityOnHand <= 0 ? PendingItemPriority.Critica : PendingItemPriority.Alta,
                null,
                null,
                DateTime.UtcNow,
                null,
                "/estoque",
                cancellationToken);
        }
    }

    private async Task UpsertAsync(
        Guid userId,
        string _,
        string sourceType,
        Guid sourceId,
        string titulo,
        string descricao,
        PendingModule modulo,
        PendingItemType tipo,
        PendingItemPriority prioridade,
        string? responsavel,
        string? setor,
        DateTime dataAbertura,
        DateTime? dataLimite,
        string linkDestino,
        CancellationToken cancellationToken)
    {
        var existing = await db.PendingItems
            .FirstOrDefaultAsync(p => p.UsuarioResponsavelId == userId
                                      && p.SourceEntityType == sourceType
                                      && p.SourceEntityId == sourceId
                                      && p.IsActive,
                cancellationToken);

        if (existing is null)
        {
            db.PendingItems.Add(new PendingItem
            {
                UsuarioResponsavelId = userId,
                Titulo = titulo,
                Descricao = descricao,
                Modulo = modulo,
                Tipo = tipo,
                Status = PendingItemStatus.Aberta,
                Prioridade = prioridade,
                Responsavel = responsavel,
                Setor = setor,
                DataAbertura = dataAbertura,
                DataLimite = dataLimite,
                LinkDestino = linkDestino,
                SourceEntityType = sourceType,
                SourceEntityId = sourceId,
            });
        }
        else if (existing.Status == PendingItemStatus.Concluida)
        {
            existing.Status = PendingItemStatus.Aberta;
            existing.Titulo = titulo;
            existing.Descricao = descricao;
            existing.Prioridade = prioridade;
            existing.DataLimite = dataLimite;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing.Titulo = titulo;
            existing.Descricao = descricao;
            existing.Prioridade = prioridade;
            existing.DataLimite = dataLimite;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static PendencyDto ToDto(PendingItem p) => new(
        p.Id,
        p.Titulo,
        p.Descricao,
        p.Modulo.ToString(),
        p.Tipo.ToString(),
        p.Status.ToString(),
        p.Prioridade.ToString(),
        p.Responsavel,
        p.Setor,
        p.DataAbertura,
        p.DataLimite,
        p.LinkDestino,
        p.UsuarioResponsavelId);

    private static PendingItemPriority MapPriority(MessagePriority priority) => priority switch
    {
        MessagePriority.Urgente or MessagePriority.Critica => PendingItemPriority.Critica,
        MessagePriority.Alta => PendingItemPriority.Alta,
        _ => PendingItemPriority.Normal,
    };
}
