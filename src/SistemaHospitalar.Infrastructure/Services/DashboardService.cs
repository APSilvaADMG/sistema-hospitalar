using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Bi;
using SistemaHospitalar.Application.DTOs.Dashboard;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Time;

namespace SistemaHospitalar.Infrastructure.Services;

public class DashboardService(
    AppDbContext dbContext,
    INotificationService notificationService) : IDashboardService
{
    public async Task<OperationalDashboardDto> GetOperationalDashboardAsync(
        Guid? userId,
        DateOnly? date = null,
        Guid? professionalId = null,
        CancellationToken cancellationToken = default)
    {
        var today = date ?? HospitalTime.TodayInBrazil;
        var (startOfDay, endOfDay) = HospitalTime.BrazilDayRangeUtc(today);
        var monthStartBrazil = new DateOnly(today.Year, today.Month, 1);
        var (startOfMonth, _) = HospitalTime.BrazilDayRangeUtc(monthStartBrazil);

        var totalPatients = await dbContext.Patients.CountAsync(p => p.IsActive, cancellationToken);

        var appointmentsTodayQuery = dbContext.Appointments
            .Where(a => a.IsActive
                && a.ScheduledAt >= startOfDay
                && a.ScheduledAt < endOfDay
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow
                && (!professionalId.HasValue || a.ProfessionalId == professionalId.Value));

        var appointmentsToday = await appointmentsTodayQuery.CountAsync(cancellationToken);
        var appointmentsPendingToday = await appointmentsTodayQuery
            .CountAsync(a => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed, cancellationToken);

        var activeHospitalizations = await dbContext.Hospitalizations
            .CountAsync(h => h.Status == HospitalizationStatus.Active, cancellationToken);

        var surgeriesToday = await dbContext.Surgeries.CountAsync(
            s => s.IsActive
                && s.ScheduledAt >= startOfDay
                && s.ScheduledAt < endOfDay
                && s.Status != SurgeryStatus.Cancelled,
            cancellationToken);

        var totalBeds = await dbContext.Beds.CountAsync(b => b.IsActive, cancellationToken);
        var occupiedBeds = await dbContext.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Occupied, cancellationToken);
        var availableBeds = await dbContext.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Available, cancellationToken);
        var cleaningBeds = await dbContext.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Cleaning, cancellationToken);
        var maintenanceBeds = await dbContext.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Maintenance, cancellationToken);
        var occupancyRate = totalBeds == 0 ? 0 : Math.Round((decimal)occupiedBeds / totalBeds * 100, 1);

        var emergencyWaiting = await dbContext.EmergencyVisits.CountAsync(
            v => v.IsActive && v.Status == EmergencyVisitStatus.Waiting, cancellationToken);
        var emergencyInCare = await dbContext.EmergencyVisits.CountAsync(
            v => v.IsActive && v.Status == EmergencyVisitStatus.InCare, cancellationToken);
        var emergencyCritical = await dbContext.EmergencyVisits.CountAsync(
            v => v.IsActive
                && v.Status == EmergencyVisitStatus.Waiting
                && (v.Urgency == TriageUrgency.Emergency || v.Urgency == TriageUrgency.High),
            cancellationToken);

        var triageToday = await dbContext.AiTriageLogs.CountAsync(
            l => l.CreatedAt >= startOfDay && l.CreatedAt < endOfDay, cancellationToken);
        var triageEmergencyToday = await dbContext.AiTriageLogs.CountAsync(
            l => l.CreatedAt >= startOfDay && l.CreatedAt < endOfDay && l.Urgency == TriageUrgency.Emergency,
            cancellationToken);

        var labOrdersPending = await dbContext.LabOrders.CountAsync(
            o => o.IsActive && o.Status != LabOrderStatus.Completed && o.Status != LabOrderStatus.Cancelled,
            cancellationToken);
        var imagingPending = await dbContext.ImagingStudies.CountAsync(
            s => s.IsActive && s.Status != ImagingStudyStatus.Completed && s.Status != ImagingStudyStatus.Cancelled,
            cancellationToken);

        var revenueToday = await dbContext.FinancialPayments
            .Where(p => p.IsActive
                && p.FinancialAccount.IsActive
                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable
                && p.PaidAt >= startOfDay
                && p.PaidAt < endOfDay)
            .SumAsync(p => p.Amount, cancellationToken);

        var revenueThisMonth = await dbContext.FinancialPayments
            .Where(p => p.IsActive
                && p.FinancialAccount.IsActive
                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable
                && p.PaidAt >= startOfMonth)
            .SumAsync(p => p.Amount, cancellationToken);
        var revenuePending = await dbContext.FinancialAccounts
            .Where(f => f.IsActive
                && f.Direction == FinancialAccountDirection.Receivable
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid))
            .SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);
        var financialAccountsOpen = await dbContext.FinancialAccounts.CountAsync(
            f => f.IsActive
                && f.Direction == FinancialAccountDirection.Receivable
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid),
            cancellationToken);

        var openAccountFilter = dbContext.FinancialAccounts.Where(f => f.IsActive
            && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid));

        var payablePending = await openAccountFilter
            .Where(f => f.Direction == FinancialAccountDirection.Payable)
            .SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);

        var payableAccountsOpen = await openAccountFilter
            .CountAsync(f => f.Direction == FinancialAccountDirection.Payable, cancellationToken);

        var expenseThisMonth = await dbContext.FinancialPayments
            .Where(p => p.IsActive
                && p.FinancialAccount.IsActive
                && p.FinancialAccount.Direction == FinancialAccountDirection.Payable
                && p.PaidAt >= startOfMonth)
            .SumAsync(p => p.Amount, cancellationToken);

        var overdueCutoff = DateTime.UtcNow;
        var overdueReceivable = await openAccountFilter
            .Where(f => f.Direction == FinancialAccountDirection.Receivable
                && f.DueDate != null
                && f.DueDate < overdueCutoff)
            .SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);

        var overduePayable = await openAccountFilter
            .Where(f => f.Direction == FinancialAccountDirection.Payable
                && f.DueDate != null
                && f.DueDate < overdueCutoff)
            .SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);

        var lowStockProducts = await dbContext.Products.CountAsync(
            p => p.IsActive && p.QuantityOnHand <= p.MinimumStock, cancellationToken);

        var expiringLotsCount = await dbContext.ProductLots.CountAsync(
            l => l.IsActive
                && l.QuantityOnHand > 0
                && l.ExpiryDate != null
                && l.ExpiryDate <= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            cancellationToken);

        var parkingOccupied = await dbContext.ParkingSessions.CountAsync(
            s => s.IsActive && s.Status == ParkingSessionStatus.Active, cancellationToken);
        var parkingAwaitingPayment = await dbContext.ParkingSessions.CountAsync(
            s => s.IsActive && s.Status == ParkingSessionStatus.Active && !s.IsPaid, cancellationToken);

        var visitorsInside = await dbContext.VisitorLogs.CountAsync(
            v => v.IsActive && v.Status == VisitorLogStatus.Inside, cancellationToken);
        var openSecurityIncidents = await dbContext.SecurityIncidents.CountAsync(
            i => i.IsActive && i.Status != SecurityIncidentStatus.Resolved, cancellationToken);

        var unreadNotifications = userId.HasValue
            ? await notificationService.GetUnreadCountAsync(userId.Value, cancellationToken)
            : 0;

        var appointmentsTodayList = await appointmentsTodayQuery
            .OrderBy(a => a.ScheduledAt)
            .Take(8)
            .Select(a => new DashboardAppointmentItemDto(
                a.Id,
                a.ScheduledAt,
                a.Patient.FullName,
                a.Professional.FullName,
                a.Professional.Specialty.Name,
                a.Status))
            .ToListAsync(cancellationToken);

        var weekStart = startOfDay.AddDays(-(int)(startOfDay.DayOfWeek == DayOfWeek.Sunday ? 6 : startOfDay.DayOfWeek - DayOfWeek.Monday));
        var weekEnd = weekStart.AddDays(7);
        var weeklyCalendar = await dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.IsActive
                && a.ScheduledAt >= weekStart
                && a.ScheduledAt < weekEnd
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow)
            .OrderBy(a => a.ScheduledAt)
            .Take(12)
            .Select(a => new DashboardWeeklyCalendarItemDto(
                a.Id,
                a.ScheduledAt,
                a.Patient.FullName,
                a.Professional.FullName,
                a.Professional.Specialty.Name))
            .ToListAsync(cancellationToken);

        var revenueExpenseMonthly = await BuildRevenueExpenseMonthlyAsync(startOfMonth, cancellationToken);
        var departmentRevenue = await BuildDepartmentRevenueAsync(startOfMonth, cancellationToken);

        var emergencyWaitingList = await dbContext.EmergencyVisits
            .AsNoTracking()
            .Where(v => v.IsActive && v.Status == EmergencyVisitStatus.Waiting)
            .Select(v => new
            {
                v.Id,
                v.Patient.FullName,
                v.ChiefComplaint,
                v.Urgency,
                v.Status,
                v.ArrivedAt
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var averageEmergencyWaitMinutes = emergencyWaitingList.Count == 0
            ? 0
            : emergencyWaitingList.Average(v => (now - v.ArrivedAt).TotalMinutes);

        var emergencySlaViolations = emergencyWaitingList.Count(v =>
            HospitalBusinessRules.IsEmergencyWaitExceeded(v.ArrivedAt, v.Urgency, now));

        var emergencyQueue = emergencyWaitingList
            .Select(v => new DashboardEmergencyItemDto(
                v.Id, v.FullName, v.ChiefComplaint, v.Urgency, v.Status, v.ArrivedAt))
            .OrderBy(v => v.Urgency switch
            {
                TriageUrgency.Emergency => 0,
                TriageUrgency.High => 1,
                TriageUrgency.Medium => 2,
                TriageUrgency.Low => 3,
                _ => 4
            })
            .ThenBy(v => v.ArrivedAt)
            .Take(6)
            .ToList();

        var completedAppointmentsToday = await appointmentsTodayQuery
            .CountAsync(a => a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.InProgress, cancellationToken);
        var emergencyToday = await dbContext.EmergencyVisits.CountAsync(
            v => v.IsActive && v.ArrivedAt >= startOfDay && v.ArrivedAt < endOfDay,
            cancellationToken);
        var attendancesToday = completedAppointmentsToday + emergencyToday;

        var appointmentHours = await appointmentsTodayQuery
            .GroupBy(a => a.ScheduledAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var emergencyHours = await dbContext.EmergencyVisits
            .Where(v => v.IsActive && v.ArrivedAt >= startOfDay && v.ArrivedAt < endOfDay)
            .GroupBy(v => v.ArrivedAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var hourlyMap = Enumerable.Range(0, 24).ToDictionary(h => h, _ => 0);
        foreach (var row in appointmentHours) hourlyMap[row.Hour] += row.Count;
        foreach (var row in emergencyHours) hourlyMap[row.Hour] += row.Count;
        var hourlyAttendances = hourlyMap
            .Where(kv => kv.Value > 0)
            .OrderBy(kv => kv.Key)
            .Select(kv => new DashboardHourlyStatDto(kv.Key, kv.Value))
            .ToList();

        var productionBySpecialtyRows = await appointmentsTodayQuery
            .GroupBy(a => a.Professional.Specialty.Name)
            .Select(g => new { Specialty = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(cancellationToken);
        var productionBySpecialty = productionBySpecialtyRows
            .Select(x => new BiStatusCountDto(x.Specialty, x.Count, null))
            .ToList();

        var integrationFailures = await dbContext.SyncMutations.CountAsync(
            m => m.IsActive && m.Status == "failed", cancellationToken)
            + await dbContext.TissGuides.CountAsync(
                g => g.IsActive && g.Status == TissGuideStatus.Glosa, cancellationToken);

        var alerts = BuildAlerts(
            occupancyRate,
            emergencySlaViolations,
            emergencyCritical,
            lowStockProducts,
            expiringLotsCount,
            integrationFailures,
            openSecurityIncidents,
            financialAccountsOpen,
            overdueReceivable,
            overduePayable,
            cleaningBeds,
            unreadNotifications);

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
        var monthlyAppointments = await dbContext.Appointments
            .Where(a => a.CreatedAt >= sixMonthsAgo && a.IsActive && a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new BiMonthlyStatDto($"{g.Key.Month:00}/{g.Key.Year}", g.Count()))
            .ToListAsync(cancellationToken);

        var labByStatus = await dbContext.LabOrders
            .Where(o => o.IsActive)
            .GroupBy(o => o.Status)
            .Select(g => new BiStatusCountDto(g.Key.ToString(), g.Count(), null))
            .ToListAsync(cancellationToken);

        var currentMonth = today.Month;
        var monthBirthdays = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.IsActive && e.BirthDate != null && e.BirthDate.Value.Month == currentMonth)
            .OrderByDescending(e => e.BirthDate)
            .Select(e => new DashboardBirthdayEmployeeDto(
                e.Id,
                e.FullName,
                e.BirthDate!.Value,
                e.PhotoData,
                e.JobTitle,
                e.Department.Name))
            .ToListAsync(cancellationToken);

        var appointmentStatusBreakdownRows = await appointmentsTodayQuery
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var appointmentStatusBreakdown = appointmentStatusBreakdownRows
            .Select(x => new BiStatusCountDto(((int)x.Status).ToString(), x.Count, null))
            .OrderBy(x => x.Label)
            .ToList();

        return new OperationalDashboardDto(
            totalPatients,
            appointmentsToday,
            appointmentsPendingToday,
            activeHospitalizations,
            surgeriesToday,
            occupiedBeds,
            totalBeds,
            occupancyRate,
            emergencyWaiting,
            emergencyInCare,
            emergencyCritical,
            triageToday,
            triageEmergencyToday,
            labOrdersPending,
            imagingPending,
            revenueThisMonth,
            revenuePending,
            financialAccountsOpen,
            payablePending,
            expenseThisMonth,
            payableAccountsOpen,
            overdueReceivable,
            overduePayable,
            lowStockProducts,
            parkingOccupied,
            parkingAwaitingPayment,
            visitorsInside,
            openSecurityIncidents,
            unreadNotifications,
            appointmentsTodayList,
            revenueExpenseMonthly,
            weeklyCalendar,
            departmentRevenue,
            emergencyQueue,
            monthlyAppointments,
            labByStatus,
            monthBirthdays,
            revenueToday,
            availableBeds,
            cleaningBeds,
            maintenanceBeds,
            attendancesToday,
            Math.Round(averageEmergencyWaitMinutes, 1),
            emergencySlaViolations,
            integrationFailures,
            hourlyAttendances,
            productionBySpecialty,
            alerts,
            appointmentStatusBreakdown,
            DateTime.UtcNow);
    }

    private async Task<List<DashboardFinancialMonthlyPointDto>> BuildRevenueExpenseMonthlyAsync(
        DateTime startOfMonth,
        CancellationToken cancellationToken)
    {
        var from = startOfMonth.AddMonths(-5);
        var revenueRows = await dbContext.FinancialPayments
            .AsNoTracking()
            .Where(p => p.IsActive
                && p.PaidAt >= from
                && p.FinancialAccount.IsActive
                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable)
            .GroupBy(p => new { p.PaidAt.Year, p.PaidAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var expenseRows = await dbContext.FinancialPayments
            .AsNoTracking()
            .Where(p => p.IsActive
                && p.PaidAt >= from
                && p.FinancialAccount.IsActive
                && p.FinancialAccount.Direction == FinancialAccountDirection.Payable)
            .GroupBy(p => new { p.PaidAt.Year, p.PaidAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var points = new List<DashboardFinancialMonthlyPointDto>();
        for (var i = 0; i < 6; i++)
        {
            var dt = from.AddMonths(i);
            var rev = revenueRows.FirstOrDefault(x => x.Year == dt.Year && x.Month == dt.Month)?.Amount ?? 0m;
            var exp = expenseRows.FirstOrDefault(x => x.Year == dt.Year && x.Month == dt.Month)?.Amount ?? 0m;
            points.Add(new DashboardFinancialMonthlyPointDto($"{dt:MM/yyyy}", rev, exp));
        }

        return points;
    }

    private async Task<List<DashboardDepartmentRevenueDto>> BuildDepartmentRevenueAsync(
        DateTime startOfMonth,
        CancellationToken cancellationToken)
    {
        var blood = await dbContext.TransfusionRequests
            .AsNoTracking()
            .CountAsync(x => x.IsActive && x.CreatedAt >= startOfMonth, cancellationToken) * 320m;
        var pharmacy = await dbContext.PharmacyBillingEntries
            .AsNoTracking()
            .Where(x => x.IsActive && x.CreatedAt >= startOfMonth)
            .SumAsync(x => x.TotalAmount, cancellationToken);
        var pathology = await dbContext.LabOrders
            .AsNoTracking()
            .CountAsync(x => x.IsActive && x.CreatedAt >= startOfMonth, cancellationToken) * 140m;
        var radiology = await dbContext.ImagingStudies
            .AsNoTracking()
            .CountAsync(x => x.IsActive && x.CreatedAt >= startOfMonth, cancellationToken) * 280m;

        return
        [
            new DashboardDepartmentRevenueDto("blood", "Hemoterapia", blood),
            new DashboardDepartmentRevenueDto("pharmacy", "Farmácia", pharmacy),
            new DashboardDepartmentRevenueDto("pathology", "Laboratório", pathology),
            new DashboardDepartmentRevenueDto("radiology", "Radiologia", radiology),
        ];
    }

    private static List<DashboardAlertDto> BuildAlerts(
        decimal occupancyRate,
        int emergencySlaViolations,
        int emergencyCritical,
        int lowStockProducts,
        int expiringLotsCount,
        int integrationFailures,
        int openSecurityIncidents,
        int financialAccountsOpen,
        decimal overdueReceivable,
        decimal overduePayable,
        int cleaningBeds,
        int unreadNotifications)
    {
        var alerts = new List<DashboardAlertDto>();

        if (occupancyRate >= HospitalBusinessRules.CriticalBedOccupancyPercent)
        {
            alerts.Add(new DashboardAlertDto(
                "BED-CRITICAL",
                "critical",
                "Ocupação crítica de leitos",
                $"Taxa de ocupação em {occupancyRate}% — acima do limite de {HospitalBusinessRules.CriticalBedOccupancyPercent}%.",
                "/internacao/leitos"));
        }

        if (emergencySlaViolations > 0)
        {
            alerts.Add(new DashboardAlertDto(
                "PS-SLA",
                "critical",
                "SLA do pronto-socorro ultrapassado",
                $"{emergencySlaViolations} paciente(s) aguardando além do tempo previsto (RN-007).",
                "/emergencia"));
        }

        if (emergencyCritical > 0)
        {
            alerts.Add(new DashboardAlertDto(
                "PS-CRITICAL",
                "warning",
                "Pacientes críticos na fila",
                $"{emergencyCritical} caso(s) com urgência vermelha/laranja aguardando atendimento.",
                "/emergencia/classificacao-risco"));
        }

        if (lowStockProducts > 0)
        {
            alerts.Add(new DashboardAlertDto(
                "STOCK-LOW",
                "warning",
                "Estoque crítico",
                $"{lowStockProducts} produto(s) abaixo do estoque mínimo.",
                "/estoque/dashboard"));
        }

        if (expiringLotsCount > 0)
        {
            alerts.Add(new DashboardAlertDto(
                "LOT-EXP",
                "warning",
                "Lotes próximos do vencimento",
                $"{expiringLotsCount} lote(s) com validade em até 30 dias (RN-MAT-020).",
                "/estoque/dashboard"));
        }

        if (integrationFailures > 0)
        {
            alerts.Add(new DashboardAlertDto(
                "INT-FAIL",
                "warning",
                "Falhas de integração",
                $"{integrationFailures} pendência(s) em sincronização ou glosas TISS.",
                "/integracoes"));
        }

        if (openSecurityIncidents > 0)
        {
            alerts.Add(new DashboardAlertDto(
                "SEC-INC",
                "warning",
                "Incidentes de segurança abertos",
                $"{openSecurityIncidents} incidente(s) aguardando resolução.",
                "/seguranca-lgpd"));
        }

        if (financialAccountsOpen > 10)
        {
            alerts.Add(new DashboardAlertDto(
                "FIN-OPEN",
                "info",
                "Títulos a receber em aberto",
                $"{financialAccountsOpen} título(s) a receber pendentes de fechamento.",
                "/financeiro/contas-a-receber/listar"));
        }

        if (overdueReceivable > 0)
        {
            alerts.Add(new DashboardAlertDto(
                "FIN-OVERDUE-REC",
                "warning",
                "Títulos a receber vencidos",
                $"Saldo vencido a receber: R$ {overdueReceivable:N2}.",
                "/financeiro/contas-a-receber/listar"));
        }

        if (overduePayable > 0)
        {
            alerts.Add(new DashboardAlertDto(
                "FIN-OVERDUE-PAY",
                "warning",
                "Títulos a pagar vencidos",
                $"Saldo vencido a pagar: R$ {overduePayable:N2}.",
                "/financeiro/contas-a-pagar/listar"));
        }

        if (cleaningBeds > 0 && occupancyRate >= 80)
        {
            alerts.Add(new DashboardAlertDto(
                "BED-CLEAN",
                "info",
                "Leitos em higienização",
                $"{cleaningBeds} leito(s) aguardando liberação pela hotelaria (RN-017).",
                "/hotelaria"));
        }

        if (unreadNotifications > 0)
        {
            alerts.Add(new DashboardAlertDto(
                "NOTIF",
                "info",
                "Notificações pendentes",
                $"{unreadNotifications} notificação(ões) não lida(s).",
                "/notificacoes"));
        }

        return alerts;
    }
}
