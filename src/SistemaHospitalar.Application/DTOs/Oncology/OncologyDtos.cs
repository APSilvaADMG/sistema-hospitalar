using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Oncology;

public record ChemotherapySessionDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string? WardName,
    string ProfessionalName,
    string ProtocolName,
    string DrugRegimen,
    int CycleNumber,
    int TotalCycles,
    ChemotherapySessionStatus Status,
    DateTime ScheduledAt,
    DateTime? AdministeredAt,
    string? Notes);

public record CreateChemotherapySessionRequest(
    Guid PatientId,
    Guid ProfessionalId,
    Guid? HospitalizationId,
    string ProtocolName,
    string DrugRegimen,
    int CycleNumber,
    int TotalCycles,
    DateTime ScheduledAt,
    string? Notes);

public record UpdateChemotherapyStatusRequest(ChemotherapySessionStatus Status);
