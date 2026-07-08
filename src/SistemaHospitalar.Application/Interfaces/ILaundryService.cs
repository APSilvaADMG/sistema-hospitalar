using SistemaHospitalar.Application.DTOs.Laundry;

namespace SistemaHospitalar.Application.Interfaces;

public interface ILaundryService
{
    Task<IReadOnlyList<LaundryBatchDto>> GetBatchesAsync(CancellationToken cancellationToken = default);
    Task<LaundryBatchDto> CreateBatchAsync(CreateLaundryBatchRequest request, CancellationToken cancellationToken = default);
    Task<LaundryBatchDto?> UpdateBatchStatusAsync(Guid id, UpdateLaundryBatchStatusRequest request, CancellationToken cancellationToken = default);
}
