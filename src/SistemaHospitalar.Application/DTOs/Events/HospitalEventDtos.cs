using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Events;

public record HospitalEventLogDto(
    Guid Id,
    string EventType,
    string RoutingKey,
    HospitalEventLogStatus Status,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    string? ErrorMessage);

public record PublishHospitalEventRequest(
    string EventType,
    object Payload,
    Guid? RelatedEntityId = null,
    string? RelatedEntityType = null);
