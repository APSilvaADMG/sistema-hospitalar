using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Integrations;

public record IntegrationMessageDto(
    Guid Id,
    IntegrationMessageType Type,
    IntegrationMessageStatus Status,
    string Source,
    string? Destination,
    string PayloadPreview,
    string? ErrorMessage,
    string? PatientName,
    DateTime CreatedAt);

public record Hl7InboundRequest(string RawMessage, string Source);

public record FhirPatientExportDto(string ResourceType, string Id, string Json);

public record FhirImportRequest(string Json);

public record IntegrationProcessResultDto(
    Guid MessageId,
    IntegrationMessageStatus Status,
    string? Summary,
    Guid? PatientId);
