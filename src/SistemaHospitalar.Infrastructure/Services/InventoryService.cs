using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Inventory;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class InventoryService(AppDbContext dbContext) : IInventoryService
{
    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        string? search,
        bool? lowStockOnly,
        ProductType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term)
                || p.Sku.Contains(term)
                || (p.Barcode != null && p.Barcode.Contains(term)));
        }

        if (lowStockOnly == true)
        {
            query = query.Where(p => p.QuantityOnHand <= p.MinimumStock);
        }

        if (type.HasValue)
        {
            query = query.Where(p => p.Type == type.Value);
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Sku,
                p.Type,
                p.Unit,
                p.QuantityOnHand,
                p.MinimumStock,
                p.QuantityOnHand <= p.MinimumStock,
                p.Presentation,
                p.Barcode,
                p.Category,
                p.AverageSalePrice,
                p.Manufacturer,
                p.DefaultLocation))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == id && p.IsActive)
            .Select(p => MapDetail(p))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductDetailDto> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var sku = ResolveSku(request.Sku, request.Barcode, request.Name);

        if (await dbContext.Products.AnyAsync(p => p.Sku == sku, cancellationToken))
        {
            throw new InvalidOperationException("SKU já cadastrado.");
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Sku = sku,
            Type = request.Type,
            Unit = request.Unit.Trim(),
            MinimumStock = request.MinimumStock,
            MaximumStock = request.MaximumStock,
            Description = request.Description?.Trim(),
            Presentation = request.Presentation?.Trim(),
            ContentQuantity = request.ContentQuantity,
            Barcode = request.Barcode?.Trim(),
            Category = request.Category?.Trim(),
            Manufacturer = request.Manufacturer?.Trim(),
            DefaultLocation = request.DefaultLocation?.Trim(),
            TussCode = request.TussCode?.Trim(),
            ExpiryWarningDays = request.ExpiryWarningDays,
            AveragePurchasePrice = request.AveragePurchasePrice,
            AverageSalePrice = request.AverageSalePrice,
            AllowOutboundFromRegister = request.AllowOutboundFromRegister,
            EntryLocations = request.EntryLocations?.Trim(),
            PhotoData = request.PhotoData,
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapDetail(product);
    }

    public async Task<ProductDetailDto> UpdateProductAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(
            p => p.Id == id && p.IsActive, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException("Produto não encontrado.");
        }

        product.Name = request.Name.Trim();
        product.Type = request.Type;
        product.Unit = request.Unit.Trim();
        product.MinimumStock = request.MinimumStock;
        product.MaximumStock = request.MaximumStock;
        product.Description = request.Description?.Trim();
        product.Presentation = request.Presentation?.Trim();
        product.ContentQuantity = request.ContentQuantity;
        product.Barcode = request.Barcode?.Trim();
        product.Category = request.Category?.Trim();
        product.Manufacturer = request.Manufacturer?.Trim();
        product.DefaultLocation = request.DefaultLocation?.Trim();
        product.TussCode = request.TussCode?.Trim();
        product.ExpiryWarningDays = request.ExpiryWarningDays;
        product.AveragePurchasePrice = request.AveragePurchasePrice;
        product.AverageSalePrice = request.AverageSalePrice;
        product.AllowOutboundFromRegister = request.AllowOutboundFromRegister;
        product.EntryLocations = request.EntryLocations?.Trim();
        product.PhotoData = request.PhotoData;
        product.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapDetail(product);
    }

    public async Task<StockMovementDto> RegisterInboundAsync(
        StockInboundRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(
            p => p.Id == request.ProductId && p.IsActive, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException("Produto não encontrado.");
        }

        product.QuantityOnHand += request.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        var movement = MapMovementFromInbound(request, product.Id);
        dbContext.StockMovements.Add(movement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapMovementAsync(movement.Id, cancellationToken);
    }

    public async Task<StockMovementDto> RegisterOutboundAsync(
        StockOutboundRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(
            p => p.Id == request.ProductId && p.IsActive, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException("Produto não encontrado.");
        }

        if (!product.AllowOutboundFromRegister)
        {
            throw new InvalidOperationException("Saída não permitida pelo cadastro deste produto.");
        }

        WarehouseRules.ValidateLotTraceabilityForMedication(
            product.Type, request.BatchNumber, null);
        WarehouseRules.ValidateDisposableNoReturn(product.Category, request.Reason);

        HospitalBusinessRules.ValidateDispenseQuantity(product.QuantityOnHand, request.Quantity);

        product.QuantityOnHand -= request.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        var movement = new StockMovement
        {
            ProductId = product.Id,
            Type = StockMovementType.Outbound,
            Quantity = request.Quantity,
            Reason = request.Reason.Trim(),
            PatientOrSupplier = request.PatientOrSupplier?.Trim(),
            ResponsibleName = request.ResponsibleName?.Trim(),
            UserName = request.UserName?.Trim(),
            BatchNumber = request.BatchNumber?.Trim(),
            IndividualCode = request.IndividualCode?.Trim(),
            Location = request.Location?.Trim(),
            InvoiceNumber = request.InvoiceNumber?.Trim(),
            UnitPrice = request.UnitPrice,
            Account = request.Account?.Trim(),
        };

        dbContext.StockMovements.Add(movement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapMovementAsync(movement.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(
        Guid? productId,
        string? search = null,
        StockMovementType? type = null,
        DateOnly? from = null,
        DateOnly? to = null,
        int limit = 300,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockMovements.AsNoTracking();

        if (productId.HasValue)
        {
            query = query.Where(m => m.ProductId == productId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(m => m.Type == type.Value);
        }

        if (from.HasValue)
        {
            var fromUtc = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(m => m.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(m => m.CreatedAt < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                m.Product.Name.Contains(term)
                || m.Product.Sku.Contains(term)
                || (m.Reason != null && m.Reason.Contains(term))
                || (m.Reference != null && m.Reference.Contains(term))
                || (m.BatchNumber != null && m.BatchNumber.Contains(term)));
        }

        var take = Math.Clamp(limit, 1, 1000);

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .Select(m => new StockMovementDto(
                m.Id,
                m.ProductId,
                m.Product.Name,
                m.Type,
                m.Quantity,
                m.Reason,
                m.Reference,
                m.CreatedAt,
                m.PatientOrSupplier,
                m.ResponsibleName,
                m.UserName,
                m.BatchNumber,
                m.IndividualCode,
                m.Location,
                m.ExpiryDate,
                m.InvoiceNumber,
                m.UnitPrice,
                m.Account))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductBillingRuleDto>> GetBillingRulesAsync(
        Guid productId,
        string? priceTable,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProductBillingRules.AsNoTracking()
            .Where(r => r.ProductId == productId);

        if (!string.IsNullOrWhiteSpace(priceTable))
        {
            query = query.Where(r => r.PriceTable == priceTable.Trim());
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        return await query
            .OrderByDescending(r => r.ValidFrom)
            .ThenBy(r => r.PriceTable)
            .Select(r => new ProductBillingRuleDto(
                r.Id,
                r.ProductId,
                r.PriceTable,
                r.ReferenceTable,
                r.Code,
                r.PricePfb,
                r.Pmc,
                r.Edition,
                r.ValidFrom,
                r.ValidTo,
                r.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductBillingRuleDto> CreateBillingRuleAsync(
        Guid productId,
        CreateProductBillingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var productExists = await dbContext.Products.AnyAsync(
            p => p.Id == productId && p.IsActive, cancellationToken);

        if (!productExists)
        {
            throw new InvalidOperationException("Produto não encontrado.");
        }

        if (string.IsNullOrWhiteSpace(request.PriceTable))
        {
            throw new InvalidOperationException("Informe a tabela de preço.");
        }

        var rule = new ProductBillingRule
        {
            ProductId = productId,
            PriceTable = request.PriceTable.Trim(),
            ReferenceTable = request.ReferenceTable?.Trim(),
            Code = request.Code?.Trim(),
            PricePfb = request.PricePfb,
            Pmc = request.Pmc,
            Edition = request.Edition?.Trim(),
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsActive = request.IsActive,
        };

        dbContext.ProductBillingRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapBillingRule(rule);
    }

    public async Task<ProductBillingRuleDto> UpdateBillingRuleAsync(
        Guid ruleId,
        UpdateProductBillingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.ProductBillingRules.FirstOrDefaultAsync(
            r => r.Id == ruleId, cancellationToken);

        if (rule is null)
        {
            throw new InvalidOperationException("Regra de faturamento não encontrada.");
        }

        if (string.IsNullOrWhiteSpace(request.PriceTable))
        {
            throw new InvalidOperationException("Informe a tabela de preço.");
        }

        rule.PriceTable = request.PriceTable.Trim();
        rule.ReferenceTable = request.ReferenceTable?.Trim();
        rule.Code = request.Code?.Trim();
        rule.PricePfb = request.PricePfb;
        rule.Pmc = request.Pmc;
        rule.Edition = request.Edition?.Trim();
        rule.ValidFrom = request.ValidFrom;
        rule.ValidTo = request.ValidTo;
        rule.IsActive = request.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapBillingRule(rule);
    }

    public async Task DeleteBillingRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.ProductBillingRules.FirstOrDefaultAsync(
            r => r.Id == ruleId, cancellationToken);

        if (rule is null)
        {
            throw new InvalidOperationException("Regra de faturamento não encontrada.");
        }

        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ProductBillingRuleDto MapBillingRule(ProductBillingRule r) => new(
        r.Id,
        r.ProductId,
        r.PriceTable,
        r.ReferenceTable,
        r.Code,
        r.PricePfb,
        r.Pmc,
        r.Edition,
        r.ValidFrom,
        r.ValidTo,
        r.IsActive);

    private static StockMovement MapMovementFromInbound(StockInboundRequest request, Guid productId) => new()
    {
        ProductId = productId,
        Type = StockMovementType.Inbound,
        Quantity = request.Quantity,
        Reason = request.Reason.Trim(),
        Reference = request.Reference?.Trim(),
        PatientOrSupplier = request.PatientOrSupplier?.Trim(),
        ResponsibleName = request.ResponsibleName?.Trim(),
        UserName = request.UserName?.Trim(),
        BatchNumber = request.BatchNumber?.Trim(),
        IndividualCode = request.IndividualCode?.Trim(),
        Location = request.Location?.Trim(),
        ExpiryDate = request.ExpiryDate,
        InvoiceNumber = request.InvoiceNumber?.Trim(),
        UnitPrice = request.UnitPrice,
        Account = request.Account?.Trim(),
    };

    private async Task<StockMovementDto> MapMovementAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.StockMovements
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new StockMovementDto(
                m.Id,
                m.ProductId,
                m.Product.Name,
                m.Type,
                m.Quantity,
                m.Reason,
                m.Reference,
                m.CreatedAt,
                m.PatientOrSupplier,
                m.ResponsibleName,
                m.UserName,
                m.BatchNumber,
                m.IndividualCode,
                m.Location,
                m.ExpiryDate,
                m.InvoiceNumber,
                m.UnitPrice,
                m.Account))
            .FirstAsync(cancellationToken);
    }

    private static string ResolveSku(string sku, string? barcode, string name)
    {
        var candidate = !string.IsNullOrWhiteSpace(sku)
            ? sku.Trim()
            : !string.IsNullOrWhiteSpace(barcode)
                ? barcode.Trim()
                : GenerateSkuFromName(name);

        return candidate.ToUpperInvariant();
    }

    private static string GenerateSkuFromName(string name)
    {
        var slug = new string(name
            .Trim()
            .ToUpperInvariant()
            .Where(ch => char.IsLetterOrDigit(ch))
            .Take(12)
            .ToArray());

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "PROD";
        }

        return $"{slug}-{DateTime.UtcNow:yyMMddHHmmss}";
    }

    private static ProductDetailDto MapDetail(Product product) => new(
        product.Id,
        product.Name,
        product.Sku,
        product.Type,
        product.Unit,
        product.QuantityOnHand,
        product.MinimumStock,
        product.MaximumStock,
        product.QuantityOnHand <= product.MinimumStock,
        product.Description,
        product.Presentation,
        product.ContentQuantity,
        product.Barcode,
        product.Category,
        product.Manufacturer,
        product.DefaultLocation,
        product.TussCode,
        product.ExpiryWarningDays,
        product.AveragePurchasePrice,
        product.AverageSalePrice,
        product.AllowOutboundFromRegister,
        product.EntryLocations,
        product.PhotoData);
}

public class PharmacyService(AppDbContext dbContext) : IPharmacyService
{
    public async Task<PharmacyDispensingDto> DispenseAsync(
        DispenseMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(
            p => p.Id == request.ProductId && p.IsActive, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException("Produto não encontrado.");
        }

        var allergies = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.MedicalRecord.PatientId == request.PatientId
                && (EF.Functions.ILike(e.Content, "%alerg%")
                    || EF.Functions.ILike(e.Content, "%Alergia%")))
            .Select(e => e.Content)
            .Take(10)
            .ToListAsync(cancellationToken);

        PrescriptionRules.ValidateNoAllergyConflict(product.Name, allergies);
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            PrescriptionRules.ValidateNoAllergyConflict(request.Notes, allergies);
        }

        HospitalBusinessRules.ValidateDispenseQuantity(product.QuantityOnHand, request.Quantity);

        var fefoExpiry = await dbContext.StockMovements
            .AsNoTracking()
            .Where(m => m.ProductId == request.ProductId
                && m.Type == StockMovementType.Inbound
                && m.ExpiryDate != null)
            .OrderBy(m => m.ExpiryDate)
            .Select(m => m.ExpiryDate)
            .FirstOrDefaultAsync(cancellationToken);

        HospitalBusinessRules.ValidateMedicationNotExpired(fefoExpiry);

        var patientExists = await dbContext.Patients.AnyAsync(
            p => p.Id == request.PatientId && p.IsActive, cancellationToken);

        if (!patientExists)
        {
            throw new InvalidOperationException("Paciente não encontrado.");
        }

        product.QuantityOnHand -= request.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        var dispensing = new PharmacyDispensing
        {
            PatientId = request.PatientId,
            ProductId = request.ProductId,
            ProfessionalId = request.ProfessionalId,
            HospitalizationId = request.HospitalizationId,
            Quantity = request.Quantity,
            Notes = request.Notes?.Trim()
        };

        dbContext.PharmacyDispensings.Add(dispensing);

        dbContext.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id,
            Type = StockMovementType.Outbound,
            Quantity = request.Quantity,
            Reason = "Dispensação farmácia",
            Reference = dispensing.Id.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetDispensingByIdAsync(dispensing.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<PharmacyDispensingDto>> GetDispensingsAsync(
        Guid? patientId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PharmacyDispensings.AsNoTracking().Where(d => d.IsActive);

        if (patientId.HasValue)
        {
            query = query.Where(d => d.PatientId == patientId.Value);
        }

        return await query
            .OrderByDescending(d => d.DispensedAt)
            .Take(100)
            .Select(d => new PharmacyDispensingDto(
                d.Id,
                d.PatientId,
                d.Patient.FullName,
                d.ProductId,
                d.Product.Name,
                d.Quantity,
                d.ReversedQuantity,
                d.Professional != null ? d.Professional.FullName : null,
                d.DispensedAt,
                d.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<PharmacyDispensingReversalDto> ReverseDispensingAsync(
        Guid dispensingId,
        ReversePharmacyDispensingRequest request,
        Guid? reversedByUserId,
        CancellationToken cancellationToken = default)
    {
        var dispensing = await dbContext.PharmacyDispensings
            .Include(d => d.Product)
            .FirstOrDefaultAsync(d => d.Id == dispensingId && d.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Dispensação não encontrada.");

        var remaining = dispensing.Quantity - dispensing.ReversedQuantity;
        if (request.Quantity <= 0 || request.Quantity > remaining)
        {
            throw new InvalidOperationException($"Quantidade inválida. Restante para estorno: {remaining}.");
        }

        dispensing.Product.QuantityOnHand += request.Quantity;
        dispensing.Product.UpdatedAt = DateTime.UtcNow;
        dispensing.ReversedQuantity += request.Quantity;
        dispensing.UpdatedAt = DateTime.UtcNow;

        var reversal = new PharmacyDispensingReversal
        {
            DispensingId = dispensing.Id,
            Quantity = request.Quantity,
            Reason = request.Reason?.Trim(),
            ReversedByUserId = reversedByUserId,
            ReversedAt = DateTime.UtcNow,
        };

        dbContext.PharmacyDispensingReversals.Add(reversal);
        dbContext.StockMovements.Add(new StockMovement
        {
            ProductId = dispensing.ProductId,
            Type = StockMovementType.Inbound,
            Quantity = request.Quantity,
            Reason = "Estorno de dispensação",
            Reference = reversal.Id.ToString(),
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PharmacyDispensingReversalDto(
            reversal.Id,
            reversal.DispensingId,
            reversal.Quantity,
            reversal.Reason,
            reversal.ReversedAt);
    }

    private async Task<PharmacyDispensingDto?> GetDispensingByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.PharmacyDispensings
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new PharmacyDispensingDto(
                d.Id,
                d.PatientId,
                d.Patient.FullName,
                d.ProductId,
                d.Product.Name,
                d.Quantity,
                d.ReversedQuantity,
                d.Professional != null ? d.Professional.FullName : null,
                d.DispensedAt,
                d.Notes))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
