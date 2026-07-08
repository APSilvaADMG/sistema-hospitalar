namespace SistemaHospitalar.Application.Interfaces;

public interface ISigtapOfficialSyncService
{
    Task<SigtapOfficialReleaseDto> DiscoverLatestAsync(CancellationToken cancellationToken = default);

    Task<SigtapOfficialDownloadDto> DownloadOfficialZipAsync(
        SigtapOfficialReleaseDto release,
        CancellationToken cancellationToken = default);
}

public sealed record SigtapOfficialReleaseDto(
    string Competence,
    string DownloadUrl,
    string Title,
    DateTime? PublishedAt);

public sealed record SigtapOfficialDownloadDto(
    byte[] Data,
    string FileName,
    string SourceUrl,
    string Sha256,
    long SizeBytes);
