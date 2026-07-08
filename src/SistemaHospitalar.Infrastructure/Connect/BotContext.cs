namespace SistemaHospitalar.Infrastructure.Connect;

public class BotContext
{
    public Guid? SpecialtyId { get; set; }
    public string? SpecialtyName { get; set; }
    public Guid? ProfessionalId { get; set; }
    public string? ProfessionalName { get; set; }
    public DateTime? SelectedSlot { get; set; }
    public Guid? PendingAppointmentId { get; set; }
    public List<Guid> OfferedSlotKeys { get; set; } = [];
    public List<AvailableSlotSnapshot> OfferedSlots { get; set; } = [];
    public List<Guid> PatientAppointmentIds { get; set; } = [];
    public string? TriageSymptoms { get; set; }
    public int? TriageDurationDays { get; set; }
    public int? TriageIntensity { get; set; }
    public string? PostCpfTarget { get; set; }
    public List<Guid> OfferedFinancialAccountIds { get; set; } = [];
    public Guid? PendingFinancialAccountId { get; set; }
}

public class AvailableSlotSnapshot
{
    public DateTime ScheduledAt { get; set; }
    public Guid ProfessionalId { get; set; }
    public string ProfessionalName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
}
