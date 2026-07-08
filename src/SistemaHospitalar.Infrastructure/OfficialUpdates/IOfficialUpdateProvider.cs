using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.OfficialUpdates;

public interface IOfficialUpdateProvider
{
    OfficialSourceType SourceType { get; }

    Task<OfficialCheckResult> CheckAsync(CancellationToken cancellationToken);

    Task<OfficialImportResult> ImportAsync(CancellationToken cancellationToken);
}

public record OfficialCheckResult(
    string CurrentVersion,
    string? RemoteVersion,
    string? InstalledFileHash,
    string? RemoteFileHash,
    OfficialVersionStatus Status,
    int? RecordCount,
    string? Notes,
    bool CanAutoImport);

public record OfficialImportResult(
    bool Success,
    string Message,
    int? ImportedCount,
    string? NewVersion,
    string? FileHash);
