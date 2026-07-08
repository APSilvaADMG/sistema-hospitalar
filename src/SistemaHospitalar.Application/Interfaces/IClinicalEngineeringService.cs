using SistemaHospitalar.Application.DTOs.ClinicalEngineering;

namespace SistemaHospitalar.Application.Interfaces;

public interface IClinicalEngineeringService
{
    Task<IReadOnlyList<MedicalEquipmentDto>> GetEquipmentAsync(CancellationToken cancellationToken = default);
    Task<MedicalEquipmentDto> CreateEquipmentAsync(CreateMedicalEquipmentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceWorkOrderDto>> GetWorkOrdersAsync(CancellationToken cancellationToken = default);
    Task<MaintenanceWorkOrderDto> CreateWorkOrderAsync(CreateWorkOrderRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceWorkOrderDto?> UpdateWorkOrderStatusAsync(Guid id, UpdateWorkOrderStatusRequest request, CancellationToken cancellationToken = default);
}
