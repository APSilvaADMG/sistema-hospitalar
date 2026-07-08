using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.ClinicalEngineering;

public record MedicalEquipmentDto(
    Guid Id,
    string Name,
    string AssetTag,
    string? Manufacturer,
    string? Model,
    string? Location,
    MedicalEquipmentStatus Status,
    DateOnly? LastMaintenanceDate,
    DateOnly? NextMaintenanceDate);

public record MaintenanceWorkOrderDto(
    Guid Id,
    Guid EquipmentId,
    string EquipmentName,
    string Title,
    string Description,
    MaintenanceWorkOrderStatus Status,
    string? TechnicianName,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public record CreateMedicalEquipmentRequest(
    string Name,
    string AssetTag,
    string? Manufacturer,
    string? Model,
    string? Location,
    DateOnly? NextMaintenanceDate);

public record CreateWorkOrderRequest(Guid EquipmentId, string Title, string Description, string? TechnicianName);

public record UpdateWorkOrderStatusRequest(MaintenanceWorkOrderStatus Status);
