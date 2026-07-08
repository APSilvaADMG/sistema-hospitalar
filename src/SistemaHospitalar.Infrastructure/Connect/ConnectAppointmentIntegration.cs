using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectAppointmentIntegration(
    AppDbContext dbContext,
    ConnectMessagingService messaging,
    IOptions<ConnectSettings> settings) : IConnectAppointmentIntegration
{
    private readonly ConnectSettings _settings = settings.Value;

    public async Task OnAppointmentCreatedAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appt = await GetAppointmentAsync(appointmentId, cancellationToken);
        if (appt is null)
        {
            return;
        }

        if (!(appt.Notes?.Contains(ConnectMarkers.AppointmentSourceNote, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            await SendConfirmationAsync(appt, cancellationToken);
        }

        await ScheduleRemindersAsync(appt, cancellationToken);
        await ScheduleCheckInAsync(appt, cancellationToken);
    }

    public async Task OnAppointmentStatusChangedAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appt = await GetAppointmentAsync(appointmentId, cancellationToken);
        if (appt is null)
        {
            return;
        }

        if (appt.Status == AppointmentStatus.Completed)
        {
            await ScheduleSatisfactionAsync(appt, cancellationToken);
        }
    }

    public async Task OnAppointmentCancelledAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appt = await GetAppointmentAsync(appointmentId, cancellationToken);
        if (appt is null)
        {
            return;
        }

        await OfferWaitlistAsync(appt, cancellationToken);
    }

    private async Task<Appointment?> GetAppointmentAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Professional).ThenInclude(p => p.Specialty)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    private async Task SendConfirmationAsync(Appointment appt, CancellationToken cancellationToken)
    {
        var phone = appt.Patient.MobilePhone ?? appt.Patient.Phone;
        if (string.IsNullOrWhiteSpace(phone))
        {
            return;
        }

        var conversation = await messaging.GetOrCreateConversationAsync(phone, appt.Patient.FullName, cancellationToken);
        var protocol = ConnectSchedulingHelper.GenerateProtocol(appt.Id);
        var body = $"""
            ✅ Consulta agendada — {_settings.HospitalName}

            Protocolo: {protocol}
            {appt.Professional.Specialty.Name} com Dr(a). {appt.Professional.FullName}
            📅 {appt.ScheduledAt.ToLocalTime():dd/MM/yyyy} às {appt.ScheduledAt.ToLocalTime():HH:mm}
            📍 {_settings.DefaultUnit}
            """;
        await messaging.SendOutboundAsync(conversation, body, ConnectReminderType.AppointmentConfirmation, appt.Id, cancellationToken: cancellationToken);
    }

    private async Task ScheduleRemindersAsync(Appointment appt, CancellationToken cancellationToken)
    {
        var confirmAt = appt.ScheduledAt.AddHours(-_settings.Reminders.ConfirmationHoursBefore);
        var remindAt = appt.ScheduledAt.AddHours(-_settings.Reminders.ReminderHoursBefore);

        if (confirmAt > DateTime.UtcNow)
        {
            dbContext.ConnectScheduledMessages.Add(BuildScheduled(appt, ConnectReminderType.Confirmation72h, confirmAt));
        }

        if (remindAt > DateTime.UtcNow)
        {
            dbContext.ConnectScheduledMessages.Add(BuildScheduled(appt, ConnectReminderType.Reminder24h, remindAt));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ScheduleCheckInAsync(Appointment appt, CancellationToken cancellationToken)
    {
        var checkInAt = appt.ScheduledAt.AddHours(-2);
        if (checkInAt <= DateTime.UtcNow)
        {
            return;
        }

        dbContext.ConnectScheduledMessages.Add(BuildScheduled(appt, ConnectReminderType.CheckInInvite, checkInAt));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ScheduleSatisfactionAsync(Appointment appt, CancellationToken cancellationToken)
    {
        dbContext.ConnectScheduledMessages.Add(BuildScheduled(appt, ConnectReminderType.SatisfactionSurvey, DateTime.UtcNow.AddMinutes(30)));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private ConnectScheduledMessage BuildScheduled(Appointment appt, ConnectReminderType type, DateTime when)
        => new()
        {
            PatientId = appt.PatientId,
            AppointmentId = appt.Id,
            ReminderType = type,
            ScheduledFor = when,
            PayloadJson = JsonSerializer.Serialize(new { appointmentId = appt.Id, type = type.ToString() }),
        };

    private async Task OfferWaitlistAsync(Appointment appt, CancellationToken cancellationToken)
    {
        var specialtyId = appt.Professional.SpecialtyId;
        var waiters = await dbContext.ConnectWaitlistEntries
            .Include(w => w.Patient)
            .Where(w => w.IsActive && w.Status == ConnectWaitlistStatus.Waiting && w.SpecialtyId == specialtyId)
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.CreatedAt)
            .Take(3)
            .ToListAsync(cancellationToken);

        foreach (var waiter in waiters)
        {
            var phone = waiter.Patient.MobilePhone ?? waiter.Patient.Phone;
            if (string.IsNullOrWhiteSpace(phone))
            {
                continue;
            }

            waiter.Status = ConnectWaitlistStatus.Offered;
            waiter.OfferedAt = DateTime.UtcNow;
            waiter.OfferedSlotAt = appt.ScheduledAt;
            waiter.OfferedProfessionalId = appt.ProfessionalId;

            var conversation = await messaging.GetOrCreateConversationAsync(phone, waiter.Patient.FullName, cancellationToken);
            conversation.BotStep = ConnectBotStep.WaitlistOffer;
            conversation.BotContextJson = JsonSerializer.Serialize(new BotContext
            {
                SelectedSlot = appt.ScheduledAt,
                ProfessionalId = appt.ProfessionalId,
                ProfessionalName = appt.Professional.FullName,
                SpecialtyName = appt.Professional.Specialty.Name,
            });

            var body = $"""
                🔔 Surgiu uma vaga!

                {appt.Professional.Specialty.Name}
                {appt.ScheduledAt.ToLocalTime():dd/MM/yyyy} às {appt.ScheduledAt.ToLocalTime():HH:mm}
                Dr(a). {appt.Professional.FullName}

                1️⃣ Sim, quero a vaga
                2️⃣ Não, obrigado
                """;
            await messaging.SendOutboundAsync(conversation, body, ConnectReminderType.WaitlistOffer, appt.Id, cancellationToken: cancellationToken);
            break;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
