using SistemaHospitalar.Application.DTOs.Events;

namespace SistemaHospitalar.Application.Interfaces;

public interface IHospitalEventEngine
{
    Task<HospitalEventLogDto> PublishAndProcessAsync(
        string eventType,
        object payload,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HospitalEventLogDto>> GetRecentAsync(
        int limit = 20,
        CancellationToken cancellationToken = default);
}
