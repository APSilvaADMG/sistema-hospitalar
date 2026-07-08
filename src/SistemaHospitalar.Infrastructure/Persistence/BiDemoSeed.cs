using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Services;
using SistemaHospitalar.Infrastructure.Time;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Histórico de demonstração para painéis BI: receitas/despesas mensais, altas e inadimplência.
/// Idempotente — marcador <see cref="DemoMarker"/> em Notes.
/// </summary>
public static class BiDemoSeed
{
    public const string DemoMarker = "bi-demo-v1";

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
            .Take(40)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var professionals = await db.Professionals
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Take(8)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var beds = await db.Beds
            .AsNoTracking()
            .Where(b => b.IsActive)
            .Take(30)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        if (patients.Count == 0 || professionals.Count == 0)
        {
            logger.LogWarning("BiDemoSeed: pacientes ou profissionais insuficientes.");
            return;
        }

        logger.LogInformation("Aplicando histórico BI de demonstração...");

        var rnd = new Random(20260706);
        var todayBrazil = HospitalTime.TodayInBrazil;
        var monthStartBrazil = new DateOnly(todayBrazil.Year, todayBrazil.Month, 1);

        var accounts = new List<FinancialAccount>();
        var payments = new List<FinancialPayment>();
        var hospitalizations = new List<Hospitalization>();
        var appointments = new List<Appointment>();
        var slotAllocator = new ProfessionalSlotAllocator();
        var existingAppointments = await db.Appointments
            .AsNoTracking()
            .Where(a => a.IsActive
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow)
            .Select(a => new
            {
                a.ProfessionalId,
                a.ScheduledAt,
                a.DurationMinutes,
                a.Status,
                a.IsActive
            })
            .ToListAsync(cancellationToken);

        slotAllocator.SeedFromExisting(existingAppointments.Select(a =>
            (a.ProfessionalId, a.ScheduledAt, a.DurationMinutes, a.Status, a.IsActive)));

        var categories = new[]
        {
            FinancialAccountCategory.Consultation,
            FinancialAccountCategory.Exam,
            FinancialAccountCategory.Hospitalization,
            FinancialAccountCategory.Copayment,
        };

        for (var monthOffset = 5; monthOffset >= 0; monthOffset--)
        {
            var refMonth = monthStartBrazil.AddMonths(-monthOffset);
            var daysInMonth = DateTime.DaysInMonth(refMonth.Year, refMonth.Month);
            var paymentsThisMonth = rnd.Next(8, 16);

            for (var i = 0; i < paymentsThisMonth; i++)
            {
                var day = rnd.Next(1, daysInMonth + 1);
                var localPaid = new DateTime(refMonth.Year, refMonth.Month, day, rnd.Next(8, 18), rnd.Next(0, 60), 0, DateTimeKind.Unspecified);
                var paidAt = HospitalTime.BrazilLocalToUtc(localPaid);
                var amount = rnd.Next(180, 4501);
                var category = categories[rnd.Next(categories.Length)];

                var account = new FinancialAccount
                {
                    Direction = FinancialAccountDirection.Receivable,
                    PatientId = patients[rnd.Next(patients.Count)],
                    Category = category,
                    Description = $"Receita BI demo — {category} {refMonth:MM/yyyy}",
                    Amount = amount,
                    PaidAmount = amount,
                    Status = FinancialAccountStatus.Paid,
                    PaidAt = paidAt,
                    DueDate = paidAt,
                    Notes = DemoMarker,
                };
                accounts.Add(account);
                payments.Add(new FinancialPayment
                {
                    FinancialAccount = account,
                    Amount = amount,
                    Method = PaymentMethod.Pix,
                    PaidAt = paidAt,
                    Notes = DemoMarker,
                });
            }

            var expensesThisMonth = rnd.Next(4, 9);
            for (var i = 0; i < expensesThisMonth; i++)
            {
                var day = rnd.Next(1, daysInMonth + 1);
                var localPaid = new DateTime(refMonth.Year, refMonth.Month, day, rnd.Next(8, 18), 0, 0, DateTimeKind.Unspecified);
                var paidAt = HospitalTime.BrazilLocalToUtc(localPaid);
                var amount = rnd.Next(500, 12001);

                var account = new FinancialAccount
                {
                    Direction = FinancialAccountDirection.Payable,
                    CounterpartyName = "Fornecedor BI demo",
                    Category = FinancialAccountCategory.Utilities,
                    Description = $"Despesa operacional BI — {refMonth:MM/yyyy}",
                    Amount = amount,
                    PaidAmount = amount,
                    Status = FinancialAccountStatus.Paid,
                    PaidAt = paidAt,
                    DueDate = paidAt,
                    Notes = DemoMarker,
                };
                accounts.Add(account);
                payments.Add(new FinancialPayment
                {
                    FinancialAccount = account,
                    Amount = amount,
                    Method = PaymentMethod.BankTransfer,
                    PaidAt = paidAt,
                    Notes = DemoMarker,
                });
            }

            var apptsThisMonth = rnd.Next(12, 28);
            for (var i = 0; i < apptsThisMonth; i++)
            {
                var day = rnd.Next(1, daysInMonth + 1);
                var localScheduled = new DateTime(refMonth.Year, refMonth.Month, day, rnd.Next(7, 18), rnd.Next(0, 4) * 15, 0, DateTimeKind.Unspecified);
                var preferredUtc = HospitalTime.BrazilLocalToUtc(localScheduled);
                var professionalId = professionals[rnd.Next(professionals.Count)];

                if (!slotAllocator.TryAllocateSlot(
                        professionalId,
                        preferredUtc,
                        AppointmentKind.Consulta,
                        out var allocatedStart,
                        rnd))
                {
                    continue;
                }

                appointments.Add(new Appointment
                {
                    PatientId = patients[rnd.Next(patients.Count)],
                    ProfessionalId = professionalId,
                    ScheduledAt = allocatedStart,
                    DurationMinutes = AppointmentDurationRules.ConsultaMinutes,
                    Status = AppointmentStatus.Completed,
                    Notes = DemoMarker,
                });
            }
        }

