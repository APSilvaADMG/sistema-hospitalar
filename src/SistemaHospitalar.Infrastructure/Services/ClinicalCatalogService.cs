using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.ClinicalCatalog;

using SistemaHospitalar.Application.DTOs.Laboratory;

using SistemaHospitalar.Application.Interfaces;

using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Bulario;
using SistemaHospitalar.Infrastructure.Persistence;



namespace SistemaHospitalar.Infrastructure.Services;



public class ClinicalCatalogService(AppDbContext dbContext, IBularioService bularioService) : IClinicalCatalogService

{

    public async Task<SpecialtyClinicalCatalogDto> GetByProfessionalAsync(

        Guid professionalId,

        CancellationToken cancellationToken = default)

    {

        var specialtyId = await dbContext.Professionals

            .AsNoTracking()

            .Where(p => p.Id == professionalId)

            .Select(p => (Guid?)p.SpecialtyId)

            .FirstOrDefaultAsync(cancellationToken);



        return await GetBySpecialtyAsync(specialtyId, cancellationToken);

    }



    public async Task<SpecialtyClinicalCatalogDto> GetBySpecialtyAsync(

        Guid? specialtyId,

        CancellationToken cancellationToken = default)

    {

        string? specialtyName = null;

        if (specialtyId.HasValue)

        {

            specialtyName = await dbContext.Specialties

                .AsNoTracking()

                .Where(s => s.Id == specialtyId.Value)

                .Select(s => s.Name)

                .FirstOrDefaultAsync(cancellationToken);

        }



        var labExams = await GetLabExamsAsync(specialtyId, cancellationToken);

        var imaging = await GetImagingAsync(specialtyId, cancellationToken);

        var medications = await GetMedicationsAsync(specialtyId, cancellationToken);



        return new SpecialtyClinicalCatalogDto(specialtyId, specialtyName, labExams, imaging, medications);

    }



    public async Task<PagedResult<MedicationCatalogDto>> SearchMedicationsAsync(
        string? search,
        int page = 1,
        int pageSize = 50,
        bool referenceOnly = false,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = BuildMedicationSearchQuery(search, referenceOnly);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapMedication())
            .ToListAsync(cancellationToken);

