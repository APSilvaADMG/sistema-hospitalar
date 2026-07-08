using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.DTOs.Appointments;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectBotService(
    AppDbContext dbContext,
    ConnectMessagingService messaging,
    IAppointmentService appointmentService,
    IFinancialAccountService financialAccountService,
    IPixPaymentService pixPaymentService,
    IConnectRealtimeNotifier realtimeNotifier,
    IOptions<ConnectSettings> settings) : IConnectBotService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly ConnectSettings _settings = settings.Value;

    public async Task<string> ProcessInboundAsync(
        string phone,
        string message,
        string? contactName,
        string? externalMessageId = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = await messaging.GetOrCreateConversationAsync(phone, contactName, cancellationToken);
        var text = message.Trim();

        if (IsOptOutKeyword(text))
        {
            await messaging.TryRecordInboundAsync(conversation, text, externalMessageId, cancellationToken);
            await messaging.RegisterOptOutAsync(conversation, cancellationToken);
            var optOutReply = """
                Você não receberá mais mensagens proativas por WhatsApp.

                Para voltar a receber lembretes, envie VOLTAR a qualquer momento.
                Atendimento humano continua disponível quando você nos escrever.
                """;
            await messaging.SendOutboundAsync(conversation, optOutReply, cancellationToken: cancellationToken);
            return optOutReply;
        }

        if (IsOptInKeyword(text) && conversation.WhatsAppOptOut)
        {
            await messaging.TryRecordInboundAsync(conversation, text, externalMessageId, cancellationToken);
            conversation.WhatsAppOptOut = false;
            conversation.WhatsAppOptOutAt = null;
            conversation.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            var welcomeBack = "Preferências atualizadas — você voltará a receber lembretes e avisos do hospital.\n\n"
                + await SendMainMenuAsync(conversation, cancellationToken);
            await messaging.SendOutboundAsync(conversation, welcomeBack, cancellationToken: cancellationToken);
            return welcomeBack;
        }

        var wasAwaitingHuman = conversation.BotStep == ConnectBotStep.AwaitingHuman;

        var inbound = await messaging.TryRecordInboundAsync(conversation, text, externalMessageId, cancellationToken);
        if (inbound is null)
        {
            return string.Empty;
        }

        await realtimeNotifier.NotifyMessageReceivedAsync(conversation.Id, inbound.Id, cancellationToken);
        await realtimeNotifier.NotifyInboxSummaryChangedAsync(cancellationToken);

        if (conversation.PatientId is null && conversation.BotStep != ConnectBotStep.AwaitingCpf)
        {
            var patient = await messaging.FindPatientByPhoneAsync(conversation.ContactPhone, cancellationToken);
            if (patient is not null)
            {
                conversation.PatientId = patient.Id;
                conversation.ContactName = patient.FullName;
            }
        }

        if (wasAwaitingHuman)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return string.Empty;
        }

        var ctx = DeserializeContext(conversation.BotContextJson);
        var reply = await DispatchAsync(conversation, ctx, text, cancellationToken);
        conversation.BotContextJson = SerializeContext(ctx);
        conversation.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(reply))
        {
            var outbound = await messaging.SendOutboundAsync(conversation, reply, cancellationToken: cancellationToken);
            await realtimeNotifier.NotifyMessageSentAsync(conversation.Id, outbound.Id, cancellationToken);
        }

        return reply;
    }

    private async Task<string> DispatchAsync(
        ConnectConversation conversation,
        BotContext ctx,
        string text,
        CancellationToken cancellationToken)
    {
        var lower = text.ToLowerInvariant();
        var choice = ParseChoice(text);

        if (IsGreeting(lower) && conversation.BotStep == ConnectBotStep.MainMenu)
        {
            return await SendMainMenuAsync(conversation, cancellationToken);
        }

        if (TryMatchNaturalLanguageScheduling(lower, out var specialtyKeyword))
        {
            return await StartNaturalLanguageSchedulingAsync(conversation, ctx, specialtyKeyword, cancellationToken);
        }

        if (IsBillingIntent(lower) && conversation.BotStep == ConnectBotStep.MainMenu)
        {
            return await StartBillingAsync(conversation, ctx, cancellationToken);
        }

        var (faqHit, faqAnswer) = await TryAnswerFaqAsync(lower, cancellationToken);
        if (faqHit)
        {
            return faqAnswer + "\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
        }

        return conversation.BotStep switch
        {
            ConnectBotStep.MainMenu => await HandleMainMenuAsync(conversation, ctx, choice, lower, cancellationToken),
            ConnectBotStep.AwaitingCpf => await HandleCpfAsync(conversation, text, cancellationToken),
            ConnectBotStep.ScheduleSpecialty => await HandleScheduleSpecialtyAsync(conversation, ctx, choice, cancellationToken),
            ConnectBotStep.ScheduleProfessional => await HandleScheduleProfessionalAsync(conversation, ctx, choice, cancellationToken),
            ConnectBotStep.ScheduleSlot => await HandleScheduleSlotAsync(conversation, ctx, choice, cancellationToken),
            ConnectBotStep.ScheduleConfirm => await HandleScheduleConfirmAsync(conversation, ctx, choice, cancellationToken),
            ConnectBotStep.RescheduleSelect => await HandleRescheduleSelectAsync(conversation, ctx, choice, cancellationToken),
            ConnectBotStep.CancelSelect => await HandleCancelSelectAsync(conversation, ctx, choice, cancellationToken),
            ConnectBotStep.ConfirmReminder => await HandleConfirmReminderAsync(conversation, ctx, choice, cancellationToken),
            ConnectBotStep.WaitlistOffer => await HandleWaitlistOfferAsync(conversation, ctx, choice, cancellationToken),
            ConnectBotStep.CheckIn => await HandleCheckInAsync(conversation, ctx, choice, cancellationToken),
            ConnectBotStep.PreTriage => await HandlePreTriageAsync(conversation, ctx, text, cancellationToken),
            ConnectBotStep.Satisfaction => await HandleSatisfactionAsync(conversation, ctx, choice, text, cancellationToken),
            ConnectBotStep.BillingSelectAccount => await HandleBillingSelectAsync(conversation, ctx, choice, cancellationToken),
            ConnectBotStep.BillingPaymentInfo => await HandleBillingPaymentInfoAsync(conversation, ctx, choice, text, cancellationToken),
            _ => await SendMainMenuAsync(conversation, cancellationToken),
        };
    }

    private async Task<string> HandleMainMenuAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, string lower, CancellationToken cancellationToken)
    {
        if (choice == 1 || lower.Contains("agendar"))
        {
            if (conversation.PatientId is null)
            {
                conversation.BotStep = ConnectBotStep.AwaitingCpf;
                return $"Olá! Sou a assistente virtual do {_settings.HospitalName}.\n\nPara agendar, informe seu CPF (somente números):";
            }

            conversation.BotStep = ConnectBotStep.ScheduleSpecialty;
            return await BuildSpecialtyMenuAsync(cancellationToken);
        }

        if (choice == 2 || lower.Contains("remarcar"))
        {
            return await StartRescheduleAsync(conversation, ctx, cancellationToken);
        }

        if (choice == 3 || lower.Contains("cancelar"))
        {
            return await StartCancelAsync(conversation, ctx, cancellationToken);
        }

        if (choice == 4 || lower.Contains("exame"))
        {
            conversation.BotStep = ConnectBotStep.PreTriage;
            ctx.TriageSymptoms = null;
            return "Pré-triagem para exames/atendimento.\n\nDescreva seus sintomas ou o exame desejado:";
        }

        if (choice == 5 || IsBillingIntent(lower))
        {
            return await StartBillingAsync(conversation, ctx, cancellationToken);
        }

        if (choice == 6 || lower.Contains("atendente"))
        {
            conversation.BotStep = ConnectBotStep.AwaitingHuman;
            conversation.HumanRequestedAt = DateTime.UtcNow;
            conversation.Queue = ConnectInboxQueue.Reception;
            conversation.ResolvedAt = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            await realtimeNotifier.NotifyAwaitingHumanAsync(conversation.Id, cancellationToken);
            await realtimeNotifier.NotifyInboxSummaryChangedAsync(cancellationToken);
            return "Um atendente foi notificado e responderá em breve. Horário: seg-sex 7h-19h.";
        }

        return await SendMainMenuAsync(conversation, cancellationToken);
    }

    private async Task<string> SendMainMenuAsync(ConnectConversation conversation, CancellationToken cancellationToken)
    {
        conversation.BotStep = ConnectBotStep.MainMenu;
        var name = conversation.ContactName ?? "paciente";
        await dbContext.SaveChangesAsync(cancellationToken);
        return $"""
            Olá, {name}! Sou a assistente virtual do {_settings.HospitalName}.

            Como posso ajudar?

            1️⃣ Agendar consulta
            2️⃣ Remarcar consulta
            3️⃣ Cancelar consulta
            4️⃣ Exames / Pré-triagem
            5️⃣ Minhas contas / Débitos
            6️⃣ Falar com atendente

            Você também pode escrever em linguagem natural, ex: "quero marcar cardiologista semana que vem".
            """;
    }

    private async Task<string> HandleCpfAsync(ConnectConversation conversation, string text, CancellationToken cancellationToken)
    {
        var ctx = DeserializeContext(conversation.BotContextJson);
        var patient = await messaging.FindPatientByCpfAsync(text, cancellationToken);
        if (patient is null)
        {
            return "CPF não encontrado. Verifique os dados ou procure a recepção.\n\nInforme o CPF novamente:";
        }

        conversation.PatientId = patient.Id;
        conversation.ContactName = patient.FullName;

        if (ctx.PostCpfTarget == "billing")
        {
            ctx.PostCpfTarget = null;
            conversation.BotContextJson = SerializeContext(ctx);
            return $"Olá, {patient.FullName}! CPF identificado.\n\n" + await ShowOutstandingAccountsAsync(conversation, ctx, cancellationToken);
        }

        conversation.BotStep = ConnectBotStep.ScheduleSpecialty;
        return $"Olá, {patient.FullName}! CPF identificado.\n\n" + await BuildSpecialtyMenuAsync(cancellationToken);
    }

    private async Task<string> BuildSpecialtyMenuAsync(CancellationToken cancellationToken)
    {
        var specialties = await dbContext.Specialties.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var lines = specialties.Select((s, i) => $"{i + 1}️⃣ {s.Name}");
        return "Escolha a especialidade:\n\n" + string.Join("\n", lines) + "\n\nUnidade: " + _settings.DefaultUnit;
    }

    private async Task<string> HandleScheduleSpecialtyAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, CancellationToken cancellationToken)
    {
        var specialties = await dbContext.Specialties.AsNoTracking()
            .Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(cancellationToken);

        if (choice is null || choice < 1 || choice > specialties.Count)
        {
            return "Opção inválida.\n\n" + await BuildSpecialtyMenuAsync(cancellationToken);
        }

        var specialty = specialties[choice.Value - 1];
        ctx.SpecialtyId = specialty.Id;
        ctx.SpecialtyName = specialty.Name;

        var professionals = await dbContext.Professionals.AsNoTracking()
            .Where(p => p.IsActive && p.SpecialtyId == specialty.Id)
            .OrderBy(p => p.FullName)
            .ToListAsync(cancellationToken);

        if (professionals.Count == 0)
        {
            conversation.BotStep = ConnectBotStep.MainMenu;
            return "Nenhum médico disponível para esta especialidade. Tente outra opção.\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
        }

        conversation.BotStep = ConnectBotStep.ScheduleProfessional;
        var profLines = professionals.Select((p, i) => $"{i + 1}️⃣ Dr(a). {p.FullName}");
        return $"Especialidade: {specialty.Name}\n\nEscolha o médico:\n\n{string.Join("\n", profLines)}";
    }

    private async Task<string> HandleScheduleProfessionalAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, CancellationToken cancellationToken)
    {
        var professionals = await dbContext.Professionals.AsNoTracking()
            .Where(p => p.IsActive && p.SpecialtyId == ctx.SpecialtyId)
            .OrderBy(p => p.FullName)
            .ToListAsync(cancellationToken);

        if (choice is null || choice < 1 || choice > professionals.Count)
        {
            return "Opção inválida. Escolha o médico pelo número.";
        }

        var prof = professionals[choice.Value - 1];
        ctx.ProfessionalId = prof.Id;
        ctx.ProfessionalName = prof.FullName;

        var slots = await ConnectSchedulingHelper.GetAvailableSlotsAsync(
            dbContext, ctx.SpecialtyId, ctx.ProfessionalId, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken: cancellationToken);

        if (slots.Count == 0)
        {
            conversation.BotStep = ConnectBotStep.MainMenu;
            return "Sem horários disponíveis nos próximos dias. Entre na lista de espera pela recepção.\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
        }

        ctx.OfferedSlots = slots.Select(s => new AvailableSlotSnapshot
        {
            ScheduledAt = s.ScheduledAt,
            ProfessionalId = s.ProfessionalId,
            ProfessionalName = s.ProfessionalName,
            SpecialtyName = s.SpecialtyName,
        }).ToList();

        conversation.BotStep = ConnectBotStep.ScheduleSlot;
        var slotLines = ctx.OfferedSlots.Select((s, i) => $"{i + 1}️⃣ {s.ScheduledAt.ToLocalTime():dd/MM HH:mm} — {s.ProfessionalName}");
        return $"Horários disponíveis ({_settings.DefaultUnit}):\n\n{string.Join("\n", slotLines)}\n\nEscolha o número do horário:";
    }

    private async Task<string> HandleScheduleSlotAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, CancellationToken cancellationToken)
    {
        if (choice is null || choice < 1 || choice > ctx.OfferedSlots.Count)
        {
            return "Opção inválida. Escolha o horário pelo número.";
        }

        var slot = ctx.OfferedSlots[choice.Value - 1];
        ctx.SelectedSlot = slot.ScheduledAt;
        ctx.ProfessionalId = slot.ProfessionalId;
        ctx.ProfessionalName = slot.ProfessionalName;
        conversation.BotStep = ConnectBotStep.ScheduleConfirm;

        return $"""
            Confirme seu agendamento:

            📋 {ctx.SpecialtyName}
            👨‍⚕️ Dr(a). {ctx.ProfessionalName}
            📍 {_settings.DefaultUnit}
            📅 {slot.ScheduledAt.ToLocalTime():dd/MM/yyyy}
            🕐 {slot.ScheduledAt.ToLocalTime():HH:mm}

            1️⃣ Confirmar
            2️⃣ Voltar ao menu
            """;
    }

    private async Task<string> HandleScheduleConfirmAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, CancellationToken cancellationToken)
    {
        if (choice == 2)
        {
            conversation.BotStep = ConnectBotStep.MainMenu;
            return await SendMainMenuAsync(conversation, cancellationToken);
        }

        if (choice != 1 || conversation.PatientId is null || ctx.SelectedSlot is null || ctx.ProfessionalId is null)
        {
            return "Responda 1 para confirmar ou 2 para voltar.";
        }

        var created = await appointmentService.CreateAsync(new CreateAppointmentRequest(
            conversation.PatientId.Value,
            ctx.ProfessionalId.Value,
            ctx.SelectedSlot.Value,
            30,
            $"Agendado via WhatsApp — {ctx.SpecialtyName}",
            ConnectMarkers.AppointmentSourceNote,
            null), cancellationToken);
        var appointment = created.Appointment;

        var protocol = ConnectSchedulingHelper.GenerateProtocol(appointment.Id);
        conversation.BotStep = ConnectBotStep.MainMenu;
        ctx.SelectedSlot = null;

        return $"""
            ✅ Consulta confirmada!

            Protocolo: {protocol}
            {appointment.SpecialtyName} com {appointment.ProfessionalName}
            {appointment.ScheduledAt.ToLocalTime():dd/MM/yyyy} às {appointment.ScheduledAt.ToLocalTime():HH:mm}
            📍 {_settings.DefaultUnit}

            Você receberá lembretes 72h e 24h antes.
            """;
    }

    private async Task<string> StartRescheduleAsync(ConnectConversation conversation, BotContext ctx, CancellationToken cancellationToken)
    {
        if (conversation.PatientId is null)
        {
            conversation.BotStep = ConnectBotStep.AwaitingCpf;
            return "Informe seu CPF para remarcar:";
        }

        var appointments = await GetUpcomingAppointmentsAsync(conversation.PatientId.Value, cancellationToken);
        if (appointments.Count == 0)
        {
            conversation.BotStep = ConnectBotStep.MainMenu;
            return "Nenhuma consulta futura encontrada.\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
        }

        ctx.PatientAppointmentIds = appointments.Select(a => a.Id).ToList();
        conversation.BotStep = ConnectBotStep.RescheduleSelect;
        var lines = appointments.Select((a, i) =>
            $"{i + 1}️⃣ {a.ScheduledAt.ToLocalTime():dd/MM HH:mm} — {a.SpecialtyName} — Dr(a). {a.ProfessionalName}");
        return "Qual consulta deseja remarcar?\n\n" + string.Join("\n", lines);
    }

    private async Task<string> HandleRescheduleSelectAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, CancellationToken cancellationToken)
    {
        if (choice is null || choice < 1 || choice > ctx.PatientAppointmentIds.Count)
        {
            return "Escolha uma consulta pelo número.";
        }

        var appointmentId = ctx.PatientAppointmentIds[choice.Value - 1];
        await appointmentService.UpdateStatusAsync(appointmentId, new UpdateAppointmentStatusRequest(AppointmentStatus.Cancelled), cancellationToken);
        ctx.PendingAppointmentId = appointmentId;

        var appt = await dbContext.Appointments.AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(a => new { a.Professional.SpecialtyId, a.ProfessionalId })
            .FirstAsync(cancellationToken);

        ctx.SpecialtyId = appt.SpecialtyId;
        ctx.ProfessionalId = appt.ProfessionalId;
        conversation.BotStep = ConnectBotStep.ScheduleProfessional;
        return "Consulta anterior cancelada.\n\n" + await HandleScheduleProfessionalAsync(conversation, ctx, 1, cancellationToken);
    }

    private async Task<string> StartCancelAsync(ConnectConversation conversation, BotContext ctx, CancellationToken cancellationToken)
    {
        if (conversation.PatientId is null)
        {
            conversation.BotStep = ConnectBotStep.AwaitingCpf;
            return "Informe seu CPF para cancelar:";
        }

        var appointments = await GetUpcomingAppointmentsAsync(conversation.PatientId.Value, cancellationToken);
        if (appointments.Count == 0)
        {
            conversation.BotStep = ConnectBotStep.MainMenu;
            return "Nenhuma consulta futura.\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
        }

        ctx.PatientAppointmentIds = appointments.Select(a => a.Id).ToList();
        conversation.BotStep = ConnectBotStep.CancelSelect;
        var lines = appointments.Select((a, i) =>
            $"{i + 1}️⃣ {a.ScheduledAt.ToLocalTime():dd/MM HH:mm} — {a.SpecialtyName}");
        return "Qual consulta deseja cancelar?\n\n" + string.Join("\n", lines);
    }

    private async Task<string> HandleCancelSelectAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, CancellationToken cancellationToken)
    {
        if (choice is null || choice < 1 || choice > ctx.PatientAppointmentIds.Count)
        {
            return "Escolha uma consulta pelo número.";
        }

        var appointmentId = ctx.PatientAppointmentIds[choice.Value - 1];
        await appointmentService.UpdateStatusAsync(appointmentId, new UpdateAppointmentStatusRequest(AppointmentStatus.Cancelled), cancellationToken);
        conversation.BotStep = ConnectBotStep.MainMenu;
        return "Consulta cancelada com sucesso. A vaga pode ser oferecida à lista de espera.\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
    }

    private async Task<string> HandleConfirmReminderAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, CancellationToken cancellationToken)
    {
        if (ctx.PendingAppointmentId is null)
        {
            conversation.BotStep = ConnectBotStep.MainMenu;
            return await SendMainMenuAsync(conversation, cancellationToken);
        }

        if (choice == 1)
        {
            await appointmentService.UpdateStatusAsync(ctx.PendingAppointmentId.Value,
                new UpdateAppointmentStatusRequest(AppointmentStatus.Confirmed), cancellationToken);
            conversation.BotStep = ConnectBotStep.MainMenu;
            return "Presença confirmada! Até lá.\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
        }

        if (choice == 2)
        {
            await appointmentService.UpdateStatusAsync(ctx.PendingAppointmentId.Value,
                new UpdateAppointmentStatusRequest(AppointmentStatus.Cancelled), cancellationToken);
            conversation.BotStep = ConnectBotStep.MainMenu;
            return "Consulta cancelada.\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
        }

        if (choice == 3)
        {
            return await StartRescheduleAsync(conversation, ctx, cancellationToken);
        }

        return "Responda 1 Confirmar, 2 Cancelar ou 3 Remarcar.";
    }

    private async Task<string> HandleWaitlistOfferAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, CancellationToken cancellationToken)
    {
        if (choice == 2)
        {
            conversation.BotStep = ConnectBotStep.MainMenu;
            return await SendMainMenuAsync(conversation, cancellationToken);
        }

        if (choice != 1 || ctx.SelectedSlot is null || conversation.PatientId is null)
        {
            return "Responda 1 para aceitar a vaga ou 2 para recusar.";
        }

        var created = await appointmentService.CreateAsync(new CreateAppointmentRequest(
            conversation.PatientId.Value,
            ctx.ProfessionalId!.Value,
            ctx.SelectedSlot.Value,
            30,
            "Lista de espera — WhatsApp",
            null,
            null), cancellationToken);
        var appointment = created.Appointment;

        conversation.BotStep = ConnectBotStep.MainMenu;
        return $"Vaga confirmada! {appointment.ScheduledAt.ToLocalTime():dd/MM HH:mm} com Dr(a). {appointment.ProfessionalName}. Protocolo: {ConnectSchedulingHelper.GenerateProtocol(appointment.Id)}";
    }

    private Task<string> HandleCheckInAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, CancellationToken cancellationToken)
    {
        if (choice == 1 && ctx.PendingAppointmentId is not null && conversation.PatientId is not null)
        {
            var checkIn = new ConnectCheckIn
            {
                AppointmentId = ctx.PendingAppointmentId.Value,
                PatientId = conversation.PatientId.Value,
                CheckedInAt = DateTime.UtcNow,
            };
            dbContext.ConnectCheckIns.Add(checkIn);
            conversation.BotStep = ConnectBotStep.MainMenu;
            return Task.FromResult("Check-in realizado! Apresente-se na recepção com documento. Seus dados já estão atualizados.");
        }

        conversation.BotStep = ConnectBotStep.MainMenu;
        return Task.FromResult("Check-in não realizado. Procure a recepção se necessário.");
    }

    private async Task<string> HandlePreTriageAsync(
        ConnectConversation conversation, BotContext ctx, string text, CancellationToken cancellationToken)
    {
        if (ctx.TriageSymptoms is null)
        {
            ctx.TriageSymptoms = text;
            return "Há quantos dias os sintomas começaram? (informe um número)";
        }

        if (ctx.TriageDurationDays is null && int.TryParse(ConnectPhoneHelper.DigitsOnly(text), out var days))
        {
            ctx.TriageDurationDays = days;
            return "Intensidade de 1 a 10:";
        }

        if (ctx.TriageIntensity is null && int.TryParse(text.Trim(), out var intensity) && intensity is >= 1 and <= 10)
        {
            ctx.TriageIntensity = intensity;
            conversation.BotStep = ConnectBotStep.MainMenu;

            if (conversation.PatientId is not null)
            {
                var recordId = await dbContext.MedicalRecords
                    .Where(m => m.PatientId == conversation.PatientId)
                    .Select(m => (Guid?)m.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (recordId is not null)
                {
                    var profId = await dbContext.Professionals.Select(p => p.Id).FirstOrDefaultAsync(cancellationToken);
                    dbContext.MedicalRecordEntries.Add(new MedicalRecordEntry
                    {
                        MedicalRecordId = recordId.Value,
                        EntryType = MedicalRecordEntryType.Anamnesis,
                        Content = $"Pré-triagem WhatsApp\nSintomas: {ctx.TriageSymptoms}\nDuração: {ctx.TriageDurationDays} dias\nIntensidade: {ctx.TriageIntensity}/10",
                        ProfessionalId = profId == Guid.Empty ? null : profId,
                    });
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            return $"""
                Ficha de pré-triagem registrada para a equipe de enfermagem.

                Sintomas: {ctx.TriageSymptoms}
                Duração: {ctx.TriageDurationDays} dias
                Intensidade: {ctx.TriageIntensity}/10

                Aguarde na recepção ou use opção 1 para agendar consulta.
                """;
        }

        return "Informe um número de 1 a 10 para intensidade.";
    }

    private async Task<string> HandleSatisfactionAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, string text, CancellationToken cancellationToken)
    {
        var score = choice ?? (int.TryParse(text.Trim(), out var s) && s is >= 1 and <= 5 ? s : 0);
        if (score is < 1 or > 5)
        {
            return "Avalie de 1 a 5 estrelas (envie um número).";
        }

        dbContext.ConnectSatisfactionSurveys.Add(new ConnectSatisfactionSurvey
        {
            PatientId = conversation.PatientId,
            AppointmentId = ctx.PendingAppointmentId,
            Score = score,
        });
        conversation.BotStep = ConnectBotStep.MainMenu;
        await dbContext.SaveChangesAsync(cancellationToken);
        return $"Obrigado pela avaliação ({score}⭐)! Sua opinião nos ajuda a melhorar.\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
    }

    private async Task<string> StartNaturalLanguageSchedulingAsync(
        ConnectConversation conversation, BotContext ctx, string keyword, CancellationToken cancellationToken)
    {
        if (conversation.PatientId is null)
        {
            conversation.BotStep = ConnectBotStep.AwaitingCpf;
            return "Identifique-se com seu CPF para eu buscar horários:";
        }

        var specialty = await dbContext.Specialties.AsNoTracking()
            .FirstOrDefaultAsync(s => s.IsActive && s.Name.ToLower().Contains(keyword), cancellationToken);

        if (specialty is null)
        {
            return $"Não encontrei especialidade para \"{keyword}\".\n\n" + await BuildSpecialtyMenuAsync(cancellationToken);
        }

        ctx.SpecialtyId = specialty.Id;
        ctx.SpecialtyName = specialty.Name;
        var slots = await ConnectSchedulingHelper.GetAvailableSlotsAsync(
            dbContext, specialty.Id, null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), cancellationToken: cancellationToken);

        if (slots.Count == 0)
        {
            return $"Sem horários para {specialty.Name}. Posso incluir você na lista de espera — responda LISTA.";
        }

        ctx.OfferedSlots = slots.Select(s => new AvailableSlotSnapshot
        {
            ScheduledAt = s.ScheduledAt,
            ProfessionalId = s.ProfessionalId,
            ProfessionalName = s.ProfessionalName,
            SpecialtyName = s.SpecialtyName,
        }).ToList();

        conversation.BotStep = ConnectBotStep.ScheduleSlot;
        var lines = ctx.OfferedSlots.Take(5).Select((s, i) => $"{i + 1}️⃣ {ConnectSchedulingHelper.FormatSlot(new(s.ScheduledAt, s.ProfessionalId, s.ProfessionalName, s.SpecialtyName))}");
        return $"Encontrei horários de {specialty.Name}:\n\n{string.Join("\n", lines)}\n\nEscolha o número:";
    }

    private async Task<List<AppointmentDto>> GetUpcomingAppointmentsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await dbContext.Appointments.AsNoTracking()
            .Where(a => a.PatientId == patientId && a.IsActive
                && a.ScheduledAt >= now
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow
                && a.Status != AppointmentStatus.Completed)
            .OrderBy(a => a.ScheduledAt)
            .Select(a => new AppointmentDto(
                a.Id, a.PatientId, a.Patient.FullName, a.ProfessionalId, a.Professional.FullName,
                a.Professional.Specialty.Name, a.ScheduledAt, a.DurationMinutes, a.Status, a.Reason, a.Room))
            .ToListAsync(cancellationToken);
    }

    private async Task<string> StartBillingAsync(
        ConnectConversation conversation, BotContext ctx, CancellationToken cancellationToken)
    {
        if (!_settings.Collection.Enabled)
        {
            return "Consulta de débitos indisponível no momento. Procure o financeiro na recepção.";
        }

        if (conversation.PatientId is null)
        {
            ctx.PostCpfTarget = "billing";
            conversation.BotContextJson = SerializeContext(ctx);
            conversation.BotStep = ConnectBotStep.AwaitingCpf;
            return $"Para consultar débitos, informe seu CPF (somente números):";
        }

        return await ShowOutstandingAccountsAsync(conversation, ctx, cancellationToken);
    }

    private async Task<string> ShowOutstandingAccountsAsync(
        ConnectConversation conversation, BotContext ctx, CancellationToken cancellationToken)
    {
        var accounts = await financialAccountService.GetOutstandingByPatientAsync(
            conversation.PatientId!.Value, cancellationToken);

        if (accounts.Count == 0)
        {
            conversation.BotStep = ConnectBotStep.MainMenu;
            return "Você não possui débitos em aberto. ✅\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
        }

        if (accounts.Count == 1)
        {
            ctx.PendingFinancialAccountId = accounts[0].Id;
            conversation.BotStep = ConnectBotStep.BillingPaymentInfo;
            conversation.BotContextJson = SerializeContext(ctx);
            return FormatAccountPaymentInfo(accounts[0]);
        }

        ctx.OfferedFinancialAccountIds = accounts.Select(a => a.Id).ToList();
        conversation.BotStep = ConnectBotStep.BillingSelectAccount;
        conversation.BotContextJson = SerializeContext(ctx);

        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var lines = accounts.Select((a, i) =>
            $"{i + 1}️⃣ {a.Description} — saldo {a.Balance.ToString("C", culture)} (venc. {a.DueDate?.ToLocalTime():dd/MM/yyyy})");
        return "Contas em aberto:\n\n" + string.Join("\n", lines) + "\n\nEscolha o número da conta:";
    }

    private async Task<string> HandleBillingSelectAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, CancellationToken cancellationToken)
    {
        if (conversation.PatientId is null)
        {
            return await StartBillingAsync(conversation, ctx, cancellationToken);
        }

        if (choice is null || choice < 1 || choice > ctx.OfferedFinancialAccountIds.Count)
        {
            return "Escolha uma conta pelo número.";
        }

        var accountId = ctx.OfferedFinancialAccountIds[choice.Value - 1];
        var accounts = await financialAccountService.GetOutstandingByPatientAsync(
            conversation.PatientId!.Value, cancellationToken);
        var account = accounts.FirstOrDefault(a => a.Id == accountId);

        if (account is null)
        {
            conversation.BotStep = ConnectBotStep.MainMenu;
            return "Conta não encontrada ou já quitada.\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
        }

        ctx.PendingFinancialAccountId = account.Id;
        conversation.BotStep = ConnectBotStep.BillingPaymentInfo;
        conversation.BotContextJson = SerializeContext(ctx);
        return FormatAccountPaymentInfo(account);
    }

    private async Task<string> HandleBillingPaymentInfoAsync(
        ConnectConversation conversation, BotContext ctx, int? choice, string text, CancellationToken cancellationToken)
    {
        if (conversation.PatientId is null)
        {
            return await StartBillingAsync(conversation, ctx, cancellationToken);
        }

        if (choice == 2)
        {
            conversation.BotStep = ConnectBotStep.MainMenu;
            return await SendMainMenuAsync(conversation, cancellationToken);
        }

        if (ctx.PendingFinancialAccountId is null)
        {
            return await StartBillingAsync(conversation, ctx, cancellationToken);
        }

        var accounts = await financialAccountService.GetOutstandingByPatientAsync(
            conversation.PatientId!.Value, cancellationToken);
        var account = accounts.FirstOrDefault(a => a.Id == ctx.PendingFinancialAccountId);

        if (account is null)
        {
            conversation.BotStep = ConnectBotStep.MainMenu;
            return "Conta quitada ou não encontrada. ✅\n\n" + await SendMainMenuAsync(conversation, cancellationToken);
        }

        if (choice == 1 || text.Trim().Equals("pix", StringComparison.OrdinalIgnoreCase))
        {
            return await GeneratePixChargeMessageAsync(account, cancellationToken);
        }

        return FormatAccountPaymentInfo(account) + "\n\n1️⃣ Gerar PIX automático\n2️⃣ Voltar ao menu";
    }

    private async Task<string> GeneratePixChargeMessageAsync(
        Application.DTOs.Financial.FinancialAccountDto account,
        CancellationToken cancellationToken)
    {
        if (!_settings.Collection.PixEnabled)
        {
            return FormatAccountPaymentInfo(account) + "\n\nPIX automático indisponível no momento.";
        }

        try
        {
            var charge = await pixPaymentService.CreateChargeForAccountAsync(account.Id, cancellationToken);
            var culture = CultureInfo.GetCultureInfo("pt-BR");
            var expires = charge.ExpiresAt.ToLocalTime();

            return $"""
                💠 PIX gerado — pagamento automático

                {account.Description}
                Valor: {charge.Amount.ToString("C", culture)}
                Válido até: {expires:dd/MM/yyyy HH:mm}
                ID: {charge.TxId}

                Copia e cola:
                {charge.CopyPasteCode}

                Após o pagamento, a conta é baixada automaticamente e você recebe a confirmação aqui.

                2️⃣ Voltar ao menu
                """;
        }
        catch (InvalidOperationException ex)
        {
            return FormatAccountPaymentInfo(account) + $"\n\nNão foi possível gerar o PIX: {ex.Message}";
        }
    }

    private string FormatAccountPaymentInfo(Application.DTOs.Financial.FinancialAccountDto account)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var collection = _settings.Collection;
        var overdue = account.DueDate.HasValue && account.DueDate.Value < DateTime.UtcNow;

        return $"""
            💳 {(overdue ? "Conta em atraso" : "Conta em aberto")}

            {account.Description}
            Valor total: {account.Amount.ToString("C", culture)}
            Já pago: {account.PaidAmount.ToString("C", culture)}
            Saldo: {account.Balance.ToString("C", culture)}
            Vencimento: {account.DueDate?.ToLocalTime():dd/MM/yyyy}

            Pagamento via PIX automático
            Chave: {collection.PixKey}
            Favorecido: {collection.PixBeneficiary}

            1️⃣ Gerar PIX automático (copia e cola + baixa automática)
            2️⃣ Voltar ao menu

            {collection.PaymentInstructions}
            """;
    }

    private static bool IsBillingIntent(string lower)
        => lower.Contains("débito") || lower.Contains("debito") || lower.Contains("cobran")
            || lower.Contains("pagar") || lower.Contains("conta") || lower.Contains("pix")
            || lower.Contains("boleto") || lower.Contains("financeiro");

    private static bool IsOptOutKeyword(string text)
    {
        var normalized = text.Trim().ToUpperInvariant();
        return normalized is "SAIR" or "PARAR" or "STOP" or "CANCELAR" or "CANCELAR MENSAGENS"
            or "NAO QUERO" or "NÃO QUERO" or "OPT OUT" or "OPT-OUT";
    }

    private static bool IsOptInKeyword(string text)
    {
        var normalized = text.Trim().ToUpperInvariant();
        return normalized is "VOLTAR" or "REATIVAR" or "QUERO RECEBER";
    }

    private static bool IsGreeting(string lower)
        => lower is "oi" or "olá" or "ola" or "bom dia" or "boa tarde" or "boa noite" or "menu" or "inicio" or "início";

    private static int? ParseChoice(string text)
    {
        var digits = ConnectPhoneHelper.DigitsOnly(text);
        if (digits.Length == 1 && int.TryParse(digits, out var n))
        {
            return n;
        }

        if (text.Length == 1 && char.IsDigit(text[0]))
        {
            return text[0] - '0';
        }

        return null;
    }

    private static bool TryMatchNaturalLanguageScheduling(string lower, out string keyword)
    {
        keyword = string.Empty;
        var map = new Dictionary<string, string>
        {
            ["cardio"] = "cardio",
            ["cardiolog"] = "cardio",
            ["pediatr"] = "pediatr",
            ["clínica geral"] = "clínica",
            ["clinica geral"] = "clínica",
            ["clínica"] = "clínica",
        };

        foreach (var (k, v) in map)
        {
            if (lower.Contains(k))
            {
                keyword = v;
                return lower.Contains("marcar") || lower.Contains("agendar") || lower.Contains("consulta") || lower.Contains("médico") || lower.Contains("medico");
            }
        }

        return false;
    }

    private async Task<(bool Hit, string Answer)> TryAnswerFaqAsync(string lower, CancellationToken cancellationToken)
    {
        var articles = await dbContext.ConnectKnowledgeArticles.AsNoTracking()
            .Where(a => a.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var article in articles)
        {
            var keywords = (article.Keywords ?? article.Question).ToLowerInvariant().Split(',', ' ', StringSplitOptions.RemoveEmptyEntries);
            if (keywords.Any(k => k.Length > 3 && lower.Contains(k)))
            {
                return (true, article.Answer);
            }
        }

        return (false, string.Empty);
    }

    private static BotContext DeserializeContext(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new BotContext();
        }

        try
        {
            return JsonSerializer.Deserialize<BotContext>(json, JsonOptions) ?? new BotContext();
        }
        catch
        {
            return new BotContext();
        }
    }

    private static string SerializeContext(BotContext ctx)
        => JsonSerializer.Serialize(ctx, JsonOptions);
}
