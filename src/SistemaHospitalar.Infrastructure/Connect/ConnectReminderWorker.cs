using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectReminderWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ConnectSettings> settings,
    ILogger<ConnectReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no worker de lembretes Connect");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessDueRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messaging = scope.ServiceProvider.GetRequiredService<ConnectMessagingService>();
        var hospitalName = settings.Value.HospitalName;
        var unit = settings.Value.DefaultUnit;
        var collection = settings.Value.Collection;

        var due = await db.ConnectScheduledMessages
            .Include(m => m.Patient)
            .Include(m => m.Appointment)!.ThenInclude(a => a!.Professional).ThenInclude(p => p.Specialty)
            .Where(m => m.IsActive && !m.IsSent && m.ScheduledFor <= DateTime.UtcNow)
            .OrderBy(m => m.ScheduledFor)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var item in due)
        {
            if (item.ReminderType is ConnectReminderType.BillingReminder or ConnectReminderType.BillingOverdue)
            {
                await ProcessBillingReminderAsync(item, db, messaging, collection, hospitalName, cancellationToken);
                continue;
            }

            var phone = item.Patient?.MobilePhone ?? item.Patient?.Phone;
            if (string.IsNullOrWhiteSpace(phone) || item.Appointment is null)
            {
                item.IsSent = true;
                item.SentAt = DateTime.UtcNow;
                continue;
            }

            var appt = item.Appointment;
            var conversation = await messaging.GetOrCreateConversationAsync(phone, item.Patient!.FullName, cancellationToken);
            var body = BuildAppointmentReminderBody(item.ReminderType, appt, hospitalName, unit);

            if (item.ReminderType is ConnectReminderType.Confirmation72h or ConnectReminderType.Reminder24h)
            {
                conversation.BotStep = ConnectBotStep.ConfirmReminder;
                conversation.BotContextJson = JsonSerializer.Serialize(new BotContext { PendingAppointmentId = appt.Id });
            }
            else if (item.ReminderType == ConnectReminderType.CheckInInvite)
            {
                conversation.BotStep = ConnectBotStep.CheckIn;
                conversation.BotContextJson = JsonSerializer.Serialize(new BotContext { PendingAppointmentId = appt.Id });
            }
            else if (item.ReminderType == ConnectReminderType.SatisfactionSurvey)
            {
                conversation.BotStep = ConnectBotStep.Satisfaction;
                conversation.BotContextJson = JsonSerializer.Serialize(new BotContext { PendingAppointmentId = appt.Id });
            }

            await messaging.SendOutboundAsync(conversation, body, item.ReminderType, appt.Id, cancellationToken: cancellationToken);
            item.IsSent = true;
            item.SentAt = DateTime.UtcNow;
        }

        if (due.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessBillingReminderAsync(
        Domain.Entities.ConnectScheduledMessage item,
        AppDbContext db,
        ConnectMessagingService messaging,
        CollectionSettings collection,
        string hospitalName,
        CancellationToken cancellationToken)
    {
        if (!collection.Enabled)
        {
            item.IsSent = true;
            item.SentAt = DateTime.UtcNow;
            return;
        }

        Guid? accountId = null;
        try
        {
            using var doc = JsonDocument.Parse(item.PayloadJson);
            if (doc.RootElement.TryGetProperty("financialAccountId", out var idProp)
                && Guid.TryParse(idProp.GetString(), out var parsed))
            {
                accountId = parsed;
            }
        }
        catch
        {
            item.IsSent = true;
            item.SentAt = DateTime.UtcNow;
            return;
        }

        if (accountId is null)
        {
            item.IsSent = true;
            item.SentAt = DateTime.UtcNow;
            return;
        }

        var account = await db.FinancialAccounts
            .Include(f => f.Patient)
            .FirstOrDefaultAsync(f => f.Id == accountId.Value && f.IsActive, cancellationToken);

        if (account is null
            || account.Status is FinancialAccountStatus.Paid or FinancialAccountStatus.Cancelled)
        {
            item.IsSent = true;
            item.SentAt = DateTime.UtcNow;
            return;
        }

        if (account.Patient is null)
        {
            item.IsSent = true;
            item.SentAt = DateTime.UtcNow;
            return;
        }

        var phone = account.Patient.MobilePhone ?? account.Patient.Phone;
        if (string.IsNullOrWhiteSpace(phone))
        {
            item.IsSent = true;
            item.SentAt = DateTime.UtcNow;
            return;
        }

        var balance = account.Amount - account.PaidAmount;
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var overdue = item.ReminderType == ConnectReminderType.BillingOverdue;
        var body = overdue
            ? $"""
                ⚠️ Cobrança em atraso — {hospitalName}

                {account.Description}
                Valor em aberto: {balance.ToString("C", culture)}
                Vencimento: {account.DueDate?.ToLocalTime():dd/MM/yyyy}

                Chave PIX: {collection.PixKey}
                Favorecido: {collection.PixBeneficiary}

                {collection.PaymentInstructions}

                1️⃣ Ver detalhes da conta
                2️⃣ Voltar ao menu
                """
            : $"""
                💳 Lembrete de pagamento — {hospitalName}

                {account.Description}
                Valor: {balance.ToString("C", culture)}
                Vencimento: {account.DueDate?.ToLocalTime():dd/MM/yyyy}

                Chave PIX: {collection.PixKey}

                1️⃣ Ver detalhes / pagar
                2️⃣ Voltar ao menu
                """;

        var patientName = string.IsNullOrWhiteSpace(account.Patient.FullName)
            ? "Paciente"
            : account.Patient.FullName;
        var conversation = await messaging.GetOrCreateConversationAsync(phone, patientName, cancellationToken);
        conversation.PatientId = account.PatientId;
        conversation.BotStep = ConnectBotStep.BillingPaymentInfo;
        conversation.BotContextJson = JsonSerializer.Serialize(new BotContext
        {
            PendingFinancialAccountId = account.Id,
        });

        await messaging.SendOutboundAsync(
            conversation,
            body,
            item.ReminderType,
            financialAccountId: account.Id,
            cancellationToken: cancellationToken);
        item.IsSent = true;
        item.SentAt = DateTime.UtcNow;
    }

    private static string BuildAppointmentReminderBody(
        ConnectReminderType type,
        Domain.Entities.Appointment appt,
        string hospital,
        string unit)
    {
        var when = appt.ScheduledAt.ToLocalTime();
        var professionalName = string.IsNullOrWhiteSpace(appt.Professional?.FullName)
            ? "Profissional"
            : appt.Professional.FullName;
        var specialtyName = string.IsNullOrWhiteSpace(appt.Professional?.Specialty?.Name)
            ? "Especialidade não informada"
            : appt.Professional.Specialty.Name;
        return type switch
        {
            ConnectReminderType.Confirmation72h => $"""
                📅 Lembrete — {hospital}

                Você possui consulta agendada para {when:dd/MM} às {when:HH:mm}.
                {specialtyName} — Dr(a). {professionalName}
                📍 {unit}

                Deseja confirmar?
                1️⃣ Confirmar
                2️⃣ Cancelar
                3️⃣ Remarcar
                """,
            ConnectReminderType.Reminder24h => $"""
                ⏰ Consulta amanhã — {hospital}

                {when:dd/MM} às {when:HH:mm}
                Dr(a). {professionalName}

                1️⃣ Confirmar presença
                2️⃣ Cancelar
                3️⃣ Remarcar
                """,
            ConnectReminderType.CheckInInvite => $"""
                ✅ Check-in digital disponível

                Sua consulta é hoje às {when:HH:mm}.
                Clique para realizar check-in e atualizar seus dados.

                1️⃣ Fazer check-in agora
                2️⃣ Depois
                """,
            ConnectReminderType.SatisfactionSurvey => """
                Como você avalia seu atendimento?

                ⭐ Envie um número de 1 a 5
                """,
            _ => $"Lembrete: consulta em {when:dd/MM HH:mm} — {hospital}",
        };
    }
}
