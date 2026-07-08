using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Purchasing;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class PurchasingService(
    AppDbContext dbContext,
    INotificationService notificationService) : IPurchasingService
{
    private static readonly Dictionary<string, decimal> DefaultUnitPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MED-DIP500"] = 0.18m,
        ["MED-SF500"] = 4.50m,
        ["MED-OME20"] = 0.35m,
        ["SUP-LUV-M"] = 28m,
        ["SUP-GAZ"] = 2.40m,
        ["SUP-EQP"] = 12m,
        ["LAB-REA01"] = 85m,
        ["IMG-CON100"] = 42m,
        ["CIR-KIT01"] = 18m,
        ["LAV-ROUP"] = 35m,
        ["ENG-MANUT"] = 120m,
        ["NUT-DIETA"] = 6.50m,
        ["CCIH-DESINF"] = 22m,
        ["HOTEL-LEN"] = 48m,
    };

    public async Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SupplierDto(s.Id, s.Name, s.Cnpj, s.Email, s.Phone, s.ContactName))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierDto> CreateSupplierAsync(
        CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            Cnpj = request.Cnpj?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            ContactName = request.ContactName?.Trim()
        };

        dbContext.Suppliers.Add(supplier);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SupplierDto(
            supplier.Id, supplier.Name, supplier.Cnpj, supplier.Email, supplier.Phone, supplier.ContactName);
    }

    public async Task<PurchaseCreateSuggestionsDto> GetCreateSuggestionsAsync(
        PurchaseSector? sector,
        PurchasePriority? priority,
        CancellationToken cancellationToken = default)
    {
        var selectedSector = sector ?? PurchaseSector.Pharmacy;
        var selectedPriority = priority ?? PurchasePriority.Normal;

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var suppliers = await GetSuppliersAsync(cancellationToken);
        var lastPrices = await GetLastUnitPricesAsync(cancellationToken);

        var relevant = FilterProductsBySector(products, selectedSector);
        var lowStock = relevant
            .Where(p => p.QuantityOnHand < p.MinimumStock)
            .Select(p => MapSuggestedItem(p, lastPrices, "Estoque abaixo do mínimo"))
            .OrderByDescending(i => i.IsLowStock)
            .ThenBy(i => i.ProductName)
            .ToList();

        var kitItems = BuildKitItems(selectedSector, products, lastPrices);
        var suggestedSupplier = SuggestSupplier(selectedSector, suppliers);
        var deliveryDays = SuggestDeliveryDays(selectedPriority, lowStock.Count);

        return new PurchaseCreateSuggestionsDto(
            BuildSectorPresets(),
            selectedSector,
            suggestedSupplier,
            deliveryDays,
            lowStock,
            kitItems,
            suppliers);
    }

    public async Task<IReadOnlyList<PurchaseOrderDto>> GetOrdersAsync(
        PurchaseOrderStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.PurchaseOrders.AsNoTracking().Where(o => o.IsActive);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.OrderedAt)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.SupplierId,
                SupplierName = o.Supplier.Name,
                o.Sector,
                o.Priority,
                o.RequestedBy,
                o.Justification,
                o.Status,
                o.OrderedAt,
                o.ExpectedAt,
                o.TotalAmount,
                o.Notes
            })
            .ToListAsync(cancellationToken);

        var result = new List<PurchaseOrderDto>();
        foreach (var o in orders)
        {
            var items = await GetOrderItemsAsync(o.Id, cancellationToken);
            result.Add(new PurchaseOrderDto(
                o.Id, o.OrderNumber, o.SupplierId, o.SupplierName, o.Sector, o.Priority,
                o.RequestedBy, o.Justification, o.Status, o.OrderedAt, o.ExpectedAt,
                o.TotalAmount, o.Notes, items));
        }

        return result;
    }

    public async Task<PurchaseOrderDto> CreateOrderAsync(
        CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("Informe ao menos um item.");
        }

        if (string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            throw new InvalidOperationException("Informe o solicitante.");
        }

        var supplierExists = await dbContext.Suppliers
            .AnyAsync(s => s.Id == request.SupplierId && s.IsActive, cancellationToken);

        if (!supplierExists)
        {
            throw new InvalidOperationException("Fornecedor não encontrado.");
        }

        var orderNumber = await GenerateOrderNumberAsync(cancellationToken);
        var expectedAt = request.ExpectedAt?.ToUniversalTime()
            ?? DateTime.UtcNow.AddDays(SuggestDeliveryDays(request.Priority, request.Items.Count));

        var order = new PurchaseOrder
        {
            OrderNumber = orderNumber,
            SupplierId = request.SupplierId,
            Sector = request.Sector,
            Priority = request.Priority,
            RequestedBy = request.RequestedBy.Trim(),
            Justification = string.IsNullOrWhiteSpace(request.Justification) ? null : request.Justification.Trim(),
            ExpectedAt = expectedAt,
            Notes = request.Notes?.Trim()
        };

        decimal total = 0;
        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0 || item.UnitPrice < 0)
            {
                throw new InvalidOperationException("Quantidade e preço devem ser válidos.");
            }

            var productExists = await dbContext.Products
                .AnyAsync(p => p.Id == item.ProductId && p.IsActive, cancellationToken);

            if (!productExists)
            {
                throw new InvalidOperationException("Produto não encontrado.");
            }

            order.Items.Add(new PurchaseOrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
            total += item.Quantity * item.UnitPrice;
        }

        order.TotalAmount = total;
        dbContext.PurchaseOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAdminsAsync(
            "Compras — novo pedido",
            $"Pedido {order.OrderNumber} ({SectorLabel(request.Sector)}) criado por {order.RequestedBy}.",
            request.Priority == PurchasePriority.Critical ? NotificationType.Alert
                : request.Priority == PurchasePriority.Urgent ? NotificationType.Warning
                : NotificationType.Info,
            "PurchaseOrder",
            order.Id,
            cancellationToken);

        return (await GetOrderByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<PurchaseOrderDto?> SendOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);

        if (order is null)
        {
            return null;
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException("Somente pedidos em rascunho podem ser enviados.");
        }

        order.Status = PurchaseOrderStatus.Sent;
        order.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAdminsAsync(
            "Compras — pedido enviado",
            $"Pedido {order.OrderNumber} enviado ao fornecedor.",
            NotificationType.Info,
            "PurchaseOrder",
            order.Id,
            cancellationToken);

        return await GetOrderByIdAsync(id, cancellationToken);
    }

    public async Task<PurchaseOrderDto?> ReceiveOrderAsync(
        Guid id, ReceivePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);

        if (order is null)
        {
            return null;
        }

        if (order.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Pedido não pode receber mercadorias neste status.");
        }

        foreach (var receive in request.Items)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == receive.ItemId);
            if (item is null || receive.Quantity <= 0)
            {
                continue;
            }

            var remaining = item.Quantity - item.ReceivedQuantity;
            if (receive.Quantity > remaining)
            {
                throw new InvalidOperationException($"Quantidade excede pendente para o item {item.ProductId}.");
            }

            item.ReceivedQuantity += receive.Quantity;
            item.UpdatedAt = DateTime.UtcNow;

            var product = await dbContext.Products.FirstAsync(p => p.Id == item.ProductId, cancellationToken);
            product.QuantityOnHand += receive.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            dbContext.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                Type = StockMovementType.Inbound,
                Quantity = receive.Quantity,
                Reason = "Recebimento compras",
                Reference = order.OrderNumber
            });
        }

        var allReceived = order.Items.All(i => i.ReceivedQuantity >= i.Quantity);
        var anyReceived = order.Items.Any(i => i.ReceivedQuantity > 0);
        order.Status = allReceived
            ? PurchaseOrderStatus.Received
            : anyReceived ? PurchaseOrderStatus.PartiallyReceived : order.Status;
        order.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetOrderByIdAsync(id, cancellationToken);
    }

    private async Task<Dictionary<Guid, decimal>> GetLastUnitPricesAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.PurchaseOrderItems
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new { i.ProductId, i.UnitPrice })
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.First().UnitPrice);
    }

    private static IReadOnlyList<PurchaseSectorPresetDto> BuildSectorPresets() =>
    [
        new(PurchaseSector.Pharmacy, "Farmácia", "Medicamentos e dispensação", 7),
        new(PurchaseSector.Laboratory, "Laboratório", "Reagentes, coleta e insumos de análise", 10),
        new(PurchaseSector.Imaging, "Imagem", "Contraste, filmes e materiais radiológicos", 10),
        new(PurchaseSector.SurgeryCenter, "Centro cirúrgico", "Materiais cirúrgicos e EPI", 5),
        new(PurchaseSector.Icu, "UTI", "Medicamentos críticos e insumos de terapia intensiva", 3),
        new(PurchaseSector.Emergency, "Pronto-socorro", "Medicamentos e insumos de urgência", 2),
        new(PurchaseSector.Nutrition, "Nutrição", "Dietas, suplementos e utensílios", 7),
        new(PurchaseSector.Laundry, "Lavanderia", "Roupas hospitalares e linho", 14),
        new(PurchaseSector.ClinicalEngineering, "Eng. clínica", "Peças e manutenção de equipamentos", 15),
        new(PurchaseSector.InfectionControl, "CCIH", "Desinfetantes e controle de infecção", 7),
        new(PurchaseSector.Hospitality, "Hotelaria", "Enxoval e materiais de acomodação", 10),
        new(PurchaseSector.Nursing, "Enfermagem", "Insumos de enfermaria e curativos", 5),
        new(PurchaseSector.Administration, "Administração", "Materiais gerais e escritório", 10),
    ];

    private static List<Product> FilterProductsBySector(IReadOnlyList<Product> products, PurchaseSector sector)
    {
        var skuPrefixes = sector switch
        {
            PurchaseSector.Pharmacy => new[] { "MED-" },
            PurchaseSector.Laboratory => new[] { "LAB-", "SUP-GAZ", "SUP-LUV" },
            PurchaseSector.Imaging => new[] { "IMG-" },
            PurchaseSector.SurgeryCenter => new[] { "CIR-", "SUP-LUV", "SUP-GAZ" },
            PurchaseSector.Icu => new[] { "MED-SF", "MED-OME", "SUP-EQP", "SUP-GAZ" },
            PurchaseSector.Emergency => new[] { "MED-DIP", "MED-SF", "SUP-LUV", "SUP-GAZ" },
            PurchaseSector.Nutrition => new[] { "NUT-" },
            PurchaseSector.Laundry => new[] { "LAV-" },
            PurchaseSector.ClinicalEngineering => new[] { "ENG-" },
            PurchaseSector.InfectionControl => new[] { "CCIH-", "SUP-LUV" },
            PurchaseSector.Hospitality => new[] { "HOTEL-" },
            PurchaseSector.Nursing => new[] { "SUP-", "MED-SF" },
            _ => Array.Empty<string>()
        };

        if (skuPrefixes.Length == 0)
        {
            return products.ToList();
        }

        return products
            .Where(p => skuPrefixes.Any(prefix => p.Sku.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static IReadOnlyList<PurchaseSuggestedItemDto> BuildKitItems(
        PurchaseSector sector,
        IReadOnlyList<Product> products,
        IReadOnlyDictionary<Guid, decimal> lastPrices)
    {
        var kitSkus = sector switch
        {
            PurchaseSector.Pharmacy => new[] { ("MED-OME20", 120), ("MED-DIP500", 300) },
            PurchaseSector.Laboratory => new[] { ("LAB-REA01", 4), ("SUP-GAZ", 80) },
            PurchaseSector.Imaging => new[] { ("IMG-CON100", 24) },
            PurchaseSector.SurgeryCenter => new[] { ("CIR-KIT01", 40), ("SUP-LUV-M", 15) },
            PurchaseSector.Icu => new[] { ("MED-SF500", 100), ("SUP-EQP", 30) },
            PurchaseSector.Emergency => new[] { ("MED-DIP500", 200), ("SUP-LUV-M", 10) },
            PurchaseSector.Nutrition => new[] { ("NUT-DIETA", 60) },
            PurchaseSector.Laundry => new[] { ("LAV-ROUP", 50) },
            PurchaseSector.ClinicalEngineering => new[] { ("ENG-MANUT", 2) },
            PurchaseSector.InfectionControl => new[] { ("CCIH-DESINF", 20), ("SUP-LUV-M", 8) },
            PurchaseSector.Hospitality => new[] { ("HOTEL-LEN", 40) },
            PurchaseSector.Nursing => new[] { ("SUP-GAZ", 100), ("SUP-LUV-M", 12) },
            _ => new[] { ("SUP-GAZ", 50), ("SUP-LUV-M", 5) }
        };

        var result = new List<PurchaseSuggestedItemDto>();
        foreach (var (sku, qty) in kitSkus)
        {
            var product = products.FirstOrDefault(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
            if (product is null)
            {
                continue;
            }

            var suggested = MapSuggestedItem(product, lastPrices, "Kit sugerido do setor");
            result.Add(suggested with { SuggestedQuantity = qty });
        }

        return result;
    }

    private static PurchaseSuggestedItemDto MapSuggestedItem(
        Product product,
        IReadOnlyDictionary<Guid, decimal> lastPrices,
        string reason)
    {
        var deficit = Math.Max(0, product.MinimumStock - product.QuantityOnHand);
        var suggestedQty = deficit > 0
            ? (int)Math.Ceiling(deficit)
            : Math.Max(1, (int)Math.Ceiling(product.MinimumStock * 0.5m));

        var unitPrice = lastPrices.TryGetValue(product.Id, out var last)
            ? last
            : DefaultUnitPrices.GetValueOrDefault(product.Sku, product.Type == ProductType.Medication ? 0.50m : 15m);

        return new PurchaseSuggestedItemDto(
            product.Id,
            product.Name,
            product.Sku,
            product.Unit,
            product.QuantityOnHand,
            product.MinimumStock,
            product.QuantityOnHand < product.MinimumStock,
            suggestedQty,
            unitPrice,
            reason);
    }

    private static Guid? SuggestSupplier(PurchaseSector sector, IReadOnlyList<SupplierDto> suppliers)
    {
        if (suppliers.Count == 0)
        {
            return null;
        }

        var medSupplier = suppliers.FirstOrDefault(s =>
            s.Name.Contains("Med", StringComparison.OrdinalIgnoreCase)
            || s.Name.Contains("Distrib", StringComparison.OrdinalIgnoreCase));

        var supplySupplier = suppliers.FirstOrDefault(s =>
            s.Name.Contains("Insumo", StringComparison.OrdinalIgnoreCase)
            || s.Name.Contains("Hospital", StringComparison.OrdinalIgnoreCase));

        return sector switch
        {
            PurchaseSector.Pharmacy or PurchaseSector.Icu or PurchaseSector.Emergency => medSupplier?.Id ?? suppliers[0].Id,
            PurchaseSector.Laboratory or PurchaseSector.Imaging or PurchaseSector.SurgeryCenter
                or PurchaseSector.Laundry or PurchaseSector.ClinicalEngineering
                or PurchaseSector.InfectionControl or PurchaseSector.Hospitality
                or PurchaseSector.Nursing => supplySupplier?.Id ?? suppliers[^1].Id,
            _ => suppliers[0].Id
        };
    }

    private static int SuggestDeliveryDays(PurchasePriority priority, int lowStockCount) => priority switch
    {
        PurchasePriority.Critical => 2,
        PurchasePriority.Urgent => lowStockCount >= 3 ? 3 : 5,
        _ => lowStockCount >= 2 ? 7 : 10
    };

    private static string SectorLabel(PurchaseSector sector) =>
        BuildSectorPresets().FirstOrDefault(s => s.Sector == sector)?.Label ?? sector.ToString();

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"PO-{DateTime.UtcNow:yyyyMMdd}";
        var count = await dbContext.PurchaseOrders
            .CountAsync(o => o.OrderNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}-{(count + 1):D4}";
    }

    private async Task<PurchaseOrderDto?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var o = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.OrderNumber,
                x.SupplierId,
                SupplierName = x.Supplier.Name,
                x.Sector,
                x.Priority,
                x.RequestedBy,
                x.Justification,
                x.Status,
                x.OrderedAt,
                x.ExpectedAt,
                x.TotalAmount,
                x.Notes
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (o is null) return null;

        var items = await GetOrderItemsAsync(id, cancellationToken);
        return new PurchaseOrderDto(
            o.Id, o.OrderNumber, o.SupplierId, o.SupplierName, o.Sector, o.Priority,
            o.RequestedBy, o.Justification, o.Status, o.OrderedAt, o.ExpectedAt,
            o.TotalAmount, o.Notes, items);
    }

    private async Task<IReadOnlyList<PurchaseOrderItemDto>> GetOrderItemsAsync(
        Guid orderId, CancellationToken cancellationToken)
    {
        return await dbContext.PurchaseOrderItems
            .AsNoTracking()
            .Where(i => i.PurchaseOrderId == orderId)
            .Select(i => new PurchaseOrderItemDto(
                i.Id, i.ProductId, i.Product.Name, i.Quantity, i.ReceivedQuantity,
                i.UnitPrice, i.Quantity * i.UnitPrice))
            .ToListAsync(cancellationToken);
    }
}
