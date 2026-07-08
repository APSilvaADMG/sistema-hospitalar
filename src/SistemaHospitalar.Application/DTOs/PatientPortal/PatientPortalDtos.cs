namespace SistemaHospitalar.Application.DTOs.PatientPortal;

public record PatientPortalDashboardDto(
    string PatientName,
    string? RecordNumber,
    IReadOnlyList<PatientAppointmentDto> UpcomingAppointments,
    IReadOnlyList<PatientLabResultDto> RecentLabResults);

public record PatientAppointmentDto(
    Guid Id,
    DateTime ScheduledAt,
    string ProfessionalName,
    string SpecialtyName,
    int Status);

public record PatientLabResultDto(
    string ExamName,
    string Value,
    string? ReferenceRange,
    bool IsAbnormal,
    DateTime? ReleasedAt);

public record PatientMedicalRecordDto(
    string RecordNumber,
    IReadOnlyList<PatientRecordEntryDto> Entries);

public record PatientRecordEntryDto(
    string EntryType,
    string Content,
    string? ProfessionalName,
    DateTime CreatedAt);
