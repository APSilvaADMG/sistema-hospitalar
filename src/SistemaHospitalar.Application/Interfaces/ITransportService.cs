using SistemaHospitalar.Application.DTOs.Transport;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface ITransportService
{
    Task<TransportDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<TransportMetricsDto> GetMetricsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransportAssetDto>> GetAssetsAsync(CancellationToken cancellationToken = default);
    Task<TransportAssetDto> CreateAssetAsync(CreateTransportAssetRequest request, CancellationToken cancellationToken = default);
    Task<TransportAssetDto?> UpdateAssetStatusAsync(Guid id, UpdateTransportAssetStatusRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransportRequestDto>> GetRequestsAsync(TransportRequestStatus? status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransportPorterDto>> GetPortersAsync(CancellationToken cancellationToken = default);
    Task<TransportRequestDto> CreateRequestAsync(CreateTransportRequestRequest request, string? requestedBy, CancellationToken cancellationToken = default);
    Task<TransportRequestDto?> AcceptRequestAsync(Guid id, AcceptTransportRequestRequest request, CancellationToken cancellationToken = default);
    Task<TransportRequestDto?> AdvanceRequestAsync(Guid id, AdvanceTransportRequestRequest request, CancellationToken cancellationToken = default);
    Task<TransportRequestDto?> CancelRequestAsync(Guid id, CancellationToken cancellationToken = default);
}
