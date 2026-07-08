using SistemaHospitalar.Application.DTOs.ClinicalOperations;

namespace SistemaHospitalar.Application.Interfaces;

public interface IFinancialCashSessionService
{
    Task<FinancialCashSessionDto?> GetOpenSessionAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialCashSessionDto>> ListSessionsAsync(
        int limit = 30,
        CancellationToken cancellationToken = default);

    Task<FinancialCashSessionDto> OpenSessionAsync(
        OpenFinancialCashSessionRequest request,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    Task<FinancialCashSessionDto> CloseSessionAsync(
        Guid sessionId,
        CloseFinancialCashSessionRequest request,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}
