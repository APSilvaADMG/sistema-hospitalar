using SistemaHospitalar.Application.DTOs.ClinicalOperations;

namespace SistemaHospitalar.Application.Interfaces;

public interface IWardPharmacyService
{
    Task<IReadOnlyList<WardStockBalanceDto>> ListBalancesAsync(
        Guid? wardId = null,
        bool lowStockOnly = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WardStockMovementDto>> ListMovementsAsync(
        Guid? wardId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    Task<WardStockBalanceDto> TransferFromCentralAsync(
        WardStockTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<WardStockMovementDto> DispenseAsync(
        WardStockDispenseRequest request,
        CancellationToken cancellationToken = default);
}
