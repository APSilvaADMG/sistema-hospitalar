using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface ITissClinicalSourceService
{
    Task<IReadOnlyList<TissClinicalSourceDto>> GetSourcesAsync(
        Guid? patientId,
        ClinicalDocumentKind? documentKind,
        TissGuideType? guideType,
        string? reportCode,
        bool pendingOnly,
        CancellationToken cancellationToken = default);

    Task<TissClinicalSourceDto?> GetSourceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TissClinicalSourceDto?> FindSourceAsync(
        ClinicalSourceLookupRequest request,
        CancellationToken cancellationToken = default);

    Task<TissClinicalSourceDto> UpsertSourceAsync(
        UpsertTissClinicalSourceRequest request,
        CancellationToken cancellationToken = default);

    Task<TissClinicalSourceDto?> LinkGeneratedGuideAsync(
        Guid sourceId,
        LinkClinicalSourceGuideRequest request,
        CancellationToken cancellationToken = default);

    Task<TissClinicalSourceDto?> LinkGeneratedArtifactAsync(
        Guid sourceId,
        LinkClinicalSourceArtifactRequest request,
        CancellationToken cancellationToken = default);
}
