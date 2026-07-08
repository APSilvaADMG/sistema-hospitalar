using SistemaHospitalar.Application.DTOs.Integrations;

namespace SistemaHospitalar.Application.Interfaces;

public interface IIntegrationReadinessService
{
    Task<IntegrationReadinessDto> GetReadinessAsync(CancellationToken cancellationToken = default);
    Task<IntegrationTestResultDto> TestWhatsAppAsync(CancellationToken cancellationToken = default);
    Task<IntegrationTestResultDto> TestPixAsync(CancellationToken cancellationToken = default);
    Task<IntegrationTestResultDto> TestTissAsync(Guid? operatorId = null, CancellationToken cancellationToken = default);
}
