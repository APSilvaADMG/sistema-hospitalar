namespace SistemaHospitalar.Application.DTOs.Patients;

public record PatientTimelineEventDto(
    string Type,
    string Title,
    string Description,
    DateTime At,
    string? ProfessionalName,
    string? Link);

public record PatientTimelineDto(
    Guid PatientId,
    string PatientName,
    IReadOnlyList<PatientTimelineEventDto> Events);