        return new PagedResult<MedicationCatalogDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<IReadOnlyList<MedicationCatalogDto>> GetAllMedicationsAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        return (await SearchMedicationsAsync(search, 1, 500, referenceOnly: false, cancellationToken)).Items;
    }

    private IQueryable<Domain.Entities.MedicationCatalog> BuildMedicationSearchQuery(string? search, bool referenceOnly)
    {
        var query = dbContext.MedicationCatalogs.AsNoTracking().Where(m => m.IsActive);

        if (referenceOnly)
            query = query.Where(m => m.ExternalBulaSlug != null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                m.Name.Contains(term) ||
                (m.ActiveIngredient != null && m.ActiveIngredient.Contains(term)));
        }

        return query;
    }

    public async Task<BularioStatsDto> GetBularioStatsAsync(CancellationToken cancellationToken = default)
    {
        var catalogTotal = await dbContext.MedicationCatalogs
            .AsNoTracking()
            .CountAsync(m => m.IsActive && m.ExternalBulaSlug != null, cancellationToken);

        var withPackageInsert = await dbContext.MedicationCatalogs
            .AsNoTracking()
            .CountAsync(m => m.IsActive && m.PackageInsert != null && m.PackageInsert != "", cancellationToken);

        var anvisaAvailable = await bularioService.IsAnvisaAvailableAsync(cancellationToken);

        return new BularioStatsDto(catalogTotal, withPackageInsert, anvisaAvailable);
    }

    public async Task<BularioSearchResultDto> SearchBularioAsync(
        string? search,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var catalogTotal = await dbContext.MedicationCatalogs
            .AsNoTracking()
            .CountAsync(m => m.IsActive && m.PackageInsert != null && m.PackageInsert != "", cancellationToken);

        var query = dbContext.MedicationCatalogs
            .AsNoTracking()
            .Where(m => m.IsActive && m.PackageInsert != null && m.PackageInsert != "");

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                m.Name.Contains(term) ||
                (m.ActiveIngredient != null && m.ActiveIngredient.Contains(term)) ||
                (m.Strength != null && m.Strength.Contains(term)) ||
                (m.PackageInsert != null && m.PackageInsert.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var rows = await query
            .OrderBy(m => m.Name.StartsWith(search ?? "") ? 0 : 1)
            .ThenBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Strength,
                m.ActiveIngredient,
                m.ExternalBulaSlug,
                HasPackageInsert = m.PackageInsert != null && m.PackageInsert != "",
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(m => new BularioMedicationListItemDto(
            m.Id,
            MedicationMetadataInferrer.FormatDisplayName(
                m.Name,
                m.Strength ?? MedicationMetadataInferrer.InferStrength(m.Name)),
            m.ActiveIngredient,
            m.ExternalBulaSlug != null ? "consulta-remedios" : "hospital",
            m.HasPackageInsert)).ToList();

        object? anvisa = null;
        var anvisaAvailable = false;
        if (!string.IsNullOrWhiteSpace(search))
        {
            anvisaAvailable = await bularioService.IsAnvisaAvailableAsync(cancellationToken);
            if (anvisaAvailable)
            {
                var anvisaDoc = await bularioService.SearchAsync(search, page, cancellationToken);
                if (anvisaDoc is not null)
                    anvisa = JsonSerializer.Deserialize<object>(anvisaDoc.RootElement.GetRawText());
            }
        }

        return new BularioSearchResultDto(
            items,
            page,
            pageSize,
            totalCount,
            totalPages,
            catalogTotal,
            anvisaAvailable,
            anvisa);
    }

    public async Task<MedicationCatalogDto?> GetMedicationByIdAsync(

        Guid id,

        CancellationToken cancellationToken = default)

    {

        return await dbContext.MedicationCatalogs

            .AsNoTracking()

            .Where(m => m.Id == id && m.IsActive)

            .Select(MapMedication())

            .FirstOrDefaultAsync(cancellationToken);

    }



    private async Task<IReadOnlyList<LabExamCatalogDto>> GetLabExamsAsync(

        Guid? specialtyId,

        CancellationToken cancellationToken)

    {

        var query = dbContext.LabExamCatalogs.AsNoTracking().Where(e => e.IsActive);



        if (specialtyId.HasValue)

        {

            var sid = specialtyId.Value;

            query = query.Where(e => e.IsGeneral || e.SpecialtyLinks.Any(l => l.SpecialtyId == sid));

        }



        return await query

            .OrderBy(e => e.Category)

            .ThenBy(e => e.Name)

            .Select(e => new LabExamCatalogDto(

                e.Id, e.Name, e.TussCode, e.SampleType, e.ReferenceRange, e.Unit, e.Category, e.IsGeneral))

            .ToListAsync(cancellationToken);

    }



    private async Task<IReadOnlyList<ImagingProcedureDto>> GetImagingAsync(

        Guid? specialtyId,

        CancellationToken cancellationToken)

    {

        var query = dbContext.ImagingProcedureCatalogs.AsNoTracking().Where(p => p.IsActive);



        if (specialtyId.HasValue)

        {

            var sid = specialtyId.Value;

            query = query.Where(p => p.IsGeneral || p.SpecialtyLinks.Any(l => l.SpecialtyId == sid));

        }



        return await query

            .OrderBy(p => p.Modality)

            .ThenBy(p => p.Name)

            .Select(p => new ImagingProcedureDto(

                p.Id, p.Name, p.TussCode, p.Modality, p.BodyPart, p.Description, p.IsGeneral))

            .ToListAsync(cancellationToken);

    }



    private async Task<IReadOnlyList<MedicationCatalogDto>> GetMedicationsAsync(

        Guid? specialtyId,

        CancellationToken cancellationToken)

    {

        var query = dbContext.MedicationCatalogs.AsNoTracking().Where(m => m.IsActive);



        if (specialtyId.HasValue)

        {

            var sid = specialtyId.Value;

            query = query.Where(m => m.IsGeneral || m.SpecialtyLinks.Any(l => l.SpecialtyId == sid));

        }



        return await query

            .OrderBy(m => m.Name)

            .Select(MapMedication())

            .ToListAsync(cancellationToken);

    }



    private static System.Linq.Expressions.Expression<Func<Domain.Entities.MedicationCatalog, MedicationCatalogDto>> MapMedication() =>

        m => new MedicationCatalogDto(

            m.Id,

            m.Name,

            m.ActiveIngredient,

            m.PharmaceuticalForm,

            m.Strength,

            m.DefaultDosage,

            m.Route,

            m.Notes,

            m.PackageInsert,

            m.IsGeneral,

            m.ProductId,

            m.Product != null ? m.Product.QuantityOnHand : null);

    public async Task<IReadOnlyList<Cid10CatalogItemDto>> GetCid10CatalogAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Cid10Catalogs.AsNoTracking().Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.Code.ToLower().Contains(term)
                || c.Description.ToLower().Contains(term)
                || (c.Category != null && c.Category.ToLower().Contains(term))
                || (c.Keywords != null && c.Keywords.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Code)
            .Select(c => new Cid10CatalogItemDto(c.Code, c.Description, c.Category, c.ParentCode))
            .Take(80)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cid10CatalogItemDto>> GetCid10ChildrenAsync(
        string? parentCode,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Cid10Catalogs.AsNoTracking().Where(c => c.IsActive);

        if (string.IsNullOrWhiteSpace(parentCode))
        {
            query = query.Where(c => c.ParentCode == null);
        }
        else
        {
            var code = parentCode.Trim();
            query = query.Where(c => c.ParentCode == code);
        }

        return await query
            .OrderBy(c => c.Code)
            .Select(c => new Cid10CatalogItemDto(c.Code, c.Description, c.Category, c.ParentCode))
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdministrationRouteDto>> GetAdministrationRoutesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.AdministrationRouteCatalogs
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.DisplayOrder)
            .Select(r => new AdministrationRouteDto(r.Code, r.Name, r.Abbreviation))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PatientReferenceCatalogItemDto>> GetPatientReferenceCatalogAsync(
        PatientReferenceCatalogType catalogType,
        CancellationToken cancellationToken = default) =>
        await dbContext.PatientReferenceCatalogItems
            .AsNoTracking()
            .Where(i => i.IsActive && i.CatalogType == catalogType)
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new PatientReferenceCatalogItemDto(i.Code, i.Name, i.CatalogType))
            .ToListAsync(cancellationToken);
}


