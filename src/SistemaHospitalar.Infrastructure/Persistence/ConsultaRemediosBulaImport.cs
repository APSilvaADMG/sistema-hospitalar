using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Bulario;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class ConsultaRemediosBulaImport
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public sealed record BulaRecord(
        string Name,
        string Slug,
        string? Url,
        string? ActiveIngredient,
        string? PharmaceuticalForm,
        string? Strength,
        string? Route,
        string? PackageInsert);

    public static async Task<int> ApplyFromJsonlAsync(
        AppDbContext db,
        string jsonlPath,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(jsonlPath))
        {
            logger?.LogInformation("Arquivo de bulas não encontrado: {Path}", jsonlPath);
            return 0;
        }

        var existingSlugs = await db.MedicationCatalogs
            .Where(m => m.ExternalBulaSlug != null)
            .Select(m => m.ExternalBulaSlug!)
            .ToDictionaryAsync(s => s, _ => true, StringComparer.OrdinalIgnoreCase, ct);

        var imported = 0;
        var updated = 0;

        foreach (var record in ReadRecords(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(record.Slug) || string.IsNullOrWhiteSpace(record.Name))
                continue;

            if (string.IsNullOrWhiteSpace(record.PackageInsert))
                continue;

            if (existingSlugs.ContainsKey(record.Slug))
            {
                var existing = await db.MedicationCatalogs
                    .FirstOrDefaultAsync(m => m.ExternalBulaSlug == record.Slug, ct);
                if (existing is null)
                    continue;

                ApplyRecord(existing, record);
                updated++;
                continue;
            }

            db.MedicationCatalogs.Add(MapRecord(record));

            existingSlugs[record.Slug] = true;
            imported++;
        }

        if (imported > 0 || updated > 0)
            await db.SaveChangesAsync(ct);

        logger?.LogInformation(
            "Bulário Consulta Remédios: {Imported} novos, {Updated} atualizados.",
            imported,
            updated);

        return imported + updated;
    }

    private static string TrimField(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string? TrimOptional(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? value : TrimField(value.Trim(), maxLength);

    private static MedicationCatalog MapRecord(BulaRecord record)
    {
        var packageInsert = BulaTextNormalizer.Normalize(record.PackageInsert);
        var form = ResolveForm(record);
        var strength = ResolveStrength(record);
        var route = ResolveRoute(record, form, packageInsert);
        var displayName = MedicationMetadataInferrer.FormatDisplayName(record.Name.Trim(), strength);

        return new MedicationCatalog
        {
            Name = TrimField(displayName, 300),
            ActiveIngredient = ResolveActiveIngredient(record),
            PharmaceuticalForm = form,
            Strength = strength,
            Route = route,
            DefaultDosage = TrimOptional(BulaTextNormalizer.ExtractPosologia(record.PackageInsert), 200),
            PackageInsert = packageInsert,
            ExternalBulaSlug = TrimField(record.Slug.Trim(), 120),
            IsGeneral = false,
            Notes = TrimOptional(record.Url, 1000),
        };
    }

    private static void ApplyRecord(MedicationCatalog existing, BulaRecord record)
    {
        var packageInsert = BulaTextNormalizer.Normalize(record.PackageInsert);
        var form = ResolveForm(record) ?? existing.PharmaceuticalForm;
        var strength = ResolveStrength(record) ?? existing.Strength;
        var route = ResolveRoute(record, form, packageInsert) ?? existing.Route;
        var activeIngredient = ResolveActiveIngredient(record) ?? existing.ActiveIngredient;

        existing.Name = TrimField(
            MedicationMetadataInferrer.FormatDisplayName(record.Name.Trim(), strength),
            300);
        existing.ActiveIngredient = activeIngredient;
        existing.PharmaceuticalForm = form;
        existing.Strength = strength;
        existing.Route = route;
        existing.DefaultDosage = TrimOptional(
            BulaTextNormalizer.ExtractPosologia(record.PackageInsert),
            200) ?? existing.DefaultDosage;
        existing.PackageInsert = packageInsert;
        existing.Notes = TrimOptional(record.Url ?? existing.Notes, 1000);
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private static string? ResolveActiveIngredient(BulaRecord record) =>
        TrimOptional(record.ActiveIngredient, 500);

    private static string? ResolveForm(BulaRecord record) =>
        TrimOptional(MedicationMetadataInferrer.NormalizeForm(record.PharmaceuticalForm), 80);

    private static string? ResolveStrength(BulaRecord record) =>
        TrimOptional(record.Strength, 80)
        ?? TrimOptional(MedicationMetadataInferrer.InferStrength(record.Name), 80);

    private static string? ResolveRoute(BulaRecord record, string? form, string? packageInsert) =>
        TrimOptional(record.Route, 80)
        ?? TrimOptional(MedicationMetadataInferrer.InferRoute(form, packageInsert), 80);

    public static string ResolveCrIndexJsonlPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "cr-index-bulas.jsonl");
            if (File.Exists(candidate))
                return candidate;

            candidate = Path.Combine(dir.FullName, "..", "..", "..", "..", "data", "cr-index-bulas.jsonl");
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);

            dir = dir.Parent;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "data", "cr-index-bulas.jsonl"));
    }

    public static string ResolveActiveJsonlPath()
    {
        var crIndex = ResolveCrIndexJsonlPath();
        if (File.Exists(crIndex))
            return crIndex;

        return ResolveDefaultJsonlPath();
    }

    public static string ResolveDefaultJsonlPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "consulta-remedios-bulas.jsonl");
            if (File.Exists(candidate))
                return candidate;

            candidate = Path.Combine(dir.FullName, "..", "..", "..", "..", "data", "consulta-remedios-bulas.jsonl");
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);

            dir = dir.Parent;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "data", "consulta-remedios-bulas.jsonl"));
    }

    private static IEnumerable<BulaRecord> ReadRecords(string jsonlPath)
    {
        foreach (var line in File.ReadLines(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            BulaRecord? record;
            try
            {
                record = JsonSerializer.Deserialize<BulaRecord>(line, JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (record is not null)
                yield return record;
        }
    }
}
