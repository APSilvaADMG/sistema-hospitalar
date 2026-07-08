namespace SistemaHospitalar.Application.DTOs.OfficialUpdates;

public record OfficialSourceStatusDto(
    string SourceType,
    string DisplayName,
    string CurrentVersion,
    string? AvailableVersion,
    string Status,
    string? SourceUrl,
    string? Notes,
    DateTime? LastCheckedAt,
    DateTime? LastImportedAt,
    int? InstalledRecordCount,
    bool CanAutoImport);

public record OfficialUpdatesDashboardDto(
    DateTime? LastCheckAt,
    IReadOnlyList<OfficialSourceStatusDto> Sources);

public record IntegrationLogDto(
    Guid Id,
    string SourceType,
    string Action,
    string Status,
    string Message,
    string? TriggeredBy,
    long? DurationMs,
    DateTime CreatedAt);

public record OfficialUpdateActionResultDto(
    string SourceType,
    string Status,
    string Message,
    int? ImportedCount);
