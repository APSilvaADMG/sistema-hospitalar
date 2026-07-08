using SistemaHospitalar.Application.DTOs.Research;

namespace SistemaHospitalar.Application.Interfaces;

public interface IMimicResearchService
{
    Task<MimicResearchStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<MimicEtlStatusDto> GetEtlStatusAsync(CancellationToken cancellationToken = default);

    Task<MimicVitalsQueryResultDto> GetVitalsAsync(
        int subjectId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<MimicEtlTriggerResultDto> TriggerSubsetImportAsync(
        int? maxSubjects = null,
        CancellationToken cancellationToken = default);
}
