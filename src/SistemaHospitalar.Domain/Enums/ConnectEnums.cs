namespace SistemaHospitalar.Domain.Enums;

public enum ConnectChannel
{
    WhatsApp,
    Sms,
    Email,
    Telegram,
    Push
}

public enum ConnectMessageDirection
{
    Inbound,
    Outbound
}

public enum ConnectMessageStatus
{
    Pending,
    Sent,
    Delivered,
    Read,
    Failed
}

public enum ConnectBotStep
{
    MainMenu,
    AwaitingCpf,
    ScheduleSpecialty,
    ScheduleProfessional,
    ScheduleDate,
    ScheduleSlot,
    ScheduleConfirm,
    RescheduleSelect,
    CancelSelect,
    ConfirmReminder,
    WaitlistOffer,
    CheckIn,
    PreTriage,
    Satisfaction,
    AwaitingHuman,
    BillingSelectAccount,
    BillingPaymentInfo
}

public enum ConnectReminderType
{
    AppointmentConfirmation,
    Confirmation72h,
    Reminder24h,
    RescheduleOffer,
    WaitlistOffer,
    CheckInInvite,
    ExamResultAvailable,
    SatisfactionSurvey,
    ReturnRecovery,
    BillingReminder,
    BillingOverdue
}

public enum ConnectWaitlistStatus
{
    Waiting,
    Offered,
    Accepted,
    Declined,
    Expired
}

public enum ConnectInboxQueue
{
    None = 0,
    Reception = 1,
    Billing = 2,
}
