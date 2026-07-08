using SistemaHospitalar.Application.DTOs.Laboratory;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface ILabService
{
    Task<IReadOnlyList<LabExamCatalogDto>> GetExamCatalogAsync(Guid? specialtyId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabOrderDto>> GetOrdersAsync(LabOrderStatus? status, CancellationToken cancellationToken = default);
    Task<LabOrderDto> CreateOrderAsync(CreateLabOrderRequest request, CancellationToken cancellationToken = default);
    Task<LabResultDto?> RegisterResultAsync(RegisterLabResultRequest request, CancellationToken cancellationToken = default);
}
