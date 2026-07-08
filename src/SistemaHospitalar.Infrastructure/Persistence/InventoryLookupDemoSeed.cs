using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Categorias, localizações e fabricantes para cadastro de produtos no estoque.
/// </summary>
public static class InventoryLookupDemoSeed
{
    public const string DemoMarker = "gth-inventory-lookup-demo-v1";

    public static async Task EnsureAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await db.InventoryLookupItems.AnyAsync(
                i => i.Type == InventoryLookupType.Location
                    && i.Name == "Almoxarifado Central — A1",
                cancellationToken))
        {
            return;
        }

        logger.LogInformation("Aplicando lookups de estoque (categorias, localizações, fabricantes)...");

        var categories = new[]
        {
            "Medicamentos", "Material hospitalar", "Descartáveis", "Curativos", "Limpeza e higiene",
            "Nutrição clínica", "Equipamentos", "Reagentes laboratoriais", "Contraste radiológico",
            "Rouparia", "EPI", "Odontológico", "Ortopedia", "Cardiologia", "UTI",
            "Farmácia magistral",
        };

        var locations = new[]
        {
            "Almoxarifado Central — A1", "Almoxarifado Central — B2", "Farmácia Central",
            "Farmácia UTI", "UTI — Posto 1", "UTI — Posto 2", "Enfermaria — Ala A",
            "Centro Cirúrgico — Sala 1", "Centro Cirúrgico — Sala 2", "Pronto-Socorro",
            "Laboratório", "Diagnóstico por Imagem", "Hotelaria", "CCIH", "Recepção",
            "Ambulatório — Consultório 101",
        };

        var manufacturers = new[]
        {
            "EMS", "Eurofarma", "Medley", "Neo Química", "Baxter", "BD", "3M", "Cremer",
            "Descarpack", "Helm", "Lysoform", "Karsten", "Supermax", "Pfizer", "Novartis",
            "Roche", "Genérico",
        };

        foreach (var name in categories)
        {
            db.InventoryLookupItems.Add(new InventoryLookupItem
            {
                Type = InventoryLookupType.Category,
                Name = name,
            });
        }

        foreach (var name in locations)
        {
            db.InventoryLookupItems.Add(new InventoryLookupItem
            {
                Type = InventoryLookupType.Location,
                Name = name,
            });
        }

        foreach (var name in manufacturers)
        {
            db.InventoryLookupItems.Add(new InventoryLookupItem
            {
                Type = InventoryLookupType.Manufacturer,
                Name = name,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Lookups de estoque: {Categories} categorias, {Locations} localizações, {Manufacturers} fabricantes.",
            categories.Length,
            locations.Length,
            manufacturers.Length);
    }
}
