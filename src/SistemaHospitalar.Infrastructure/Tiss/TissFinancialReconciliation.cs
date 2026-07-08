using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Tiss;

public static class TissFinancialReconciliation
{
    public static async Task<FinancialAccount> EnsureReceivableForGuideAsync(
        AppDbContext db,
        TissGuide guide,
        CancellationToken cancellationToken)
    {
        var existing = await db.FinancialAccounts
            .FirstOrDefaultAsync(f => f.TissGuideId == guide.Id && f.IsActive, cancellationToken);

        if (existing is not null)
            return existing;

        var patient = await db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == guide.PatientId, cancellationToken);

        var insurer = await db.HealthInsurances.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == guide.HealthInsuranceId, cancellationToken);

        var account = new FinancialAccount
        {
            Direction = FinancialAccountDirection.Receivable,
            PatientId = guide.PatientId,
            HealthInsuranceId = guide.HealthInsuranceId,
            TissGuideId = guide.Id,
            HospitalizationId = guide.HospitalizationId,
            AppointmentId = guide.AppointmentId,
            Category = FinancialAccountCategory.InsuranceReceivable,
            Description = $"TISS {guide.GuideNumber} — {insurer?.Name ?? "Convênio"}",
            Notes = $"Conta a receber gerada automaticamente da guia {guide.GuideNumber}.",
            Amount = guide.TotalAmount,
            PaidAmount = 0,
            Status = FinancialAccountStatus.Open,
            DueDate = DateTime.UtcNow.AddDays(45),
            ExpectedPaymentMethod = PaymentMethod.BankTransfer,
            CounterpartyName = insurer?.Name,
        };

        db.FinancialAccounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public static async Task ApplyDemonstrativoPaymentAsync(
        AppDbContext db,
        TissGuide guide,
        decimal paidAmount,
        CancellationToken cancellationToken)
    {
        var account = await db.FinancialAccounts
            .FirstOrDefaultAsync(f => f.TissGuideId == guide.Id && f.IsActive, cancellationToken);

        if (account is null && paidAmount > 0)
            account = await EnsureReceivableForGuideAsync(db, guide, cancellationToken);

        if (account is null)
            return;

        var delta = paidAmount - account.PaidAmount;
        if (delta <= 0)
            return;

        account.PaidAmount += delta;
        account.PaidAt = DateTime.UtcNow;
        account.Status = account.PaidAmount >= account.Amount
            ? FinancialAccountStatus.Paid
            : FinancialAccountStatus.PartiallyPaid;
        account.UpdatedAt = DateTime.UtcNow;

        db.FinancialPayments.Add(new FinancialPayment
        {
            FinancialAccountId = account.Id,
            Amount = delta,
            Method = PaymentMethod.BankTransfer,
            PaidAt = DateTime.UtcNow,
            Notes = $"Baixa via demonstrativo TISS — guia {guide.GuideNumber}.",
        });

        if (account.PaidAmount >= account.Amount * 0.99m)
        {
            guide.Status = TissGuideStatus.Paid;
            guide.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task MarkGuidePaidFinancialAsync(
        AppDbContext db,
        TissGuide guide,
        CancellationToken cancellationToken)
    {
        var account = await EnsureReceivableForGuideAsync(db, guide, cancellationToken);
        var remaining = account.Amount - account.PaidAmount;
        if (remaining <= 0)
            return;

        account.PaidAmount = account.Amount;
        account.PaidAt = DateTime.UtcNow;
        account.Status = FinancialAccountStatus.Paid;
        account.UpdatedAt = DateTime.UtcNow;

        db.FinancialPayments.Add(new FinancialPayment
        {
            FinancialAccountId = account.Id,
            Amount = remaining,
            Method = PaymentMethod.BankTransfer,
            PaidAt = DateTime.UtcNow,
            Notes = $"Baixa manual vinculada à guia {guide.GuideNumber}.",
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
