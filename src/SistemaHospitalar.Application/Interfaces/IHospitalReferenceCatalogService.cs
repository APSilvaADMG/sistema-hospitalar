using SistemaHospitalar.Application.DTOs.HospitalCatalog;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IHospitalReferenceCatalogService
{
    IReadOnlyList<HospitalReferenceCatalogTypeInfoDto> GetCatalogTypes();

    Task<IReadOnlyList<HospitalReferenceCatalogSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HospitalReferenceCatalogItemDto>> GetByTypeAsync(
        HospitalReferenceCatalogType catalogType,
        string? parentGroup = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HospitalReferenceCatalogGroupDto>> GetGroupsAsync(
        HospitalReferenceCatalogType catalogType,
        CancellationToken cancellationToken = default);
}
