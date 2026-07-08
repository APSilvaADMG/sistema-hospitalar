using SistemaHospitalar.Application.DTOs.Financial;

namespace SistemaHospitalar.Application.Interfaces;

public interface IPixPaymentService
{
    Task<PixChargeDto> CreateChargeForAccountAsync(Guid financialAccountId, CancellationToken cancellationToken = default);
    Task<PixChargeDto?> GetChargeAsync(Guid chargeId, CancellationToken cancellationToken = default);
    Task<PixChargeDto?> GetActiveChargeForAccountAsync(Guid financialAccountId, CancellationToken cancellationToken = default);
    Task<PixWebhookResult> ProcessWebhookAsync(PixWebhookRequest request, CancellationToken cancellationToken = default);
    Task<PixChargeDto?> SimulatePaymentAsync(Guid chargeId, CancellationToken cancellationToken = default);
}
