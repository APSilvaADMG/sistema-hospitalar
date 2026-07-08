using SistemaHospitalar.Application.DTOs.Inventory;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IStockRequisitionService
{
    Task<IReadOnlyList<StockRequisitionDto>> GetRequisitionsAsync(
        StockRequisitionStatus? status,
        StockRequisitionPriority? priority,
        DateOnly? dueDateBefore,
        CancellationToken cancellationToken = default);

    Task<StockRequisitionDetailDto?> GetRequisitionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StockRequisitionDetailDto> CreateRequisitionAsync(
        CreateStockRequisitionRequest request,
        CancellationToken cancellationToken = default);

    Task<StockRequisitionDetailDto> UpdateRequisitionAsync(
        Guid id,
        UpdateStockRequisitionRequest request,
        CancellationToken cancellationToken = default);

    Task<StockRequisitionDetailDto> ApproveRequisitionAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StockRequisitionDetailDto> FulfillRequisitionAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task CancelRequisitionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<StockRequisitionDetailDto> DenyRequisitionAsync(
        Guid id,
        DenyStockRequisitionRequest request,
        CancellationToken cancellationToken = default);
}
