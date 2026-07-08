using System.Globalization;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Connect;

public record ConnectTemplateSendPlan(
    string TemplateName,
    IReadOnlyList<string> BodyParameters,
    string DisplayBody);

public class ConnectTemplateBuilder(IOptions<ConnectSettings> settings)
{
    private readonly WhatsAppSettings _wa = settings.Value.WhatsApp;
    private readonly ConnectSettings _settings = settings.Value;

    public string LanguageCode => string.IsNullOrWhiteSpace(_wa.TemplateLanguageCode)
        ? "pt_BR"
        : _wa.TemplateLanguageCode;

    public bool TryBuildForReminder(
        ConnectReminderType reminderType,
        ConnectConversation conversation,
        Appointment? appointment,
        FinancialAccount? account,
        string fallbackBody,
        out ConnectTemplateSendPlan plan)
    {
        plan = null!;
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var patientName = conversation.ContactName ?? conversation.Patient?.FullName ?? "Paciente";
        var hospital = _settings.HospitalName;
        var unit = _settings.DefaultUnit;

        switch (reminderType)
        {
            case ConnectReminderType.AppointmentConfirmation when appointment is not null:
            {
                var when = appointment.ScheduledAt.ToLocalTime();
                plan = new ConnectTemplateSendPlan(
                    _wa.ConfirmationTemplateName,
                    [
                        patientName,
                        when.ToString("dd/MM/yyyy"),
                        when.ToString("HH:mm"),
                        appointment.Professional.FullName,
                        unit,
                    ],
                    fallbackBody);
                return true;
            }
            case ConnectReminderType.Confirmation72h or ConnectReminderType.Reminder24h when appointment is not null:
            {
                var when = appointment.ScheduledAt.ToLocalTime();
                plan = new ConnectTemplateSendPlan(
                    _wa.ReminderTemplateName,
                    [
                        patientName,
                        when.ToString("dd/MM/yyyy"),
                        when.ToString("HH:mm"),
                        appointment.Professional.FullName,
                        unit,
                    ],
                    fallbackBody);
                return true;
            }
            case ConnectReminderType.CheckInInvite when appointment is not null:
            {
                var when = appointment.ScheduledAt.ToLocalTime();
                plan = new ConnectTemplateSendPlan(
                    _wa.CheckInTemplateName,
                    [patientName, when.ToString("dd/MM/yyyy"), when.ToString("HH:mm"), unit],
                    fallbackBody);
                return true;
            }
            case ConnectReminderType.WaitlistOffer when appointment is not null:
            {
                var when = appointment.ScheduledAt.ToLocalTime();
                plan = new ConnectTemplateSendPlan(
                    _wa.WaitlistTemplateName,
                    [
                        patientName,
                        when.ToString("dd/MM/yyyy"),
                        when.ToString("HH:mm"),
                        appointment.Professional.Specialty.Name,
                    ],
                    fallbackBody);
                return true;
            }
            case ConnectReminderType.SatisfactionSurvey:
                plan = new ConnectTemplateSendPlan(
                    _wa.SatisfactionTemplateName,
                    [patientName, hospital],
                    fallbackBody);
                return true;
            case ConnectReminderType.BillingReminder or ConnectReminderType.BillingOverdue when account is not null:
            {
                var balance = account.Amount - account.PaidAmount;
                plan = new ConnectTemplateSendPlan(
                    _wa.BillingTemplateName,
                    [
                        patientName,
                        account.Description,
                        balance.ToString("C", culture),
                        account.DueDate?.ToLocalTime().ToString("dd/MM/yyyy") ?? "—",
                    ],
                    fallbackBody);
                return true;
            }
            case ConnectReminderType.RescheduleOffer when appointment is not null:
            {
                var when = appointment.ScheduledAt.ToLocalTime();
                plan = new ConnectTemplateSendPlan(
                    _wa.ReminderTemplateName,
                    [
                        patientName,
                        when.ToString("dd/MM/yyyy"),
                        when.ToString("HH:mm"),
                        appointment.Professional.FullName,
                        hospital,
                    ],
                    fallbackBody);
                return true;
            }
            default:
                return TryBuildUtility(fallbackBody, out plan);
        }
    }

    public bool TryBuildUtility(string body, out ConnectTemplateSendPlan plan)
    {
        if (string.IsNullOrWhiteSpace(_wa.UtilityTemplateName))
        {
            plan = null!;
            return false;
        }

        var trimmed = body.Length > 900 ? body[..900] + "…" : body;
        plan = new ConnectTemplateSendPlan(_wa.UtilityTemplateName, [trimmed], body);
        return true;
    }
}
