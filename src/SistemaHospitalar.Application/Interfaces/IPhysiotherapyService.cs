using SistemaHospitalar.Application.DTOs.Physiotherapy;

namespace SistemaHospitalar.Application.Interfaces;

public interface IPhysiotherapyService
{
    Task<IReadOnlyList<PhysiotherapySessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default);
    Task<PhysiotherapySessionDto> CreateSessionAsync(CreatePhysiotherapySessionRequest request, CancellationToken cancellationToken = default);
    Task<PhysiotherapySessionDto?> UpdateSessionStatusAsync(Guid id, UpdatePhysiotherapyStatusRequest request, CancellationToken cancellationToken = default);
}
