using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Lançamentos de demonstração para painéis Feegow: propostas, honorários, TEF, cheques e cartões.
/// Idempotente — marcador <see cref="DemoMarker"/> em Notes.
/// </summary>
public static class FinanceVarietyDemoSeed
{
    public const string DemoMarker = "finance-variety-demo-v1";

    public static async Task EnsureAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await db.FinancialAccounts.AnyAsync(
                f => f.IsActive && f.Notes != null && f.Notes.Contains(DemoMarker),
                cancellationToken))
        {
            return;
        }

        var patients = await db.Patients
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.CreatedAt)
            .Take(30)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (patients.Count == 0)
        {
            logger.LogWarning("FinanceVarietyDemoSeed: nenhum paciente ativo para gerar lançamentos.");
            return;
        }

        logger.LogInformation("Aplicando lançamentos financeiros variados de demonstração...");

        var rnd = new Random(20260706);
        var accounts = new List<FinancialAccount>();
        var payments = new List<FinancialPayment>();
        var marker = DemoMarker;

        var proposalLabels = new[]
        {
            "cirurgia ambulatorial",
            "pacote internação",
            "check-up executivo",
            "parto humanizado",
            "oncologia ambulatorial",
        };

        for (var i = 0; i < 8; i++)
        {
            var label = proposalLabels[rnd.Next(proposalLabels.Length)];
            accounts.Add(new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = patients[rnd.Next(patients.Count)],
                Category = FinancialAccountCategory.Consultation,
                Description = $"Proposta — orçamento {label}",
                Amount = rnd.Next(800, 15001),
                Status = FinancialAccountStatus.Open,
                DueDate = DateTime.UtcNow.AddDays(rnd.Next(7, 60)),
                Notes = marker,
            });
        }

        var professionals = await db.Professionals
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Take(10)
            .Select(p => p.FullName)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < 6; i++)
        {
            var name = professionals.Count > 0
                ? professionals[rnd.Next(professionals.Count)]
                : "Especialista Demo";
            accounts.Add(new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = patients[rnd.Next(patients.Count)],
                Category = FinancialAccountCategory.Consultation,
                Description = $"Honorários médicos — Dr. {name}",
                Amount = rnd.Next(350, 6001),
                Status = rnd.NextDouble() < 0.4 ? FinancialAccountStatus.Paid : FinancialAccountStatus.Open,
                DueDate = DateTime.UtcNow.AddDays(rnd.Next(-10, 45)),
                Notes = marker,
            });
        }

        void AddPaid(string description, string paymentNotes, PaymentMethod method, decimal amount)
        {
            var account = new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = patients[rnd.Next(patients.Count)],
                Category = FinancialAccountCategory.Consultation,
                Description = description,
                Amount = amount,
                PaidAmount = amount,
                Status = FinancialAccountStatus.Paid,
                PaidAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 20)),
                DueDate = DateTime.UtcNow.AddDays(-rnd.Next(1, 20)),
                ExpectedPaymentMethod = method,
                Notes = $"{marker}|{paymentNotes}",
            };
            accounts.Add(account);
            payments.Add(new FinancialPayment
            {
                FinancialAccount = account,
                Amount = amount,
                Method = method,
                PaidAt = account.PaidAt!.Value,
                Notes = $"{marker}|{paymentNotes}",
            });
        }

        for (var i = 0; i < 4; i++)
        {
            AddPaid(
                $"Recebimento consulta — cartão crédito TEF #{i + 1}",
                "TEF cartão crédito",
                PaymentMethod.CreditCard,
                rnd.Next(150, 1201));
        }

        for (var i = 0; i < 3; i++)
        {
            AddPaid(
                $"Recebimento exame — cartão débito TEF #{i + 1}",
                "TEF débito",
                PaymentMethod.DebitCard,
                rnd.Next(80, 601));
        }

        for (var i = 0; i < 3; i++)
        {
            var chequeNo = rnd.Next(10000, 99999);
            AddPaid(
                $"Recebimento cheque nº {chequeNo}",
                $"cheque nº {chequeNo}",
                PaymentMethod.Cash,
                rnd.Next(300, 2501));
        }

        for (var i = 0; i < 4; i++)
        {
            AddPaid(
                $"Recebimento cartão crédito — ambulatório #{i + 1}",
                "cartão crédito",
                PaymentMethod.CreditCard,
                rnd.Next(120, 901));
        }

        db.FinancialAccounts.AddRange(accounts);
        await db.SaveChangesAsync(cancellationToken);

        if (payments.Count > 0)
        {
            db.FinancialPayments.AddRange(payments);
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "FinanceVarietyDemoSeed: {Accounts} contas e {Payments} pagamentos ({Marker}).",
            accounts.Count,
            payments.Count,
            marker);
    }
}
