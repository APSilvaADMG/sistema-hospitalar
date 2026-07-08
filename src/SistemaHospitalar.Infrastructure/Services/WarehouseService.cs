using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Inventory;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class WarehouseService(AppDbContext dbContext) : IWarehouseService
{
    public async Task<WarehouseDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var expiringBefore = today.AddDays(30);

        var totalProducts = await dbContext.Products.CountAsync(p => p.IsActive, cancellationToken);
        var lowStockCount = await dbContext.Products
            .CountAsync(p => p.IsActive && p.QuantityOnHand <= p.MinimumStock, cancellationToken);
        var expiringLotsCount = await dbContext.ProductLots
            .CountAsync(
                l => l.IsActive
                    && l.QuantityOnHand > 0
                    && l.ExpiryDate != null
                    && l.ExpiryDate <= expiringBefore,
                cancellationToken);
        var pendingRequisitions = await dbContext.StockRequisitions
            .CountAsync(r => r.IsActive && r.Status == StockRequisitionStatus.Pending, cancellationToken);

        var todayInbound = await dbContext.StockMovements
            .Where(m => m.Type == StockMovementType.Inbound && m.CreatedAt >= todayStart)
            .SumAsync(m => (decimal?)m.Quantity, cancellationToken) ?? 0m;
        var todayOutbound = await dbContext.StockMovements
            .Where(m => m.Type == StockMovementType.Outbound && m.CreatedAt >= todayStart)
            .SumAsync(m => (decimal?)m.Quantity, cancellationToken) ?? 0m;

        return new WarehouseDashboardDto(
            totalProducts,
            lowStockCount,
            expiringLotsCount,
            todayInbound,
            todayOutbound,
            pendingRequisitions);
    }

    public async Task<IReadOnlyList<ProductLotDto>> GetLotsAsync(
        Guid? productId,
        int? expiringWithinDays,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProductLots.AsNoTracking().Where(l => l.IsActive && l.QuantityOnHand > 0);

        if (productId.HasValue)
        {
            query = query.Where(l => l.ProductId == productId.Value);
        }

        if (expiringWithinDays.HasValue)
        {
            var limit = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(expiringWithinDays.Value);
            query = query.Where(l => l.ExpiryDate != null && l.ExpiryDate <= limit);
        }

        return await MapLotsAsync(query.OrderBy(l => l.ExpiryDate), cancellationToken);
    }

    public async Task<IReadOnlyList<ProductLotDto>> GetExpiringLotsAsync(
        int days,
        CancellationToken cancellationToken = default)
        => await GetLotsAsync(null, days, cancellationToken);

    public async Task<IReadOnlyList<ProductDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.QuantityOnHand <= p.MinimumStock)
            .OrderBy(p => p.QuantityOnHand - p.MinimumStock)
            .ThenBy(p => p.Name)
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

    public async Task<IReadOnlyList<SectorConsumptionDto>> GetConsumptionBySectorAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var issueRows = await dbContext.StockIssues
            .AsNoTracking()
            .Where(i => i.IsActive && i.CreatedAt >= fromUtc && i.CreatedAt < toUtc)
            .SelectMany(i => i.Items.Where(it => it.IsActive).Select(it => new { i.SectorName, it.Quantity }))
            .GroupBy(x => x.SectorName)
            .Select(g => new SectorConsumptionDto(g.Key, g.Sum(x => x.Quantity), g.Count()))
            .ToListAsync(cancellationToken);

        var requisitionRows = await dbContext.StockRequisitions
            .AsNoTracking()
            .Where(r => r.IsActive
                && r.Status == StockRequisitionStatus.Fulfilled
                && r.UpdatedAt >= fromUtc
                && r.UpdatedAt < toUtc)
            .Select(r => new
            {
                Sector = r.DestinationLocation ?? r.RequestingSector.ToString(),
                Qty = r.Items.Where(i => i.IsActive).Sum(i => i.FulfilledQuantity),
            })
            .GroupBy(x => x.Sector)
            .Select(g => new SectorConsumptionDto(g.Key, g.Sum(x => x.Qty), g.Count()))
            .ToListAsync(cancellationToken);

        return issueRows
            .Concat(requisitionRows)
            .GroupBy(x => x.SectorName)
            .Select(g => new SectorConsumptionDto(
                g.Key,
                g.Sum(x => x.TotalQuantity),
                g.Sum(x => x.MovementCount)))
            .OrderByDescending(x => x.TotalQuantity)
            .ToList();
    }

    public async Task<StockReceiptDto> CreateReceiptAsync(
        CreateStockReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SupplierName))
        {
            throw new InvalidOperationException("Informe o fornecedor.");
        }

        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("Adicione ao menos um item à entrada.");
        }

        var receipt = new StockReceipt
        {
            SupplierName = request.SupplierName.Trim(),
            SupplierCnpj = NormalizeDigits(request.SupplierCnpj),
            InvoiceNumber = request.InvoiceNumber?.Trim(),
            InvoiceSeries = request.InvoiceSeries?.Trim(),
            InvoiceIssueDate = request.InvoiceIssueDate,
            NfeAccessKey = NormalizeDigits(request.NfeAccessKey),
            ReceivedAt = request.ReceivedAt ?? DateTime.UtcNow,
            FreightAmount = request.FreightAmount,
            DiscountAmount = request.DiscountAmount,
            PaymentCondition = request.PaymentCondition?.Trim(),
            Notes = request.Notes?.Trim(),
            ReceivedByUserName = request.ReceivedByUserName?.Trim(),
        };

        decimal totalAmount = 0;

        foreach (var itemReq in request.Items)
        {
            if (itemReq.Quantity <= 0)
            {
                throw new InvalidOperationException("Quantidade do item deve ser maior que zero.");
            }

            if (string.IsNullOrWhiteSpace(itemReq.BatchNumber))
            {
                throw new InvalidOperationException("Informe o lote de cada item.");
            }

            var product = await dbContext.Products.FirstOrDefaultAsync(
                p => p.Id == itemReq.ProductId && p.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Produto não encontrado.");

            WarehouseRules.ValidateDisposableNoReturn(product.Category, "Entrada NF");

            var lineTotal = itemReq.Quantity * itemReq.UnitPrice;
            totalAmount += lineTotal;

            var receiptItem = new StockReceiptItem
            {
                ProductId = product.Id,
                BatchNumber = itemReq.BatchNumber.Trim(),
                ExpiryDate = itemReq.ExpiryDate,
                Quantity = itemReq.Quantity,
                UnitPrice = itemReq.UnitPrice,
                LineTotal = lineTotal,
                Ncm = itemReq.Ncm?.Trim(),
                Cfop = itemReq.Cfop?.Trim(),
            };
            receipt.Items.Add(receiptItem);

            product.QuantityOnHand += itemReq.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            var lot = await LotInventoryHelper.UpsertLotFromInboundAsync(
                dbContext,
                product,
                itemReq.BatchNumber,
                itemReq.ExpiryDate,
                itemReq.Quantity,
                itemReq.Manufacturer ?? product.Manufacturer,
                itemReq.LocationName ?? product.DefaultLocation,
                itemReq.UnitPrice,
                cancellationToken);

            receiptItem.ProductLot = lot;

            dbContext.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                Type = StockMovementType.Inbound,
                Quantity = itemReq.Quantity,
                Reason = "Entrada NF — almoxarifado",
                Reference = receipt.InvoiceNumber,
                PatientOrSupplier = receipt.SupplierName,
                ResponsibleName = receipt.ReceivedByUserName,
                BatchNumber = receiptItem.BatchNumber,
                ExpiryDate = receiptItem.ExpiryDate,
                InvoiceNumber = receipt.InvoiceNumber,
                UnitPrice = itemReq.UnitPrice,
                Location = itemReq.LocationName,
            });
        }

        receipt.TotalAmount = totalAmount + request.FreightAmount - request.DiscountAmount;
        dbContext.StockReceipts.Add(receipt);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetReceiptByIdAsync(receipt.Id, cancellationToken))!;
    }

    public async Task<StockIssueDto> CreateIssueAsync(
        CreateStockIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SectorName))
        {
            throw new InvalidOperationException("Informe o setor.");
        }

        if (string.IsNullOrWhiteSpace(request.ResponsibleName))
        {
            throw new InvalidOperationException("Informe o responsável.");
        }

        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("Adicione ao menos um item à saída.");
        }

        var issue = new StockIssue
        {
            SectorName = request.SectorName.Trim(),
            ResponsibleName = request.ResponsibleName.Trim(),
            IssueType = request.IssueType,
            PatientId = request.PatientId,
            HospitalizationId = request.HospitalizationId,
            Notes = request.Notes?.Trim(),
        };

        foreach (var itemReq in request.Items)
        {
            var product = await dbContext.Products.FirstOrDefaultAsync(
                p => p.Id == itemReq.ProductId && p.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Produto não encontrado.");

            if (!product.AllowOutboundFromRegister)
            {
                throw new InvalidOperationException("Saída não permitida pelo cadastro deste produto.");
            }

            HospitalBusinessRules.ValidateDispenseQuantity(product.QuantityOnHand, itemReq.Quantity);

            var hasLots = await dbContext.ProductLots.AnyAsync(
                l => l.ProductId == product.Id && l.IsActive && l.QuantityOnHand > 0,
                cancellationToken);

            WarehouseRules.ValidateLotTraceabilityForMedication(
                product.Type,
                null,
                hasLots ? itemReq.ProductLotId ?? Guid.Empty : itemReq.ProductLotId);

            if (product.Type == ProductType.Medication && hasLots && !itemReq.ProductLotId.HasValue)
            {
                throw new InvalidOperationException(
                    $"[{BusinessRuleCodes.LotTraceabilityRequired}] Medicamentos exigem rastreabilidade por lote na saída.");
            }

            var lotId = itemReq.ProductLotId;
            var deductionLines = await LotInventoryHelper.DeductLotsFefoAsync(
                dbContext,
                product,
                itemReq.Quantity,
                lotId,
                cancellationToken);

            product.QuantityOnHand -= itemReq.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            var issueItem = new StockIssueItem
            {
                ProductId = product.Id,
                ProductLotId = deductionLines.FirstOrDefault()?.ProductLotId,
                Quantity = itemReq.Quantity,
            };
            issue.Items.Add(issueItem);

            foreach (var line in deductionLines)
            {
                WarehouseRules.ValidateLotTraceabilityForMedication(
                    product.Type,
                    line.BatchNumber,
                    line.ProductLotId);

                dbContext.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    Type = StockMovementType.Outbound,
                    Quantity = line.Quantity,
                    Reason = $"Saída almoxarifado — {request.IssueType}",
                    Reference = issue.Id.ToString(),
                    PatientOrSupplier = request.SectorName,
                    ResponsibleName = request.ResponsibleName,
                    UserName = request.UserName?.Trim(),
                    BatchNumber = line.BatchNumber,
                    ExpiryDate = line.ExpiryDate,
                    Location = request.SectorName,
                });
            }
        }

        dbContext.StockIssues.Add(issue);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetIssueByIdAsync(issue.Id, cancellationToken))!;
    }

    private async Task<IReadOnlyList<ProductLotDto>> MapLotsAsync(
        IQueryable<ProductLot> query,
        CancellationToken cancellationToken)
    {
        var warningDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        return await query
            .Select(l => new ProductLotDto(
                l.Id,
                l.ProductId,
                l.Product.Name,
                l.Product.Sku,
                l.BatchNumber,
                l.ExpiryDate,
                l.Manufacturer,
                l.QuantityOnHand,
                l.LocationName,
                l.UnitCost,
                l.ExpiryDate != null && l.ExpiryDate <= warningDate,
                l.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<StockReceiptDto?> GetReceiptByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.StockReceipts
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new StockReceiptDto(
                r.Id,
                r.SupplierName,
                r.SupplierCnpj,
                r.InvoiceNumber,
                r.InvoiceSeries,
                r.InvoiceIssueDate,
                r.NfeAccessKey,
                r.ReceivedAt,
                r.TotalAmount,
                r.FreightAmount,
                r.DiscountAmount,
                r.PaymentCondition,
                r.Notes,
                r.ReceivedByUserName,
                r.Items
                    .Where(i => i.IsActive)
                    .Select(i => new StockReceiptItemDto(
                        i.Id,
                        i.ProductId,
                        i.Product.Name,
                        i.ProductLotId,
                        i.BatchNumber,
                        i.ExpiryDate,
                        i.Quantity,
                        i.UnitPrice,
                        i.LineTotal,
                        i.Ncm,
                        i.Cfop))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? NormalizeDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return new string(value.Where(char.IsDigit).ToArray());
    }

    private async Task<StockIssueDto?> GetIssueByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.StockIssues
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new StockIssueDto(
                i.Id,
                i.SectorName,
                i.ResponsibleName,
                i.IssueType,
                i.PatientId,
                i.HospitalizationId,
                i.Notes,
                i.CreatedAt,
                i.Items
                    .Where(it => it.IsActive)
                    .Select(it => new StockIssueItemDto(
                        it.Id,
                        it.ProductId,
                        it.Product.Name,
                        it.ProductLotId,
                        it.ProductLot != null ? it.ProductLot.BatchNumber : null,
                        it.Quantity))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
