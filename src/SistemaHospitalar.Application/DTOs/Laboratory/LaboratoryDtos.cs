using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Laboratory;

public record LabExamCatalogDto(
    Guid Id, string Name, string? TussCode, string? SampleType, string? ReferenceRange, string? Unit,
    string? Category, bool IsGeneral);

public record LabOrderItemDto(
    Guid Id, Guid ExamCatalogId, string ExamName, LabItemStatus Status,
    LabResultDto? Result);

public record LabResultDto(
    Guid Id, string Value, string? Unit, string? ReferenceRange, bool IsAbnormal, DateTime? ReleasedAt);

public record LabOrderDto(
    Guid Id, Guid PatientId, string PatientName, string RequestingProfessionalName,
    LabOrderStatus Status, DateTime CreatedAt, IReadOnlyList<LabOrderItemDto> Items);

public record CreateLabOrderRequest(
    Guid PatientId, Guid RequestingProfessionalId, IReadOnlyList<Guid> ExamCatalogIds, string? Notes);

public record RegisterLabResultRequest(
    Guid OrderItemId, string Value, string? Unit, string? ReferenceRange, bool IsAbnormal, string? Notes);
