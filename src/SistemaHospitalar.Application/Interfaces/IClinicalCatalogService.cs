using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.ClinicalCatalog;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;


public interface IClinicalCatalogService

{

    Task<SpecialtyClinicalCatalogDto> GetBySpecialtyAsync(Guid? specialtyId, CancellationToken cancellationToken = default);

    Task<SpecialtyClinicalCatalogDto> GetByProfessionalAsync(Guid professionalId, CancellationToken cancellationToken = default);

    Task<PagedResult<MedicationCatalogDto>> SearchMedicationsAsync(
        string? search,
        int page = 1,
        int pageSize = 50,
        bool referenceOnly = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedicationCatalogDto>> GetAllMedicationsAsync(string? search, CancellationToken cancellationToken = default);

    Task<MedicationCatalogDto?> GetMedicationByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cid10CatalogItemDto>> GetCid10CatalogAsync(string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cid10CatalogItemDto>> GetCid10ChildrenAsync(string? parentCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdministrationRouteDto>> GetAdministrationRoutesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientReferenceCatalogItemDto>> GetPatientReferenceCatalogAsync(
        PatientReferenceCatalogType catalogType,
        CancellationToken cancellationToken = default);

    Task<BularioSearchResultDto> SearchBularioAsync(
        string? search,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<BularioStatsDto> GetBularioStatsAsync(CancellationToken cancellationToken = default);

}

