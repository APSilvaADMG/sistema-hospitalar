using SistemaHospitalar.Application.DTOs.Dialysis;

namespace SistemaHospitalar.Application.Interfaces;

public interface IDialysisService
{
    Task<IReadOnlyList<DialysisSessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default);
    Task<DialysisSessionDto> CreateSessionAsync(CreateDialysisSessionRequest request, CancellationToken cancellationToken = default);
    Task<DialysisSessionDto?> UpdateSessionStatusAsync(Guid id, UpdateDialysisSessionStatusRequest request, CancellationToken cancellationToken = default);
}
