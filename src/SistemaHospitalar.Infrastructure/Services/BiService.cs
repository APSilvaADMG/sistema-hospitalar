using Microsoft.EntityFrameworkCore;

using SistemaHospitalar.Application.DTOs.Bi;

using SistemaHospitalar.Application.Interfaces;

using SistemaHospitalar.Domain.Enums;

using SistemaHospitalar.Infrastructure.Persistence;

using SistemaHospitalar.Infrastructure.Time;



namespace SistemaHospitalar.Infrastructure.Services;



public class BiService(AppDbContext dbContext) : IBiService

{

    public async Task<BiDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)

    {

        var todayBrazil = HospitalTime.TodayInBrazil;

        var monthStartBrazil = new DateOnly(todayBrazil.Year, todayBrazil.Month, 1);

        var lastMonthStartBrazil = monthStartBrazil.AddMonths(-1);

        var sixMonthsStartBrazil = monthStartBrazil.AddMonths(-5);



        var (startOfDay, endOfDay) = HospitalTime.BrazilDayRangeUtc(todayBrazil);

        var (startOfMonth, _) = HospitalTime.BrazilDayRangeUtc(monthStartBrazil);

        var (startOfLastMonth, _) = HospitalTime.BrazilDayRangeUtc(lastMonthStartBrazil);

        var (monthStart, _) = HospitalTime.BrazilDayRangeUtc(sixMonthsStartBrazil);

        var now = DateTime.UtcNow;



        var totalPatients = await dbContext.Patients.CountAsync(p => p.IsActive, cancellationToken);

        var activeHospitalizations = await dbContext.Hospitalizations.CountAsync(

            h => h.IsActive && h.Status == HospitalizationStatus.Active, cancellationToken);



        var appointmentsToday = await dbContext.Appointments.CountAsync(

            a => a.ScheduledAt >= startOfDay && a.ScheduledAt < endOfDay && a.IsActive

                && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow,

            cancellationToken);



        var surgeriesToday = await dbContext.Surgeries.CountAsync(

            s => s.ScheduledAt >= startOfDay && s.ScheduledAt < endOfDay && s.IsActive

                && s.Status != SurgeryStatus.Cancelled,

            cancellationToken);



        var labOrdersPending = await dbContext.LabOrders.CountAsync(

            o => o.IsActive && o.Status != LabOrderStatus.Completed && o.Status != LabOrderStatus.Cancelled,

            cancellationToken);



        var imagingPending = await dbContext.ImagingStudies.CountAsync(

            s => s.IsActive && s.Status != ImagingStudyStatus.Completed && s.Status != ImagingStudyStatus.Cancelled,

            cancellationToken);



        var revenueThisMonth = await dbContext.FinancialPayments

            .Where(p => p.IsActive

                && p.FinancialAccount.IsActive

                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable

                && p.PaidAt >= startOfMonth)

            .SumAsync(p => p.Amount, cancellationToken);



        var revenueLastMonth = await dbContext.FinancialPayments

            .Where(p => p.IsActive

                && p.FinancialAccount.IsActive

                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable

                && p.PaidAt >= startOfLastMonth && p.PaidAt < startOfMonth)

            .SumAsync(p => p.Amount, cancellationToken);



        var revenueGrowth = revenueLastMonth == 0

            ? (revenueThisMonth > 0 ? 100m : 0m)

            : Math.Round((revenueThisMonth - revenueLastMonth) / revenueLastMonth * 100, 1);



        var revenuePending = await dbContext.FinancialAccounts

            .Where(f => f.IsActive

                && f.Direction == FinancialAccountDirection.Receivable

                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid))

            .SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);



        var financialAccountsOpen = await dbContext.FinancialAccounts.CountAsync(

            f => f.IsActive && f.Direction == FinancialAccountDirection.Receivable

                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid),

            cancellationToken);



        var expenseThisMonth = await dbContext.FinancialPayments

            .Where(p => p.IsActive

                && p.FinancialAccount.IsActive

                && p.FinancialAccount.Direction == FinancialAccountDirection.Payable

                && p.PaidAt >= startOfMonth)

            .SumAsync(p => p.Amount, cancellationToken);



        var expenseLastMonth = await dbContext.FinancialPayments

            .Where(p => p.IsActive

                && p.FinancialAccount.IsActive

                && p.FinancialAccount.Direction == FinancialAccountDirection.Payable

                && p.PaidAt >= startOfLastMonth && p.PaidAt < startOfMonth)

            .SumAsync(p => p.Amount, cancellationToken);



        var expenseGrowth = expenseLastMonth == 0

            ? (expenseThisMonth > 0 ? 100m : 0m)

            : Math.Round((expenseThisMonth - expenseLastMonth) / expenseLastMonth * 100, 1);



        var openReceivableFilter = dbContext.FinancialAccounts.Where(f => f.IsActive

            && f.Direction == FinancialAccountDirection.Receivable

            && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid));



        var overdueReceivable = await openReceivableFilter

            .Where(f => f.DueDate != null && f.DueDate < now)

            .SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);



        var overdueReceivableCount = await openReceivableFilter

            .CountAsync(f => f.DueDate != null && f.DueDate < now, cancellationToken);



        var defaultRatePercent = revenuePending == 0

            ? 0

            : Math.Round(overdueReceivable / revenuePending * 100, 1);



        var totalBeds = await dbContext.Beds.CountAsync(b => b.IsActive, cancellationToken);

        var occupiedBeds = await dbContext.Beds.CountAsync(

            b => b.IsActive && b.Status == BedStatus.Occupied, cancellationToken);

        var occupancyRate = totalBeds == 0 ? 0 : Math.Round((decimal)occupiedBeds / totalBeds * 100, 1);



        var admissionsThisMonth = await dbContext.Hospitalizations.CountAsync(

            h => h.IsActive && h.AdmittedAt >= startOfMonth, cancellationToken);

        var bedTurnoverRate = totalBeds == 0 ? 0 : Math.Round((decimal)admissionsThisMonth / totalBeds, 2);

        var daysInMonth = DateTime.DaysInMonth(todayBrazil.Year, todayBrazil.Month);

        var monthlyBedTurnover = totalBeds == 0 || daysInMonth == 0

            ? 0

            : Math.Round(admissionsThisMonth / (decimal)totalBeds / daysInMonth * 30, 2);



        var dischargesThisMonth = await dbContext.Hospitalizations.CountAsync(

            h => h.IsActive

                && h.DischargedAt >= startOfMonth

                && h.Status == HospitalizationStatus.Discharged,

            cancellationToken);



        var lengthOfStayRows = await dbContext.Hospitalizations.AsNoTracking()

            .Where(h => h.IsActive

                && h.DischargedAt >= startOfMonth

                && h.Status == HospitalizationStatus.Discharged)

            .Select(h => new { h.AdmittedAt, DischargedAt = h.DischargedAt!.Value })

            .ToListAsync(cancellationToken);



        var averageLengthOfStayDays = lengthOfStayRows.Count == 0

            ? 0

            : Math.Round((decimal)lengthOfStayRows.Average(x => (x.DischargedAt - x.AdmittedAt).TotalDays), 1);



        var medicalProductionThisMonth = await dbContext.Appointments.CountAsync(

            a => a.IsActive

                && a.Status == AppointmentStatus.Completed

                && a.ScheduledAt >= startOfMonth,

            cancellationToken)

            + await dbContext.Surgeries.CountAsync(

                s => s.IsActive

                    && s.Status == SurgeryStatus.Completed

                    && s.ScheduledAt >= startOfMonth,

                cancellationToken);



        var hospitalProductionThisMonth = admissionsThisMonth

            + await dbContext.EmergencyVisits.CountAsync(

                e => e.IsActive && e.ArrivedAt >= startOfMonth, cancellationToken)

            + await dbContext.LabOrders.CountAsync(

                o => o.IsActive && o.Status == LabOrderStatus.Completed && (o.UpdatedAt ?? o.CreatedAt) >= startOfMonth,

                cancellationToken)

            + await dbContext.ImagingStudies.CountAsync(

                s => s.IsActive && s.Status == ImagingStudyStatus.Completed && (s.CompletedAt ?? s.UpdatedAt ?? s.CreatedAt) >= startOfMonth,

                cancellationToken);



        var emergencyWaiting = await dbContext.EmergencyVisits.CountAsync(

            v => v.IsActive && v.Status == EmergencyVisitStatus.Waiting, cancellationToken);

        var emergencyInCare = await dbContext.EmergencyVisits.CountAsync(

            v => v.IsActive && v.Status == EmergencyVisitStatus.InCare, cancellationToken);



        var lowStockProducts = await dbContext.Products.CountAsync(

            p => p.IsActive && p.QuantityOnHand <= p.MinimumStock, cancellationToken);



        var purchaseOrdersPending = await dbContext.PurchaseOrders.CountAsync(

            o => o.IsActive && (o.Status == PurchaseOrderStatus.Sent || o.Status == PurchaseOrderStatus.PartiallyReceived),

            cancellationToken);



        var tissGuidesPending = await dbContext.TissGuides.CountAsync(

            g => g.IsActive && g.Status != TissGuideStatus.Paid && g.Status != TissGuideStatus.Cancelled,

            cancellationToken);



        var tissAmountPending = await dbContext.TissGuides

            .Where(g => g.IsActive && g.Status != TissGuideStatus.Paid && g.Status != TissGuideStatus.Cancelled)

            .SumAsync(g => g.TotalAmount, cancellationToken);



        var appointmentGroups = await dbContext.Appointments

            .Where(a => a.ScheduledAt >= monthStart && a.IsActive && a.Status != AppointmentStatus.Cancelled)

            .GroupBy(a => new { a.ScheduledAt.Year, a.ScheduledAt.Month })

            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })

            .ToListAsync(cancellationToken);



        var revenueGroups = await dbContext.FinancialPayments

            .Where(p => p.IsActive

                && p.FinancialAccount.IsActive

                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable

                && p.PaidAt >= monthStart)

            .GroupBy(p => new { p.PaidAt.Year, p.PaidAt.Month })

            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.Amount) })

            .ToListAsync(cancellationToken);



        var expenseGroups = await dbContext.FinancialPayments

            .Where(p => p.IsActive

                && p.FinancialAccount.IsActive

                && p.FinancialAccount.Direction == FinancialAccountDirection.Payable

                && p.PaidAt >= monthStart)

            .GroupBy(p => new { p.PaidAt.Year, p.PaidAt.Month })

            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.Amount) })

            .ToListAsync(cancellationToken);



        var hospitalizationGroups = await dbContext.Hospitalizations

            .Where(h => h.IsActive && h.AdmittedAt >= monthStart)

            .GroupBy(h => new { h.AdmittedAt.Year, h.AdmittedAt.Month })

            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })

            .ToListAsync(cancellationToken);



        var monthlyAppointments = BuildMonthlySeries(appointmentGroups.Select(x => (x.Year, x.Month, x.Count)));

        var monthlyRevenue = BuildMonthlyRevenueSeries(revenueGroups.Select(x => (x.Year, x.Month, x.Amount)));

        var monthlyExpenses = BuildMonthlyRevenueSeries(expenseGroups.Select(x => (x.Year, x.Month, x.Amount)));

        var monthlyHospitalizations = BuildMonthlySeries(hospitalizationGroups.Select(x => (x.Year, x.Month, x.Count)));



        var revenueByCategory = (await dbContext.FinancialPayments

            .Where(p => p.IsActive

                && p.FinancialAccount.IsActive

                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable

                && p.PaidAt >= startOfMonth)

            .GroupBy(p => p.FinancialAccount.Category)

            .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() })

            .OrderByDescending(x => x.Amount)

            .ToListAsync(cancellationToken))

            .Select(x => new BiCategoryStatDto(x.Category.ToString(), x.Amount, x.Count))

            .ToList();



        var tissByStatus = (await dbContext.TissGuides

            .Where(g => g.IsActive)

            .GroupBy(g => g.Status)

            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(x => x.TotalAmount) })

            .ToListAsync(cancellationToken))

            .Select(g => new BiStatusCountDto(g.Status.ToString(), g.Count, g.Amount))

            .ToList();



        var labByStatus = (await dbContext.LabOrders

            .Where(o => o.IsActive)

            .GroupBy(o => o.Status)

            .Select(g => new { Status = g.Key, Count = g.Count() })

            .ToListAsync(cancellationToken))

            .Select(g => new BiStatusCountDto(g.Status.ToString(), g.Count, null))

            .ToList();



        var financialByStatus = (await dbContext.FinancialAccounts

            .Where(f => f.IsActive)

            .GroupBy(f => f.Status)

            .Select(g => new

            {

                Status = g.Key,

                Count = g.Count(),

                Amount = g.Sum(x => x.Amount - x.PaidAmount)

            })

            .ToListAsync(cancellationToken))

            .Select(g => new BiStatusCountDto(g.Status.ToString(), g.Count, g.Amount))

            .ToList();



        var imagingByStatus = (await dbContext.ImagingStudies

            .Where(s => s.IsActive)

            .GroupBy(s => s.Status)

            .Select(g => new { Status = g.Key, Count = g.Count() })

            .ToListAsync(cancellationToken))

            .Select(g => new BiStatusCountDto(g.Status.ToString(), g.Count, null))

            .ToList();



        var emergencyByUrgency = (await dbContext.EmergencyVisits

            .Where(v => v.IsActive && v.Status != EmergencyVisitStatus.Discharged)

            .GroupBy(v => v.Urgency)

            .Select(g => new { Urgency = g.Key, Count = g.Count() })

            .ToListAsync(cancellationToken))

            .Select(g => new BiStatusCountDto(g.Urgency.ToString(), g.Count, null))

            .ToList();



        var wardOccupancy = await dbContext.Wards

            .AsNoTracking()

            .Where(w => w.IsActive)

            .Select(w => new BiWardOccupancyDto(

                w.Name,

                w.Beds.Count(b => b.IsActive),

                w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Occupied),

                0))

            .ToListAsync(cancellationToken);



        wardOccupancy = wardOccupancy

            .Select(w => w with

            {

                OccupancyRate = w.TotalBeds == 0 ? 0 : Math.Round((decimal)w.OccupiedBeds / w.TotalBeds * 100, 1)

            })

            .OrderByDescending(w => w.OccupancyRate)

            .ToList();



        var topSpecialties = (await dbContext.Appointments

            .Where(a => a.IsActive && a.ScheduledAt >= startOfMonth

                && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)

            .GroupBy(a => a.Professional.Specialty.Name)

            .Select(g => new { Name = g.Key, Count = g.Count() })

            .OrderByDescending(s => s.Count)

            .Take(8)

            .ToListAsync(cancellationToken))

            .Select(g => new BiSpecialtyStatDto(g.Name, g.Count))

            .ToList();



        var lowStockItems = (await dbContext.Products

            .AsNoTracking()

            .Where(p => p.IsActive && p.QuantityOnHand <= p.MinimumStock)

            .Select(p => new { p.Name, p.Sku, p.QuantityOnHand, p.MinimumStock, p.Unit })

            .ToListAsync(cancellationToken))

            .OrderBy(p => p.MinimumStock == 0 ? p.QuantityOnHand : p.QuantityOnHand / p.MinimumStock)

            .Take(8)

            .Select(p => new BiLowStockItemDto(p.Name, p.Sku, p.QuantityOnHand, p.MinimumStock, p.Unit))

            .ToList();



        return new BiDashboardDto(

            totalPatients,

            activeHospitalizations,

            appointmentsToday,

            surgeriesToday,

            labOrdersPending,

            imagingPending,

            revenueThisMonth,

            revenueLastMonth,

            revenueGrowth,

            revenuePending,

            occupancyRate,

            occupiedBeds,

            totalBeds,

            emergencyWaiting,

            emergencyInCare,

            financialAccountsOpen,

            lowStockProducts,

            purchaseOrdersPending,

            tissGuidesPending,

            tissAmountPending,

            monthlyAppointments,

            monthlyRevenue,

            monthlyExpenses,

            monthlyHospitalizations,

            averageLengthOfStayDays,

            dischargesThisMonth,

            bedTurnoverRate,

            monthlyBedTurnover,

            expenseThisMonth,

            expenseLastMonth,

            expenseGrowth,

            overdueReceivable,

            overdueReceivableCount,

            defaultRatePercent,

            medicalProductionThisMonth,

            hospitalProductionThisMonth,

            LocalizeCategoryStats(revenueByCategory),

            LocalizeStatusStats(tissByStatus, TissStatusLabel),

            LocalizeStatusStats(labByStatus, LabStatusLabel),

            LocalizeStatusStats(financialByStatus, FinancialStatusLabel),

            LocalizeStatusStats(imagingByStatus, ImagingStatusLabel),

            LocalizeStatusStats(emergencyByUrgency, UrgencyLabel),

            wardOccupancy,

            topSpecialties,

            lowStockItems,

            now);

    }



    private static IReadOnlyList<BiMonthlyStatDto> BuildMonthlySeries(IEnumerable<(int Year, int Month, int Count)> data)

    {

        var lookup = data.ToDictionary(x => (x.Year, x.Month), x => x.Count);

        return BuildMonthRange().Select(m => new BiMonthlyStatDto(

            $"{m.Month:00}/{m.Year}",

            lookup.GetValueOrDefault((m.Year, m.Month)))).ToList();

    }



    private static IReadOnlyList<BiMonthlyStatDto> BuildMonthlyRevenueSeries(IEnumerable<(int Year, int Month, decimal Amount)> data)

    {

        var lookup = data.ToDictionary(x => (x.Year, x.Month), x => x.Amount);

        return BuildMonthRange().Select(m => new BiMonthlyStatDto(

            $"{m.Month:00}/{m.Year}",

            0,

            lookup.GetValueOrDefault((m.Year, m.Month)))).ToList();

    }



    private static IEnumerable<(int Year, int Month)> BuildMonthRange()

    {

        var todayBrazil = HospitalTime.TodayInBrazil;

        var monthStartBrazil = new DateOnly(todayBrazil.Year, todayBrazil.Month, 1);

        for (var i = 5; i >= 0; i--)

        {

            var d = monthStartBrazil.AddMonths(-i);

            yield return (d.Year, d.Month);

        }

    }



    private static IReadOnlyList<BiCategoryStatDto> LocalizeCategoryStats(IReadOnlyList<BiCategoryStatDto> items) =>

        items.Select(i => i with { Label = FinancialCategoryLabel(i.Label) }).ToList();



    private static IReadOnlyList<BiStatusCountDto> LocalizeStatusStats(

        IReadOnlyList<BiStatusCountDto> items,

        Func<string, string> labelFn) =>

        items.Select(i => i with { Label = labelFn(i.Label) }).ToList();



    private static string FinancialCategoryLabel(string key) => key switch

    {

        "Consultation" => "Consultas",

        "Hospitalization" => "Internação",

        "Exam" => "Exames",

        "Copayment" => "Coparticipação",

        "Parking" => "Estacionamento",

        "Other" => "Outros",

        _ => key

    };



    private static string TissStatusLabel(string key) => key switch

    {

        "Draft" => "Rascunho",

        "Sent" => "Enviada",

        "Paid" => "Paga",

        "Glosa" => "Glosa",

        "Cancelled" => "Cancelada",

        _ => key

    };



    private static string LabStatusLabel(string key) => key switch

    {

        "Requested" => "Solicitado",

        "InProgress" => "Em análise",

        "Completed" => "Concluído",

        "Cancelled" => "Cancelado",

        _ => key

    };



    private static string FinancialStatusLabel(string key) => key switch

    {

        "Open" => "Em aberto",

        "PartiallyPaid" => "Parcial",

        "Paid" => "Pago",

        "Cancelled" => "Cancelado",

        _ => key

    };



    private static string ImagingStatusLabel(string key) => key switch

    {

        "Scheduled" => "Agendado",

        "InProgress" => "Em execução",

        "Completed" => "Concluído",

        "Cancelled" => "Cancelado",

        _ => key

    };



    private static string UrgencyLabel(string key) => key switch

    {

        "Emergency" => "Emergência",

        "High" => "Muito urgente",

        "Medium" => "Urgente",

        "Low" => "Pouco urgente",

        "NonUrgent" => "Não urgente",

        _ => key

    };

}


