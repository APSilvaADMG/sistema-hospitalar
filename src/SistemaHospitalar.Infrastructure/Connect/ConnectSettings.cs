namespace SistemaHospitalar.Infrastructure.Connect;

public static class ConnectMarkers
{
    public const string AppointmentSourceNote = "Via WhatsApp Connect";
}

public class ConnectSettings
{
    public const string SectionName = "Connect";
    public string HospitalName { get; set; } = "Hospital Management System";
    public string DefaultUnit { get; set; } = "Unidade Principal";
    public WhatsAppSettings WhatsApp { get; set; } = new();
    public ReminderSettings Reminders { get; set; } = new();
    public CollectionSettings Collection { get; set; } = new();
    public int SlaMonitorIntervalMinutes { get; set; } = 15;
    public int CalendarReminderCheckMinutes { get; set; } = 5;
    public long MaxAttachmentBytes { get; set; } = 10 * 1024 * 1024;
}

public class WhatsAppSettings
{
    public bool Enabled { get; set; } = true;
    public bool UseMockProvider { get; set; } = true;
    public string VerifyToken { get; set; } = "connect-demo-token";
    public string AppSecret { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string TemplateLanguageCode { get; set; } = "pt_BR";
    public string PublicWebhookUrl { get; set; } = string.Empty;
    public string ConfirmationTemplateName { get; set; } = "appointment_confirmation";
    public string ReminderTemplateName { get; set; } = "appointment_reminder";
    public string BillingTemplateName { get; set; } = "billing_reminder";
    public string CheckInTemplateName { get; set; } = "checkin_invite";
    public string WaitlistTemplateName { get; set; } = "waitlist_offer";
    public string SatisfactionTemplateName { get; set; } = "satisfaction_survey";
    public string UtilityTemplateName { get; set; } = "utility_message";
}

public class ReminderSettings
{
    public int ConfirmationHoursBefore { get; set; } = 72;
    public int ReminderHoursBefore { get; set; } = 24;
}

public class CollectionSettings
{
    public bool Enabled { get; set; } = true;
    public int MinDaysBetweenReminders { get; set; } = 7;
    public bool PixEnabled { get; set; } = true;
    public bool UseMockPixProvider { get; set; } = true;
    public bool PixAutoConfirmEnabled { get; set; } = true;
    public int PixChargeExpirationHours { get; set; } = 24;
    public string PixKey { get; set; } = "financeiro@hospital.local";
    public string PixBeneficiary { get; set; } = "Hospital Management System";
    public string PixCity { get; set; } = "SAO PAULO";
    public string PixWebhookSecret { get; set; } = "pix-demo-secret";
    public string PaymentInstructions { get; set; } =
        "Use a opção Gerar PIX no WhatsApp Connect — a baixa é automática após o pagamento.";
}
