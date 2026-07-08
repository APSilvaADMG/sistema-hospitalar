using SistemaHospitalar.Application.DTOs.Integrations;

namespace SistemaHospitalar.Application.Interfaces;

public interface IIntegrationService
{
    Task<IReadOnlyList<IntegrationMessageDto>> GetMessagesAsync(int limit, CancellationToken cancellationToken = default);
    Task<IntegrationProcessResultDto> ProcessHl7InboundAsync(Hl7InboundRequest request, CancellationToken cancellationToken = default);
    Task<FhirPatientExportDto?> ExportFhirPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IntegrationProcessResultDto> ImportFhirPatientAsync(string fhirJson, CancellationToken cancellationToken = default);
}
