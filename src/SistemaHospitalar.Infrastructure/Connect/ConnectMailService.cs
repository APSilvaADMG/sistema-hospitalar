using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectMailService(
    AppDbContext db,
    IConnectRealtimeNotifier realtimeNotifier,
    IConnectContextService contextService,
    IConnectAttachmentStorage attachmentStorage,
    Microsoft.Extensions.Options.IOptions<ConnectSettings> connectSettings) : IConnectMailService
{
    public async Task<IReadOnlyList<MailListItemDto>> ListAsync(
        Guid userId, MailFolder folder, string? search, CancellationToken cancellationToken = default)
    {
        var query = db.InternalMessageRecipients
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Folder == folder && r.IsActive)
            .Where(r => r.Message.IsActive && r.Message.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r =>
                r.Message.Subject.ToLower().Contains(term) ||
                r.Message.Content.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(r => r.Message.CreatedAt)
            .Take(100)
            .Select(r => new MailListItemDto(
                r.MessageId,
                r.Message.Subject,
                r.Message.Content.Length > 120 ? r.Message.Content.Substring(0, 120) : r.Message.Content,
                r.Message.Priority,
                r.Message.Sender.FullName,
                r.Message.CreatedAt,
                r.IsRead,
                r.Message.Attachments.Count(a => a.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<MailDetailDto?> GetAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default)
    {
        var recipient = await db.InternalMessageRecipients
            .AsNoTracking()
            .Include(r => r.Message).ThenInclude(m => m.Sender)
            .Include(r => r.Message).ThenInclude(m => m.Recipients).ThenInclude(rr => rr.User)
            .Include(r => r.Message).ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(r =>
                r.MessageId == messageId &&
                r.UserId == userId &&
                r.IsActive &&
                r.Message.IsActive &&
                r.Message.DeletedAt == null,
                cancellationToken);

        return recipient is null ? null : MapDetail(recipient);
    }

    public async Task<MailDetailDto> CreateAsync(
        Guid userId, CreateMailRequest request, CancellationToken cancellationToken = default)
    {
        var message = new InternalMessage
        {
            Subject = request.Subject.Trim(),
            Content = request.Content.Trim(),
            Priority = request.Priority,
            SenderId = userId,
            Status = request.SendNow ? InternalMessageStatus.Sent : InternalMessageStatus.Draft,
        };

        db.InternalMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        await AddAttachmentsAsync(message, request.Attachments, cancellationToken);

        if (request.SendNow)
        {
            await ApplyRecipientsAsync(message, userId, request.Recipients, MailFolder.Inbox, cancellationToken);
            await LogAsync(userId, "mail.send", nameof(InternalMessage), message.Id, "Mensagem enviada", cancellationToken);
        }
        else
        {
            await EnsureDraftRecipientsAsync(message, userId, request.Recipients, cancellationToken);
            await LogAsync(userId, "mail.draft", nameof(InternalMessage), message.Id, "Rascunho criado", cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyCommSummaryChangedAsync(userId, cancellationToken);
        await realtimeNotifier.NotifyMailUpdatedAsync(userId, message.Id, cancellationToken);

        if (request.Context is not null &&
            (request.Context.PatientId != null || request.Context.TissGuideId != null ||
             request.Context.SusGuideId != null || request.Context.AppointmentId != null ||
             request.Context.TicketId != null))
        {
            await contextService.LinkMessageContextAsync(message.Id, request.Context, cancellationToken);
        }

        return (await GetAsync(userId, message.Id, cancellationToken))!;
    }

    public async Task<MailDetailDto?> UpdateDraftAsync(
        Guid userId, Guid messageId, UpdateMailRequest request, CancellationToken cancellationToken = default)
    {
        var message = await db.InternalMessages
            .Include(m => m.Recipients)
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m =>
                m.Id == messageId &&
                m.SenderId == userId &&
                m.Status == InternalMessageStatus.Draft &&
                m.IsActive &&
                m.DeletedAt == null,
                cancellationToken);

        if (message is null) return null;

        message.Subject = request.Subject.Trim();
        message.Content = request.Content.Trim();
        message.Priority = request.Priority;
        message.UpdatedAt = DateTime.UtcNow;

        db.InternalMessageAttachments.RemoveRange(message.Attachments);
        await AddAttachmentsAsync(message, request.Attachments, cancellationToken);

        db.InternalMessageRecipients.RemoveRange(message.Recipients.Where(r => r.Folder == MailFolder.Drafts));
        await EnsureDraftRecipientsAsync(message, userId, request.Recipients, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(userId, messageId, cancellationToken);
    }

    public async Task<(byte[] Content, string FileName, string MimeType)?> GetAttachmentAsync(
        Guid userId, Guid messageId, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await db.InternalMessageRecipients.AsNoTracking()
            .AnyAsync(r =>
                r.MessageId == messageId && r.UserId == userId && r.IsActive &&
                r.Message.IsActive && r.Message.DeletedAt == null, cancellationToken);

        if (!hasAccess) return null;

        var attachment = await db.InternalMessageAttachments.AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.Id == attachmentId && a.MessageId == messageId && a.IsActive, cancellationToken);

        if (attachment is null) return null;

        if (!string.IsNullOrWhiteSpace(attachment.StoragePath))
        {
            var fromDisk = await attachmentStorage.TryReadAsync(attachment.StoragePath, cancellationToken);
            if (fromDisk is not null)
            {
                return (fromDisk.Value.Content, attachment.FileName, fromDisk.Value.MimeType);
            }
        }

        if (!string.IsNullOrWhiteSpace(attachment.ContentBase64))
        {
            var bytes = Convert.FromBase64String(attachment.ContentBase64);
            return (bytes, attachment.FileName, attachment.MimeType);
        }

        return null;
    }

    public async Task<MailDetailDto?> SendDraftAsync(
        Guid userId, Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await db.InternalMessages
            .Include(m => m.Recipients)
            .FirstOrDefaultAsync(m =>
                m.Id == messageId &&
                m.SenderId == userId &&
                m.Status == InternalMessageStatus.Draft &&
                m.IsActive &&
                m.DeletedAt == null,
                cancellationToken);

        if (message is null) return null;

        var draftRecipients = message.Recipients.Where(r => r.Folder == MailFolder.Drafts).ToList();
        if (draftRecipients.Count == 0) return null;

        var inputs = draftRecipients
            .Where(r => r.UserId != userId)
            .Select(r => new MailRecipientInputDto(r.UserId, r.RecipientType))
            .ToList();

        db.InternalMessageRecipients.RemoveRange(message.Recipients);
        message.Status = InternalMessageStatus.Sent;
        message.UpdatedAt = DateTime.UtcNow;

        await ApplyRecipientsAsync(message, userId, inputs, MailFolder.Inbox, cancellationToken);
        await LogAsync(userId, "mail.send", nameof(InternalMessage), message.Id, "Rascunho enviado", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyCommSummaryChangedAsync(userId, cancellationToken);
        await realtimeNotifier.NotifyMailUpdatedAsync(userId, messageId, cancellationToken);

        return await GetAsync(userId, messageId, cancellationToken);
    }

    public async Task<bool> MarkReadAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default)
    {
        var recipient = await db.InternalMessageRecipients
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.IsActive, cancellationToken);

        if (recipient is null || recipient.IsRead) return recipient is not null;

        recipient.IsRead = true;
        recipient.ReadAt = DateTime.UtcNow;
        recipient.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyCommSummaryChangedAsync(userId, cancellationToken);
        await realtimeNotifier.NotifyHubNotificationUpdatedAsync(userId, cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default)
    {
        var recipient = await db.InternalMessageRecipients
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.IsActive, cancellationToken);

        if (recipient is null) return false;

        recipient.Folder = MailFolder.Archive;
        recipient.IsArchived = true;
        recipient.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TrashAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await db.InternalMessages
            .Include(m => m.Recipients)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.IsActive, cancellationToken);

        if (message is null) return false;

        var recipient = message.Recipients.FirstOrDefault(r => r.UserId == userId && r.IsActive);
        if (recipient is not null)
        {
            recipient.Folder = MailFolder.Trash;
            recipient.UpdatedAt = DateTime.UtcNow;
        }

        if (message.SenderId == userId)
        {
            message.DeletedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            foreach (var r in message.Recipients.Where(r => r.IsActive))
            {
                r.Folder = MailFolder.Trash;
                r.UpdatedAt = DateTime.UtcNow;
            }
        }

        await LogAsync(userId, "mail.trash", nameof(InternalMessage), messageId, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => db.InternalMessageRecipients.CountAsync(r =>
            r.UserId == userId &&
            r.Folder == MailFolder.Inbox &&
            !r.IsRead &&
            r.IsActive &&
            r.Message.IsActive &&
            r.Message.DeletedAt == null,
            cancellationToken);

    private async Task ApplyRecipientsAsync(
        InternalMessage message,
        Guid senderId,
        IReadOnlyList<MailRecipientInputDto> recipients,
        MailFolder inboxFolder,
        CancellationToken cancellationToken)
    {
        var distinct = recipients
            .GroupBy(r => r.UserId)
            .Select(g => g.First())
            .Where(r => r.UserId != senderId)
            .ToList();

        foreach (var input in distinct)
        {
            db.InternalMessageRecipients.Add(new InternalMessageRecipient
            {
                MessageId = message.Id,
                UserId = input.UserId,
                RecipientType = input.Type,
                Folder = inboxFolder,
            });

            await realtimeNotifier.NotifyCommSummaryChangedAsync(input.UserId, cancellationToken);
            await realtimeNotifier.NotifyMailUpdatedAsync(input.UserId, message.Id, cancellationToken);
        }

        db.InternalMessageRecipients.Add(new InternalMessageRecipient
        {
            MessageId = message.Id,
            UserId = senderId,
            RecipientType = MessageRecipientType.To,
            Folder = MailFolder.Sent,
            IsRead = true,
            ReadAt = DateTime.UtcNow,
        });
    }

    private Task EnsureDraftRecipientsAsync(
        InternalMessage message,
        Guid userId,
        IReadOnlyList<MailRecipientInputDto> recipients,
        CancellationToken cancellationToken)
    {
        db.InternalMessageRecipients.Add(new InternalMessageRecipient
        {
            MessageId = message.Id,
            UserId = userId,
            RecipientType = MessageRecipientType.To,
            Folder = MailFolder.Drafts,
            IsRead = true,
            ReadAt = DateTime.UtcNow,
        });

        foreach (var input in recipients
                     .GroupBy(r => r.UserId)
                     .Select(g => g.First())
                     .Where(r => r.UserId != userId))
        {
            db.InternalMessageRecipients.Add(new InternalMessageRecipient
            {
                MessageId = message.Id,
                UserId = input.UserId,
                RecipientType = input.Type,
                Folder = MailFolder.Drafts,
            });
        }

        return Task.CompletedTask;
    }

    private async Task AddAttachmentsAsync(
        InternalMessage message,
        IReadOnlyList<MailAttachmentInputDto>? attachments,
        CancellationToken cancellationToken)
    {
        if (attachments is null) return;

        var maxBytes = connectSettings.Value.MaxAttachmentBytes;

        foreach (var attachment in attachments.Take(5))
        {
            if (attachment.SizeBytes > maxBytes)
            {
                throw new InvalidOperationException(
                    $"Anexo \"{attachment.FileName}\" excede o limite de {maxBytes / (1024 * 1024)} MB.");
            }

            var entity = new InternalMessageAttachment
            {
                MessageId = message.Id,
                FileName = attachment.FileName.Trim(),
                MimeType = attachment.MimeType,
                SizeBytes = attachment.SizeBytes,
            };

            if (!string.IsNullOrWhiteSpace(attachment.ContentBase64))
            {
                var bytes = Convert.FromBase64String(attachment.ContentBase64);
                if (bytes.Length > maxBytes)
                {
                    throw new InvalidOperationException(
                        $"Anexo \"{attachment.FileName}\" excede o limite de {maxBytes / (1024 * 1024)} MB.");
                }

                db.InternalMessageAttachments.Add(entity);
                await db.SaveChangesAsync(cancellationToken);

                entity.StoragePath = await attachmentStorage.SaveAsync(
                    message.Id, entity.Id, entity.FileName, bytes, cancellationToken);
                entity.ContentBase64 = null;
            }
            else
            {
                db.InternalMessageAttachments.Add(entity);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static MailDetailDto MapDetail(InternalMessageRecipient recipient)
    {
        var message = recipient.Message;
        return new MailDetailDto(
            message.Id,
            message.Subject,
            message.Content,
            message.Priority,
            message.Status,
            message.SenderId,
            message.Sender.FullName,
            message.CreatedAt,
            recipient.IsRead,
            recipient.ReadAt,
            recipient.Folder,
            message.Recipients
                .Where(r => r.IsActive && r.Folder != MailFolder.Sent)
                .Select(r => new MailRecipientDto(r.UserId, r.User.FullName, r.RecipientType, r.IsRead, r.ReadAt))
                .ToList(),
            message.Attachments
                .Where(a => a.IsActive)
                .Select(a => new MailAttachmentDto(a.Id, a.FileName, a.MimeType, a.SizeBytes))
                .ToList());
    }

    private async Task LogAsync(
        Guid userId, string action, string entityType, Guid entityId, string? details, CancellationToken cancellationToken)
    {
        db.CommunicationAuditLogs.Add(new CommunicationAuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
        });
        await Task.CompletedTask;
    }
}
