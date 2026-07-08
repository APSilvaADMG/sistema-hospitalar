using SistemaHospitalar.Application.DTOs.Inventory;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IInventoryConfigService
{
    Task<IReadOnlyList<InventoryLookupItemDto>> GetLookupItemsAsync(
        InventoryLookupType type,
        string? search,
        CancellationToken cancellationToken = default);

    Task<InventoryLookupItemDto> CreateLookupItemAsync(
        InventoryLookupType type,
        CreateInventoryLookupItemRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryLookupItemDto> UpdateLookupItemAsync(
        Guid id,
        UpdateInventoryLookupItemRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteLookupItemAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedicationInsuranceMappingDto>> GetMedicationMappingsAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task<MedicationInsuranceMappingDto> CreateMedicationMappingAsync(
        CreateMedicationInsuranceMappingRequest request,
        CancellationToken cancellationToken = default);

    Task<MedicationInsuranceMappingDto> UpdateMedicationMappingAsync(
        Guid id,
        UpdateMedicationInsuranceMappingRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteMedicationMappingAsync(Guid id, CancellationToken cancellationToken = default);
}
