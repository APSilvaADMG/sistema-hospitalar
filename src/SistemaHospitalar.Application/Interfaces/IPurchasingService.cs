using SistemaHospitalar.Application.DTOs.Purchasing;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IPurchasingService
{
    Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrderDto>> GetOrdersAsync(PurchaseOrderStatus? status, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> CreateOrderAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> SendOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> ReceiveOrderAsync(Guid id, ReceivePurchaseOrderRequest request, CancellationToken cancellationToken = default);

    Task<PurchaseCreateSuggestionsDto> GetCreateSuggestionsAsync(
        PurchaseSector? sector,
        PurchasePriority? priority,
        CancellationToken cancellationToken = default);
}
