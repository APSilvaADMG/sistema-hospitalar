using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectMessagingService(
    AppDbContext dbContext,
    IWhatsAppProvider whatsAppProvider,
    ConnectTemplateBuilder templateBuilder,
    IOptions<ConnectSettings> settings,
    ILogger<ConnectMessagingService> logger)
{
    private readonly ConnectSettings _settings = settings.Value;

    public async Task<ConnectConversation> GetOrCreateConversationAsync(
        string phone,
        string? contactName,
        CancellationToken cancellationToken = default)
    {
        var normalized = ConnectPhoneHelper.Normalize(phone);
        var conversation = await dbContext.ConnectConversations
            .FirstOrDefaultAsync(c => c.Channel == ConnectChannel.WhatsApp && c.ContactPhone == normalized && c.IsActive, cancellationToken);

        if (conversation is not null)
        {
            return conversation;
        }

        var patient = await FindPatientByPhoneAsync(normalized, cancellationToken);
        conversation = new ConnectConversation
        {
            Channel = ConnectChannel.WhatsApp,
            ContactPhone = normalized,
            ContactName = contactName ?? patient?.FullName,
            PatientId = patient?.Id,
            BotStep = ConnectBotStep.MainMenu,
        };
        dbContext.ConnectConversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<ConnectMessage?> TryRecordInboundAsync(
        ConnectConversation conversation,
        string body,
        string? externalMessageId,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(externalMessageId))
        {
            var existing = await dbContext.ConnectMessages.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ExternalId == externalMessageId, cancellationToken);
            if (existing is not null)
            {
                logger.LogDebug("Webhook WhatsApp idempotente — mensagem {ExternalId} já processada.", externalMessageId);
                return null;
            }
        }

        if (conversation.WhatsAppConsentAt is null)
        {
            conversation.WhatsAppConsentAt = DateTime.UtcNow;
        }

        var message = new ConnectMessage
        {
            ConversationId = conversation.Id,
            Direction = ConnectMessageDirection.Inbound,
            Status = ConnectMessageStatus.Delivered,
            Body = body,
            ExternalId = externalMessageId,
        };
        dbContext.ConnectMessages.Add(message);
        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "WhatsApp inbound registrado conv={ConversationId} phone={PhoneMasked}",
            conversation.Id,
            MaskPhone(conversation.ContactPhone));

        return message;
    }

    public async Task<ConnectMessage> SendOutboundAsync(
        ConnectConversation conversation,
        string body,
        ConnectReminderType? reminderType = null,
        Guid? appointmentId = null,
        Guid? financialAccountId = null,
        Guid? sentByUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.WhatsApp.Enabled)
        {
            logger.LogWarning("WhatsApp desabilitado — mensagem não enviada para {Phone}", MaskPhone(conversation.ContactPhone));
            return await PersistOutboundAsync(
                conversation, body, null, reminderType, appointmentId, ConnectMessageStatus.Failed,
                "WhatsApp desabilitado.", sentByUserId, cancellationToken);
        }

        if (reminderType is not null && conversation.WhatsAppOptOut)
        {
            logger.LogInformation(
                "LGPD opt-out — lembrete {ReminderType} não enviado para {Phone}",
                reminderType,
                MaskPhone(conversation.ContactPhone));
            return await PersistOutboundAsync(
                conversation, body, null, reminderType, appointmentId, ConnectMessageStatus.Failed,
                "Paciente optou por não receber mensagens proativas.", sentByUserId, cancellationToken);
        }

        Appointment? appointment = null;
        if (appointmentId is not null)
        {
            appointment = await dbContext.Appointments.AsNoTracking()
                .Include(a => a.Professional).ThenInclude(p => p.Specialty)
                .FirstOrDefaultAsync(a => a.Id == appointmentId.Value, cancellationToken);
        }

        FinancialAccount? account = null;
        if (financialAccountId is not null)
        {
            account = await dbContext.FinancialAccounts.AsNoTracking()
                .Include(f => f.Patient)
                .FirstOrDefaultAsync(f => f.Id == financialAccountId.Value, cancellationToken);
        }

        var withinWindow = await IsWithinServiceWindowAsync(conversation.Id, cancellationToken);
        var forceTemplate = reminderType is not null || !withinWindow;
        var useTemplate = forceTemplate
            && !whatsAppProvider.IsMock
            && !string.IsNullOrWhiteSpace(_settings.WhatsApp.PhoneNumberId);

        WhatsAppSendResult? sendResult = null;
        var displayBody = body;

        if (useTemplate)
        {
            ConnectTemplateSendPlan? plan = null;
            if (reminderType is not null)
            {
                if (templateBuilder.TryBuildForReminder(
                        reminderType.Value, conversation, appointment, account, body, out var reminderPlan))
                {
                    plan = reminderPlan;
                }
            }
            else if (templateBuilder.TryBuildUtility(body, out var utilityPlan))
            {
                plan = utilityPlan;
            }

            if (plan is not null)
            {
                displayBody = plan.DisplayBody;
                sendResult = await whatsAppProvider.SendTemplateAsync(
                    conversation.ContactPhone,
                    plan.TemplateName,
                    templateBuilder.LanguageCode,
                    plan.BodyParameters,
                    cancellationToken);
            }
        }

        sendResult ??= await whatsAppProvider.SendTextAsync(conversation.ContactPhone, body, cancellationToken);

        var status = sendResult.Success ? ConnectMessageStatus.Sent : ConnectMessageStatus.Failed;
        var message = await PersistOutboundAsync(
            conversation,
            displayBody,
            sendResult.ExternalId,
            reminderType,
            appointmentId,
            status,
            sendResult.ErrorMessage,
            sentByUserId,
            cancellationToken);

        if (sendResult.Success)
        {
            logger.LogInformation(
                "WhatsApp outbound enviado provider={Provider} conv={ConversationId} external={ExternalId} reminder={ReminderType}",
                whatsAppProvider.ProviderName,
                conversation.Id,
                sendResult.ExternalId,
                reminderType?.ToString() ?? "none");
        }
        else
        {
            logger.LogWarning(
                "WhatsApp outbound falhou provider={Provider} conv={ConversationId} code={ErrorCode} msg={ErrorMessage}",
                whatsAppProvider.ProviderName,
                conversation.Id,
                sendResult.ErrorCode,
                sendResult.ErrorMessage);
        }

        return message;
    }

    public async Task<ConnectMessage> SendStaffReplyAsync(
        ConnectConversation conversation,
        string body,
        Guid? sentByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var message = await SendOutboundAsync(conversation, body, sentByUserId: sentByUserId, cancellationToken: cancellationToken);
        conversation.BotStep = ConnectBotStep.MainMenu;
        conversation.ResolvedAt = null;
        conversation.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task RegisterOptOutAsync(ConnectConversation conversation, CancellationToken cancellationToken = default)
    {
        conversation.WhatsAppOptOut = true;
        conversation.WhatsAppOptOutAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "LGPD WhatsApp opt-out registrado conv={ConversationId} phone={PhoneMasked}",
            conversation.Id,
            MaskPhone(conversation.ContactPhone));
    }

    private async Task<ConnectMessage> PersistOutboundAsync(
        ConnectConversation conversation,
        string body,
        string? externalId,
        ConnectReminderType? reminderType,
        Guid? appointmentId,
        ConnectMessageStatus status,
        string? failureReason,
        Guid? sentByUserId,
        CancellationToken cancellationToken)
    {
        var message = new ConnectMessage
        {
            ConversationId = conversation.Id,
            Direction = ConnectMessageDirection.Outbound,
            Status = status,
            Body = body,
            ExternalId = externalId,
            FailureReason = failureReason,
            SentByUserId = sentByUserId,
            ReminderType = reminderType,
            AppointmentId = appointmentId,
        };
        dbContext.ConnectMessages.Add(message);
        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return message;
    }

    private async Task<bool> IsWithinServiceWindowAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var lastInbound = await dbContext.ConnectMessages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.Direction == ConnectMessageDirection.Inbound)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastInbound == default)
        {
            return false;
        }

        return DateTime.UtcNow - lastInbound < TimeSpan.FromHours(24);
    }

    public async Task<Patient?> FindPatientByPhoneAsync(string normalizedPhone, CancellationToken cancellationToken)
    {
        var suffix = normalizedPhone.Length >= 8 ? normalizedPhone[^8..] : normalizedPhone;
        return await dbContext.Patients.AsNoTracking()
            .Where(p => p.IsActive && (
                (p.MobilePhone != null && p.MobilePhone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(suffix))
                || (p.Phone != null && p.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(suffix))))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Patient?> FindPatientByCpfAsync(string cpf, CancellationToken cancellationToken)
    {
        var digits = ConnectPhoneHelper.DigitsOnly(cpf);
        return await dbContext.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.Cpf.Replace(".", "").Replace("-", "") == digits, cancellationToken);
    }

    private static string MaskPhone(string phone)
        => phone.Length <= 4 ? "****" : new string('*', phone.Length - 4) + phone[^4..];
}
