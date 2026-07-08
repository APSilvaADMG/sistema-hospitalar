using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Mapeamentos medicamento × convênio para a tela Feegow de configuração de estoque.
/// Idempotente — verifica combinações (prescrito, referência, convênio) antes de inserir.
/// </summary>
public static class MedicationInsuranceDemoSeed
{
    public const string DemoMarker = "gth-medication-insurance-demo-v1";
    public const int MinimumMappings = 12;

    private static readonly string[] InsuranceNames =
    [
        "Amil",
        "Unimed",
        "Bradesco Saúde",
        "SUS",
        "Particular",
        "Hapvida",
    ];

    private static readonly (string PrescribedSku, string ReferenceSku)[] MappingPairs =
    [
        ("MED-DIP500", "MED-DIP500"),
        ("MED-OME20", "MED-OME20"),
        ("MED-PAR750", "MED-PAR750"),
        ("MED-SF500", "MED-SF500"),
        ("MED-DIP500", "MED-PAR750"),
        ("MED-OME20", "MED-DIP500"),
    ];

    public static async Task EnsureAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var activeCount = await db.MedicationInsuranceMappings
            .CountAsync(m => m.IsActive, cancellationToken);

        if (activeCount >= MinimumMappings)
        {
            return;
        }

        var medications = await db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Type == ProductType.Medication)
            .ToListAsync(cancellationToken);

        if (medications.Count == 0)
        {
            logger.LogWarning("MedicationInsuranceDemoSeed: nenhum medicamento ativo disponível.");
            return;
        }

        var insurances = await db.HealthInsurances
            .AsNoTracking()
            .Where(h => h.IsActive && InsuranceNames.Contains(h.Name))
            .ToListAsync(cancellationToken);

        if (insurances.Count == 0)
        {
            logger.LogWarning("MedicationInsuranceDemoSeed: nenhum convênio do catálogo encontrado.");
            return;
        }

        var productsBySku = medications.ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);
        var existingKeys = await db.MedicationInsuranceMappings
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => new { m.PrescribedProductId, m.ReferenceProductId, m.HealthInsuranceId })
            .ToListAsync(cancellationToken);

        var existingSet = existingKeys
            .Select(k => $"{k.PrescribedProductId}|{k.ReferenceProductId}|{k.HealthInsuranceId}")
            .ToHashSet(StringComparer.Ordinal);

        var added = 0;
        foreach (var insurance in insurances)
        {
            foreach (var (prescribedSku, referenceSku) in MappingPairs)
            {
                if (!productsBySku.TryGetValue(prescribedSku, out var prescribed))
                {
                    continue;
                }

                if (!productsBySku.TryGetValue(referenceSku, out var reference))
                {
                    reference = prescribed;
                }

                var key = $"{prescribed.Id}|{reference.Id}|{insurance.Id}";
                if (existingSet.Contains(key))
                {
                    continue;
                }

                db.MedicationInsuranceMappings.Add(new MedicationInsuranceMapping
                {
                    PrescribedProductId = prescribed.Id,
                    ReferenceProductId = reference.Id,
                    HealthInsuranceId = insurance.Id,
                });
                existingSet.Add(key);
                added++;
            }
        }

        if (added == 0)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "MedicationInsuranceDemoSeed: {Count} mapeamentos medicamento × convênio aplicados ({Marker}).",
            added,
            DemoMarker);
    }
}
