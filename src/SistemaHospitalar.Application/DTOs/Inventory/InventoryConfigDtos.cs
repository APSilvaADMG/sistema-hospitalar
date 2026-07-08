using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Inventory;

public record InventoryLookupItemDto(
    Guid Id,
    InventoryLookupType Type,
    string Name);

public record CreateInventoryLookupItemRequest(string Name);

public record UpdateInventoryLookupItemRequest(string Name);

public record MedicationInsuranceMappingDto(
    Guid Id,
    Guid PrescribedProductId,
    string PrescribedProductName,
    Guid ReferenceProductId,
    string ReferenceProductName,
    Guid HealthInsuranceId,
    string HealthInsuranceName);

public record CreateMedicationInsuranceMappingRequest(
    Guid PrescribedProductId,
    Guid ReferenceProductId,
    Guid HealthInsuranceId);

public record UpdateMedicationInsuranceMappingRequest(
    Guid PrescribedProductId,
    Guid ReferenceProductId,
    Guid HealthInsuranceId);
