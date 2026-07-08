using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class WasteDemoSeed
{
    public const string DemoMarker = "gth-waste-demo-v1";

    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.WasteCollections.AnyAsync(
                w => w.IsActive && w.Notes != null && w.Notes.Contains(DemoMarker),
                cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        db.WasteCollections.AddRange(
            new WasteCollection
            {
                Code = "RES-DEMO-001",
                WasteType = WasteType.Infectious,
                SectorName = "UTI Adulto",
                QuantityKg = 12.5m,
                ContainerCode = "CX-INF-01",
                CollectedBy = "Enf. Carla Souza",
                Status = WasteCollectionStatus.Stored,
                Notes = $"Coleta rotina UTI. {DemoMarker}",
                CollectedAt = now.AddHours(-8),
            },
            new WasteCollection
            {
                Code = "RES-DEMO-002",
                WasteType = WasteType.Sharps,
                SectorName = "Centro Cirúrgico",
                QuantityKg = 3.2m,
                ContainerCode = "CX-PERF-02",
                CollectedBy = "Téc. Marcos Lima",
                Status = WasteCollectionStatus.Registered,
                Notes = $"Perfurocortantes pós-cirurgia. {DemoMarker}",
                CollectedAt = now.AddHours(-4),
            },
            new WasteCollection
            {
                Code = "RES-DEMO-003",
                WasteType = WasteType.Common,
                SectorName = "Ambulatório",
                QuantityKg = 28.0m,
                ContainerCode = "CX-COM-05",
                CollectedBy = "Aux. Fernanda Dias",
                Status = WasteCollectionStatus.PickedUp,
                ManifestNumber = "MTR-2026-0042",
                Notes = $"Resíduo comum administrativo. {DemoMarker}",
                CollectedAt = now.AddDays(-1),
            },
            new WasteCollection
            {
                Code = "RES-DEMO-004",
                WasteType = WasteType.Chemical,
                SectorName = "Laboratório",
                QuantityKg = 1.8m,
                ContainerCode = "CX-QUIM-01",
                CollectedBy = "Bioméd. Paulo Rocha",
                Status = WasteCollectionStatus.Registered,
                Notes = $"Reagentes vencidos segregados. {DemoMarker}",
                CollectedAt = now.AddHours(-2),
            },
            new WasteCollection
            {
                Code = "RES-DEMO-005",
                WasteType = WasteType.Pharmaceutical,
                SectorName = "Farmácia Central",
                QuantityKg = 5.4m,
                ContainerCode = "CX-FARM-03",
                CollectedBy = "Farm. Beatriz Alves",
                Status = WasteCollectionStatus.Disposed,
                ManifestNumber = "MTR-2026-0038",
                Notes = $"Medicamentos vencidos — descarte controlado. {DemoMarker}",
                CollectedAt = now.AddDays(-3),
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}
