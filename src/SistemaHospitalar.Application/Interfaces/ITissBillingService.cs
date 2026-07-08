using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface ITissBillingService
{
    Task<IReadOnlyList<TissGuideDto>> GetGuidesAsync(
        TissGuideStatus? status,
        Guid? patientId,
        string? search,
        CancellationToken cancellationToken = default);

    Task<TissGuideDto?> GetGuideByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TissGuideDto> CreateGuideAsync(CreateTissGuideRequest request, CancellationToken cancellationToken = default);

    Task<TissGuideDto?> UpdateGuideAsync(
        Guid id, UpdateTissGuideRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteGuideAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TissGuideDto?> CloseGuideAccountAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TissGuideDto?> SendGuideAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TissGuideDto?> CancelGuideAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TissGuideDto?> MarkGuidePaidAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TissGlosaDto?> RegisterGlosaAsync(
        Guid guideId, RegisterGlosaRequest request, CancellationToken cancellationToken = default);

    Task<TissGlosaDto?> UpdateGlosaAsync(
        Guid glosaId, UpdateGlosaRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteGlosaAsync(Guid glosaId, CancellationToken cancellationToken = default);

    Task<TissGlosaDto?> ResolveGlosaAsync(Guid glosaId, CancellationToken cancellationToken = default);

    Task<TissGlosaDto?> ContestGlosaAsync(
        Guid glosaId,
        ContestGlosaRequest request,
        CancellationToken cancellationToken = default);
}
