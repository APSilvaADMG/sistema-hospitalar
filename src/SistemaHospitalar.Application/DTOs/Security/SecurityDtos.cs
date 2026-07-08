using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Security;

public record SecuritySettingsDto(bool VisitorPhotoRequired);

public record SecurityDashboardDto(
    int VisitorsInside,
    int OpenIncidents,
    IReadOnlyList<VisitorLogDto> RecentVisitors,
    IReadOnlyList<SecurityIncidentDto> OpenIncidentsList);

public record SecurityIncidentDto(
    Guid Id,
    SecurityIncidentType Type,
    SecurityIncidentStatus Status,
    string Location,
    string Description,
    string? ReportedBy,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    Guid? PatientId = null,
    string? PatientName = null,
    ClinicalIncidentSeverity? Severity = null);

public record CreateSecurityIncidentRequest(
    SecurityIncidentType Type,
    string Location,
    string Description,
    string? ReportedBy,
    Guid? PatientId = null,
    ClinicalIncidentSeverity? Severity = null);

public record ResolveIncidentRequest(string? ResolutionNotes);

public record VisitorLogDto(
    Guid Id,
    string VisitorName,
    string? DocumentNumber,
    string? PatientName,
    string? Destination,
    string? BadgeNumber,
    VisitorLogStatus Status,
    DateTime EnteredAt,
    DateTime? ExitedAt,
    string? PhotoData,
    bool HasPhoto);

public record RegisterVisitorRequest(
    string VisitorName,
    string? DocumentNumber,
    Guid? PatientId,
    string? Destination,
    string? BadgeNumber,
    string? PhotoData);
