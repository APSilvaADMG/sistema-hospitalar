using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectChatService(
    AppDbContext db,
    IConnectRealtimeNotifier realtimeNotifier) : IConnectChatService
{
    public async Task<IReadOnlyList<ChatRoomDto>> ListRoomsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rooms = await db.ChatParticipants
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.IsActive && p.Room.IsActive)
            .Select(p => p.Room)
            .Distinct()
            .ToListAsync(cancellationToken);

        var result = new List<ChatRoomDto>();
        foreach (var room in rooms)
        {
            var lastMessage = await db.ChatMessages
                .AsNoTracking()
                .Where(m => m.RoomId == room.Id && m.IsActive)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var participant = await db.ChatParticipants
                .AsNoTracking()
                .FirstAsync(p => p.RoomId == room.Id && p.UserId == userId, cancellationToken);

            var unread = await db.ChatMessages.CountAsync(m =>
                m.RoomId == room.Id &&
                m.IsActive &&
                m.SenderId != userId &&
                m.CreatedAt > (participant.LastReadAt ?? DateTime.MinValue),
                cancellationToken);

            result.Add(new ChatRoomDto(
                room.Id,
                room.Name,
                room.RoomType,
                lastMessage?.CreatedAt,
                lastMessage?.Content.Length > 80 ? lastMessage.Content[..80] : lastMessage?.Content,
                unread));
        }

        return result.OrderByDescending(r => r.LastMessageAt ?? DateTime.MinValue).ToList();
    }

    public async Task<ChatRoomDto?> CreateRoomAsync(
        Guid userId, CreateChatRoomRequest request, CancellationToken cancellationToken = default)
    {
        var participantIds = request.ParticipantUserIds
            .Append(userId)
            .Distinct()
            .ToList();

        if (request.RoomType == ChatRoomType.Private && participantIds.Count == 2)
        {
            var existing = await FindPrivateRoomAsync(participantIds[0], participantIds[1], cancellationToken);
            if (existing is not null)
            {
                return (await ListRoomsAsync(userId, cancellationToken)).FirstOrDefault(r => r.Id == existing.Id);
            }
        }

        var room = new ChatRoom
        {
            Name = request.Name.Trim(),
            RoomType = request.RoomType,
            SectorId = request.SectorId,
            CreatedByUserId = userId,
        };

        db.ChatRooms.Add(room);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var participantId in participantIds)
        {
            db.ChatParticipants.Add(new ChatParticipant
            {
                RoomId = room.Id,
                UserId = participantId,
            });
        }

        db.CommunicationAuditLogs.Add(new CommunicationAuditLog
        {
            UserId = userId,
            Action = "chat.room.create",
            EntityType = nameof(ChatRoom),
            EntityId = room.Id,
            Details = room.Name,
        });

        await db.SaveChangesAsync(cancellationToken);
        return (await ListRoomsAsync(userId, cancellationToken)).FirstOrDefault(r => r.Id == room.Id);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> ListMessagesAsync(
        Guid userId, Guid roomId, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (!await IsParticipantAsync(userId, roomId, cancellationToken))
            return [];

        return await db.ChatMessages
            .AsNoTracking()
            .Where(m => m.RoomId == roomId && m.IsActive)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto(
                m.Id,
                m.RoomId,
                m.SenderId,
                m.Sender.FullName,
                m.Content,
                m.CreatedAt,
                m.IsRead))
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatMessageDto?> SendMessageAsync(
        Guid userId, Guid roomId, SendChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!await IsParticipantAsync(userId, roomId, cancellationToken))
            return null;

        var message = new ChatMessage
        {
            RoomId = roomId,
            SenderId = userId,
            Content = request.Content.Trim(),
        };

        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        var dto = await db.ChatMessages
            .AsNoTracking()
            .Where(m => m.Id == message.Id)
            .Select(m => new ChatMessageDto(
                m.Id,
                m.RoomId,
                m.SenderId,
                m.Sender.FullName,
                m.Content,
                m.CreatedAt,
                m.IsRead))
            .FirstAsync(cancellationToken);

        await realtimeNotifier.NotifyChatMessageAsync(roomId, dto, cancellationToken);

        var participantIds = await db.ChatParticipants
            .AsNoTracking()
            .Where(p => p.RoomId == roomId && p.IsActive)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);

        foreach (var participantId in participantIds.Where(id => id != userId))
        {
            await realtimeNotifier.NotifyCommSummaryChangedAsync(participantId, cancellationToken);
            await realtimeNotifier.NotifyHubNotificationUpdatedAsync(participantId, cancellationToken);
        }

        return dto;
    }

    public async Task<bool> MarkRoomReadAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default)
    {
        var participant = await db.ChatParticipants
            .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId && p.IsActive, cancellationToken);

        if (participant is null) return false;

        var hadUnread = await db.ChatMessages.AnyAsync(m =>
            m.RoomId == roomId &&
            m.IsActive &&
            m.SenderId != userId &&
            m.CreatedAt > (participant.LastReadAt ?? DateTime.MinValue),
            cancellationToken);

        if (!hadUnread) return true;

        participant.LastReadAt = DateTime.UtcNow;
        participant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyCommSummaryChangedAsync(userId, cancellationToken);
        await realtimeNotifier.NotifyHubNotificationUpdatedAsync(userId, cancellationToken);
        return true;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var participants = await db.ChatParticipants
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.IsActive)
            .ToListAsync(cancellationToken);

        var total = 0;
        foreach (var participant in participants)
        {
            total += await db.ChatMessages.CountAsync(m =>
                m.RoomId == participant.RoomId &&
                m.IsActive &&
                m.SenderId != userId &&
                m.CreatedAt > (participant.LastReadAt ?? DateTime.MinValue),
                cancellationToken);
        }

        return total;
    }

    private async Task<bool> IsParticipantAsync(Guid userId, Guid roomId, CancellationToken cancellationToken)
        => await db.ChatParticipants.AnyAsync(
            p => p.RoomId == roomId && p.UserId == userId && p.IsActive,
            cancellationToken);

    private async Task<ChatRoom?> FindPrivateRoomAsync(Guid userA, Guid userB, CancellationToken cancellationToken)
    {
        var roomIdsA = await db.ChatParticipants
            .AsNoTracking()
            .Where(p => p.UserId == userA && p.Room.RoomType == ChatRoomType.Private && p.IsActive)
            .Select(p => p.RoomId)
            .ToListAsync(cancellationToken);

        foreach (var roomId in roomIdsA)
        {
            var members = await db.ChatParticipants
                .AsNoTracking()
                .Where(p => p.RoomId == roomId && p.IsActive)
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken);

            if (members.Count == 2 && members.Contains(userA) && members.Contains(userB))
            {
                return await db.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);
            }
        }

        return null;
    }
}
