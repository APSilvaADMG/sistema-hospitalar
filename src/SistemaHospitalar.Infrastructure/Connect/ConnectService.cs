using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.DTOs.Appointments;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectService(
    AppDbContext dbContext,
    IConnectBotService botService,
    ConnectMessagingService messaging,
    IAppointmentService appointmentService,
    IConnectRealtimeNotifier realtimeNotifier,
    WhatsAppHealthService whatsAppHealth,
    IOptions<ConnectSettings> connectSettings) : IConnectService
{
    public async Task<ConnectDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activeConversations = await dbContext.ConnectConversations
            .CountAsync(c => c.IsActive && c.LastMessageAt >= today, cancellationToken);

        var messagesToday = await dbContext.ConnectMessages
            .CountAsync(m => m.CreatedAt >= today, cancellationToken);

        var pendingReminders = await dbContext.ConnectScheduledMessages
            .CountAsync(m => m.IsActive && !m.IsSent && m.ScheduledFor >= DateTime.UtcNow, cancellationToken);

        var waitlist = await dbContext.ConnectWaitlistEntries
            .CountAsync(w => w.IsActive && w.Status == ConnectWaitlistStatus.Waiting, cancellationToken);

        var surveys = await dbContext.ConnectSatisfactionSurveys
            .Where(s => s.CreatedAt >= monthStart)
            .ToListAsync(cancellationToken);

        var checkIns = await dbContext.ConnectCheckIns
            .CountAsync(c => c.CheckedInAt >= today, cancellationToken);

        return new ConnectDashboardDto(
            activeConversations,
            messagesToday,
            pendingReminders,
            waitlist,
            surveys.Count,
            surveys.Count > 0 ? surveys.Average(s => s.Score) : 0,
            checkIns);
    }

    public async Task<IReadOnlyList<ConnectConversationDto>> GetConversationsAsync(
        ConnectConversationQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = dbContext.ConnectConversations.AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.AssignedUser)
            .Where(c => c.IsActive);

        if (query.BotStep is not null)
        {
            q = q.Where(c => c.BotStep == query.BotStep);
        }

        if (query.Queue is not null)
        {
            q = q.Where(c => c.Queue == query.Queue);
        }

        if (query.AwaitingHumanOnly)
        {
            q = q.Where(c => c.BotStep == ConnectBotStep.AwaitingHuman && c.ResolvedAt == null);
        }

        var rows = await q
            .OrderByDescending(c => c.HumanRequestedAt ?? c.LastMessageAt)
            .Take(Math.Clamp(query.Limit, 1, 200))
            .ToListAsync(cancellationToken);

        var ids = rows.Select(c => c.Id).ToList();
        var previews = await dbContext.ConnectMessages.AsNoTracking()
            .Where(m => ids.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => new
            {
                ConversationId = g.Key,
                Body = g.OrderByDescending(m => m.CreatedAt).Select(m => m.Body).FirstOrDefault(),
            })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Body, cancellationToken);

        return rows.Select(c => MapConversation(c, previews.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<ConnectConversationDetailDto?> GetConversationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ConnectConversations.AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.AssignedUser)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var messages = await dbContext.ConnectMessages.AsNoTracking()
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ConnectMessageDto(
                m.Id, m.ConversationId, m.Direction, m.Status, m.Body, m.CreatedAt, m.ReminderType))
            .ToListAsync(cancellationToken);

        return new ConnectConversationDetailDto(MapConversation(entity, null), messages);
    }

    public async Task<IReadOnlyList<ConnectWaitlistDto>> GetWaitlistAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ConnectWaitlistEntries.AsNoTracking()
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.CreatedAt)
            .Select(w => new ConnectWaitlistDto(
                w.Id,
                w.PatientId,
                w.Patient.FullName,
                w.Specialty.Name,
                w.Professional != null ? w.Professional.FullName : null,
                w.Status,
                w.Priority,
                w.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConnectKnowledgeArticleDto>> GetKnowledgeArticlesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ConnectKnowledgeArticles.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Category)
            .Select(a => new ConnectKnowledgeArticleDto(a.Id, a.Category, a.Question, a.Answer, a.Keywords))
            .ToListAsync(cancellationToken);
    }

    public async Task<ConnectSatisfactionStatsDto> GetSatisfactionStatsAsync(CancellationToken cancellationToken = default)
    {
        var surveys = await dbContext.ConnectSatisfactionSurveys.AsNoTracking()
            .Include(s => s.Professional)
            .Include(s => s.Specialty)
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        var byProf = surveys.Where(s => s.Professional != null)
            .GroupBy(s => s.Professional!.FullName)
            .Select(g => new ConnectSatisfactionByGroupDto(g.Key, g.Average(x => x.Score), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var bySpec = surveys.Where(s => s.Specialty != null)
            .GroupBy(s => s.Specialty!.Name)
            .Select(g => new ConnectSatisfactionByGroupDto(g.Key, g.Average(x => x.Score), g.Count()))
            .ToList();

        return new ConnectSatisfactionStatsDto(
            surveys.Count > 0 ? surveys.Average(s => s.Score) : 0,
            surveys.Count,
            byProf,
            bySpec);
    }

    public async Task<SimulateInboundResponse> SimulateInboundAsync(SimulateInboundRequest request, CancellationToken cancellationToken = default)
    {
        var reply = await botService.ProcessInboundAsync(
            request.Phone, request.Message, request.ContactName, cancellationToken: cancellationToken);
        var conversation = await dbContext.ConnectConversations
            .AsNoTracking()
            .Where(c => c.ContactPhone == ConnectPhoneHelper.Normalize(request.Phone))
            .OrderByDescending(c => c.LastMessageAt)
            .FirstAsync(cancellationToken);
        return new SimulateInboundResponse(reply, conversation.Id);
    }

    public async Task<BlockProfessionalScheduleResult> BlockProfessionalScheduleAsync(
        BlockProfessionalScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var start = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var appointments = await dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Professional).ThenInclude(p => p.Specialty)
            .Where(a => a.ProfessionalId == request.ProfessionalId
                && a.IsActive
                && a.ScheduledAt >= start
                && a.ScheduledAt < end
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.Completed)
            .ToListAsync(cancellationToken);

        var notified = 0;
        foreach (var appt in appointments)
        {
            await appointmentService.UpdateStatusAsync(appt.Id, new UpdateAppointmentStatusRequest(AppointmentStatus.Cancelled), cancellationToken);

            var phone = appt.Patient.MobilePhone ?? appt.Patient.Phone;
            if (string.IsNullOrWhiteSpace(phone))
            {
                continue;
            }

            var conversation = await messaging.GetOrCreateConversationAsync(phone, appt.Patient.FullName, cancellationToken);
            conversation.BotStep = ConnectBotStep.RescheduleSelect;
            var body = $"""
                O Dr(a). {appt.Professional.FullName} não poderá atender em {request.Date:dd/MM/yyyy}.

                Motivo: {request.Reason}

                Escolha um novo horário — responda REMARCAR ou acesse o menu.
                """;
            await messaging.SendOutboundAsync(conversation, body, ConnectReminderType.RescheduleOffer, appt.Id, cancellationToken: cancellationToken);
            notified++;
        }

        return new BlockProfessionalScheduleResult(appointments.Count, notified);
    }

    public async Task<ConnectWaitlistDto> JoinWaitlistAsync(JoinWaitlistRequest request, CancellationToken cancellationToken = default)
    {
        var entry = new ConnectWaitlistEntry
        {
            PatientId = request.PatientId,
            SpecialtyId = request.SpecialtyId,
            ProfessionalId = request.ProfessionalId,
            Status = ConnectWaitlistStatus.Waiting,
        };
        dbContext.ConnectWaitlistEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetWaitlistAsync(cancellationToken)).First(w => w.Id == entry.Id);
    }

    public async Task<ConnectIntegrationStatusDto> GetIntegrationStatusAsync(CancellationToken cancellationToken = default)
    {
        var wa = connectSettings.Value.WhatsApp;
        var health = whatsAppHealth.GetReport();
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var overdueAccounts = await dbContext.FinancialAccounts
            .CountAsync(f => f.IsActive
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid)
                && f.DueDate != null
                && f.DueDate < now, cancellationToken);

        var collectionRemindersToday = await dbContext.ConnectScheduledMessages
            .CountAsync(m => m.IsActive && m.IsSent
                && (m.ReminderType == ConnectReminderType.BillingReminder || m.ReminderType == ConnectReminderType.BillingOverdue)
                && m.SentAt >= today, cancellationToken);

        var failedToday = await dbContext.ConnectMessages
            .CountAsync(m => m.Direction == ConnectMessageDirection.Outbound
                && m.Status == ConnectMessageStatus.Failed
                && m.CreatedAt >= today, cancellationToken);

        var publicUrl = string.IsNullOrWhiteSpace(wa.PublicWebhookUrl)
            ? null
            : wa.PublicWebhookUrl;

        return new ConnectIntegrationStatusDto(
            wa.Enabled,
            wa.UseMockProvider,
            health.ProviderName,
            health.MetaConfigured,
            health.WebhookSecretConfigured,
            health.VerifyTokenConfigured,
            health.LiveMode,
            health.Ready,
            "/api/whatsapp/webhook",
            overdueAccounts,
            collectionRemindersToday,
            connectSettings.Value.Collection.Enabled,
            publicUrl,
            wa.TemplateLanguageCode,
            wa.ReminderTemplateName,
            wa.BillingTemplateName,
            wa.ConfirmationTemplateName,
            failedToday,
            health.Issues);
    }

    public async Task<ConnectInboxSummaryDto> GetInboxSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var awaitingHuman = await dbContext.ConnectConversations.CountAsync(
            c => c.IsActive && c.BotStep == ConnectBotStep.AwaitingHuman && c.ResolvedAt == null,
            cancellationToken);
        var assignedOpen = await dbContext.ConnectConversations.CountAsync(
            c => c.IsActive && c.AssignedUserId != null && c.ResolvedAt == null,
            cancellationToken);
        var messagesToday = await dbContext.ConnectMessages.CountAsync(m => m.CreatedAt >= today, cancellationToken);
        var failedToday = await dbContext.ConnectMessages.CountAsync(
            m => m.Direction == ConnectMessageDirection.Outbound
                && m.Status == ConnectMessageStatus.Failed
                && m.CreatedAt >= today,
            cancellationToken);

        return new ConnectInboxSummaryDto(awaitingHuman, assignedOpen, messagesToday, failedToday);
    }

    public async Task<ConnectMessageDto> ReplyAsync(
        Guid conversationId,
        ConnectReplyRequest request,
        Guid? staffUserId,
        CancellationToken cancellationToken = default)
    {
        var body = request.Body.Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Mensagem vazia.");
        }

        var conversation = await dbContext.ConnectConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Conversa não encontrada.");

        if (staffUserId is not null)
        {
            conversation.AssignedUserId = staffUserId;
        }

        conversation.ResolvedAt = null;
        conversation.UpdatedAt = DateTime.UtcNow;

        var message = await messaging.SendStaffReplyAsync(conversation, body, staffUserId, cancellationToken);
        await realtimeNotifier.NotifyMessageSentAsync(conversation.Id, message.Id, cancellationToken);
        await realtimeNotifier.NotifyConversationUpdatedAsync(conversation.Id, cancellationToken);
        await realtimeNotifier.NotifyInboxSummaryChangedAsync(cancellationToken);

        return new ConnectMessageDto(
            message.Id,
            message.ConversationId,
            message.Direction,
            message.Status,
            message.Body,
            message.CreatedAt,
            message.ReminderType);
    }

    public async Task<ConnectConversationDto?> AssignConversationAsync(
        Guid conversationId,
        ConnectAssignRequest request,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.ConnectConversations
            .Include(c => c.Patient)
            .Include(c => c.AssignedUser)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.IsActive, cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        conversation.AssignedUserId = request.UserId;
        if (request.Queue is not null)
        {
            conversation.Queue = request.Queue.Value;
        }

        conversation.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyConversationUpdatedAsync(conversation.Id, cancellationToken);
        await realtimeNotifier.NotifyInboxSummaryChangedAsync(cancellationToken);

        return MapConversation(conversation, null);
    }

    public async Task<ConnectConversationDto?> ResolveConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.ConnectConversations
            .Include(c => c.Patient)
            .Include(c => c.AssignedUser)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.IsActive, cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        conversation.ResolvedAt = DateTime.UtcNow;
        conversation.BotStep = ConnectBotStep.MainMenu;
        conversation.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyConversationUpdatedAsync(conversation.Id, cancellationToken);
        await realtimeNotifier.NotifyInboxSummaryChangedAsync(cancellationToken);

        return MapConversation(conversation, null);
    }

    private static ConnectConversationDto MapConversation(ConnectConversation c, string? preview) =>
        new(
            c.Id,
            c.PatientId,
            c.Patient?.FullName ?? c.ContactName,
            c.Channel,
            c.ContactPhone,
            c.BotStep,
            c.LastMessageAt,
            preview,
            c.Queue,
            c.AssignedUserId,
            c.AssignedUser?.FullName,
            c.HumanRequestedAt,
            c.ResolvedAt);
}
