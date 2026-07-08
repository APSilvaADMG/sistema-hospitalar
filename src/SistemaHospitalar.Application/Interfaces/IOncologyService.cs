using SistemaHospitalar.Application.DTOs.Oncology;

namespace SistemaHospitalar.Application.Interfaces;

public interface IOncologyService
{
    Task<IReadOnlyList<ChemotherapySessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default);
    Task<ChemotherapySessionDto> CreateSessionAsync(CreateChemotherapySessionRequest request, CancellationToken cancellationToken = default);
    Task<ChemotherapySessionDto?> UpdateSessionStatusAsync(Guid id, UpdateChemotherapyStatusRequest request, CancellationToken cancellationToken = default);
}
