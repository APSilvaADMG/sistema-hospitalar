using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Pagamentos de recepção do dia para demonstração realista do caixa hospitalar.
/// Idempotente — marcador <see cref="DemoMarker"/>.
/// </summary>
public static class CashSessionDemoSeed
{
    public const string DemoMarker = "cash-demo-v1";

    private static readonly PaymentMethod[] CounterMethods =
    [
        PaymentMethod.Cash,
        PaymentMethod.Pix,
        PaymentMethod.DebitCard,
        PaymentMethod.CreditCard,
    ];

    private static readonly FinancialAccountCategory[] ReceivableCategories =
    [
        FinancialAccountCategory.Consultation,
        FinancialAccountCategory.Exam,
        FinancialAccountCategory.Copayment,
        FinancialAccountCategory.Hospitalization,
    ];

    private static readonly string[] ServiceLabels =
    [
        "Consulta ambulatorial",
        "Exame laboratorial",
        "Coparticipação convênio",
        "Taxa de internação",
        "Procedimento ambulatorial",
        "Retorno médico",
    ];

    public static async Task EnsureAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await db.FinancialPayments.AnyAsync(
                p => p.IsActive
                    && p.Notes != null
                    && p.Notes.Contains(DemoMarker),
                cancellationToken))
        {
            return;
        }

        var todayStart = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var todayEnd = todayStart.AddDays(1);

        var hasTodayCounterPayments = await db.FinancialPayments.AnyAsync(
            p => p.IsActive
                && p.FinancialAccount.IsActive
                && p.PaidAt >= todayStart
                && p.PaidAt < todayEnd
                && CounterMethods.Contains(p.Method),
            cancellationToken);

        if (hasTodayCounterPayments)
        {
            return;
        }

        var patients = await db.Patients
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.CreatedAt)
            .Take(50)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (patients.Count == 0)
        {
            logger.LogWarning("CashSessionDemoSeed: nenhum paciente ativo para gerar pagamentos.");
            return;
        }

        logger.LogInformation("Aplicando pagamentos de recepção do dia para demonstração do caixa...");

        var rnd = new Random(20260706);
        var targetTotal = rnd.Next(120_000, 280_001);
        var paymentCount = rnd.Next(80, 121);
        var amounts = DistributeAmounts(targetTotal, paymentCount, rnd);

        var accounts = new List<FinancialAccount>();
        var payments = new List<FinancialPayment>();

        for (var i = 0; i < paymentCount; i++)
        {
            var amount = amounts[i];
            var method = PickCounterMethod(rnd);
            var paidAt = todayStart
                .AddHours(8 + rnd.Next(0, 10))
                .AddMinutes(rnd.Next(0, 60));

            var account = new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = patients[rnd.Next(patients.Count)],
                Category = ReceivableCategories[rnd.Next(ReceivableCategories.Length)],
                Description = $"{ServiceLabels[rnd.Next(ServiceLabels.Length)]} — recepção",
                Amount = amount,
                PaidAmount = amount,
                Status = FinancialAccountStatus.Paid,
                PaidAt = paidAt,
                DueDate = paidAt,
                ExpectedPaymentMethod = method,
                Notes = DemoMarker,
            };
            accounts.Add(account);

            payments.Add(new FinancialPayment
            {
                FinancialAccount = account,
                Amount = amount,
                Method = method,
                PaidAt = paidAt,
                Notes = DemoMarker,
            });
        }

        db.FinancialAccounts.AddRange(accounts);
        await db.SaveChangesAsync(cancellationToken);
        db.FinancialPayments.AddRange(payments);
        await db.SaveChangesAsync(cancellationToken);

        var sangriaCount = rnd.Next(2, 5);
        var payables = new List<FinancialAccount>();
        var sangriaPayments = new List<FinancialPayment>();

        for (var i = 0; i < sangriaCount; i++)
        {
            var amount = rnd.Next(80, 601);
            var paidAt = todayStart
                .AddHours(9 + rnd.Next(0, 8))
                .AddMinutes(rnd.Next(0, 60));

            var payable = new FinancialAccount
            {
                Direction = FinancialAccountDirection.Payable,
                CounterpartyName = "Caixa recepção",
                Category = FinancialAccountCategory.Other,
                Description = "Sangria — suprimento e troco",
                Amount = amount,
                PaidAmount = amount,
                Status = FinancialAccountStatus.Paid,
                PaidAt = paidAt,
                DueDate = paidAt,
                Notes = DemoMarker,
            };
            payables.Add(payable);

            sangriaPayments.Add(new FinancialPayment
            {
                FinancialAccount = payable,
                Amount = amount,
                Method = PaymentMethod.Cash,
                PaidAt = paidAt,
                Notes = $"{DemoMarker}|sangria",
            });
        }

        db.FinancialAccounts.AddRange(payables);
        await db.SaveChangesAsync(cancellationToken);
        db.FinancialPayments.AddRange(sangriaPayments);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "CashSessionDemoSeed: {ReceivableCount} recebimentos (R$ {Total:N2}) e {SangriaCount} sangrias.",
            paymentCount,
            targetTotal,
            sangriaCount);
    }

    private static PaymentMethod PickCounterMethod(Random rnd)
    {
        var roll = rnd.NextDouble();
        if (roll < 0.18)
        {
            return PaymentMethod.Cash;
        }

        if (roll < 0.56)
        {
            return PaymentMethod.Pix;
        }

        if (roll < 0.78)
        {
            return PaymentMethod.DebitCard;
        }

        return PaymentMethod.CreditCard;
    }

    private static decimal[] DistributeAmounts(decimal targetTotal, int count, Random rnd)
    {
        var weights = Enumerable.Range(0, count).Select(_ => rnd.Next(50, 500)).ToArray();
        var weightSum = weights.Sum();
        var amounts = new decimal[count];
        var allocated = 0m;

        for (var i = 0; i < count - 1; i++)
        {
            var share = decimal.Round(targetTotal * weights[i] / weightSum, 2);
            share = Math.Max(share, 25m);
            amounts[i] = share;
            allocated += share;
        }

        amounts[count - 1] = Math.Max(targetTotal - allocated, 25m);
        return amounts;
    }
}
