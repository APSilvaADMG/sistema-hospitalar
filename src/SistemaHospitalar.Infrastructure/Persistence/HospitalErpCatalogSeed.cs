using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>Catálogo ERP hospitalar de referência — tipos de usuário, setores, menu, etc.</summary>
public static class HospitalErpCatalogSeed
{
    private const int TargetRevision = 1;

    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await UpsertGroupedAsync(dbContext, HospitalReferenceCatalogType.UserType, HospitalErpCatalogSeedData.UserTypes, cancellationToken);
        await UpsertGroupedAsync(dbContext, HospitalReferenceCatalogType.HospitalSector, HospitalErpCatalogSeedData.Sectors, cancellationToken);
        await UpsertGroupedAsync(dbContext, HospitalReferenceCatalogType.Ward, HospitalErpCatalogSeedData.Wards, cancellationToken);
        await UpsertFlatAsync(dbContext, HospitalReferenceCatalogType.BedType, HospitalErpCatalogSeedData.BedTypes, cancellationToken);
        await UpsertGroupedAsync(dbContext, HospitalReferenceCatalogType.SupplierType, HospitalErpCatalogSeedData.SupplierTypes, cancellationToken);
        await UpsertGroupedAsync(dbContext, HospitalReferenceCatalogType.ProductType, HospitalErpCatalogSeedData.ProductTypes, cancellationToken);
        await UpsertGroupedAsync(dbContext, HospitalReferenceCatalogType.ServiceType, HospitalErpCatalogSeedData.ServiceTypes, cancellationToken);
        await UpsertSpecialtiesAsync(dbContext, cancellationToken);
        await UpsertGroupedAsync(dbContext, HospitalReferenceCatalogType.LabExam, HospitalErpCatalogSeedData.LabExams, cancellationToken);
        await UpsertGroupedAsync(dbContext, HospitalReferenceCatalogType.ImagingExam, HospitalErpCatalogSeedData.ImagingExams, cancellationToken);
        await UpsertFlatAsync(dbContext, HospitalReferenceCatalogType.TissGuideType, HospitalErpCatalogSeedData.TissGuideTypes, cancellationToken);
        await UpsertSystemMenuAsync(dbContext, cancellationToken);
        await UpsertFlatAsync(dbContext, HospitalReferenceCatalogType.PermissionAction, HospitalErpCatalogSeedData.PermissionActions, cancellationToken);
        await UpsertReadyProfilesAsync(dbContext, cancellationToken);
        await UpsertRegulatoryBasesAsync(dbContext, cancellationToken);
        await UpsertFlatAsync(dbContext, HospitalReferenceCatalogType.RecommendedModule, HospitalErpCatalogSeedData.RecommendedModules, cancellationToken);
        await EnsureDepartmentsFromSectorsAsync(dbContext, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertGroupedAsync(
        AppDbContext db,
        HospitalReferenceCatalogType type,
        (string Group, string[] Items)[] groups,
        CancellationToken ct)
    {
        var order = 1;
        foreach (var (group, items) in groups)
        {
            foreach (var name in items)
            {
                var code = BuildCode(name);
                await UpsertItemAsync(db, type, code, name, group, order++, null, null, ct);
            }
        }
    }

    private static async Task UpsertFlatAsync(
        AppDbContext db,
        HospitalReferenceCatalogType type,
        string[] items,
        CancellationToken ct,
        string? group = null)
    {
        for (var i = 0; i < items.Length; i++)
        {
            var name = items[i];
            await UpsertItemAsync(db, type, BuildCode(name), name, group, i + 1, null, null, ct);
        }
    }

    private static async Task UpsertSpecialtiesAsync(AppDbContext db, CancellationToken ct)
    {
        var order = 1;
        foreach (var (name, cbo) in HospitalErpCatalogSeedData.Specialties)
        {
            var metadata = JsonSerializer.Serialize(new { cbo });
            await UpsertItemAsync(
                db,
                HospitalReferenceCatalogType.MedicalSpecialty,
                BuildCode(name),
                name,
                "Especialidades",
                order++,
                $"CBO {cbo}",
                metadata,
                ct);
        }
    }

    private static async Task UpsertSystemMenuAsync(AppDbContext db, CancellationToken ct)
    {
        var order = 1;
        foreach (var (module, submenus) in HospitalErpCatalogSeedData.SystemMenus)
        {
            await UpsertItemAsync(
                db,
                HospitalReferenceCatalogType.SystemMenu,
                module,
                module.Replace('_', ' '),
                null,
                order++,
                "Módulo principal",
                null,
                ct);

            foreach (var submenu in submenus)
            {
                await UpsertItemAsync(
                    db,
                    HospitalReferenceCatalogType.SystemMenu,
                    $"{module}_{BuildCode(submenu)}",
                    submenu,
                    module,
                    order++,
                    null,
                    null,
                    ct);
            }
        }
    }

    private static async Task UpsertReadyProfilesAsync(AppDbContext db, CancellationToken ct)
    {
        for (var i = 0; i < HospitalErpCatalogSeedData.ReadyProfiles.Length; i++)
        {
            var (name, description) = HospitalErpCatalogSeedData.ReadyProfiles[i];
            await UpsertItemAsync(
                db,
                HospitalReferenceCatalogType.ReadyProfile,
                BuildCode(name),
                name,
                "Perfis",
                i + 1,
                description,
                null,
                ct);
        }
    }

    private static async Task UpsertRegulatoryBasesAsync(AppDbContext db, CancellationToken ct)
    {
        for (var i = 0; i < HospitalErpCatalogSeedData.RegulatoryBases.Length; i++)
        {
            var (name, description) = HospitalErpCatalogSeedData.RegulatoryBases[i];
            await UpsertItemAsync(
                db,
                HospitalReferenceCatalogType.RegulatoryBase,
                name.ToUpperInvariant(),
                name,
                "Regulatório",
                i + 1,
                description,
                null,
                ct);
        }
    }

    private static async Task UpsertItemAsync(
        AppDbContext db,
        HospitalReferenceCatalogType type,
        string code,
        string name,
        string? parentGroup,
        int displayOrder,
        string? description,
        string? metadataJson,
        CancellationToken ct)
    {
        var existing = await db.HospitalReferenceCatalogItems
            .FirstOrDefaultAsync(i => i.CatalogType == type && i.Code == code, ct)
            ?? db.HospitalReferenceCatalogItems.Local
                .FirstOrDefault(i => i.CatalogType == type && i.Code == code);

        if (existing is null)
        {
            db.HospitalReferenceCatalogItems.Add(new HospitalReferenceCatalogItem
            {
                CatalogType = type,
                Code = code,
                Name = name,
                ParentGroup = parentGroup,
                DisplayOrder = displayOrder,
                Description = description,
                MetadataJson = metadataJson,
                ContentRevision = TargetRevision,
            });
            return;
        }

        existing.Name = name;
        existing.ParentGroup = parentGroup;
        existing.DisplayOrder = displayOrder;
        existing.Description = description;
        existing.MetadataJson = metadataJson;
        existing.ContentRevision = TargetRevision;
        existing.IsActive = true;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task EnsureDepartmentsFromSectorsAsync(AppDbContext db, CancellationToken ct)
    {
        var sectorNames = HospitalErpCatalogSeedData.Sectors
            .SelectMany(g => g.Items)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = await db.Departments
            .Select(d => d.Name)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in sectorNames)
        {
            if (existingSet.Contains(name))
            {
                continue;
            }

            db.Departments.Add(new Department
            {
                Name = name,
                Description = $"Setor hospitalar — {name}",
            });
            existingSet.Add(name);
        }
    }

    private static string BuildCode(string name)
    {
        var normalized = name
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Aggregate("", (current, c) => current + c);

        var slug = new string(normalized
            .ToUpperInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == ' ')
            .ToArray())
            .Replace(' ', '_');

        return slug.Length > 40 ? slug[..40] : slug;
    }
}
