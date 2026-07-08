using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.InfectionControl;

public record InfectionSurveillanceDto(
    Guid Id,
    Guid? PatientId,
    string? PatientName,
    string? WardName,
    string Location,
    InfectionType InfectionType,
    string Organism,
    string? Site,
    InfectionSurveillanceStatus Status,
    DateTime DetectedAt,
    string? ReportedBy,
    string? Notes,
    DateTime? ResolvedAt);

public record IsolationPrecautionDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string? WardName,
    IsolationPrecautionType PrecautionType,
    IsolationPrecautionStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Reason);

public record InfectionControlDashboardDto(
    int ActiveIsolations,
    int OpenSurveillanceCases,
    IReadOnlyList<InfectionSurveillanceDto> RecentCases,
    IReadOnlyList<IsolationPrecautionDto> ActivePrecautions);

public record CreateInfectionSurveillanceRequest(
    Guid? PatientId,
    Guid? HospitalizationId,
    string Location,
    InfectionType InfectionType,
    string Organism,
    string? Site,
    string? ReportedBy,
    string? Notes);

public record CreateIsolationPrecautionRequest(
    Guid PatientId,
    Guid? HospitalizationId,
    IsolationPrecautionType PrecautionType,
    DateOnly StartDate,
    string Reason);

public record ResolveInfectionRequest(string? Notes);
