using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Inventory;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class StockRequisitionService(AppDbContext dbContext) : IStockRequisitionService
{
    public async Task<IReadOnlyList<StockRequisitionDto>> GetRequisitionsAsync(
        StockRequisitionStatus? status,
        StockRequisitionPriority? priority,
        DateOnly? dueDateBefore,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockRequisitions
            .AsNoTracking()
            .Where(r => r.IsActive);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(r => r.Priority == priority.Value);
        }

        if (dueDateBefore.HasValue)
        {
            query = query.Where(r => r.DueDate != null && r.DueDate <= dueDateBefore.Value);
        }

        return await query
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new StockRequisitionDto(
                r.Id,
                r.SequenceNumber,
                r.RequestNumber,
                r.RequestingSector,
                r.OriginLocation,
                r.DestinationLocation,
                r.RequestedBy,
                r.RecipientName,
                r.Priority,
                r.DueDate,
                r.Status,
                r.RequestedAt,
                r.Items.Count(i => i.IsActive),
                r.Items.Where(i => i.IsActive).Sum(i => i.Quantity)))
            .ToListAsync(cancellationToken);
    }

    public async Task<StockRequisitionDetailDto?> GetRequisitionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.StockRequisitions
            .AsNoTracking()
            .Where(r => r.Id == id && r.IsActive)
            .Select(r => new StockRequisitionDetailDto(
                r.Id,
                r.SequenceNumber,
                r.RequestNumber,
                r.RequestingSector,
                r.OriginLocation,
                r.DestinationLocation,
                r.RequestedBy,
                r.RecipientName,
                r.Priority,
                r.DueDate,
                r.Notes,
                r.Status,
                r.RequestedAt,
                r.Items
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => new StockRequisitionItemDto(
                        i.Id,
                        i.ProductId,
                        i.Product.Name,
                        i.Product.Sku,
                        i.Product.Unit,
                        i.Product.QuantityOnHand,
                        i.Quantity,
                        i.FulfilledQuantity,
                        i.ItemStatus,
                        i.UnitPrice,
                        i.Notes))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StockRequisitionDetailDto> CreateRequisitionAsync(
        CreateStockRequisitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.RequestedBy, request.RecipientName, request.Items);

        var sequenceNumber = await GetNextSequenceNumberAsync(cancellationToken);
        var requisition = new StockRequisition
        {
            SequenceNumber = sequenceNumber,
            RequestNumber = await GenerateRequestNumberAsync(cancellationToken),
            RequestedBy = request.RequestedBy.Trim(),
            RecipientName = request.RecipientName?.Trim(),
            Priority = request.Priority,
            DueDate = request.DueDate,
            DestinationLocation = request.DestinationLocation?.Trim(),
            Notes = request.Notes?.Trim(),
            Status = StockRequisitionStatus.Pending,
            RequestedAt = DateTime.UtcNow,
        };

        await AddItemsAsync(requisition, request.Items, cancellationToken);

        dbContext.StockRequisitions.Add(requisition);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetRequisitionByIdAsync(requisition.Id, cancellationToken))!;
    }

    public async Task<StockRequisitionDetailDto> UpdateRequisitionAsync(
        Guid id,
        UpdateStockRequisitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.RequestedBy, request.RecipientName, request.Items);

        var requisition = await dbContext.StockRequisitions
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);

        if (requisition is null)
        {
            throw new InvalidOperationException("Requisição não encontrada.");
        }

        if (requisition.Status != StockRequisitionStatus.Pending)
        {
            throw new InvalidOperationException("Somente requisições pendentes podem ser editadas.");
        }

        requisition.RequestedBy = request.RequestedBy.Trim();
        requisition.RecipientName = request.RecipientName?.Trim();
        requisition.Priority = request.Priority;
        requisition.DueDate = request.DueDate;
        requisition.DestinationLocation = request.DestinationLocation?.Trim();
        requisition.Notes = request.Notes?.Trim();
        requisition.UpdatedAt = DateTime.UtcNow;

        foreach (var item in requisition.Items)
        {
            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await AddItemsAsync(requisition, request.Items, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetRequisitionByIdAsync(requisition.Id, cancellationToken))!;
    }

    public async Task<StockRequisitionDetailDto> ApproveRequisitionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var requisition = await GetEditableRequisitionAsync(id, StockRequisitionStatus.Pending, cancellationToken);
        requisition.Status = StockRequisitionStatus.Approved;
        requisition.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetRequisitionByIdAsync(requisition.Id, cancellationToken))!;
    }

    public async Task<StockRequisitionDetailDto> FulfillRequisitionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var requisition = await dbContext.StockRequisitions
            .Include(r => r.Items.Where(i => i.IsActive))
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);

        if (requisition is null)
        {
            throw new InvalidOperationException("Requisição não encontrada.");
        }

        if (requisition.Status != StockRequisitionStatus.Approved)
        {
            throw new InvalidOperationException("Somente requisições aprovadas podem ser atendidas.");
        }

        foreach (var item in requisition.Items.Where(i => i.IsActive))
        {
            if (item.Product.QuantityOnHand < item.Quantity)
            {
                throw new InvalidOperationException(
                    $"Estoque insuficiente para {item.Product.Name}. Saldo: {item.Product.QuantityOnHand} {item.Product.Unit}.");
            }
        }

        foreach (var item in requisition.Items.Where(i => i.IsActive))
        {
            var product = item.Product;

            if (!product.AllowOutboundFromRegister)
            {
                throw new InvalidOperationException("Saída não permitida pelo cadastro deste produto.");
            }

            HospitalBusinessRules.ValidateDispenseQuantity(product.QuantityOnHand, item.Quantity);

            var deductionLines = await LotInventoryHelper.DeductLotsFefoAsync(
                dbContext,
                product,
                item.Quantity,
                null,
                cancellationToken);

            foreach (var line in deductionLines)
            {
                WarehouseRules.ValidateLotTraceabilityForMedication(
                    product.Type,
                    line.BatchNumber,
                    line.ProductLotId);
            }

            product.QuantityOnHand -= item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;
            item.FulfilledQuantity = item.Quantity;
            item.ItemStatus = StockRequisitionStatus.Fulfilled;
            item.UpdatedAt = DateTime.UtcNow;

            foreach (var line in deductionLines)
            {
                dbContext.StockMovements.Add(new StockMovement
                {
                    ProductId = item.ProductId,
                    Type = StockMovementType.Outbound,
                    Quantity = line.Quantity,
                    Reason = "Requisição de estoque",
                    Reference = requisition.RequestNumber,
                    PatientOrSupplier = requisition.DestinationLocation ?? requisition.RequestingSector.ToString(),
                    BatchNumber = line.BatchNumber,
                    ExpiryDate = line.ExpiryDate,
                });
            }
        }

        requisition.Status = StockRequisitionStatus.Fulfilled;
        requisition.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetRequisitionByIdAsync(requisition.Id, cancellationToken))!;
    }

    public async Task CancelRequisitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requisition = await dbContext.StockRequisitions
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);

        if (requisition is null)
        {
            throw new InvalidOperationException("Requisição não encontrada.");
        }

        if (requisition.Status is StockRequisitionStatus.Fulfilled or StockRequisitionStatus.Cancelled)
        {
            throw new InvalidOperationException("Esta requisição não pode ser cancelada.");
        }

        requisition.Status = StockRequisitionStatus.Cancelled;
        requisition.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<StockRequisitionDetailDto> DenyRequisitionAsync(
        Guid id,
        DenyStockRequisitionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Informe o motivo da negativa.");
        }

        var requisition = await dbContext.StockRequisitions
            .Include(r => r.Items.Where(i => i.IsActive))
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);

        if (requisition is null)
        {
            throw new InvalidOperationException("Requisição não encontrada.");
        }

        if (requisition.Status is not (StockRequisitionStatus.Pending or StockRequisitionStatus.Approved))
        {
            throw new InvalidOperationException("Somente requisições pendentes ou aprovadas podem ser negadas.");
        }

        var reason = request.Reason.Trim();
        requisition.Status = StockRequisitionStatus.Denied;
        requisition.Notes = string.IsNullOrWhiteSpace(requisition.Notes)
            ? $"[Negada] {reason}"
            : $"{requisition.Notes}\n[Negada] {reason}";
        requisition.UpdatedAt = DateTime.UtcNow;

        foreach (var item in requisition.Items.Where(i => i.IsActive))
        {
            item.ItemStatus = StockRequisitionStatus.Denied;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetRequisitionByIdAsync(requisition.Id, cancellationToken))!;
    }

    private async Task<StockRequisition> GetEditableRequisitionAsync(
        Guid id,
        StockRequisitionStatus expectedStatus,
        CancellationToken cancellationToken)
    {
        var requisition = await dbContext.StockRequisitions
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);

        if (requisition is null)
        {
            throw new InvalidOperationException("Requisição não encontrada.");
        }

        if (requisition.Status != expectedStatus)
        {
            throw new InvalidOperationException("Status da requisição não permite esta operação.");
        }

        return requisition;
    }

    private async Task AddItemsAsync(
        StockRequisition requisition,
        IReadOnlyList<StockRequisitionItemRequest> items,
        CancellationToken cancellationToken)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            throw new InvalidOperationException("Um ou mais produtos da requisição não foram encontrados.");
        }

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException("Quantidade do produto deve ser maior que zero.");
            }

            var product = products[item.ProductId];
            var unitPrice = item.UnitPrice > 0 ? item.UnitPrice : product.AverageSalePrice;

            requisition.Items.Add(new StockRequisitionItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                ItemStatus = item.ItemStatus,
                UnitPrice = unitPrice,
                Notes = item.Notes?.Trim(),
            });
        }
    }

    private static void ValidateRequest(
        string requestedBy,
        string? recipientName,
        IReadOnlyList<StockRequisitionItemRequest> items)
    {
        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            throw new InvalidOperationException("Informe o solicitante.");
        }

        if (string.IsNullOrWhiteSpace(recipientName))
        {
            throw new InvalidOperationException("Informe o destinatário.");
        }

        if (items.Count == 0)
        {
            throw new InvalidOperationException("Adicione ao menos um produto à requisição.");
        }
    }

    private async Task<int> GetNextSequenceNumberAsync(CancellationToken cancellationToken)
    {
        var max = await dbContext.StockRequisitions.MaxAsync(r => (int?)r.SequenceNumber, cancellationToken);
        return (max ?? 0) + 1;
    }

    private async Task<string> GenerateRequestNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"REQ-{DateTime.UtcNow:yyyyMMdd}";
        var count = await dbContext.StockRequisitions
            .CountAsync(r => r.RequestNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}-{(count + 1):D4}";
    }
}
