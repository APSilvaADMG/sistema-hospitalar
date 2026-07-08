using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.Financial;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class MiscellaneousReceiptService(AppDbContext dbContext) : IMiscellaneousReceiptService
{
    public async Task<PagedResult<MiscellaneousReceiptDto>> SearchAsync(
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.MiscellaneousReceipts
            .AsNoTracking()
            .Where(r => r.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.ReceiptNumber.Contains(term)
                || r.PayerName.Contains(term)
                || r.ReceiverName.Contains(term)
                || r.Description.Contains(term)
                || (r.Reference != null && r.Reference.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.ReceiptDate)
            .ThenByDescending(r => r.ReceiptNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapReceipt())
            .ToListAsync(cancellationToken);

        return new PagedResult<MiscellaneousReceiptDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<MiscellaneousReceiptDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.MiscellaneousReceipts
            .AsNoTracking()
            .Where(r => r.IsActive && r.Id == id)
            .Select(MapReceipt())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MiscellaneousReceiptDto> CreateAsync(
        CreateMiscellaneousReceiptRequest request,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.PayerName, request.ReceiverName, request.Amount, request.Description);

        var receipt = new MiscellaneousReceipt
        {
            ReceiptNumber = await GenerateReceiptNumberAsync(cancellationToken),
            ReceiptDate = request.ReceiptDate.Date,
            PayerName = request.PayerName.Trim(),
            ReceiverName = request.ReceiverName.Trim(),
            Amount = request.Amount,
            Description = request.Description.Trim(),
            PaymentMethod = request.PaymentMethod,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            CreatedByUserId = userId,
        };

        dbContext.MiscellaneousReceipts.Add(receipt);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(receipt.Id, cancellationToken))!;
    }

    public async Task<MiscellaneousReceiptDto?> UpdateAsync(
        Guid id,
        UpdateMiscellaneousReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.PayerName, request.ReceiverName, request.Amount, request.Description);

        var receipt = await dbContext.MiscellaneousReceipts
            .FirstOrDefaultAsync(r => r.IsActive && r.Id == id, cancellationToken);
        if (receipt is null)
        {
            return null;
        }

        receipt.ReceiptDate = request.ReceiptDate.Date;
        receipt.PayerName = request.PayerName.Trim();
        receipt.ReceiverName = request.ReceiverName.Trim();
        receipt.Amount = request.Amount;
        receipt.Description = request.Description.Trim();
        receipt.PaymentMethod = request.PaymentMethod;
        receipt.Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim();
        receipt.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.MiscellaneousReceipts
            .FirstOrDefaultAsync(r => r.IsActive && r.Id == id, cancellationToken);
        if (receipt is null)
        {
            return false;
        }

        receipt.IsActive = false;
        receipt.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateRequest(string payerName, string receiverName, decimal amount, string description)
    {
        if (string.IsNullOrWhiteSpace(payerName))
        {
            throw new InvalidOperationException("Informe o nome do pagador.");
        }

        if (string.IsNullOrWhiteSpace(receiverName))
        {
            throw new InvalidOperationException("Informe o nome do recebedor.");
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("Informe um valor maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException("Informe a descrição ou histórico do recibo.");
        }
    }

    private async Task<string> GenerateReceiptNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"RD-{year}-";
        var lastNumber = await dbContext.MiscellaneousReceipts
            .AsNoTracking()
            .Where(r => r.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReceiptNumber)
            .Select(r => r.ReceiptNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            var suffix = lastNumber[prefix.Length..];
            if (int.TryParse(suffix, out var parsed))
            {
                sequence = parsed + 1;
            }
        }

        return $"{prefix}{sequence:D5}";
    }

    private static System.Linq.Expressions.Expression<Func<MiscellaneousReceipt, MiscellaneousReceiptDto>> MapReceipt()
        => r => new MiscellaneousReceiptDto(
            r.Id,
            r.ReceiptNumber,
            r.ReceiptDate,
            r.PayerName,
            r.ReceiverName,
            r.Amount,
            r.Description,
            r.PaymentMethod,
            r.Reference,
            r.CreatedAt);
}
