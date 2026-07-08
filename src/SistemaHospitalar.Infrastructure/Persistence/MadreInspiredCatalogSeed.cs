using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Catálogos funcionais inspirados no sistema Madre (Basis TI) — vias de administração,
/// referências de cadastro e CID-10 hierárquico.
/// </summary>
public static class MadreInspiredCatalogSeed
{
    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await EnsureAdministrationRoutesAsync(dbContext, cancellationToken);
        await EnsurePatientReferenceCatalogAsync(dbContext, cancellationToken);
        await EnsureCid10HierarchyAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureAdministrationRoutesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.AdministrationRouteCatalogs.AnyAsync(cancellationToken))
        {
            return;
        }

        var routes = new (string Code, string Name, string Abbr, int Order)[]
        {
            ("1", "Pré-dialisador", "AD", 1),
            ("2", "Intra-articular", "AR", 2),
            ("3", "Intra-vesical", "BX", 3),
            ("4", "Caudal", "CA", 4),
            ("5", "Pós-dialisador", "DD", 5),
            ("6", "Intra-arterial", "IA", 6),
            ("7", "Intra-dérmica", "ID", 7),
            ("8", "Intra-muscular", "IM", 8),
            ("9", "Inalatória", "IN", 9),
            ("10", "Inalatória oral", "IO", 10),
            ("11", "Intra-peritonial", "IP", 11),
            ("12", "Intra-tecal", "IT", 12),
            ("13", "Intra-venosa", "IV", 13),
            ("14", "Oftálmica", "OC", 14),
            ("15", "Otológica", "OT", 15),
            ("16", "Peribulbar", "PB", 16),
            ("17", "Epidural", "PD", 17),
            ("18", "Perineural", "PN", 18),
            ("19", "Subcutânea", "SC", 19),
            ("20", "Sublingual", "SL", 20),
            ("21", "Bucal", "TB", 21),
            ("22", "Dermatológica", "TC", 22),
            ("23", "Transdérmica", "TD", 23),
            ("24", "Traqueostomia", "TR", 24),
            ("25", "Uretral", "UR", 25),
            ("26", "Dialisador", "VD", 26),
            ("27", "Gastrostomia", "VG", 27),
            ("28", "Jejunostomia", "VJ", 28),
            ("29", "Nasal", "VN", 29),
            ("30", "Oral", "VO", 30),
            ("31", "Retal", "VR", 31),
            ("32", "Por Sonda", "VS", 32),
            ("33", "Intra-vaginal", "VV", 33),
        };

        dbContext.AdministrationRouteCatalogs.AddRange(routes.Select(r => new AdministrationRouteCatalog
        {
            Code = r.Code,
            Name = r.Name,
            Abbreviation = r.Abbr,
            DisplayOrder = r.Order,
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsurePatientReferenceCatalogAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.PatientReferenceCatalogItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var races = new[] { "Branca", "Preta", "Parda", "Amarela", "Indígena", "Sem Declaração" };
        var ethnicities = new[] { "Não indígena", "Indígena", "Quilombola", "Sem declaração" };
        var religions = new[] { "Católica", "Evangélica", "Espírita", "Umbanda/Candomblé", "Judaica", "Islâmica", "Budista", "Sem religião", "Outra", "Não informado" };
        var marital = new[] { "Solteiro(a)", "Casado(a)", "Divorciado(a)", "Viúvo(a)", "União estável", "Separado(a)", "Não informado" };

        var items = new List<PatientReferenceCatalogItem>();
        AddItems(items, PatientReferenceCatalogType.Race, races);
        AddItems(items, PatientReferenceCatalogType.Ethnicity, ethnicities);
        AddItems(items, PatientReferenceCatalogType.Religion, religions);
        AddItems(items, PatientReferenceCatalogType.MaritalStatus, marital);

        dbContext.PatientReferenceCatalogItems.AddRange(items);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void AddItems(List<PatientReferenceCatalogItem> target, PatientReferenceCatalogType type, string[] names)
    {
        for (var i = 0; i < names.Length; i++)
        {
            target.Add(new PatientReferenceCatalogItem
            {
                CatalogType = type,
                Code = (i + 1).ToString(CultureInfo.InvariantCulture),
                Name = names[i],
                DisplayOrder = i + 1,
            });
        }
    }

    private static async Task EnsureCid10HierarchyAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var csvPath = ResolveCidCsvPath();
        if (csvPath is null || !File.Exists(csvPath))
        {
            return;
        }

        var existingCount = await dbContext.Cid10Catalogs.CountAsync(cancellationToken);
        if (existingCount > 500)
        {
            return;
        }

        var rows = await ParseCidCsvAsync(csvPath, cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        var existingCodes = await dbContext.Cid10Catalogs
            .Select(c => c.Code)
            .ToListAsync(cancellationToken);
        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = rows
            .Where(r => !existingSet.Contains(r.Code))
            .Select(r => new Cid10Catalog
            {
                Code = r.Code,
                Description = r.Description,
                ParentCode = r.ParentCode,
                Category = r.ParentCode is null ? "Capítulo" : null,
            })
            .ToList();

        if (toAdd.Count == 0)
        {
            return;
        }

        const int batchSize = 500;
        for (var i = 0; i < toAdd.Count; i += batchSize)
        {
            var batch = toAdd.Skip(i).Take(batchSize).ToList();
            dbContext.Cid10Catalogs.AddRange(batch);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string? ResolveCidCsvPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "madre-cid10.csv"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "SistemaHospitalar.Infrastructure", "Persistence", "Data", "madre-cid10.csv"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "SistemaHospitalar.Infrastructure", "Persistence", "Data", "madre-cid10.csv"),
            @"C:\Users\Anderson\Downloads\madre-master\madre-master\internacao\src\main\resources\config\liquibase\data\cid.csv",
        };

        return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }

    private static async Task<List<(string Code, string Description, string? ParentCode)>> ParseCidCsvAsync(
        string csvPath,
        CancellationToken cancellationToken)
    {
        var rawRows = new List<(int Id, string Code, string Description, int? ParentId)>();
        await using var stream = File.OpenRead(csvPath);
        using var reader = new StreamReader(stream);

        var headerSkipped = false;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!headerSkipped)
            {
                headerSkipped = true;
                continue;
            }

            var parts = line.Split(';');
            if (parts.Length < 4)
            {
                continue;
            }

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }

            var code = parts[1].Trim();
            var description = parts[2].Trim();
            int? parentId = null;
            if (!string.Equals(parts[3].Trim(), "NULL", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedParent))
            {
                parentId = parsedParent;
            }

            rawRows.Add((id, code, description, parentId));
        }

        var idToCode = rawRows.ToDictionary(r => r.Id, r => r.Code);
        return rawRows
            .Select(r =>
            {
                string? parentCode = null;
                if (r.ParentId.HasValue && idToCode.TryGetValue(r.ParentId.Value, out var pc))
                {
                    parentCode = pc;
                }

                return (r.Code, r.Description, parentCode);
            })
            .ToList();
    }
}