        for (var i = 0; i < 10; i++)
        {
            var amount = rnd.Next(400, 8001);
            accounts.Add(new FinancialAccount
            {
                Direction = FinancialAccountDirection.Receivable,
                PatientId = patients[rnd.Next(patients.Count)],
                Category = FinancialAccountCategory.Consultation,
                Description = $"Título vencido BI demo #{i + 1}",
                Amount = amount,
                PaidAmount = rnd.NextDouble() < 0.3 ? Math.Round(amount * 0.2m, 2) : 0,
                Status = rnd.NextDouble() < 0.3 ? FinancialAccountStatus.PartiallyPaid : FinancialAccountStatus.Open,
                DueDate = DateTime.UtcNow.AddDays(-rnd.Next(5, 90)),
                Notes = DemoMarker,
            });
        }

        if (beds.Count > 0)
        {
            for (var i = 0; i < 36; i++)
            {
                var stayDays = rnd.Next(2, 16);
                var monthOffset = rnd.Next(0, 6);
                var refMonth = monthStartBrazil.AddMonths(-monthOffset);
                var day = rnd.Next(1, DateTime.DaysInMonth(refMonth.Year, refMonth.Month) - stayDays);
                var localAdmitted = new DateTime(refMonth.Year, refMonth.Month, day, rnd.Next(6, 22), 0, 0, DateTimeKind.Unspecified);
                var admittedAt = HospitalTime.BrazilLocalToUtc(localAdmitted);
                var dischargedAt = admittedAt.AddDays(stayDays);

                hospitalizations.Add(new Hospitalization
                {
                    PatientId = patients[rnd.Next(patients.Count)],
                    BedId = beds[rnd.Next(beds.Count)],
                    ProfessionalId = professionals[rnd.Next(professionals.Count)],
                    AdmittedAt = admittedAt,
                    DischargedAt = dischargedAt,
                    Status = HospitalizationStatus.Discharged,
                    Reason = "Internação histórica BI demo",
                    Diagnosis = "Alta programada — seed",
                    Notes = DemoMarker,
                });
            }
        }

        db.FinancialAccounts.AddRange(accounts);
        if (appointments.Count > 0)
        {
            db.Appointments.AddRange(appointments);
        }

        if (hospitalizations.Count > 0)
        {
            db.Hospitalizations.AddRange(hospitalizations);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (payments.Count > 0)
        {
            db.FinancialPayments.AddRange(payments);
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "BiDemoSeed: {Accounts} contas, {Payments} pagamentos, {Appts} consultas, {Hosp} altas ({Marker}).",
            accounts.Count,
            payments.Count,
            appointments.Count,
            hospitalizations.Count,
            DemoMarker);
    }
}
