using SistemaHospitalar.Application.DTOs.Reports;

namespace SistemaHospitalar.Application.Interfaces;

public interface IReportsService
{
    Task<ReportCatalogSummaryDto> GetCatalogSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportCatalogItemDto>> GetCatalogAsync(
        string? module = null,
        bool? essentialOnly = null,
        bool? implementedOnly = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<ReportResultDto> ExecuteAsync(
        string code,
        ReportFilterDto filter,
        CancellationToken cancellationToken = default);
}
