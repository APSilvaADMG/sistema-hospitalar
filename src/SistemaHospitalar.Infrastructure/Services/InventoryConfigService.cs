using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Inventory;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class InventoryConfigService(AppDbContext dbContext) : IInventoryConfigService
{
    private const string LegacyLookupDemoMarker = "gth-inventory-lookup-demo-v1";

    internal static string CleanLookupDisplayName(string name)
    {
        var suffix = $" |{LegacyLookupDemoMarker}";
        return name.EndsWith(suffix, StringComparison.Ordinal)
            ? name[..^suffix.Length]
            : name;
    }

    public async Task<IReadOnlyList<InventoryLookupItemDto>> GetLookupItemsAsync(
        InventoryLookupType type,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.InventoryLookupItems.AsNoTracking()
            .Where(i => i.Type == type && i.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i => i.Name.Contains(term));
        }

        var items = await query
            .OrderBy(i => i.Name)
            .Select(i => new { i.Id, i.Type, i.Name })
            .ToListAsync(cancellationToken);

        return items
            .Select(i => new InventoryLookupItemDto(i.Id, i.Type, CleanLookupDisplayName(i.Name)))
            .ToList();
    }

    public async Task<InventoryLookupItemDto> CreateLookupItemAsync(
        InventoryLookupType type,
        CreateInventoryLookupItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Informe o nome.");
        }

        if (await dbContext.InventoryLookupItems.AnyAsync(
            i => i.Type == type && i.Name == name && i.IsActive, cancellationToken))
        {
            throw new InvalidOperationException("Registro já cadastrado.");
        }

        var item = new InventoryLookupItem { Type = type, Name = name };
        dbContext.InventoryLookupItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new InventoryLookupItemDto(item.Id, item.Type, item.Name);
    }

    public async Task<InventoryLookupItemDto> UpdateLookupItemAsync(
        Guid id,
        UpdateInventoryLookupItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.InventoryLookupItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null || !item.IsActive)
        {
            throw new InvalidOperationException("Registro não encontrado.");
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Informe o nome.");
        }

        if (await dbContext.InventoryLookupItems.AnyAsync(
            i => i.Id != id && i.Type == item.Type && i.Name == name && i.IsActive, cancellationToken))
        {
            throw new InvalidOperationException("Registro já cadastrado.");
        }

        item.Name = name;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new InventoryLookupItemDto(item.Id, item.Type, item.Name);
    }

    public async Task DeleteLookupItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.InventoryLookupItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            throw new InvalidOperationException("Registro não encontrado.");
        }

        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MedicationInsuranceMappingDto>> GetMedicationMappingsAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.MedicationInsuranceMappings.AsNoTracking().Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                m.PrescribedProduct.Name.Contains(term)
                || m.ReferenceProduct.Name.Contains(term)
                || m.HealthInsurance.Name.Contains(term));
        }

        return await query
            .OrderBy(m => m.HealthInsurance.Name)
            .ThenBy(m => m.PrescribedProduct.Name)
            .Select(m => new MedicationInsuranceMappingDto(
                m.Id,
                m.PrescribedProductId,
                m.PrescribedProduct.Name,
                m.ReferenceProductId,
                m.ReferenceProduct.Name,
                m.HealthInsuranceId,
                m.HealthInsurance.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicationInsuranceMappingDto> CreateMedicationMappingAsync(
        CreateMedicationInsuranceMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateMedicationMappingAsync(
            request.PrescribedProductId,
            request.ReferenceProductId,
            request.HealthInsuranceId,
            null,
            cancellationToken);

        var mapping = new MedicationInsuranceMapping
        {
            PrescribedProductId = request.PrescribedProductId,
            ReferenceProductId = request.ReferenceProductId,
            HealthInsuranceId = request.HealthInsuranceId,
        };

        dbContext.MedicationInsuranceMappings.Add(mapping);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapMedicationMappingAsync(mapping.Id, cancellationToken);
    }

    public async Task<MedicationInsuranceMappingDto> UpdateMedicationMappingAsync(
        Guid id,
        UpdateMedicationInsuranceMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        var mapping = await dbContext.MedicationInsuranceMappings.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (mapping is null || !mapping.IsActive)
        {
            throw new InvalidOperationException("Cadastro não encontrado.");
        }

        await ValidateMedicationMappingAsync(
            request.PrescribedProductId,
            request.ReferenceProductId,
            request.HealthInsuranceId,
            id,
            cancellationToken);

        mapping.PrescribedProductId = request.PrescribedProductId;
        mapping.ReferenceProductId = request.ReferenceProductId;
        mapping.HealthInsuranceId = request.HealthInsuranceId;
        mapping.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapMedicationMappingAsync(mapping.Id, cancellationToken);
    }

    public async Task DeleteMedicationMappingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mapping = await dbContext.MedicationInsuranceMappings.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (mapping is null)
        {
            throw new InvalidOperationException("Cadastro não encontrado.");
        }

        mapping.IsActive = false;
        mapping.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateMedicationMappingAsync(
        Guid prescribedProductId,
        Guid referenceProductId,
        Guid healthInsuranceId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (prescribedProductId == Guid.Empty
            || referenceProductId == Guid.Empty
            || healthInsuranceId == Guid.Empty)
        {
            throw new InvalidOperationException("Preencha todos os campos obrigatórios.");
        }

        var prescribedExists = await dbContext.Products.AnyAsync(
            p => p.Id == prescribedProductId && p.IsActive, cancellationToken);
        if (!prescribedExists)
        {
            throw new InvalidOperationException("Medicamento prescrito não encontrado.");
        }

        var referenceExists = await dbContext.Products.AnyAsync(
            p => p.Id == referenceProductId && p.IsActive, cancellationToken);
        if (!referenceExists)
        {
            throw new InvalidOperationException("Medicamento referência não encontrado.");
        }

        var insuranceExists = await dbContext.HealthInsurances.AnyAsync(
            h => h.Id == healthInsuranceId && h.IsActive, cancellationToken);
        if (!insuranceExists)
        {
            throw new InvalidOperationException("Convênio não encontrado.");
        }

        var duplicateQuery = dbContext.MedicationInsuranceMappings.AsNoTracking()
            .Where(m => m.IsActive
                && m.PrescribedProductId == prescribedProductId
                && m.ReferenceProductId == referenceProductId
                && m.HealthInsuranceId == healthInsuranceId);

        if (excludeId.HasValue)
        {
            duplicateQuery = duplicateQuery.Where(m => m.Id != excludeId.Value);
        }

        if (await duplicateQuery.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Já existe um cadastro com estes dados.");
        }
    }

    private async Task<MedicationInsuranceMappingDto> MapMedicationMappingAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.MedicationInsuranceMappings
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MedicationInsuranceMappingDto(
                m.Id,
                m.PrescribedProductId,
                m.PrescribedProduct.Name,
                m.ReferenceProductId,
                m.ReferenceProduct.Name,
                m.HealthInsuranceId,
                m.HealthInsurance.Name))
            .FirstAsync(cancellationToken);
    }
}
