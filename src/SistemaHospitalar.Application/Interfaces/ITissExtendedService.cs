using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface ITissExtendedService
{
    Task<PagedResult<TussCatalogDto>> GetTussCatalogAsync(
        string? search,
        TussTableType? tableType,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
    Task<int> ImportTussCatalogAsync(ImportTussRequest request, CancellationToken cancellationToken = default);

    Task<ImportTussResultDto> ImportTussCsvAsync(ImportTussCsvRequest request, CancellationToken cancellationToken = default);

    Task<ImportTussResultDto> ImportTussXlsxFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    Task<ImportTussResultDto> ImportBundledTuss202601Async(CancellationToken cancellationToken = default);

    Task<ImportTussResultDto> SeedExpandedTussCatalogAsync(CancellationToken cancellationToken = default);
    Task<ImportSigtapResultDto> ImportSigtapZipAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<SyncSigtapOfficialResultDto> SyncSigtapOfficialAsync(CancellationToken cancellationToken = default);
    Task<SigtapCatalogSummaryDto> GetSigtapSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<SigtapProcedureDto>> GetSigtapProceduresAsync(
        string? search,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TissDemonstrativoDto>> GetDemonstrativosAsync(CancellationToken cancellationToken = default);
    Task<TissDemonstrativoDetailDto?> GetDemonstrativoByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TissDemonstrativoDetailDto> ImportDemonstrativoCsvAsync(ImportDemonstrativoRequest request, CancellationToken cancellationToken = default);
    Task<TissDemonstrativoDetailDto> ProcessDemonstrativoAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TissDemonstrativoDetailDto> FetchDemonstrativoFromOperatorAsync(FetchOperatorDemonstrativoRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TissGuideAnnexDto>> GetGuideAnnexesAsync(Guid guideId, CancellationToken cancellationToken = default);
    Task<TissGuideAnnexDto> CreateGuideAnnexAsync(CreateTissGuideAnnexRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HealthInsuranceIntegrationDto>> GetInsuranceIntegrationsAsync(CancellationToken cancellationToken = default);
    Task<HealthInsuranceIntegrationDto?> UpdateInsuranceIntegrationAsync(Guid id, UpdateHealthInsuranceIntegrationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperatorTransactionLogDto>> GetOperatorTransactionLogsAsync(int limit, CancellationToken cancellationToken = default);
    Task<TissReconciliationSummaryDto> GetReconciliationSummaryAsync(CancellationToken cancellationToken = default);
}
