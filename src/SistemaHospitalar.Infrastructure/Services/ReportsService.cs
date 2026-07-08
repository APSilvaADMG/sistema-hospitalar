using Microsoft.EntityFrameworkCore;
using System.Globalization;
using SistemaHospitalar.Application.DTOs.Reports;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Reports;

namespace SistemaHospitalar.Infrastructure.Services;

public class ReportsService(AppDbContext dbContext) : IReportsService
{
    public Task<ReportCatalogSummaryDto> GetCatalogSummaryAsync(CancellationToken cancellationToken = default)
    {
        var modules = ReportCatalog.All
            .GroupBy(r => r.Module)
            .Select(g => new ReportModuleSummaryDto(
                g.Key.ToString(),
                ReportCatalog.GetModuleLabel(g.Key),
                g.Count(),
                g.Count(x => x.IsEssential),
                g.Count(x => x.IsImplemented)))
            .OrderBy(m => m.Label)
            .ToList();

        var summary = new ReportCatalogSummaryDto(
            ReportCatalog.All.Count,
            ReportCatalog.All.Count(r => r.IsEssential),
            ReportCatalog.All.Count(r => r.IsImplemented),
            modules);

        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<ReportCatalogItemDto>> GetCatalogAsync(
        string? module = null,
        bool? essentialOnly = null,
        bool? implementedOnly = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<ReportDefinition> query = ReportCatalog.All;

        if (!string.IsNullOrWhiteSpace(module) &&
            Enum.TryParse<ReportModule>(module, true, out var moduleEnum))
        {
            query = query.Where(r => r.Module == moduleEnum);
        }

        if (essentialOnly == true)
        {
            query = query.Where(r => r.IsEssential);
        }

        if (implementedOnly == true)
        {
            query = query.Where(r => r.IsImplemented);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var items = query
            .OrderBy(r => r.Module)
            .ThenBy(r => r.Name)
            .Select(r => new ReportCatalogItemDto(
                r.Code,
                r.Name,
                r.Module.ToString(),
                ReportCatalog.GetModuleLabel(r.Module),
                r.Description,
                r.IsEssential,
                r.IsImplemented,
                r.Phase))
            .ToList();

        return Task.FromResult<IReadOnlyList<ReportCatalogItemDto>>(items);
    }

    public async Task<ReportResultDto> ExecuteAsync(
        string code,
        ReportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var definition = ReportCatalog.Find(code)
            ?? throw new KeyNotFoundException($"Relatório '{code}' não encontrado.");

        if (!definition.IsImplemented)
        {
            return Planned(definition);
        }

        var normalized = definition.Code.ToLowerInvariant();
        var range = ResolveRange(filter);

        return normalized switch
        {
            "admin.patients.registered" => await PatientsRegisteredAsync(definition, filter, cancellationToken),
            "admin.patients.new-by-period" => await PatientsNewAsync(definition, range, cancellationToken),
            "admin.patients.active-inactive" => await PatientsActiveInactiveAsync(definition, cancellationToken),
            "admin.patients.by-city" => await PatientsByCityAsync(definition, cancellationToken),
            "admin.patients.by-insurance" => await PatientsByInsuranceAsync(definition, cancellationToken),
            "admin.patients.consultation-history" => await ConsultationHistoryAsync(definition, range, filter, cancellationToken),
            "admin.patients.hospitalization-history" => await HospitalizationHistoryAsync(definition, range, filter, cancellationToken),
            "admin.patients.indicators" => await PatientIndicatorsAsync(definition, range, cancellationToken),
            "admin.appointments.total" or "reception.appointments.completed"
                => await AppointmentsByStatusAsync(definition, range, AppointmentStatus.Completed, filter, cancellationToken),
            "admin.appointments.by-specialty" or "reception.schedule.by-specialty" or "bi.production.by-specialty"
                => await AppointmentsBySpecialtyAsync(definition, range, filter, cancellationToken),
            "admin.appointments.by-doctor" or "reception.schedule.by-doctor" or "bi.medical-production"
                => await AppointmentsByDoctorAsync(definition, range, filter, cancellationToken),
            "admin.appointments.by-unit" => await AppointmentsByUnitAsync(definition, range, filter, cancellationToken),
            "admin.appointments.avg-time" => await AppointmentsAvgDurationAsync(definition, range, filter, cancellationToken),
            "admin.beds.occupancy" or "hosp.beds.occupancy" or "bi.occupancy-rate"
                => await BedOccupancyAsync(definition, cancellationToken),
            "admin.indicators.summary" => await HospitalIndicatorsAsync(definition, range, cancellationToken),

            "reception.appointments.scheduled"
                => await AppointmentsByStatusAsync(definition, range, AppointmentStatus.Scheduled, filter, cancellationToken),
            "reception.appointments.cancelled"
                => await AppointmentsByStatusAsync(definition, range, AppointmentStatus.Cancelled, filter, cancellationToken),
            "reception.appointments.no-show"
                => await AppointmentsByStatusAsync(definition, range, AppointmentStatus.NoShow, filter, cancellationToken),
            "reception.appointments.rescheduled" => await AppointmentsRescheduledAsync(definition, range, filter, cancellationToken),
            "reception.wait.avg-time" => await ReceptionAvgWaitAsync(definition, range, cancellationToken),
            "reception.schedule.by-insurance" => await AppointmentsByInsuranceAsync(definition, range, cancellationToken),

            "er.visits.by-triage" => await EmergencyByTriageAsync(definition, range, cancellationToken),
            "er.wait.by-triage" => await EmergencyWaitByTriageAsync(definition, range, cancellationToken),
            "er.patients.served" => await EmergencyServedAsync(definition, range, cancellationToken),
            "er.patients.transferred" => await EmergencyByStatusAsync(definition, range, EmergencyVisitStatus.Referred, cancellationToken),
            "er.patients.admitted" => await EmergencyAdmittedAsync(definition, range, cancellationToken),
            "er.stay.avg-time" => await EmergencyAvgStayAsync(definition, range, cancellationToken),
            "er.diagnoses.top" => await EmergencyTopComplaintsAsync(definition, range, cancellationToken),

            "hosp.patients.current" => await CurrentHospitalizationsAsync(definition, cancellationToken),
            "hosp.admissions.by-period" or "bi.hospitalizations"
                => await AdmissionsByPeriodAsync(definition, range, cancellationToken),
            "hosp.discharges" or "bi.discharges" => await DischargesAsync(definition, range, cancellationToken),
            "hosp.transfers.internal" => await InternalTransfersAsync(definition, range, cancellationToken),
            "hosp.deaths" or "bi.deaths" => await DeathsAsync(definition, range, cancellationToken),
            "hosp.los.avg" => await AvgLengthOfStayAsync(definition, range, cancellationToken),
            "hosp.beds.turnover" => await BedTurnoverAsync(definition, range, cancellationToken),
            "hosp.beds.available" => await BedsByStatusAsync(definition, BedStatus.Available, cancellationToken),
            "hosp.beds.blocked" => await BedsByStatusAsync(definition, BedStatus.Maintenance, cancellationToken),

            "pep.evolutions.medical" or "pep.evolutions.nursing"
                => await MedicalRecordEntriesAsync(definition, range, MedicalRecordEntryType.Evolution, filter, cancellationToken),
            "pep.prescriptions" => await MedicalRecordEntriesAsync(definition, range, MedicalRecordEntryType.Prescription, filter, cancellationToken),
            "pep.prescriptions.expired" => await ExpiredPrescriptionsAsync(definition, range, cancellationToken),
            "pep.diagnoses" => await DiagnosesAsync(definition, range, cancellationToken),
            "pep.cid.top" => await TopCidAsync(definition, range, cancellationToken),
            "pep.procedures" => await MedicalRecordEntriesAsync(definition, range, MedicalRecordEntryType.Procedure, filter, cancellationToken),
            "pep.patient.history" => await PatientHistoryAsync(definition, filter, cancellationToken),

            "nursing.meds.administered" => await PharmacyDispensedAsync(definition, range, cancellationToken),
            "nursing.vitals" => await VitalSignsAsync(definition, range, cancellationToken),

            "surgery.completed" => await SurgeriesByStatusAsync(definition, range, SurgeryStatus.Completed, cancellationToken),
            "surgery.cancelled" => await SurgeriesByStatusAsync(definition, range, SurgeryStatus.Cancelled, cancellationToken),
            "surgery.by-specialty" => await SurgeriesBySpecialtyAsync(definition, range, cancellationToken),
            "surgery.by-surgeon" => await SurgeriesBySurgeonAsync(definition, range, cancellationToken),
            "surgery.avg-duration" => await SurgeryAvgDurationAsync(definition, range, cancellationToken),
            "surgery.room.occupancy" => await SurgeryRoomOccupancyAsync(definition, range, cancellationToken),

            "pharmacy.stock.current" => await StockCurrentAsync(definition, cancellationToken),
            "pharmacy.stock.movements" => await StockMovementsAsync(definition, range, cancellationToken),
            "pharmacy.dispensed" => await PharmacyDispensedAsync(definition, range, cancellationToken),
            "pharmacy.expired" => await ExpiredProductsAsync(definition, cancellationToken),
            "pharmacy.expiring-soon" => await ExpiringProductsAsync(definition, cancellationToken),
            "pharmacy.consumption.by-sector" => await PharmacyConsumptionBySectorAsync(definition, range, cancellationToken),
            "pharmacy.consumption.by-patient" => await PharmacyConsumptionByPatientAsync(definition, range, cancellationToken),
            "pharmacy.abc-curve" => await AbcCurveAsync(definition, range, ProductType.Medication, cancellationToken),

            "pharmacy.ward.stock" => await WardPharmacyStockAsync(definition, cancellationToken),
            "pep.vaccinations" => await PatientVaccinationsReportAsync(definition, range, cancellationToken),
            "fin.cash.sessions" => await FinancialCashSessionsAsync(definition, cancellationToken),

            "supply.entries" => await StockMovementsByTypeAsync(definition, range, StockMovementType.Inbound, cancellationToken),
            "supply.exits" => await StockMovementsByTypeAsync(definition, range, StockMovementType.Outbound, cancellationToken),
            "supply.consumption.by-sector" => await StockLowAsync(definition, cancellationToken),
            "supply.stock.minimum" => await StockLowAsync(definition, cancellationToken),
            "supply.expired" => await SupplyExpiredAsync(definition, cancellationToken),
            "supply.abc-curve" => await AbcCurveAsync(definition, range, ProductType.Supply, cancellationToken),

            "lab.orders.requested" => await LabOrdersAsync(definition, range, null, cancellationToken),
            "lab.orders.completed" => await LabOrdersAsync(definition, range, LabOrderStatus.Completed, cancellationToken),
            "lab.orders.pending" => await LabOrdersPendingAsync(definition, cancellationToken),
            "lab.release.avg-time" => await LabAvgReleaseAsync(definition, range, cancellationToken),
            "lab.orders.by-doctor" => await LabByDoctorAsync(definition, range, cancellationToken),
            "lab.orders.by-insurance" => await LabByInsuranceAsync(definition, range, cancellationToken),
            "lab.production" => await LabProductionAsync(definition, range, cancellationToken),
            "lab.pathology.summary" => await PathologySummaryAsync(definition, range, cancellationToken),

            "img.xray" => await ImagingByModalityAsync(definition, range, ImagingModality.XRay, cancellationToken),
            "img.ct" => await ImagingByModalityAsync(definition, range, ImagingModality.CT, cancellationToken),
            "img.mri" => await ImagingByModalityAsync(definition, range, ImagingModality.MRI, cancellationToken),
            "img.ultrasound" => await ImagingByModalityAsync(definition, range, ImagingModality.Ultrasound, cancellationToken),
            "img.report.avg-time" => await ImagingAvgReportTimeAsync(definition, range, cancellationToken),
            "img.production.by-doctor" => await ImagingByDoctorAsync(definition, range, cancellationToken),

            "fin.revenue.gross" or "fin.revenue.by-period" or "bi.revenue.monthly"
                => await RevenueAsync(definition, range, cancellationToken),
            "fin.revenue.net" => await NetRevenueAsync(definition, range, cancellationToken),
            "fin.expenses.by-period" => await ExpensesAsync(definition, range, cancellationToken),
            "fin.cashflow" => await CashflowAsync(definition, range, cancellationToken),
            "fin.payables" => await PayablesAsync(definition, cancellationToken),
            "fin.receivables" or "fin.delinquency" => await ReceivablesAsync(definition, cancellationToken),
            "fin.statement" => await FinancialStatementAsync(definition, range, cancellationToken),
            "fin.dre" => await DreAsync(definition, range, cancellationToken),
            "bi.revenue.daily" => await DailyRevenueAsync(definition, range, cancellationToken),
            "bi.ticket.avg" => await AvgTicketAsync(definition, range, cancellationToken),
            "bi.indicators.financial" => await FinancialIndicatorsAsync(definition, range, cancellationToken),
            "bi.indicators.clinical" => await ClinicalIndicatorsAsync(definition, range, cancellationToken),

            "ins.production.by-insurance" or "ins.billing.by-operator"
                => await TissByInsuranceAsync(definition, range, cancellationToken),
            "ins.guides.issued" => await TissGuidesAsync(definition, range, null, cancellationToken),
            "ins.guides.authorized" => await AuthorizationsAsync(definition, range, cancellationToken),
            "ins.guides.glosas" or "bill.accounts.glosas" => await TissGlosasAsync(definition, range, cancellationToken),
            "ins.glosas.by-reason" => await GlosasByReasonAsync(definition, range, cancellationToken),
            "ins.glosas.appeals" => await GlosaAppealsAsync(definition, range, cancellationToken),
            "ins.tiss.sent" => await TissGuidesAsync(definition, range, TissGuideStatus.Sent, cancellationToken),
            "ins.tiss.rejected" => await TissGuidesAsync(definition, range, TissGuideStatus.Glosa, cancellationToken),
            "ins.billing.pending" => await TissPendingAsync(definition, cancellationToken),
            "ins.tpa.summary" => await TpaSummaryAsync(definition, range, cancellationToken),
            "reg.tiss" => await TissSummaryAsync(definition, range, cancellationToken),
            "reg.compulsory-notifications" => await CompulsoryNotificationsAsync(definition, range, cancellationToken),
            "reg.bpa" or "reg.ambulatory-production" => await BpaProductionAsync(definition, range, cancellationToken),
            "reg.aih" or "reg.sih-sus" => await AihProductionAsync(definition, range, cancellationToken),
            "reg.sia-sus" => await SiaProductionAsync(definition, range, cancellationToken),
            "reg.hospital-production" => await HospitalProductionAsync(definition, range, cancellationToken),
            "reg.ambulance.operations" => await AmbulanceOperationsAsync(definition, range, cancellationToken),
            "reg.cnes" => await CnesReportAsync(definition, cancellationToken),
            "reg.esus" => await EsusProductionAsync(definition, range, cancellationToken),
            "reg.ciha" => await CihaProductionAsync(definition, range, cancellationToken),
            "reg.apac" => await ApacProductionAsync(definition, range, cancellationToken),

            "bill.procedures.billed" => await BilledProceduresAsync(definition, range, cancellationToken),
            "bill.accounts.open" => await FinancialAccountsAsync(definition, FinancialAccountStatus.Open, cancellationToken),
            "bill.accounts.closed" => await FinancialAccountsAsync(definition, FinancialAccountStatus.Paid, cancellationToken),
            "bill.revenue.by-procedure" => await RevenueByProcedureAsync(definition, range, cancellationToken),
            "bill.revenue.by-specialty" => await RevenueBySpecialtyAsync(definition, range, cancellationToken),

            "hr.employees.active" => await ActiveEmployeesAsync(definition, cancellationToken),
            "hr.shifts" => await EmployeeShiftsAsync(definition, range, cancellationToken),
            "hr.schedules" => await EmployeeSchedulesAsync(definition, range, cancellationToken),
            "hr.overtime" => await EmployeeOvertimeAsync(definition, range, cancellationToken),
            "hr.productivity" => await HrProductivityAsync(definition, range, cancellationToken),
            "hr.payroll.summary" => await PayrollSummaryAsync(definition, range, cancellationToken),
            "reception.productivity" => await ReceptionProductivityAsync(definition, range, cancellationToken),

            "quality.adverse-events" => await SecurityIncidentsAsync(definition, range, cancellationToken),
            "quality.patient-falls" => await PatientFallsAsync(definition, range, cancellationToken),
            "quality.indicators" => await QualityIndicatorsAsync(definition, range, cancellationToken),
            "quality.infections" or "ccih.infections.by-period" or "ccih.infections.by-sector"
                => await InfectionsAsync(definition, range, cancellationToken),
            "ccih.infection-rate" => await InfectionRateAsync(definition, range, cancellationToken),
            "ccih.antibiotics" => await AntibioticsUsageAsync(definition, range, cancellationToken),
            "ccih.monitored-cases" => await MonitoredInfectionsAsync(definition, cancellationToken),
            "ccih.epidemic.curve" => await EpidemicCurveAsync(definition, range, cancellationToken),
            "ccih.mortality.surveillance" => await MortalitySurveillanceAsync(definition, range, cancellationToken),
            "ccih.vaccination.coverage" => await VaccinationCoverageAsync(definition, range, cancellationToken),
            "ccih.outbreak.indicators" => await OutbreakIndicatorsAsync(definition, range, cancellationToken),

            "audit.record-changes" or "audit.change-log" or "audit.access-log"
                => await AuditLogAsync(definition, range, cancellationToken),
            "audit.access-by-user" => await AuditByUserAsync(definition, range, cancellationToken),
            "audit.unauthorized-attempts" => await UnauthorizedAccessAsync(definition, range, cancellationToken),

            _ => Planned(definition),
        };
    }

    private static (DateTime From, DateTime To) ResolveRange(ReportFilterDto filter)
    {
        var to = filter.DateTo ?? DateTime.UtcNow;
        var from = filter.DateFrom ?? to.AddDays(-30);
        if (from > to)
        {
            (from, to) = (to, from);
        }

        return (from, to);
    }

    private static ReportResultDto Planned(ReportDefinition def) => new(
        def.Code,
        def.Name,
        $"Relatório planejado para a fase {def.Phase}. Em breve no APSMedCore.",
        false,
        [],
        [],
        [new ReportKpiDto("Situação", "Planejado", "warning")],
        DateTime.UtcNow);

    private static ReportResultDto Result(
        ReportDefinition def,
        string? subtitle,
        IReadOnlyList<ReportColumnDto> columns,
        IReadOnlyList<Dictionary<string, object?>> rows,
        IReadOnlyList<ReportKpiDto>? kpis = null)
    {
        var mappedColumns = ReportFieldMappings.ApplyColumns(def.Code, columns);
        var localizedRows = ReportLabels.LocalizeRows(rows, columns);
        var mappedKpis = ReportLabels.LocalizeKpis(ReportFieldMappings.ApplyKpis(def.Code, kpis ?? []));
        var mappedSubtitle = ReportFieldMappings.ResolveSubtitle(def.Code, ReportLabels.TranslateSubtitle(subtitle));

        return new ReportResultDto(
            def.Code,
            def.Name,
            mappedSubtitle,
            true,
            mappedColumns,
            localizedRows,
            mappedKpis,
            DateTime.UtcNow);
    }

    private static string FormatDate(DateTime value) => value.ToString("dd/MM/yyyy HH:mm");
    private static string FormatMoney(decimal value) => value.ToString("N2");

    // ── Implementations ──

    private async Task<ReportResultDto> PatientsRegisteredAsync(
        ReportDefinition def, ReportFilterDto filter, CancellationToken ct)
    {
        var query = dbContext.Patients.AsNoTracking().Where(p => p.IsActive);
        if (filter.PatientId.HasValue)
        {
            query = query.Where(p => p.Id == filter.PatientId.Value);
        }

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(500)
            .Select(p => new { p.FullName, p.Cpf, p.BirthDate, p.Phone, p.CreatedAt })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, p => ReportRowBuilder.Row(
            ("name", p.FullName),
            ("cpf", p.Cpf),
            ("birthDate", p.BirthDate.ToString("dd/MM/yyyy")),
            ("phone", p.Phone),
            ("registeredAt", FormatDate(p.CreatedAt))));

        var total = await dbContext.Patients.CountAsync(p => p.IsActive, ct);
        return Result(
            def,
            $"Total de {total} pacientes ativos",
            [
                new("name", "Paciente"),
                new("cpf", "CPF"),
                new("birthDate", "Nascimento"),
                new("phone", "Telefone"),
                new("registeredAt", "Cadastro"),
            ],
            rows,
            [new ReportKpiDto("Total", total.ToString(), "primary")]);
    }

    private async Task<ReportResultDto> PatientsNewAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.Patients.AsNoTracking()
            .Where(p => p.CreatedAt >= range.From && p.CreatedAt <= range.To)
            .GroupBy(p => p.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("date", g.Date.ToString("dd/MM/yyyy")),
            ("count", g.Count)));

        var total = rows.Sum(r => Convert.ToInt32(r["count"]));
        return Result(def, $"{range.From:dd/MM/yyyy} — {range.To:dd/MM/yyyy}", [
            new("date", "Data"),
            new("count", "Novos pacientes"),
        ], rows, [new ReportKpiDto("Total no período", total.ToString(), "success")]);
    }

    private async Task<ReportResultDto> PatientsActiveInactiveAsync(
        ReportDefinition def, CancellationToken ct)
    {
        var active = await dbContext.Patients.CountAsync(p => p.IsActive, ct);
        var inactive = await dbContext.Patients.CountAsync(p => !p.IsActive, ct);
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["status"] = "Ativos", ["count"] = active },
            new() { ["status"] = "Inativos", ["count"] = inactive },
        };
        return Result(def, null, [
            new("status", "Situação"),
            new("count", "Quantidade"),
        ], rows, [new ReportKpiDto("Ativos", active.ToString(), "success"), new ReportKpiDto("Inativos", inactive.ToString(), "neutral")]);
    }

    private async Task<ReportResultDto> PatientsByCityAsync(ReportDefinition def, CancellationToken ct)
    {
        var grouped = await dbContext.Patients.AsNoTracking()
            .Where(p => p.IsActive)
            .GroupBy(p => p.AddressCity ?? "Não informada")
            .Select(g => new { City = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("city", g.City),
            ("count", g.Count)));

        var total = grouped.Sum(g => g.Count);
        return Result(def, null, [
            new("city", "Cidade"),
            new("count", "Pacientes"),
        ], rows, [new ReportKpiDto("Total", total.ToString(), "primary")]);
    }

    private async Task<ReportResultDto> PatientsByInsuranceAsync(ReportDefinition def, CancellationToken ct)
    {
        var grouped = await dbContext.PatientInsurances.AsNoTracking()
            .Where(pi => pi.IsActive && pi.Patient.IsActive)
            .GroupBy(pi => pi.HealthInsurance.Name)
            .Select(g => new { Insurance = g.Key, Count = g.Select(x => x.PatientId).Distinct().Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("insurance", g.Insurance),
            ("count", g.Count)));

        var without = await dbContext.Patients.AsNoTracking()
            .CountAsync(p => p.IsActive && !p.Insurances.Any(i => i.IsActive), ct);
        if (without > 0)
        {
            rows.Add(new Dictionary<string, object?> { ["insurance"] = "Particular / sem convênio", ["count"] = without });
        }

        var total = grouped.Sum(g => g.Count) + without;
        return Result(def, null, [
            new("insurance", "Convênio"),
            new("count", "Pacientes"),
        ], rows, [new ReportKpiDto("Total", total.ToString(), "primary")]);
    }

    private async Task<ReportResultDto> ConsultationHistoryAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, ReportFilterDto filter, CancellationToken ct)
    {
        var query = dbContext.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt >= range.From && a.ScheduledAt <= range.To);

        if (filter.PatientId.HasValue)
        {
            query = query.Where(a => a.PatientId == filter.PatientId.Value);
        }

        if (filter.ProfessionalId.HasValue)
        {
            query = query.Where(a => a.ProfessionalId == filter.ProfessionalId.Value);
        }

        if (filter.SpecialtyId.HasValue)
        {
            query = query.Where(a => a.Professional.SpecialtyId == filter.SpecialtyId.Value);
        }

        if (filter.HealthInsuranceId.HasValue)
        {
            query = query.Where(a => a.Patient.Insurances.Any(i =>
                i.IsActive && i.HealthInsuranceId == filter.HealthInsuranceId.Value));
        }

        var items = await query
            .OrderByDescending(a => a.ScheduledAt)
            .Take(500)
            .Select(a => new
            {
                a.ScheduledAt,
                Patient = a.Patient.FullName,
                Doctor = a.Professional.FullName,
                Specialty = a.Professional.Specialty != null ? a.Professional.Specialty.Name : "—",
                Status = a.Status,
            })
            .ToListAsync(ct);

        var rows = items.Select(a => new Dictionary<string, object?>
        {
            ["date"] = FormatDate(a.ScheduledAt),
            ["patient"] = a.Patient,
            ["doctor"] = a.Doctor,
            ["specialty"] = a.Specialty,
            ["status"] = a.Status.ToString(),
        }).ToList();

        return Result(def, $"{range.From:dd/MM/yyyy} — {range.To:dd/MM/yyyy}", [
            new("date", "Data"),
            new("patient", "Paciente"),
            new("doctor", "Profissional"),
            new("specialty", "Especialidade"),
            new("status", "Situação"),
        ], rows, [new ReportKpiDto("Consultas", items.Count.ToString(), "primary")]);
    }

    private async Task<ReportResultDto> HospitalizationHistoryAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, ReportFilterDto filter, CancellationToken ct)
    {
        var query = dbContext.Hospitalizations.AsNoTracking()
            .Where(h => h.AdmittedAt >= range.From && h.AdmittedAt <= range.To);

        if (filter.PatientId.HasValue)
        {
            query = query.Where(h => h.PatientId == filter.PatientId.Value);
        }

        var items = await query
            .OrderByDescending(h => h.AdmittedAt)
            .Take(500)
            .Select(h => new
            {
                h.AdmittedAt,
                h.DischargedAt,
                Patient = h.Patient.FullName,
                Ward = h.Bed.Ward.Name,
                Bed = h.Bed.BedNumber,
                Doctor = h.Professional.FullName,
            })
            .ToListAsync(ct);

        var rows = items.Select(h => new Dictionary<string, object?>
        {
            ["admittedAt"] = FormatDate(h.AdmittedAt),
            ["dischargedAt"] = h.DischargedAt.HasValue ? FormatDate(h.DischargedAt.Value) : "Em andamento",
            ["patient"] = h.Patient,
            ["ward"] = h.Ward,
            ["bed"] = h.Bed,
            ["doctor"] = h.Doctor,
        }).ToList();

        return Result(def, $"{range.From:dd/MM/yyyy} — {range.To:dd/MM/yyyy}", [
            new("admittedAt", "Internação"),
            new("dischargedAt", "Alta"),
            new("patient", "Paciente"),
            new("ward", "Ala"),
            new("bed", "Leito"),
            new("doctor", "Médico"),
        ], rows, [new ReportKpiDto("Internações", items.Count.ToString(), "primary")]);
    }

    private async Task<ReportResultDto> PatientIndicatorsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var total = await dbContext.Patients.CountAsync(p => p.IsActive, ct);
        var newInPeriod = await dbContext.Patients.CountAsync(
            p => p.IsActive && p.CreatedAt >= range.From && p.CreatedAt <= range.To, ct);
        var withInsurance = await dbContext.PatientInsurances.AsNoTracking()
            .Where(pi => pi.IsActive && pi.Patient.IsActive)
            .Select(pi => pi.PatientId)
            .Distinct()
            .CountAsync(ct);
        var hospitalized = await dbContext.Hospitalizations.CountAsync(
            h => h.IsActive && h.DischargedAt == null, ct);

        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["indicator"] = "Pacientes ativos", ["value"] = total },
            new() { ["indicator"] = "Novos no período", ["value"] = newInPeriod },
            new() { ["indicator"] = "Com convênio ativo", ["value"] = withInsurance },
            new() { ["indicator"] = "Internados agora", ["value"] = hospitalized },
        };

        return Result(def, $"{range.From:dd/MM/yyyy} — {range.To:dd/MM/yyyy}", [
            new("indicator", "Indicador"),
            new("value", "Valor"),
        ], rows, [
            new ReportKpiDto("Ativos", total.ToString(), "primary"),
            new ReportKpiDto("Novos", newInPeriod.ToString(), "success"),
        ]);
    }

    private async Task<ReportResultDto> AppointmentsByStatusAsync(
        ReportDefinition def,
        (DateTime From, DateTime To) range,
        AppointmentStatus status,
        ReportFilterDto filter,
        CancellationToken ct)
    {
        var query = dbContext.Appointments.AsNoTracking()
            .Where(a => a.IsActive && a.Status == status
                && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To);

        if (filter.ProfessionalId.HasValue)
        {
            query = query.Where(a => a.ProfessionalId == filter.ProfessionalId.Value);
        }

        var items = await query
            .OrderByDescending(a => a.ScheduledAt)
            .Take(300)
            .Select(a => new
            {
                a.ScheduledAt,
                Patient = a.Patient.FullName,
                Doctor = a.Professional.FullName,
                Specialty = a.Professional.Specialty.Name,
                a.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, a => ReportRowBuilder.Row(
            ("scheduledAt", FormatDate(a.ScheduledAt)),
            ("patient", a.Patient),
            ("doctor", a.Doctor),
            ("specialty", a.Specialty),
            ("status", a.Status.ToString())));

        return Result(def, status.ToString(), [
            new("scheduledAt", "Agendamento"),
            new("patient", "Paciente"),
            new("doctor", "Profissional"),
            new("specialty", "Especialidade"),
            new("status", "Situação"),
        ], rows, [new ReportKpiDto("Total", rows.Count.ToString())]);
    }

    private async Task<ReportResultDto> AppointmentsBySpecialtyAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, ReportFilterDto filter, CancellationToken ct)
    {
        var query = dbContext.Appointments.AsNoTracking()
            .Where(a => a.IsActive && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To
                && a.Status != AppointmentStatus.Cancelled);

        if (filter.SpecialtyId.HasValue)
        {
            query = query.Where(a => a.Professional.SpecialtyId == filter.SpecialtyId.Value);
        }

        var grouped = await query
            .GroupBy(a => a.Professional.Specialty.Name)
            .Select(g => new { Specialty = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            grouped.OrderByDescending(g => g.Count),
            g => ReportRowBuilder.Row(
                ("specialty", g.Specialty),
                ("count", g.Count)));

        return Result(def, null, [
            new("specialty", "Especialidade"),
            new("count", "Atendimentos"),
        ], rows);
    }

    private async Task<ReportResultDto> AppointmentsByDoctorAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, ReportFilterDto filter, CancellationToken ct)
    {
        var query = dbContext.Appointments.AsNoTracking()
            .Where(a => a.IsActive && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To
                && a.Status != AppointmentStatus.Cancelled);

        if (filter.ProfessionalId.HasValue)
        {
            query = query.Where(a => a.ProfessionalId == filter.ProfessionalId.Value);
        }

        var grouped = await query
            .GroupBy(a => a.Professional.FullName)
            .Select(g => new { Doctor = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            grouped.OrderByDescending(g => g.Count),
            g => ReportRowBuilder.Row(
                ("doctor", g.Doctor),
                ("count", g.Count)));

        return Result(def, null, [
            new("doctor", "Profissional"),
            new("count", "Atendimentos"),
        ], rows);
    }

    private async Task<ReportResultDto> AppointmentsByUnitAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, ReportFilterDto filter, CancellationToken ct)
    {
        var query = dbContext.Appointments.AsNoTracking()
            .Where(a => a.IsActive && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To
                && a.Status != AppointmentStatus.Cancelled);

        if (filter.SpecialtyId.HasValue)
        {
            query = query.Where(a => a.Professional.SpecialtyId == filter.SpecialtyId.Value);
        }

        var items = await query
            .Select(a => new
            {
                Unit = string.IsNullOrWhiteSpace(a.Room) ? a.Professional.Specialty.Name : a.Room!,
            })
            .ToListAsync(ct);

        var grouped = items
            .GroupBy(a => a.Unit)
            .Select(g => new { Unit = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("unit", g.Unit),
            ("count", g.Count)));

        return Result(def, "Agrupado por sala ou especialidade", [
            new("unit", "Unidade / Sala"),
            new("count", "Atendimentos"),
        ], rows);
    }

    private async Task<ReportResultDto> AppointmentsAvgDurationAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, ReportFilterDto filter, CancellationToken ct)
    {
        var query = dbContext.Appointments.AsNoTracking()
            .Where(a => a.IsActive && a.Status == AppointmentStatus.Completed
                && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To);

        if (filter.ProfessionalId.HasValue)
        {
            query = query.Where(a => a.ProfessionalId == filter.ProfessionalId.Value);
        }

        var durations = await query.Select(a => a.DurationMinutes).ToListAsync(ct);
        var avg = durations.Count == 0 ? 0 : Math.Round(durations.Average(), 1);

        var byDoctor = await query
            .GroupBy(a => a.Professional.FullName)
            .Select(g => new { Doctor = g.Key, Avg = g.Average(x => x.DurationMinutes), Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(byDoctor, g => ReportRowBuilder.Row(
            ("doctor", g.Doctor),
            ("avgMinutes", Math.Round(g.Avg, 1)),
            ("count", g.Count)));

        return Result(def, $"Média geral: {avg} min", [
            new("doctor", "Profissional"),
            new("avgMinutes", "Média (min)"),
            new("count", "Consultas"),
        ], rows, [new ReportKpiDto("Tempo médio", $"{avg} min", "info")]);
    }

    private async Task<ReportResultDto> AppointmentsRescheduledAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, ReportFilterDto filter, CancellationToken ct)
    {
        var query = dbContext.Appointments.AsNoTracking()
            .Where(a => a.IsActive && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To
                && a.Status == AppointmentStatus.Cancelled
                && (EF.Functions.ILike(a.Reason ?? "", "%remarc%")
                    || EF.Functions.ILike(a.Notes ?? "", "%remarc%")));

        if (filter.PatientId.HasValue)
        {
            query = query.Where(a => a.PatientId == filter.PatientId.Value);
        }

        var items = await query
            .OrderByDescending(a => a.ScheduledAt)
            .Select(a => new
            {
                a.ScheduledAt,
                Patient = a.Patient.FullName,
                Doctor = a.Professional.FullName,
                Reason = a.Reason ?? a.Notes ?? "—",
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, a => ReportRowBuilder.Row(
            ("scheduledAt", FormatDate(a.ScheduledAt)),
            ("patient", a.Patient),
            ("doctor", a.Doctor),
            ("reason", a.Reason)));

        return Result(def, "Cancelamentos com menção a remarcação", [
            new("scheduledAt", "Agendamento original"),
            new("patient", "Paciente"),
            new("doctor", "Profissional"),
            new("reason", "Motivo"),
        ], rows, [new ReportKpiDto("Remarcações", rows.Count.ToString(), "warning")]);
    }

    private async Task<ReportResultDto> AppointmentsByInsuranceAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var data = await dbContext.Appointments.AsNoTracking()
            .Where(a => a.IsActive && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To)
            .Select(a => new
            {
                InsuranceName = a.Patient.Insurances
                    .Where(i => i.IsPrimary)
                    .Select(i => i.HealthInsurance.Name)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var rows = data
            .GroupBy(x => string.IsNullOrWhiteSpace(x.InsuranceName) ? "Particular" : x.InsuranceName!)
            .Select(g => new Dictionary<string, object?>
            {
                ["insurance"] = g.Key,
                ["count"] = g.Count(),
            })
            .OrderByDescending(r => r["count"])
            .ToList();

        return Result(def, null, [
            new("insurance", "Convênio"),
            new("count", "Consultas"),
        ], rows);
    }

    private async Task<ReportResultDto> BedOccupancyAsync(ReportDefinition def, CancellationToken ct)
    {
        var total = await dbContext.Beds.CountAsync(b => b.IsActive, ct);
        var occupied = await dbContext.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Occupied, ct);
        var available = await dbContext.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Available, ct);
        var blocked = await dbContext.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Maintenance, ct);
        var rate = total == 0 ? 0 : Math.Round(occupied * 100m / total, 1);

        var items = await dbContext.Wards.AsNoTracking()
            .Where(w => w.IsActive)
            .Select(w => new
            {
                Ward = w.Name,
                Beds = w.Beds.Count(b => b.IsActive),
                Occupied = w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Occupied),
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, w => ReportRowBuilder.Row(
            ("ward", w.Ward),
            ("beds", w.Beds),
            ("occupied", w.Occupied)));

        return Result(def, null, [
            new("ward", "Ala"),
            new("beds", "Leitos"),
            new("occupied", "Ocupados"),
        ], rows, [
            new ReportKpiDto("Ocupação", $"{rate}%", "primary"),
            new ReportKpiDto("Ocupados", occupied.ToString()),
            new ReportKpiDto("Disponíveis", available.ToString(), "success"),
            new ReportKpiDto("Bloqueados", blocked.ToString(), "warning"),
        ]);
    }

    private async Task<ReportResultDto> HospitalIndicatorsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var patients = await dbContext.Patients.CountAsync(p => p.IsActive, ct);
        var appointments = await dbContext.Appointments.CountAsync(
            a => a.IsActive && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To, ct);
        var hospitalizations = await dbContext.Hospitalizations.CountAsync(
            h => h.AdmittedAt >= range.From && h.AdmittedAt <= range.To, ct);
        var emergencies = await dbContext.EmergencyVisits.CountAsync(
            e => e.ArrivedAt >= range.From && e.ArrivedAt <= range.To, ct);

        return Result(def, $"{range.From:dd/MM/yyyy} — {range.To:dd/MM/yyyy}", [
            new("indicator", "Indicador"),
            new("value", "Valor"),
        ], [
            new() { ["indicator"] = "Pacientes ativos", ["value"] = patients },
            new() { ["indicator"] = "Consultas no período", ["value"] = appointments },
            new() { ["indicator"] = "Internações no período", ["value"] = hospitalizations },
            new() { ["indicator"] = "Atendimentos PS no período", ["value"] = emergencies },
        ]);
    }

    private async Task<ReportResultDto> EmergencyByTriageAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.EmergencyVisits.AsNoTracking()
            .Where(e => e.ArrivedAt >= range.From && e.ArrivedAt <= range.To)
            .GroupBy(e => e.Urgency)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("urgency", g.Key.ToString()),
            ("count", g.Count)));

        return Result(def, null, [
            new("urgency", "Classificação"),
            new("count", "Atendimentos"),
        ], rows);
    }

    private async Task<ReportResultDto> EmergencyWaitByTriageAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var data = await dbContext.EmergencyVisits.AsNoTracking()
            .Where(e => e.ArrivedAt >= range.From && e.ArrivedAt <= range.To && e.StartedAt != null)
            .Select(e => new { e.Urgency, e.ArrivedAt, StartedAt = e.StartedAt!.Value })
            .ToListAsync(ct);

        var rows = data
            .GroupBy(e => e.Urgency)
            .Select(g => new Dictionary<string, object?>
            {
                ["urgency"] = g.Key.ToString(),
                ["avgMinutes"] = Math.Round(g.Average(x => (x.StartedAt - x.ArrivedAt).TotalMinutes), 1),
            })
            .ToList();

        return Result(def, null, [
            new("urgency", "Classificação"),
            new("avgMinutes", "Espera média (min)"),
        ], rows);
    }

    private async Task<ReportResultDto> EmergencyServedAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var count = await dbContext.EmergencyVisits.CountAsync(
            e => e.ArrivedAt >= range.From && e.ArrivedAt <= range.To
                && (e.Status == EmergencyVisitStatus.Discharged || e.Status == EmergencyVisitStatus.Referred), ct);

        var items = await dbContext.EmergencyVisits.AsNoTracking()
            .Where(e => e.ArrivedAt >= range.From && e.ArrivedAt <= range.To)
            .OrderByDescending(e => e.ArrivedAt)
            .Take(200)
            .Select(e => new
            {
                e.ArrivedAt,
                Patient = e.Patient.FullName,
                e.Urgency,
                e.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, e => ReportRowBuilder.Row(
            ("arrivedAt", FormatDate(e.ArrivedAt)),
            ("patient", e.Patient),
            ("urgency", e.Urgency.ToString()),
            ("status", e.Status.ToString())));

        return Result(def, null, [
            new("arrivedAt", "Chegada"),
            new("patient", "Paciente"),
            new("urgency", "Risco"),
            new("status", "Situação"),
        ], rows, [new ReportKpiDto("Atendidos", count.ToString())]);
    }

    private async Task<ReportResultDto> EmergencyByStatusAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, EmergencyVisitStatus status, CancellationToken ct)
    {
        var items = await dbContext.EmergencyVisits.AsNoTracking()
            .Where(e => e.ArrivedAt >= range.From && e.ArrivedAt <= range.To && e.Status == status)
            .Select(e => new
            {
                Patient = e.Patient.FullName,
                e.ArrivedAt,
                e.ChiefComplaint,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, e => ReportRowBuilder.Row(
            ("patient", e.Patient),
            ("arrivedAt", FormatDate(e.ArrivedAt)),
            ("complaint", e.ChiefComplaint)));

        return Result(def, status.ToString(), [
            new("patient", "Paciente"),
            new("arrivedAt", "Chegada"),
            new("complaint", "Queixa"),
        ], rows, [new ReportKpiDto("Total", rows.Count.ToString())]);
    }

    private async Task<ReportResultDto> EmergencyAdmittedAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.Hospitalizations.AsNoTracking()
            .Where(h => h.AdmittedAt >= range.From && h.AdmittedAt <= range.To)
            .Where(h => dbContext.EmergencyVisits.Any(e =>
                e.PatientId == h.PatientId && e.ArrivedAt >= range.From && e.ArrivedAt <= h.AdmittedAt))
            .Select(h => new
            {
                Patient = h.Patient.FullName,
                h.AdmittedAt,
                Diagnosis = h.Diagnosis ?? h.Reason,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, h => ReportRowBuilder.Row(
            ("patient", h.Patient),
            ("admittedAt", FormatDate(h.AdmittedAt)),
            ("diagnosis", h.Diagnosis)));

        return Result(def, "Internações após passagem pelo PS", [
            new("patient", "Paciente"),
            new("admittedAt", "Internação"),
            new("diagnosis", "Diagnóstico"),
        ], rows);
    }

    private async Task<ReportResultDto> EmergencyAvgStayAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var data = await dbContext.EmergencyVisits.AsNoTracking()
            .Where(e => e.ArrivedAt >= range.From && e.ArrivedAt <= range.To && e.DischargedAt != null)
            .Select(e => new { e.ArrivedAt, DischargedAt = e.DischargedAt!.Value })
            .ToListAsync(ct);

        var avg = data.Count == 0 ? 0 : Math.Round(data.Average(x => (x.DischargedAt - x.ArrivedAt).TotalMinutes), 1);
        return Result(def, null, [
            new("metric", "Métrica"),
            new("value", "Valor"),
        ], [
            new() { ["metric"] = "Permanência média (min)", ["value"] = avg },
            new() { ["metric"] = "Atendimentos com alta", ["value"] = data.Count },
        ], [new ReportKpiDto("Média", $"{avg} min")]);
    }

    private async Task<ReportResultDto> EmergencyTopComplaintsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.EmergencyVisits.AsNoTracking()
            .Where(e => e.ArrivedAt >= range.From && e.ArrivedAt <= range.To)
            .GroupBy(e => e.ChiefComplaint)
            .Select(g => new { Complaint = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            grouped.OrderByDescending(g => g.Count).Take(20),
            g => ReportRowBuilder.Row(
                ("complaint", g.Complaint),
                ("count", g.Count)));

        return Result(def, "Top queixas no pronto atendimento", [
            new("complaint", "Queixa"),
            new("count", "Ocorrências"),
        ], rows);
    }

    private async Task<ReportResultDto> CurrentHospitalizationsAsync(ReportDefinition def, CancellationToken ct)
    {
        var items = await dbContext.Hospitalizations.AsNoTracking()
            .Where(h => h.Status == HospitalizationStatus.Active)
            .Select(h => new
            {
                Patient = h.Patient.FullName,
                Ward = h.Bed.Ward.Name,
                Bed = h.Bed.BedNumber,
                Doctor = h.Professional.FullName,
                h.AdmittedAt,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, h => ReportRowBuilder.Row(
            ("patient", h.Patient),
            ("ward", h.Ward),
            ("bed", h.Bed),
            ("doctor", h.Doctor),
            ("admittedAt", FormatDate(h.AdmittedAt))));

        return Result(def, null, [
            new("patient", "Paciente"),
            new("ward", "Ala"),
            new("bed", "Leito"),
            new("doctor", "Médico"),
            new("admittedAt", "Internação"),
        ], rows, [new ReportKpiDto("Internados", rows.Count.ToString(), "primary")]);
    }

    private async Task<ReportResultDto> AdmissionsByPeriodAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.Hospitalizations.AsNoTracking()
            .Where(h => h.AdmittedAt >= range.From && h.AdmittedAt <= range.To)
            .GroupBy(h => h.AdmittedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            grouped.OrderBy(g => g.Date),
            g => ReportRowBuilder.Row(
                ("date", g.Date.ToString("dd/MM/yyyy")),
                ("count", g.Count)));

        return Result(def, null, [
            new("date", "Data"),
            new("count", "Internações"),
        ], rows);
    }

    private async Task<ReportResultDto> InternalTransfersAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.BedTransfers.AsNoTracking()
            .Where(t => t.IsActive && t.TransferredAt >= range.From && t.TransferredAt <= range.To)
            .Select(t => new
            {
                Patient = t.Hospitalization.Patient.FullName,
                From = t.FromBed.Ward.Name + " / " + t.FromBed.BedNumber,
                To = t.ToBed.Ward.Name + " / " + t.ToBed.BedNumber,
                t.TransferredAt,
                Reason = t.Reason ?? "—",
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, t => ReportRowBuilder.Row(
            ("patient", t.Patient),
            ("from", t.From),
            ("to", t.To),
            ("transferredAt", FormatDate(t.TransferredAt)),
            ("reason", t.Reason)));

        return Result(def, null, [
            new("patient", "Paciente"),
            new("from", "Origem"),
            new("to", "Destino"),
            new("transferredAt", "Data"),
            new("reason", "Motivo"),
        ], rows, [new ReportKpiDto("Transferências", rows.Count.ToString())]);
    }

    private async Task<ReportResultDto> DischargesAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.Hospitalizations.AsNoTracking()
            .Where(h => h.DischargedAt >= range.From && h.DischargedAt <= range.To)
            .Select(h => new
            {
                Patient = h.Patient.FullName,
                DischargedAt = h.DischargedAt!.Value,
                Diagnosis = h.Diagnosis ?? "—",
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, h => ReportRowBuilder.Row(
            ("patient", h.Patient),
            ("dischargedAt", FormatDate(h.DischargedAt)),
            ("diagnosis", h.Diagnosis)));

        return Result(def, null, [
            new("patient", "Paciente"),
            new("dischargedAt", "Alta"),
            new("diagnosis", "Diagnóstico"),
        ], rows, [new ReportKpiDto("Altas", rows.Count.ToString())]);
    }

    private async Task<ReportResultDto> DeathsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.Hospitalizations.AsNoTracking()
            .Where(h => h.DischargedAt >= range.From && h.DischargedAt <= range.To)
            .Where(h => EF.Functions.ILike(h.Diagnosis ?? h.Reason, "%óbito%")
                || EF.Functions.ILike(h.Diagnosis ?? h.Reason, "%obito%"))
            .Select(h => new
            {
                Patient = h.Patient.FullName,
                Date = h.DischargedAt!.Value,
                Notes = h.Diagnosis ?? h.Reason,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, h => ReportRowBuilder.Row(
            ("patient", h.Patient),
            ("date", FormatDate(h.Date)),
            ("notes", h.Notes)));

        return Result(def, "Registros com menção a óbito no diagnóstico", [
            new("patient", "Paciente"),
            new("date", "Data"),
            new("notes", "Observação"),
        ], rows, [new ReportKpiDto("Óbitos", rows.Count.ToString())]);
    }

    private async Task<ReportResultDto> BedTurnoverAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var admissions = await dbContext.Hospitalizations
            .CountAsync(h => h.AdmittedAt >= range.From && h.AdmittedAt <= range.To, ct);
        var totalBeds = await dbContext.Beds.CountAsync(b => b.IsActive, ct);
        var days = Math.Max(1, (range.To - range.From).TotalDays);
        var turnover = totalBeds == 0 ? 0 : Math.Round(admissions / (decimal)totalBeds, 2);
        var dailyRate = totalBeds == 0 ? 0 : Math.Round(admissions / (decimal)totalBeds / (decimal)days * 30, 2);

        return Result(def, $"Período: {days:F0} dias", [
            new("metric", "Métrica"),
            new("value", "Valor"),
        ], [
            new() { ["metric"] = "Internações no período", ["value"] = admissions },
            new() { ["metric"] = "Leitos ativos", ["value"] = totalBeds },
            new() { ["metric"] = "Giro (internações/leito)", ["value"] = turnover },
            new() { ["metric"] = "Giro mensal estimado", ["value"] = dailyRate },
        ], [new ReportKpiDto("Giro de leitos", turnover.ToString(), "primary")]);
    }

    private async Task<ReportResultDto> AvgLengthOfStayAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var data = await dbContext.Hospitalizations.AsNoTracking()
            .Where(h => h.DischargedAt >= range.From && h.DischargedAt <= range.To)
            .Select(h => new { h.AdmittedAt, DischargedAt = h.DischargedAt!.Value })
            .ToListAsync(ct);

        var avgHours = data.Count == 0 ? 0 : Math.Round(data.Average(x => (x.DischargedAt - x.AdmittedAt).TotalHours) / 24.0, 1);
        return Result(def, null, [
            new("metric", "Métrica"),
            new("value", "Valor"),
        ], [
            new() { ["metric"] = "Permanência média (dias)", ["value"] = avgHours },
            new() { ["metric"] = "Altas analisadas", ["value"] = data.Count },
        ]);
    }

    private async Task<ReportResultDto> BedsByStatusAsync(
        ReportDefinition def, BedStatus status, CancellationToken ct)
    {
        var items = await dbContext.Beds.AsNoTracking()
            .Where(b => b.IsActive && b.Status == status)
            .Select(b => new
            {
                Ward = b.Ward.Name,
                Bed = b.BedNumber,
                b.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, b => ReportRowBuilder.Row(
            ("ward", b.Ward),
            ("bed", b.Bed),
            ("status", b.Status.ToString())));

        return Result(def, status.ToString(), [
            new("ward", "Ala"),
            new("bed", "Leito"),
            new("status", "Situação"),
        ], rows, [new ReportKpiDto("Total", rows.Count.ToString())]);
    }

    private async Task<ReportResultDto> MedicalRecordEntriesAsync(
        ReportDefinition def,
        (DateTime From, DateTime To) range,
        MedicalRecordEntryType type,
        ReportFilterDto filter,
        CancellationToken ct)
    {
        var query = dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.IsActive && e.EntryType == type
                && e.CreatedAt >= range.From && e.CreatedAt <= range.To);

        if (filter.PatientId.HasValue)
        {
            query = query.Where(e => e.MedicalRecord.PatientId == filter.PatientId.Value);
        }

        if (filter.ProfessionalId.HasValue)
        {
            query = query.Where(e => e.ProfessionalId == filter.ProfessionalId.Value);
        }

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(300)
            .Select(e => new
            {
                e.CreatedAt,
                Patient = e.MedicalRecord.Patient.FullName,
                Professional = e.Professional != null ? e.Professional.FullName : "—",
                Cid = e.Cid10Code ?? "—",
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, e => ReportRowBuilder.Row(
            ("date", FormatDate(e.CreatedAt)),
            ("patient", e.Patient),
            ("professional", e.Professional),
            ("cid", e.Cid)));

        return Result(def, type.ToString(), [
            new("date", "Data"),
            new("patient", "Paciente"),
            new("professional", "Profissional"),
            new("cid", "CID"),
        ], rows);
    }

    private async Task<ReportResultDto> ExpiredPrescriptionsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var items = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.IsActive && e.EntryType == MedicalRecordEntryType.Prescription
                && e.CreatedAt <= cutoff && !e.IsSigned)
            .Select(e => new
            {
                e.CreatedAt,
                Patient = e.MedicalRecord.Patient.FullName,
                Professional = e.Professional != null ? e.Professional.FullName : "—",
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, e => ReportRowBuilder.Row(
            ("date", FormatDate(e.CreatedAt)),
            ("patient", e.Patient),
            ("professional", e.Professional)));

        return Result(def, "Prescrições não assinadas há mais de 7 dias", [
            new("date", "Data"),
            new("patient", "Paciente"),
            new("professional", "Profissional"),
        ], rows);
    }

    private async Task<ReportResultDto> DiagnosesAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.IsActive && e.Cid10Code != null
                && e.CreatedAt >= range.From && e.CreatedAt <= range.To)
            .OrderByDescending(e => e.CreatedAt)
            .Take(300)
            .Select(e => new
            {
                e.CreatedAt,
                Patient = e.MedicalRecord.Patient.FullName,
                e.Cid10Code,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, e => ReportRowBuilder.Row(
            ("date", FormatDate(e.CreatedAt)),
            ("patient", e.Patient),
            ("cid", e.Cid10Code)));

        return Result(def, null, [
            new("date", "Data"),
            new("patient", "Paciente"),
            new("cid", "CID-10"),
        ], rows);
    }

    private async Task<ReportResultDto> TopCidAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.Cid10Code != null && e.CreatedAt >= range.From && e.CreatedAt <= range.To)
            .GroupBy(e => e.Cid10Code!)
            .Select(g => new { Cid = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            grouped.OrderByDescending(g => g.Count).Take(25),
            g => ReportRowBuilder.Row(
                ("cid", g.Cid),
                ("count", g.Count)));

        return Result(def, null, [
            new("cid", "CID-10"),
            new("count", "Ocorrências"),
        ], rows);
    }

    private async Task<ReportResultDto> PatientHistoryAsync(
        ReportDefinition def, ReportFilterDto filter, CancellationToken ct)
    {
        if (!filter.PatientId.HasValue)
        {
            return Result(def, "Informe o paciente no filtro", [
                new("info", "Instrução"),
            ], [
                new Dictionary<string, object?> { ["info"] = "Selecione um paciente para gerar o histórico completo." },
            ]);
        }

        var items = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.MedicalRecord.PatientId == filter.PatientId.Value)
            .OrderByDescending(e => e.CreatedAt)
            .Take(500)
            .Select(e => new
            {
                e.CreatedAt,
                e.EntryType,
                Cid = e.Cid10Code ?? "—",
                Professional = e.Professional != null ? e.Professional.FullName : "—",
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, e => ReportRowBuilder.Row(
            ("date", FormatDate(e.CreatedAt)),
            ("type", e.EntryType.ToString()),
            ("cid", e.Cid),
            ("professional", e.Professional)));

        return Result(def, "Histórico clínico consolidado", [
            new("date", "Data"),
            new("type", "Tipo"),
            new("cid", "CID"),
            new("professional", "Profissional"),
        ], rows);
    }

    private async Task<ReportResultDto> PharmacyDispensedAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.PharmacyDispensings.AsNoTracking()
            .Where(d => d.DispensedAt >= range.From && d.DispensedAt <= range.To)
            .Select(d => new
            {
                d.DispensedAt,
                Patient = d.Patient.FullName,
                Product = d.Product.Name,
                d.Quantity,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, d => ReportRowBuilder.Row(
            ("date", FormatDate(d.DispensedAt)),
            ("patient", d.Patient),
            ("product", d.Product),
            ("quantity", d.Quantity)));

        return Result(def, null, [
            new("date", "Dispensação"),
            new("patient", "Paciente"),
            new("product", "Medicamento"),
            new("quantity", "Qtd"),
        ], rows);
    }

    private async Task<ReportResultDto> VitalSignsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.VitalSignRecords.AsNoTracking()
            .Where(v => v.CreatedAt >= range.From && v.CreatedAt <= range.To)
            .OrderByDescending(v => v.CreatedAt)
            .Take(300)
            .Select(v => new
            {
                v.CreatedAt,
                v.HeartRate,
                v.SystolicBp,
                v.DiastolicBp,
                v.SpO2,
                v.Temperature,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, v => ReportRowBuilder.Row(
            ("date", FormatDate(v.CreatedAt)),
            ("hr", v.HeartRate),
            ("bp", $"{v.SystolicBp}/{v.DiastolicBp}"),
            ("spo2", v.SpO2),
            ("temp", v.Temperature)));

        return Result(def, null, [
            new("date", "Registro"),
            new("hr", "FC"),
            new("bp", "PA"),
            new("spo2", "SpO₂"),
            new("temp", "Temp"),
        ], rows);
    }

    private async Task<ReportResultDto> SurgeriesByStatusAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, SurgeryStatus status, CancellationToken ct)
    {
        var items = await dbContext.Surgeries.AsNoTracking()
            .Where(s => s.IsActive && s.Status == status
                && s.ScheduledAt >= range.From && s.ScheduledAt <= range.To)
            .Select(s => new
            {
                s.ScheduledAt,
                Patient = s.Patient.FullName,
                s.ProcedureName,
                Surgeon = s.Surgeon.FullName,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, s => ReportRowBuilder.Row(
            ("date", FormatDate(s.ScheduledAt)),
            ("patient", s.Patient),
            ("procedure", s.ProcedureName),
            ("surgeon", s.Surgeon)));

        return Result(def, status.ToString(), [
            new("date", "Data"),
            new("patient", "Paciente"),
            new("procedure", "Procedimento"),
            new("surgeon", "Cirurgião"),
        ], rows);
    }

    private async Task<ReportResultDto> SurgeriesBySpecialtyAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.Surgeries.AsNoTracking()
            .Where(s => s.IsActive && s.ScheduledAt >= range.From && s.ScheduledAt <= range.To)
            .GroupBy(s => s.Surgeon.Specialty.Name)
            .Select(g => new { Specialty = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("specialty", g.Specialty),
            ("count", g.Count)));

        return Result(def, null, [
            new("specialty", "Especialidade"),
            new("count", "Cirurgias"),
        ], rows);
    }

    private async Task<ReportResultDto> SurgeriesBySurgeonAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.Surgeries.AsNoTracking()
            .Where(s => s.IsActive && s.ScheduledAt >= range.From && s.ScheduledAt <= range.To)
            .GroupBy(s => s.Surgeon.FullName)
            .Select(g => new { Surgeon = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            grouped.OrderByDescending(g => g.Count),
            g => ReportRowBuilder.Row(
                ("surgeon", g.Surgeon),
                ("count", g.Count)));

        return Result(def, null, [
            new("surgeon", "Cirurgião"),
            new("count", "Cirurgias"),
        ], rows);
    }

    private async Task<ReportResultDto> SurgeryAvgDurationAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var avg = await dbContext.Surgeries.AsNoTracking()
            .Where(s => s.IsActive && s.ScheduledAt >= range.From && s.ScheduledAt <= range.To)
            .AverageAsync(s => (double?)s.EstimatedDurationMinutes, ct) ?? 0;

        return Result(def, null, [
            new("metric", "Métrica"),
            new("value", "Valor"),
        ], [
            new() { ["metric"] = "Duração média estimada (min)", ["value"] = Math.Round(avg, 1) },
        ]);
    }

    private async Task<ReportResultDto> SurgeryRoomOccupancyAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.OperatingRooms.AsNoTracking()
            .Select(r => new
            {
                Room = r.Name,
                Surgeries = r.Surgeries.Count(s => s.ScheduledAt >= range.From && s.ScheduledAt <= range.To),
                r.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, r => ReportRowBuilder.Row(
            ("room", r.Room),
            ("surgeries", r.Surgeries),
            ("status", r.Status.ToString())));

        return Result(def, null, [
            new("room", "Sala"),
            new("surgeries", "Cirurgias"),
            new("status", "Situação"),
        ], rows);
    }

    private async Task<ReportResultDto> StockCurrentAsync(ReportDefinition def, CancellationToken ct)
    {
        var items = await dbContext.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                Product = p.Name,
                p.Sku,
                p.QuantityOnHand,
                p.MinimumStock,
                p.Unit,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, p => ReportRowBuilder.Row(
            ("product", p.Product),
            ("sku", p.Sku),
            ("quantity", p.QuantityOnHand),
            ("minimum", p.MinimumStock),
            ("unit", p.Unit)));

        return Result(def, null, [
            new("product", "Produto"),
            new("sku", "SKU"),
            new("quantity", "Saldo"),
            new("minimum", "Mínimo"),
            new("unit", "Unidade"),
        ], rows);
    }

    private async Task<ReportResultDto> StockMovementsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.StockMovements.AsNoTracking()
            .Where(m => m.CreatedAt >= range.From && m.CreatedAt <= range.To)
            .OrderByDescending(m => m.CreatedAt)
            .Take(300)
            .Select(m => new
            {
                m.CreatedAt,
                Product = m.Product.Name,
                m.Type,
                m.Quantity,
                m.Reason,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, m => ReportRowBuilder.Row(
            ("date", FormatDate(m.CreatedAt)),
            ("product", m.Product),
            ("type", m.Type.ToString()),
            ("quantity", m.Quantity),
            ("reason", m.Reason)));

        return Result(def, null, [
            new("date", "Data"),
            new("product", "Produto"),
            new("type", "Tipo"),
            new("quantity", "Qtd"),
            new("reason", "Motivo"),
        ], rows);
    }

    private async Task<ReportResultDto> StockMovementsByTypeAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, StockMovementType type, CancellationToken ct)
    {
        var grouped = await dbContext.StockMovements.AsNoTracking()
            .Where(m => m.CreatedAt >= range.From && m.CreatedAt <= range.To && m.Type == type)
            .GroupBy(m => m.Product.Name)
            .Select(g => new { Product = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            grouped.OrderByDescending(g => g.Quantity),
            g => ReportRowBuilder.Row(
                ("product", g.Product),
                ("quantity", g.Quantity)));

        return Result(def, type.ToString(), [
            new("product", "Produto"),
            new("quantity", "Quantidade"),
        ], rows);
    }

    private async Task<ReportResultDto> StockLowAsync(ReportDefinition def, CancellationToken ct)
    {
        var items = await dbContext.Products.AsNoTracking()
            .Where(p => p.IsActive && p.QuantityOnHand <= p.MinimumStock)
            .Select(p => new
            {
                Product = p.Name,
                p.QuantityOnHand,
                p.MinimumStock,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, p => ReportRowBuilder.Row(
            ("product", p.Product),
            ("quantity", p.QuantityOnHand),
            ("minimum", p.MinimumStock)));

        return Result(def, "Itens abaixo ou no estoque mínimo", [
            new("product", "Produto"),
            new("quantity", "Saldo"),
            new("minimum", "Mínimo"),
        ], rows, [new ReportKpiDto("Itens críticos", rows.Count.ToString(), "warning")]);
    }

    private async Task<ReportResultDto> ExpiredProductsAsync(ReportDefinition def, CancellationToken ct)
    {
        var items = await dbContext.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Type == ProductType.Medication && p.QuantityOnHand <= p.MinimumStock)
            .Select(p => new
            {
                Product = p.Name,
                p.QuantityOnHand,
                p.MinimumStock,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, p => ReportRowBuilder.Row(
            ("product", p.Product),
            ("quantity", p.QuantityOnHand),
            ("minimum", p.MinimumStock)));

        return Result(def, "Medicamentos abaixo do estoque mínimo (controle de validade em fase 2)", [
            new("product", "Produto"),
            new("quantity", "Saldo"),
            new("minimum", "Mínimo"),
        ], rows);
    }

    private async Task<ReportResultDto> ExpiringProductsAsync(ReportDefinition def, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var limit = today.AddDays(90);

        var items = await dbContext.StockMovements.AsNoTracking()
            .Where(m => m.IsActive && m.ExpiryDate != null
                && m.ExpiryDate >= today && m.ExpiryDate <= limit
                && m.Product.Type == ProductType.Medication)
            .Select(m => new
            {
                Product = m.Product.Name,
                m.BatchNumber,
                Expiry = m.ExpiryDate!.Value,
                m.Quantity,
            })
            .ToListAsync(ct);

        var distinct = items
            .GroupBy(i => new { i.Product, i.BatchNumber, i.Expiry })
            .Select(g => g.First())
            .OrderBy(i => i.Expiry)
            .ToList();

        var rows = ReportRowBuilder.From(distinct, p => ReportRowBuilder.Row(
            ("product", p.Product),
            ("batch", p.BatchNumber ?? "—"),
            ("expiry", p.Expiry.ToString("dd/MM/yyyy")),
            ("quantity", p.Quantity),
            ("daysLeft", (p.Expiry.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days)));

        return Result(def, "Lotes com validade nos próximos 90 dias", [
            new("product", "Produto"),
            new("batch", "Lote"),
            new("expiry", "Validade"),
            new("quantity", "Qtd. movimento"),
            new("daysLeft", "Dias restantes"),
        ], rows, [new ReportKpiDto("Lotes", rows.Count.ToString(), "warning")]);
    }

    private async Task<ReportResultDto> LabOrdersAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, LabOrderStatus? status, CancellationToken ct)
    {
        var query = dbContext.LabOrders.AsNoTracking()
            .Where(o => o.IsActive && o.CreatedAt >= range.From && o.CreatedAt <= range.To);
        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var items = await query
            .Select(o => new
            {
                o.CreatedAt,
                Patient = o.Patient.FullName,
                o.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, o => ReportRowBuilder.Row(
            ("date", FormatDate(o.CreatedAt)),
            ("patient", o.Patient),
            ("status", o.Status.ToString())));

        return Result(def, status?.ToString() ?? "Todos", [
            new("date", "Solicitação"),
            new("patient", "Paciente"),
            new("status", "Situação"),
        ], rows);
    }

    private async Task<ReportResultDto> LabOrdersPendingAsync(ReportDefinition def, CancellationToken ct)
    {
        var items = await dbContext.LabOrders.AsNoTracking()
            .Where(o => o.IsActive && o.Status != LabOrderStatus.Completed && o.Status != LabOrderStatus.Cancelled)
            .Select(o => new
            {
                o.CreatedAt,
                Patient = o.Patient.FullName,
                o.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, o => ReportRowBuilder.Row(
            ("date", FormatDate(o.CreatedAt)),
            ("patient", o.Patient),
            ("status", o.Status.ToString())));

        return Result(def, null, [
            new("date", "Solicitação"),
            new("patient", "Paciente"),
            new("status", "Situação"),
        ], rows, [new ReportKpiDto("Pendentes", rows.Count.ToString(), "warning")]);
    }

    private async Task<ReportResultDto> LabAvgReleaseAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var data = await dbContext.LabOrders.AsNoTracking()
            .Where(o => o.Status == LabOrderStatus.Completed
                && o.UpdatedAt >= range.From && o.UpdatedAt <= range.To)
            .Select(o => new { o.CreatedAt, CompletedAt = o.UpdatedAt!.Value })
            .ToListAsync(ct);

        var avg = data.Count == 0 ? 0 : Math.Round(data.Average(x => (x.CompletedAt - x.CreatedAt).TotalHours), 1);
        return Result(def, null, [
            new("metric", "Métrica"),
            new("value", "Valor"),
        ], [
            new() { ["metric"] = "Liberação média (horas)", ["value"] = avg },
        ]);
    }

    private async Task<ReportResultDto> LabByDoctorAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.LabOrders.AsNoTracking()
            .Where(o => o.CreatedAt >= range.From && o.CreatedAt <= range.To)
            .GroupBy(o => o.RequestingProfessional.FullName)
            .Select(g => new { Doctor = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("doctor", g.Doctor),
            ("count", g.Count)));

        return Result(def, null, [
            new("doctor", "Solicitante"),
            new("count", "Exames"),
        ], rows);
    }

    private async Task<ReportResultDto> LabProductionAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.LabOrders.AsNoTracking()
            .Where(o => o.CreatedAt >= range.From && o.CreatedAt <= range.To)
            .GroupBy(o => o.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("status", g.Key.ToString()),
            ("count", g.Count)));

        return Result(def, "Produção por status", [
            new("status", "Situação"),
            new("count", "Quantidade"),
        ], rows);
    }

    private async Task<ReportResultDto> PathologySummaryAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.LabOrderItems.AsNoTracking()
            .Where(i => i.IsActive
                && i.CreatedAt >= range.From
                && i.CreatedAt <= range.To
                && (EF.Functions.ILike(i.LabExamCatalog.Name, "%pato%")
                    || EF.Functions.ILike(i.LabExamCatalog.Name, "%biops%")))
            .Select(i => new
            {
                i.CreatedAt,
                Exam = i.LabExamCatalog.Name,
                Patient = i.LabOrder.Patient.FullName,
                Status = i.Status.ToString(),
            })
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, i => ReportRowBuilder.Row(
            ("date", i.CreatedAt.ToString("dd/MM/yyyy HH:mm")),
            ("exam", i.Exam),
            ("patient", i.Patient),
            ("status", i.Status)));

        return Result(def, "Pedidos e resultados de patologia/biopsia", [
            new("date", "Data"),
            new("exam", "Exame"),
            new("patient", "Paciente"),
            new("status", "Status"),
        ], rows, [
            new ReportKpiDto("Itens", items.Count.ToString(), "primary"),
            new ReportKpiDto("Concluídos", items.Count(i => i.Status == LabItemStatus.Completed.ToString()).ToString(), "success"),
        ]);
    }

    private async Task<ReportResultDto> ImagingByModalityAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, ImagingModality modality, CancellationToken ct)
    {
        var items = await dbContext.ImagingStudies.AsNoTracking()
            .Where(s => s.IsActive && s.Modality == modality
                && s.ScheduledAt >= range.From && s.ScheduledAt <= range.To)
            .Select(s => new
            {
                s.ScheduledAt,
                Patient = s.Patient.FullName,
                s.StudyDescription,
                s.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, s => ReportRowBuilder.Row(
            ("date", FormatDate(s.ScheduledAt)),
            ("patient", s.Patient),
            ("description", s.StudyDescription),
            ("status", s.Status.ToString())));

        return Result(def, modality.ToString(), [
            new("date", "Agendamento"),
            new("patient", "Paciente"),
            new("description", "Exame"),
            new("status", "Situação"),
        ], rows);
    }

    private async Task<ReportResultDto> ImagingAvgReportTimeAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var data = await dbContext.ImagingStudies.AsNoTracking()
            .Where(s => s.ReportedAt >= range.From && s.ReportedAt <= range.To && s.CompletedAt != null)
            .Select(s => new { CompletedAt = s.CompletedAt!.Value, ReportedAt = s.ReportedAt!.Value })
            .ToListAsync(ct);

        var avg = data.Count == 0 ? 0 : Math.Round(data.Average(x => (x.ReportedAt - x.CompletedAt).TotalHours), 1);
        return Result(def, null, [
            new("metric", "Métrica"),
            new("value", "Valor"),
        ], [
            new() { ["metric"] = "Entrega média do laudo (horas)", ["value"] = avg },
        ]);
    }

    private async Task<ReportResultDto> ImagingByDoctorAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.ImagingStudies.AsNoTracking()
            .Where(s => s.ReportedAt >= range.From && s.ReportedAt <= range.To && s.ReportingProfessionalId != null)
            .GroupBy(s => s.ReportingProfessional!.FullName)
            .Select(g => new { Doctor = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("doctor", g.Doctor),
            ("count", g.Count)));

        return Result(def, null, [
            new("doctor", "Radiologista"),
            new("count", "Laudos"),
        ], rows);
    }

    private async Task<ReportResultDto> RevenueAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var amount = await dbContext.FinancialPayments.AsNoTracking()
            .Where(p => p.IsActive && p.PaidAt >= range.From && p.PaidAt <= range.To
                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable)
            .SumAsync(p => p.Amount, ct);

        var grouped = await dbContext.FinancialPayments.AsNoTracking()
            .Where(p => p.IsActive && p.PaidAt >= range.From && p.PaidAt <= range.To
                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable)
            .GroupBy(p => p.PaidAt.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            grouped.OrderBy(g => g.Date),
            g => ReportRowBuilder.Row(
                ("date", g.Date.ToString("dd/MM/yyyy")),
                ("amount", FormatMoney(g.Amount))));

        return Result(def, null, [
            new("date", "Data"),
            new("amount", "Receita (R$)"),
        ], rows, [new ReportKpiDto("Faturamento bruto", FormatMoney(amount), "success")]);
    }

    private async Task<ReportResultDto> NetRevenueAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var revenue = await dbContext.FinancialPayments.AsNoTracking()
            .Where(p => p.IsActive && p.PaidAt >= range.From && p.PaidAt <= range.To
                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable)
            .SumAsync(p => p.Amount, ct);

        var expenses = await dbContext.FinancialPayments.AsNoTracking()
            .Where(p => p.IsActive && p.PaidAt >= range.From && p.PaidAt <= range.To
                && p.FinancialAccount.Direction == FinancialAccountDirection.Payable)
            .SumAsync(p => p.Amount, ct);

        return Result(def, null, [
            new("item", "Item"),
            new("amount", "Valor (R$)"),
        ], [
            new() { ["item"] = "Receitas", ["amount"] = FormatMoney(revenue) },
            new() { ["item"] = "Despesas", ["amount"] = FormatMoney(expenses) },
            new() { ["item"] = "Líquido", ["amount"] = FormatMoney(revenue - expenses) },
        ], [new ReportKpiDto("Líquido", FormatMoney(revenue - expenses), "primary")]);
    }

    private async Task<ReportResultDto> ExpensesAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.FinancialPayments.AsNoTracking()
            .Where(p => p.IsActive && p.PaidAt >= range.From && p.PaidAt <= range.To
                && p.FinancialAccount.Direction == FinancialAccountDirection.Payable)
            .GroupBy(p => p.FinancialAccount.Category)
            .Select(g => new { g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("category", g.Key.ToString()),
            ("amount", FormatMoney(g.Amount))));

        return Result(def, null, [
            new("category", "Categoria"),
            new("amount", "Valor (R$)"),
        ], rows);
    }

    private async Task<ReportResultDto> CashflowAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.FinancialPayments.AsNoTracking()
            .Where(p => p.IsActive && p.PaidAt >= range.From && p.PaidAt <= range.To)
            .GroupBy(p => new { p.PaidAt.Date, p.FinancialAccount.Direction })
            .Select(g => new { g.Key.Date, g.Key.Direction, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            grouped.OrderBy(g => g.Date),
            g => ReportRowBuilder.Row(
                ("date", g.Date.ToString("dd/MM/yyyy")),
                ("direction", g.Direction.ToString()),
                ("amount", FormatMoney(g.Amount))));

        return Result(def, null, [
            new("date", "Data"),
            new("direction", "Tipo"),
            new("amount", "Valor (R$)"),
        ], rows);
    }

    private async Task<ReportResultDto> PayablesAsync(ReportDefinition def, CancellationToken ct)
    {
        var items = await dbContext.FinancialAccounts.AsNoTracking()
            .Where(f => f.IsActive && f.Direction == FinancialAccountDirection.Payable
                && f.Status != FinancialAccountStatus.Paid)
            .Select(f => new
            {
                f.Description,
                f.DueDate,
                Balance = f.Amount - f.PaidAmount,
                f.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, f => ReportRowBuilder.Row(
            ("description", f.Description),
            ("dueDate", f.DueDate?.ToString("dd/MM/yyyy") ?? "—"),
            ("balance", FormatMoney(f.Balance)),
            ("status", f.Status.ToString())));

        return Result(def, null, [
            new("description", "Descrição"),
            new("dueDate", "Vencimento"),
            new("balance", "Saldo (R$)"),
            new("status", "Situação"),
        ], rows);
    }

    private async Task<ReportResultDto> ReceivablesAsync(ReportDefinition def, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var items = await dbContext.FinancialAccounts.AsNoTracking()
            .Where(f => f.IsActive && f.Direction == FinancialAccountDirection.Receivable
                && f.Status != FinancialAccountStatus.Paid)
            .Select(f => new
            {
                f.Description,
                f.DueDate,
                Balance = f.Amount - f.PaidAmount,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, f => ReportRowBuilder.Row(
            ("description", f.Description),
            ("dueDate", f.DueDate?.ToString("dd/MM/yyyy") ?? "—"),
            ("balance", FormatMoney(f.Balance)),
            ("overdue", f.DueDate.HasValue && f.DueDate.Value < now ? "Sim" : "Não")));

        var overdue = rows.Count(r => r["overdue"]?.ToString() == "Sim");
        return Result(def, null, [
            new("description", "Descrição"),
            new("dueDate", "Vencimento"),
            new("balance", "Saldo (R$)"),
            new("overdue", "Inadimplente"),
        ], rows, [
            new ReportKpiDto("Em aberto", rows.Count.ToString()),
            new ReportKpiDto("Inadimplentes", overdue.ToString(), "danger"),
        ]);
    }

    private async Task<ReportResultDto> DailyRevenueAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        return await RevenueAsync(def, range, ct);
    }

    private async Task<ReportResultDto> AvgTicketAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var payments = await dbContext.FinancialPayments.AsNoTracking()
            .Where(p => p.IsActive && p.PaidAt >= range.From && p.PaidAt <= range.To
                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable)
            .Select(p => p.Amount)
            .ToListAsync(ct);

        var avg = payments.Count == 0 ? 0 : payments.Average();
        return Result(def, null, [
            new("metric", "Métrica"),
            new("value", "Valor"),
        ], [
            new() { ["metric"] = "Ticket médio (R$)", ["value"] = FormatMoney(avg) },
            new() { ["metric"] = "Pagamentos", ["value"] = payments.Count },
        ]);
    }

    private async Task<ReportResultDto> FinancialIndicatorsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var openReceivables = await dbContext.FinancialAccounts
            .Where(f => f.Direction == FinancialAccountDirection.Receivable && f.Status != FinancialAccountStatus.Paid)
            .SumAsync(f => f.Amount - f.PaidAmount, ct);

        var openPayables = await dbContext.FinancialAccounts
            .Where(f => f.Direction == FinancialAccountDirection.Payable && f.Status != FinancialAccountStatus.Paid)
            .SumAsync(f => f.Amount - f.PaidAmount, ct);

        return Result(def, null, [
            new("indicator", "Indicador"),
            new("value", "Valor (R$)"),
        ], [
            new() { ["indicator"] = "A receber em aberto", ["value"] = FormatMoney(openReceivables) },
            new() { ["indicator"] = "A pagar em aberto", ["value"] = FormatMoney(openPayables) },
        ]);
    }

    private async Task<ReportResultDto> TissByInsuranceAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.TissGuides.AsNoTracking()
            .Where(g => g.IsActive && g.CreatedAt >= range.From && g.CreatedAt <= range.To)
            .GroupBy(g => g.HealthInsurance.Name)
            .Select(g => new { Insurance = g.Key, Guides = g.Count(), Amount = g.Sum(x => x.TotalAmount) })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("insurance", g.Insurance),
            ("guides", g.Guides),
            ("amount", FormatMoney(g.Amount))));

        return Result(def, null, [
            new("insurance", "Operadora"),
            new("guides", "Guias"),
            new("amount", "Valor (R$)"),
        ], rows);
    }

    private async Task<ReportResultDto> TissGuidesAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, TissGuideStatus? status, CancellationToken ct)
    {
        var query = dbContext.TissGuides.AsNoTracking()
            .Where(g => g.IsActive && g.CreatedAt >= range.From && g.CreatedAt <= range.To);
        if (status.HasValue)
        {
            query = query.Where(g => g.Status == status.Value);
        }

        var items = await query
            .Select(g => new
            {
                g.GuideNumber,
                Patient = g.Patient.FullName,
                Insurance = g.HealthInsurance.Name,
                g.TotalAmount,
                g.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, g => ReportRowBuilder.Row(
            ("guide", g.GuideNumber),
            ("patient", g.Patient),
            ("insurance", g.Insurance),
            ("amount", FormatMoney(g.TotalAmount)),
            ("status", g.Status.ToString())));

        return Result(def, status?.ToString() ?? "Todas", [
            new("guide", "Guia"),
            new("patient", "Paciente"),
            new("insurance", "Convênio"),
            new("amount", "Valor"),
            new("status", "Situação"),
        ], rows);
    }

    private async Task<ReportResultDto> AuthorizationsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.InsuranceAuthorizations.AsNoTracking()
            .Where(a => a.IsActive && a.CreatedAt >= range.From && a.CreatedAt <= range.To
                && a.Status == InsuranceAuthorizationStatus.Approved)
            .Select(a => new
            {
                a.AuthorizationNumber,
                Patient = a.Patient.FullName,
                Insurance = a.HealthInsurance.Name,
                a.ValidUntil,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, a => ReportRowBuilder.Row(
            ("number", a.AuthorizationNumber),
            ("patient", a.Patient),
            ("insurance", a.Insurance),
            ("validUntil", a.ValidUntil.HasValue ? FormatDate(a.ValidUntil.Value) : "—")));

        return Result(def, null, [
            new("number", "Autorização"),
            new("patient", "Paciente"),
            new("insurance", "Convênio"),
            new("validUntil", "Validade"),
        ], rows);
    }

    private async Task<ReportResultDto> TissGlosasAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.TissGlosas.AsNoTracking()
            .Where(g => g.CreatedAt >= range.From && g.CreatedAt <= range.To)
            .Select(g => new
            {
                Guide = g.TissGuide.GuideNumber,
                g.Reason,
                g.GlosaAmount,
                g.IsResolved,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, g => ReportRowBuilder.Row(
            ("guide", g.Guide),
            ("reason", g.Reason),
            ("amount", FormatMoney(g.GlosaAmount)),
            ("resolved", g.IsResolved ? "Sim" : "Não")));

        return Result(def, null, [
            new("guide", "Guia"),
            new("reason", "Motivo"),
            new("amount", "Valor (R$)"),
            new("resolved", "Resolvida"),
        ], rows);
    }

    private async Task<ReportResultDto> GlosasByReasonAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.TissGlosas.AsNoTracking()
            .Where(g => g.CreatedAt >= range.From && g.CreatedAt <= range.To)
            .GroupBy(g => g.Reason)
            .Select(g => new { Reason = g.Key, Count = g.Count(), Amount = g.Sum(x => x.GlosaAmount) })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("reason", g.Reason),
            ("count", g.Count),
            ("amount", FormatMoney(g.Amount))));

        return Result(def, null, [
            new("reason", "Motivo"),
            new("count", "Ocorrências"),
            new("amount", "Valor (R$)"),
        ], rows);
    }

    private async Task<ReportResultDto> GlosaAppealsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.TissGlosas.AsNoTracking()
            .Where(g => g.IsActive
                && g.CreatedAt >= range.From
                && g.CreatedAt <= range.To
                && g.ContestationStatus != GlosaContestationStatus.None)
            .Select(g => new
            {
                Guide = g.TissGuide.GuideNumber,
                Insurance = g.TissGuide.HealthInsurance.Name,
                g.Reason,
                g.GlosaAmount,
                g.ContestationStatus,
                g.ContestationNotes,
                g.IsResolved,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, g => ReportRowBuilder.Row(
            ("guide", g.Guide),
            ("insurance", g.Insurance),
            ("reason", g.Reason),
            ("amount", FormatMoney(g.GlosaAmount)),
            ("status", g.ContestationStatus.ToString()),
            ("notes", g.ContestationNotes ?? "—"),
            ("resolved", g.IsResolved ? "Sim" : "Não")));

        return Result(def, "Recursos e contestações de glosa TISS", [
            new("guide", "Guia"),
            new("insurance", "Convênio"),
            new("reason", "Motivo glosa"),
            new("amount", "Valor (R$)"),
            new("status", "Status recurso"),
            new("notes", "Observações"),
            new("resolved", "Resolvida"),
        ], rows, [
            new ReportKpiDto("Recursos", rows.Count.ToString(), "primary"),
            new ReportKpiDto("Pendentes", items.Count(i => !i.IsResolved).ToString(), "warning"),
        ]);
    }

    private async Task<ReportResultDto> TissPendingAsync(ReportDefinition def, CancellationToken ct)
    {
        var items = await dbContext.TissGuides.AsNoTracking()
            .Where(g => g.IsActive && (g.Status == TissGuideStatus.Draft || g.Status == TissGuideStatus.Sent))
            .Select(g => new
            {
                g.GuideNumber,
                Insurance = g.HealthInsurance.Name,
                g.TotalAmount,
                g.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, g => ReportRowBuilder.Row(
            ("guide", g.GuideNumber),
            ("insurance", g.Insurance),
            ("amount", FormatMoney(g.TotalAmount)),
            ("status", g.Status.ToString())));

        return Result(def, null, [
            new("guide", "Guia"),
            new("insurance", "Convênio"),
            new("amount", "Valor"),
            new("status", "Situação"),
        ], rows);
    }

    private async Task<ReportResultDto> TissSummaryAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.TissGuides.AsNoTracking()
            .Where(g => g.CreatedAt >= range.From && g.CreatedAt <= range.To)
            .GroupBy(g => g.Status)
            .Select(g => new { g.Key, Count = g.Count(), Amount = g.Sum(x => x.TotalAmount) })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("status", g.Key.ToString()),
            ("count", g.Count),
            ("amount", FormatMoney(g.Amount))));

        return Result(def, "Resumo regulatório TISS", [
            new("status", "Situação"),
            new("count", "Guias"),
            new("amount", "Valor (R$)"),
        ], rows);
    }

    private async Task<ReportResultDto> TpaSummaryAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.TpaClaims.AsNoTracking()
            .Where(x => x.IsActive
                && x.ServiceDate >= DateOnly.FromDateTime(range.From)
                && x.ServiceDate <= DateOnly.FromDateTime(range.To))
            .Select(x => new
            {
                x.ServiceDate,
                Administrator = x.TpaAdministrator.Name,
                Patient = x.Patient.FullName,
                Gross = x.GrossAmount,
                Net = x.NetAmount,
                Status = x.Status.ToString(),
            })
            .OrderByDescending(x => x.ServiceDate)
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, i => ReportRowBuilder.Row(
            ("serviceDate", i.ServiceDate.ToString("dd/MM/yyyy")),
            ("administrator", i.Administrator),
            ("patient", i.Patient),
            ("gross", i.Gross),
            ("net", i.Net),
            ("status", i.Status)));

        return Result(def, "Consolidação de claims TPA por administradora", [
            new("serviceDate", "Data"),
            new("administrator", "Administradora"),
            new("patient", "Paciente"),
            new("gross", "Bruto"),
            new("net", "Líquido"),
            new("status", "Status"),
        ], rows, [
            new ReportKpiDto("Claims", items.Count.ToString(), "primary"),
            new ReportKpiDto("Bruto", FormatMoney(items.Sum(i => i.Gross))),
            new ReportKpiDto("Líquido", FormatMoney(items.Sum(i => i.Net)), "success"),
        ]);
    }

    private async Task<ReportResultDto> BilledProceduresAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.TissGuideItems.AsNoTracking()
            .Where(i => i.TissGuide.CreatedAt >= range.From && i.TissGuide.CreatedAt <= range.To)
            .GroupBy(i => i.TussCode)
            .Select(g => new
            {
                Tuss = g.Key,
                Description = g.Select(x => x.Description).First(),
                Quantity = g.Sum(x => x.Quantity),
                Amount = g.Sum(x => x.UnitPrice * x.Quantity),
            })
            .Take(50)
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("tuss", g.Tuss),
            ("description", g.Description),
            ("quantity", g.Quantity),
            ("amount", FormatMoney(g.Amount))));

        return Result(def, null, [
            new("tuss", "TUSS"),
            new("description", "Procedimento"),
            new("quantity", "Qtd"),
            new("amount", "Valor (R$)"),
        ], rows);
    }

    private async Task<ReportResultDto> FinancialAccountsAsync(
        ReportDefinition def, FinancialAccountStatus status, CancellationToken ct)
    {
        var items = await dbContext.FinancialAccounts.AsNoTracking()
            .Where(f => f.IsActive && f.Status == status)
            .Select(f => new
            {
                f.Description,
                f.Amount,
                f.DueDate,
                f.Category,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, f => ReportRowBuilder.Row(
            ("description", f.Description),
            ("amount", FormatMoney(f.Amount)),
            ("dueDate", f.DueDate?.ToString("dd/MM/yyyy") ?? "—"),
            ("category", f.Category.ToString())));

        return Result(def, status.ToString(), [
            new("description", "Descrição"),
            new("amount", "Valor (R$)"),
            new("dueDate", "Vencimento"),
            new("category", "Categoria"),
        ], rows);
    }

    private async Task<ReportResultDto> RevenueByProcedureAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        return await BilledProceduresAsync(def, range, ct);
    }

    private async Task<ReportResultDto> RevenueBySpecialtyAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var fromGuides = await dbContext.TissGuides.AsNoTracking()
            .Where(g => g.IsActive && g.CreatedAt >= range.From && g.CreatedAt <= range.To)
            .Select(g => new
            {
                Specialty = g.RequestingProfessional != null && g.RequestingProfessional.Specialty != null
                    ? g.RequestingProfessional.Specialty.Name
                    : "Não informado",
                g.TotalAmount,
            })
            .ToListAsync(ct);

        var fromAppts = await dbContext.Appointments.AsNoTracking()
            .Where(a => a.IsActive
                && a.Status == AppointmentStatus.Completed
                && a.ScheduledAt >= range.From
                && a.ScheduledAt <= range.To)
            .Select(a => new
            {
                Specialty = a.Professional.Specialty.Name,
                Count = 1,
            })
            .ToListAsync(ct);

        var guideGroups = fromGuides
            .GroupBy(g => g.Specialty)
            .Select(g => new
            {
                Specialty = g.Key,
                Guides = g.Count(),
                Revenue = g.Sum(x => x.TotalAmount),
                Appointments = 0,
            });

        var apptGroups = fromAppts
            .GroupBy(a => a.Specialty)
            .Select(g => new
            {
                Specialty = g.Key,
                Guides = 0,
                Revenue = 0m,
                Appointments = g.Count(),
            });

        var merged = guideGroups
            .Concat(apptGroups)
            .GroupBy(x => x.Specialty)
            .Select(g => new
            {
                Specialty = g.Key,
                Guides = g.Sum(x => x.Guides),
                Revenue = g.Sum(x => x.Revenue),
                Appointments = g.Sum(x => x.Appointments),
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        var rows = ReportRowBuilder.From(merged, g => ReportRowBuilder.Row(
            ("specialty", g.Specialty),
            ("guides", g.Guides),
            ("appointments", g.Appointments),
            ("revenue", FormatMoney(g.Revenue))));

        return Result(def, "Faturamento TISS e consultas realizadas por especialidade", [
            new("specialty", "Especialidade"),
            new("guides", "Guias"),
            new("appointments", "Consultas"),
            new("revenue", "Receita TISS (R$)"),
        ], rows, [
            new ReportKpiDto("Especialidades", merged.Count.ToString()),
            new ReportKpiDto("Receita TISS", FormatMoney(merged.Sum(m => m.Revenue)), "success"),
        ]);
    }

    private async Task<ReportResultDto> ActiveEmployeesAsync(ReportDefinition def, CancellationToken ct)
    {
        var items = await dbContext.Employees.AsNoTracking()
            .Where(e => e.IsActive)
            .Select(e => new
            {
                Name = e.FullName,
                e.Role,
                Department = e.Department.Name,
                e.Email,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, e => ReportRowBuilder.Row(
            ("name", e.Name),
            ("role", e.Role.ToString()),
            ("department", e.Department),
            ("email", e.Email ?? "—")));

        return Result(def, null, [
            new("name", "Colaborador"),
            new("role", "Função"),
            new("department", "Setor"),
            new("email", "E-mail"),
        ], rows, [new ReportKpiDto("Ativos", rows.Count.ToString())]);
    }

    private async Task<ReportResultDto> PayrollSummaryAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.PayrollRuns.AsNoTracking()
            .Where(x => x.IsActive
                && x.ReferenceDate >= DateOnly.FromDateTime(range.From)
                && x.ReferenceDate <= DateOnly.FromDateTime(range.To))
            .OrderByDescending(x => x.ReferenceDate)
            .Select(x => new
            {
                Period = $"{x.Month:D2}/{x.Year}",
                x.Status,
                x.TotalGross,
                x.TotalDiscounts,
                x.TotalNet,
                Employees = x.Items.Count(i => i.IsActive),
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, i => ReportRowBuilder.Row(
            ("period", i.Period),
            ("status", i.Status.ToString()),
            ("employees", i.Employees),
            ("gross", i.TotalGross),
            ("discounts", i.TotalDiscounts),
            ("net", i.TotalNet)));

        return Result(def, "Resumo mensal da folha por período", [
            new("period", "Período"),
            new("status", "Status"),
            new("employees", "Colaboradores"),
            new("gross", "Bruto"),
            new("discounts", "Descontos"),
            new("net", "Líquido"),
        ], rows, [
            new ReportKpiDto("Folhas", items.Count.ToString(), "primary"),
            new ReportKpiDto("Líquido", FormatMoney(items.Sum(i => i.TotalNet)), "success"),
        ]);
    }

    private async Task<ReportResultDto> SecurityIncidentsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.SecurityIncidents.AsNoTracking()
            .Where(s => s.CreatedAt >= range.From && s.CreatedAt <= range.To)
            .Select(s => new
            {
                s.CreatedAt,
                s.Type,
                s.Location,
                s.Description,
                Patient = s.Patient != null ? s.Patient.FullName : null,
                s.Severity,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, s => ReportRowBuilder.Row(
            ("date", FormatDate(s.CreatedAt)),
            ("type", s.Type.ToString()),
            ("severity", s.Severity?.ToString() ?? "—"),
            ("patient", s.Patient ?? "—"),
            ("location", s.Location),
            ("description", s.Description)));

        return Result(def, "Eventos adversos e incidentes clínicos (HospitalRun / NPSG)", [
            new("date", "Data"),
            new("type", "Tipo"),
            new("severity", "Gravidade"),
            new("patient", "Paciente"),
            new("location", "Local"),
            new("description", "Descrição"),
        ], rows);
    }

    private async Task<ReportResultDto> InfectionsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.InfectionSurveillances.AsNoTracking()
            .Where(i => i.CreatedAt >= range.From && i.CreatedAt <= range.To)
            .Select(i => new
            {
                i.CreatedAt,
                i.Location,
                i.InfectionType,
                i.Organism,
                i.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, i => ReportRowBuilder.Row(
            ("date", FormatDate(i.CreatedAt)),
            ("location", i.Location),
            ("type", i.InfectionType.ToString()),
            ("organism", i.Organism ?? "—"),
            ("status", i.Status.ToString())));

        return Result(def, null, [
            new("date", "Data"),
            new("location", "Setor"),
            new("type", "Tipo"),
            new("organism", "Organismo"),
            new("status", "Situação"),
        ], rows);
    }

    private async Task<ReportResultDto> InfectionRateAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var infections = await dbContext.InfectionSurveillances
            .CountAsync(i => i.CreatedAt >= range.From && i.CreatedAt <= range.To, ct);
        var hospitalizations = await dbContext.Hospitalizations
            .CountAsync(h => h.AdmittedAt >= range.From && h.AdmittedAt <= range.To, ct);
        var rate = hospitalizations == 0 ? 0 : Math.Round(infections * 100m / hospitalizations, 2);

        return Result(def, null, [
            new("metric", "Métrica"),
            new("value", "Valor"),
        ], [
            new() { ["metric"] = "Infecções no período", ["value"] = infections },
            new() { ["metric"] = "Internações no período", ["value"] = hospitalizations },
            new() { ["metric"] = "Taxa (%)", ["value"] = rate },
        ]);
    }

    private async Task<ReportResultDto> MonitoredInfectionsAsync(ReportDefinition def, CancellationToken ct)
    {
        var items = await dbContext.InfectionSurveillances.AsNoTracking()
            .Where(i => i.Status == InfectionSurveillanceStatus.Suspected
                || i.Status == InfectionSurveillanceStatus.Confirmed)
            .Select(i => new
            {
                Patient = i.Patient.FullName,
                i.Location,
                i.InfectionType,
                i.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, i => ReportRowBuilder.Row(
            ("patient", i.Patient),
            ("location", i.Location),
            ("type", i.InfectionType.ToString()),
            ("status", i.Status.ToString())));

        return Result(def, null, [
            new("patient", "Paciente"),
            new("location", "Setor"),
            new("type", "Tipo"),
            new("status", "Situação"),
        ], rows);
    }

    private async Task<ReportResultDto> AuditLogAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.AuditLogs.AsNoTracking()
            .Where(a => a.CreatedAt >= range.From && a.CreatedAt <= range.To)
            .OrderByDescending(a => a.CreatedAt)
            .Take(500)
            .Select(a => new
            {
                a.CreatedAt,
                a.UserEmail,
                a.Action,
                a.EntityType,
                a.Details,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, a => ReportRowBuilder.Row(
            ("date", FormatDate(a.CreatedAt)),
            ("user", a.UserEmail),
            ("action", a.Action),
            ("entity", a.EntityType),
            ("details", a.Details)));

        return Result(def, null, [
            new("date", "Data"),
            new("user", "Usuário"),
            new("action", "Ação"),
            new("entity", "Entidade"),
            new("details", "Detalhes"),
        ], rows);
    }

    private async Task<ReportResultDto> AuditByUserAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.AuditLogs.AsNoTracking()
            .Where(a => a.CreatedAt >= range.From && a.CreatedAt <= range.To)
            .GroupBy(a => a.UserEmail)
            .Select(g => new { User = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            grouped.OrderByDescending(g => g.Count),
            g => ReportRowBuilder.Row(
                ("user", g.User),
                ("count", g.Count)));

        return Result(def, null, [
            new("user", "Usuário"),
            new("count", "Acessos/ações"),
        ], rows);
    }

    private async Task<ReportResultDto> WardPharmacyStockAsync(ReportDefinition def, CancellationToken ct)
    {
        var items = await dbContext.WardStockBalances.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Ward.Name)
            .ThenBy(b => b.Product.Name)
            .Select(b => new
            {
                Ward = b.Ward.Name,
                Product = b.Product.Name,
                b.QuantityOnHand,
                b.MinimumStock,
                b.Unit,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, b => ReportRowBuilder.Row(
            ("ward", b.Ward),
            ("product", b.Product),
            ("quantity", b.QuantityOnHand),
            ("minimum", b.MinimumStock),
            ("unit", b.Unit)));

        return Result(def, "Saldo de medicamentos por ala", [
            new("ward", "Ala"),
            new("product", "Produto"),
            new("quantity", "Saldo"),
            new("minimum", "Mínimo"),
            new("unit", "Unidade"),
        ], rows, [new ReportKpiDto("Posições", rows.Count.ToString(), "info")]);
    }

    private async Task<ReportResultDto> PatientVaccinationsReportAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.PatientVaccinations.AsNoTracking()
            .Where(v => v.IsActive && v.AdministeredAt >= range.From && v.AdministeredAt <= range.To)
            .OrderByDescending(v => v.AdministeredAt)
            .Select(v => new
            {
                Date = v.AdministeredAt,
                Patient = v.Patient.FullName,
                Vaccine = v.VaccineCatalog.Name,
                v.DoseNumber,
                v.BatchNumber,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, v => ReportRowBuilder.Row(
            ("date", v.Date.ToString("dd/MM/yyyy")),
            ("patient", v.Patient),
            ("vaccine", v.Vaccine),
            ("dose", v.DoseNumber),
            ("batch", v.BatchNumber ?? "—")));

        return Result(def, "Vacinações no período", [
            new("date", "Data"),
            new("patient", "Paciente"),
            new("vaccine", "Vacina"),
            new("dose", "Dose"),
            new("batch", "Lote"),
        ], rows, [new ReportKpiDto("Aplicações", rows.Count.ToString(), "success")]);
    }

    private async Task<ReportResultDto> FinancialStatementAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var paidRevenue = await dbContext.FinancialPayments.AsNoTracking()
            .Where(p => p.IsActive && p.PaidAt >= range.From && p.PaidAt <= range.To
                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable)
            .SumAsync(p => p.Amount, ct);

        var paidExpenses = await dbContext.FinancialPayments.AsNoTracking()
            .Where(p => p.IsActive && p.PaidAt >= range.From && p.PaidAt <= range.To
                && p.FinancialAccount.Direction == FinancialAccountDirection.Payable)
            .SumAsync(p => p.Amount, ct);

        var openReceivables = await dbContext.FinancialAccounts.AsNoTracking()
            .Where(f => f.IsActive && f.Direction == FinancialAccountDirection.Receivable
                && f.Status != FinancialAccountStatus.Paid)
            .SumAsync(f => f.Amount, ct);

        var openPayables = await dbContext.FinancialAccounts.AsNoTracking()
            .Where(f => f.IsActive && f.Direction == FinancialAccountDirection.Payable
                && f.Status != FinancialAccountStatus.Paid)
            .SumAsync(f => f.Amount, ct);

        return Result(def, $"{range.From:dd/MM/yyyy} — {range.To:dd/MM/yyyy}", [
            new("item", "Item"),
            new("amount", "Valor (R$)"),
        ], [
            new() { ["item"] = "Receitas recebidas no período", ["amount"] = FormatMoney(paidRevenue) },
            new() { ["item"] = "Despesas pagas no período", ["amount"] = FormatMoney(paidExpenses) },
            new() { ["item"] = "Resultado do período", ["amount"] = FormatMoney(paidRevenue - paidExpenses) },
            new() { ["item"] = "Contas a receber em aberto", ["amount"] = FormatMoney(openReceivables) },
            new() { ["item"] = "Contas a pagar em aberto", ["amount"] = FormatMoney(openPayables) },
            new() { ["item"] = "Posição líquida em aberto", ["amount"] = FormatMoney(openReceivables - openPayables) },
        ], [new ReportKpiDto("Resultado", FormatMoney(paidRevenue - paidExpenses), "success")]);
    }

    private async Task<ReportResultDto> DreAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var revenueByCategory = await dbContext.FinancialPayments.AsNoTracking()
            .Where(p => p.IsActive && p.PaidAt >= range.From && p.PaidAt <= range.To
                && p.FinancialAccount.Direction == FinancialAccountDirection.Receivable)
            .GroupBy(p => p.FinancialAccount.Category)
            .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var expenseByCategory = await dbContext.FinancialPayments.AsNoTracking()
            .Where(p => p.IsActive && p.PaidAt >= range.From && p.PaidAt <= range.To
                && p.FinancialAccount.Direction == FinancialAccountDirection.Payable)
            .GroupBy(p => p.FinancialAccount.Category)
            .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var gross = revenueByCategory.Sum(r => r.Amount);
        var expenses = expenseByCategory.Sum(e => e.Amount);
        var rows = new List<Dictionary<string, object?>>();

        foreach (var r in revenueByCategory.OrderByDescending(r => r.Amount))
        {
            rows.Add(new() { ["line"] = $"(+) {r.Category}", ["amount"] = FormatMoney(r.Amount) });
        }

        rows.Add(new() { ["line"] = "Receita bruta", ["amount"] = FormatMoney(gross) });

        foreach (var e in expenseByCategory.OrderByDescending(e => e.Amount))
        {
            rows.Add(new() { ["line"] = $"(-) {e.Category}", ["amount"] = FormatMoney(e.Amount) });
        }

        rows.Add(new() { ["line"] = "Total despesas", ["amount"] = FormatMoney(expenses) });
        rows.Add(new() { ["line"] = "Resultado líquido", ["amount"] = FormatMoney(gross - expenses) });

        return Result(def, "DRE simplificada por categoria de conta", [
            new("line", "Linha"),
            new("amount", "Valor (R$)"),
        ], rows, [new ReportKpiDto("Resultado líquido", FormatMoney(gross - expenses), gross >= expenses ? "success" : "danger")]);
    }

    private async Task<ReportResultDto> CompulsoryNotificationsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var notifiable = await dbContext.EpidemicDiseaseCatalogs.AsNoTracking()
            .Where(d => d.IsActive && d.DiseaseClass == EpidemicDiseaseClass.Notifiable)
            .OrderBy(d => d.DisplayOrder)
            .ToListAsync(ct);

        var diagnoses = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.IsActive && e.CreatedAt >= range.From && e.CreatedAt <= range.To)
            .Select(e => new { e.Content, e.Cid10Code, Patient = e.MedicalRecord.Patient.FullName, e.CreatedAt })
            .ToListAsync(ct);

        var rows = new List<Dictionary<string, object?>>();
        foreach (var disease in notifiable)
        {
            var matches = diagnoses.Where(d =>
                (!string.IsNullOrWhiteSpace(d.Content)
                    && d.Content.Contains(disease.Name, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(d.Cid10Code)
                    && disease.Name.Contains(d.Cid10Code, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            rows.Add(new()
            {
                ["code"] = disease.Code,
                ["disease"] = disease.Name,
                ["cases"] = matches.Count,
                ["lastCase"] = matches.Count == 0
                    ? "—"
                    : matches.Max(m => m.CreatedAt).ToString("dd/MM/yyyy"),
            });
        }

        var totalCases = rows.Sum(r => (int)(r["cases"] ?? 0));
        return Result(def, "Doenças de notificação compulsória (catálogo OpenHospital/sitrep)", [
            new("code", "Código"),
            new("disease", "Doença"),
            new("cases", "Suspeitas/registros"),
            new("lastCase", "Último registro"),
        ], rows.Where(r => (int)(r["cases"] ?? 0) > 0).Concat(
            rows.Where(r => (int)(r["cases"] ?? 0) == 0).Take(10)).ToList(),
        [new ReportKpiDto("Registros no período", totalCases.ToString(), totalCases > 0 ? "warning" : "success")]);
    }

    private async Task<ReportResultDto> EpidemicCurveAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.InfectionSurveillances.AsNoTracking()
            .Where(i => i.DetectedAt >= range.From && i.DetectedAt <= range.To)
            .Select(i => i.DetectedAt)
            .ToListAsync(ct);

        var populationBase = await dbContext.Beds.CountAsync(b => b.IsActive, ct);
        if (populationBase < 1)
        {
            populationBase = await dbContext.Hospitalizations.CountAsync(
                h => h.IsActive && h.DischargedAt == null, ct);
        }

        var population = Math.Max(populationBase, 1);

        var grouped = items
            .GroupBy(d => (ISOWeek.GetWeekOfYear(d), d.Year))
            .Select(g => new
            {
                Week = g.Key.Item1,
                Year = g.Key.Item2,
                Count = g.Count(),
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Week)
            .ToList();

        var rows = grouped.Select(g =>
        {
            var ar = Math.Round(g.Count * 10000m / population, 2);
            return new Dictionary<string, object?>
            {
                ["week"] = $"S{g.Week}/{g.Year}",
                ["cases"] = g.Count,
                ["population"] = population,
                ["arPer10000"] = ar,
                ["count"] = g.Count,
            };
        }).ToList();

        return Result(def, "Attack rate by Epiweek (sitrep measles_outbreak)", [
            new("week", "Semana epidemiológica"),
            new("cases", "Casos"),
            new("population", "População em risco"),
            new("arPer10000", "AR (por 10.000)"),
        ], rows, [new ReportKpiDto("Total de casos", items.Count.ToString(), "warning")]);
    }

    private async Task<ReportResultDto> MortalitySurveillanceAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.Hospitalizations.AsNoTracking()
            .Where(h => h.DischargedAt >= range.From && h.DischargedAt <= range.To)
            .Where(h => EF.Functions.ILike(h.Diagnosis ?? h.Reason, "%óbito%")
                || EF.Functions.ILike(h.Diagnosis ?? h.Reason, "%obito%")
                || h.Patient.IsDeceased)
            .Select(h => new { Date = h.DischargedAt!.Value, Ward = h.Bed.Ward.Name })
            .ToListAsync(ct);

        var grouped = items
            .GroupBy(h => ISOWeek.GetWeekOfYear(h.Date))
            .Select(g => new { Week = g.Key, Year = g.First().Date.Year, Count = g.Count() })
            .OrderBy(g => g.Year).ThenBy(g => g.Week)
            .ToList();

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("week", $"S{g.Week}/{g.Year}"),
            ("deaths", g.Count)));

        return Result(def, "Vigilância de mortalidade hospitalar (sitrep)", [
            new("week", "Semana"),
            new("deaths", "Óbitos"),
        ], rows, [new ReportKpiDto("Óbitos no período", items.Count.ToString(), "danger")]);
    }

    private async Task<ReportResultDto> VaccinationCoverageAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.PatientVaccinations.AsNoTracking()
            .Where(v => v.IsActive && v.AdministeredAt >= range.From && v.AdministeredAt <= range.To)
            .GroupBy(v => v.VaccineCatalog.Name)
            .Select(g => new { Vaccine = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("vaccine", g.Vaccine),
            ("doses", g.Count)));

        var total = grouped.Sum(g => g.Count);
        return Result(def, "Cobertura por imunobiológico (sitrep vacinação)", [
            new("vaccine", "Vacina"),
            new("doses", "Doses aplicadas"),
        ], rows, [new ReportKpiDto("Total de doses", total.ToString(), "success")]);
    }

    private async Task<ReportResultDto> OutbreakIndicatorsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var infections = await dbContext.InfectionSurveillances
            .CountAsync(i => i.DetectedAt >= range.From && i.DetectedAt <= range.To, ct);
        var hospitalizations = await dbContext.Hospitalizations
            .CountAsync(h => h.AdmittedAt >= range.From && h.AdmittedAt <= range.To, ct);
        var rate = hospitalizations == 0 ? 0 : Math.Round(infections * 100m / hospitalizations, 2);

        var last7 = DateTime.UtcNow.AddDays(-7);
        var recent = await dbContext.InfectionSurveillances
            .CountAsync(i => i.DetectedAt >= last7, ct);
        var respiratory = await dbContext.InfectionSurveillances
            .CountAsync(i => i.DetectedAt >= range.From && i.DetectedAt <= range.To
                && (i.InfectionType == InfectionType.Respiratory
                    || EF.Functions.ILike(i.Organism, "%respirat%")
                    || EF.Functions.ILike(i.Notes ?? "", "%respirat%")), ct);

        var alert = recent >= 5 || rate > 5 ? "Alto" : recent >= 2 ? "Moderado" : "Baixo";
        var confiabilidade = infections >= 10 ? "alta" : infections >= 3 ? "media" : "baixa";
        var percentual = infections == 0 ? 0 : Math.Round(respiratory * 100m / infections, 1);

        return Result(def, "Indicadores agregados — estrutura dev-queiroz/Groq (resumo_executivo + indicadores)", [
            new("indicator", "Indicador"),
            new("value", "Valor"),
        ], [
            new() { ["indicator"] = "Risco (resumo_executivo.risco)", ["value"] = alert },
            new() { ["indicator"] = "Casos respiratórios (indicadores.casos_respiratorios)", ["value"] = respiratory },
            new() { ["indicator"] = "Infecções no período (indicadores.consultas_totais)", ["value"] = infections },
            new() { ["indicator"] = "Percentual alvo (indicadores.percentual)", ["value"] = $"{percentual}%" },
            new() { ["indicator"] = "Confiabilidade (indicadores.confiabilidade)", ["value"] = confiabilidade },
            new() { ["indicator"] = "Casos últimos 7 dias", ["value"] = recent },
            new() { ["indicator"] = "Taxa infecção hospitalar (%)", ["value"] = rate },
        ], [new ReportKpiDto("Alerta", alert, alert == "Baixo" ? "success" : "danger")]);
    }

    private async Task<ReportResultDto> ReceptionAvgWaitAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var data = await dbContext.EmergencyVisits.AsNoTracking()
            .Where(e => e.IsActive && e.ArrivedAt >= range.From && e.ArrivedAt <= range.To && e.StartedAt != null)
            .Select(e => new { e.Urgency, Wait = (e.StartedAt!.Value - e.ArrivedAt).TotalMinutes })
            .ToListAsync(ct);

        var avg = data.Count == 0 ? 0 : Math.Round(data.Average(d => d.Wait), 1);
        var byUrgency = data.GroupBy(d => d.Urgency)
            .Select(g => new { Urgency = g.Key.ToString(), Avg = Math.Round(g.Average(x => x.Wait), 1), Count = g.Count() })
            .OrderBy(g => g.Urgency)
            .ToList();

        var rows = ReportRowBuilder.From(byUrgency, g => ReportRowBuilder.Row(
            ("urgency", g.Urgency),
            ("avgMinutes", g.Avg),
            ("count", g.Count)));

        return Result(def, $"Espera média geral: {avg} min (PS)", [
            new("urgency", "Classificação"),
            new("avgMinutes", "Espera média (min)"),
            new("count", "Atendimentos"),
        ], rows, [new ReportKpiDto("Espera média", $"{avg} min", "info")]);
    }

    private async Task<ReportResultDto> PharmacyConsumptionBySectorAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.PharmacyDispensings.AsNoTracking()
            .Where(d => d.DispensedAt >= range.From && d.DispensedAt <= range.To)
            .Select(d => new
            {
                Sector = d.Hospitalization != null
                    ? d.Hospitalization.Bed.Ward.Name
                    : "Ambulatorial",
                d.Quantity,
            })
            .ToListAsync(ct);

        var grouped = items.GroupBy(i => i.Sector)
            .Select(g => new { Sector = g.Key, Qty = g.Sum(x => x.Quantity), Count = g.Count() })
            .OrderByDescending(g => g.Qty)
            .ToList();

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("sector", g.Sector),
            ("quantity", g.Qty),
            ("dispensings", g.Count)));

        return Result(def, null, [
            new("sector", "Setor"),
            new("quantity", "Quantidade"),
            new("dispensings", "Dispensações"),
        ], rows);
    }

    private async Task<ReportResultDto> PharmacyConsumptionByPatientAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var grouped = await dbContext.PharmacyDispensings.AsNoTracking()
            .Where(d => d.DispensedAt >= range.From && d.DispensedAt <= range.To)
            .GroupBy(d => d.Patient.FullName)
            .Select(g => new { Patient = g.Key, Qty = g.Sum(x => x.Quantity), Count = g.Count() })
            .OrderByDescending(g => g.Qty)
            .Take(100)
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("patient", g.Patient),
            ("quantity", g.Qty),
            ("dispensings", g.Count)));

        return Result(def, "Top 100 pacientes por volume dispensado", [
            new("patient", "Paciente"),
            new("quantity", "Quantidade"),
            new("dispensings", "Dispensações"),
        ], rows);
    }

    private async Task<ReportResultDto> SupplyExpiredAsync(ReportDefinition def, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = await dbContext.StockMovements.AsNoTracking()
            .Where(m => m.IsActive && m.ExpiryDate != null && m.ExpiryDate < today)
            .Select(m => new { Product = m.Product.Name, m.BatchNumber, Expiry = m.ExpiryDate!.Value, m.Quantity })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            items.OrderBy(i => i.Expiry),
            m => ReportRowBuilder.Row(
                ("product", m.Product),
                ("batch", m.BatchNumber ?? "—"),
                ("expiry", m.Expiry.ToString("dd/MM/yyyy")),
                ("quantity", m.Quantity)));

        return Result(def, "Lotes com validade vencida", [
            new("product", "Produto"),
            new("batch", "Lote"),
            new("expiry", "Validade"),
            new("quantity", "Qtd."),
        ], rows, [new ReportKpiDto("Lotes vencidos", rows.Count.ToString(), "danger")]);
    }

    private async Task<ReportResultDto> LabByInsuranceAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.LabOrders.AsNoTracking()
            .Where(o => o.IsActive && o.CreatedAt >= range.From && o.CreatedAt <= range.To)
            .Select(o => new
            {
                Insurance = o.Patient.Insurances
                    .Where(i => i.IsActive)
                    .OrderByDescending(i => i.IsPrimary)
                    .Select(i => i.HealthInsurance.Name)
                    .FirstOrDefault() ?? "Particular",
            })
            .ToListAsync(ct);

        var grouped = items.GroupBy(i => i.Insurance)
            .Select(g => new { Insurance = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("insurance", g.Insurance),
            ("count", g.Count)));

        return Result(def, null, [
            new("insurance", "Convênio"),
            new("count", "Exames"),
        ], rows);
    }

    private async Task<ReportResultDto> EmployeeShiftsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var fromDate = DateOnly.FromDateTime(range.From);
        var toDate = DateOnly.FromDateTime(range.To);

        var items = await dbContext.EmployeeShifts.AsNoTracking()
            .Where(s => s.IsActive && s.ShiftDate >= fromDate && s.ShiftDate <= toDate)
            .Select(s => new
            {
                s.ShiftDate,
                Employee = s.Employee.FullName,
                s.ShiftType,
                Department = s.Department.Name,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, s => ReportRowBuilder.Row(
            ("date", s.ShiftDate.ToString("dd/MM/yyyy")),
            ("employee", s.Employee),
            ("type", s.ShiftType.ToString()),
            ("department", s.Department)));

        return Result(def, null, [
            new("date", "Data"),
            new("employee", "Funcionário"),
            new("type", "Plantão"),
            new("department", "Setor"),
        ], rows, [new ReportKpiDto("Plantões", rows.Count.ToString(), "primary")]);
    }

    private async Task<ReportResultDto> EmployeeSchedulesAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var fromDate = DateOnly.FromDateTime(range.From);
        var toDate = DateOnly.FromDateTime(range.To);

        var items = await dbContext.EmployeeShifts.AsNoTracking()
            .Where(s => s.IsActive && s.ShiftDate >= fromDate && s.ShiftDate <= toDate)
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.Employee.FullName)
            .Select(s => new
            {
                s.ShiftDate,
                Employee = s.Employee.FullName,
                Role = s.Employee.Role,
                s.ShiftType,
                Department = s.Department.Name,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, s =>
        {
            var dow = s.ShiftDate.DayOfWeek switch
            {
                DayOfWeek.Saturday or DayOfWeek.Sunday => "Fim de semana",
                _ => s.ShiftDate.DayOfWeek.ToString(),
            };
            var hours = s.ShiftType switch
            {
                ShiftType.Morning => "06:00–14:00",
                ShiftType.Afternoon => "14:00–22:00",
                ShiftType.Night => "22:00–06:00",
                _ => "—",
            };
            return ReportRowBuilder.Row(
                ("date", s.ShiftDate.ToString("dd/MM/yyyy")),
                ("weekday", dow),
                ("employee", s.Employee),
                ("role", s.Role.ToString()),
                ("shift", s.ShiftType.ToString()),
                ("hours", hours),
                ("department", s.Department));
        });

        return Result(def, "Escalas detalhadas por colaborador e turno", [
            new("date", "Data"),
            new("weekday", "Dia"),
            new("employee", "Colaborador"),
            new("role", "Função"),
            new("shift", "Turno"),
            new("hours", "Horário"),
            new("department", "Setor"),
        ], rows, [
            new ReportKpiDto("Escalas", rows.Count.ToString(), "primary"),
            new ReportKpiDto("Colaboradores", items.Select(i => i.Employee).Distinct().Count().ToString()),
        ]);
    }

    private async Task<ReportResultDto> EmployeeOvertimeAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        const decimal hoursPerShift = 8m;
        const int standardShiftsPerMonth = 20;

        var fromDate = DateOnly.FromDateTime(range.From);
        var toDate = DateOnly.FromDateTime(range.To);

        var shifts = await dbContext.EmployeeShifts.AsNoTracking()
            .Where(s => s.IsActive && s.ShiftDate >= fromDate && s.ShiftDate <= toDate)
            .Select(s => new
            {
                Employee = s.Employee.FullName,
                Department = s.Department.Name,
                s.ShiftDate,
                s.ShiftType,
            })
            .ToListAsync(ct);

        var grouped = shifts
            .GroupBy(s => new { s.Employee, s.Department })
            .Select(g =>
            {
                var nightHours = g.Count(x => x.ShiftType == ShiftType.Night) * hoursPerShift;
                var weekendHours = g.Count(x =>
                    x.ShiftDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) * 4m;
                var excessShifts = Math.Max(0, g.Count() - standardShiftsPerMonth);
                var excessHours = excessShifts * hoursPerShift;
                var total = nightHours + weekendHours + excessHours;
                return new
                {
                    g.Key.Employee,
                    g.Key.Department,
                    Shifts = g.Count(),
                    NightHours = nightHours,
                    WeekendHours = weekendHours,
                    ExcessHours = excessHours,
                    TotalOvertime = total,
                };
            })
            .Where(x => x.TotalOvertime > 0)
            .OrderByDescending(x => x.TotalOvertime)
            .ToList();

        var rows = ReportRowBuilder.From(grouped, g => ReportRowBuilder.Row(
            ("employee", g.Employee),
            ("department", g.Department),
            ("shifts", g.Shifts),
            ("nightHours", g.NightHours),
            ("weekendHours", g.WeekendHours),
            ("excessHours", g.ExcessHours),
            ("totalHours", g.TotalOvertime)));

        var totalHours = grouped.Sum(g => g.TotalOvertime);

        return Result(def, "Estimativa: plantão noturno, fim de semana e escalas acima de 20 turnos/mês", [
            new("employee", "Colaborador"),
            new("department", "Setor"),
            new("shifts", "Turnos"),
            new("nightHours", "H. noturnas"),
            new("weekendHours", "H. fim de semana"),
            new("excessHours", "H. excedentes"),
            new("totalHours", "Total H. extras"),
        ], rows, [
            new ReportKpiDto("Colaboradores c/ extras", grouped.Count.ToString(), "warning"),
            new ReportKpiDto("Horas extras (est.)", totalHours.ToString("0.#"), "primary"),
        ]);
    }

    private async Task<ReportResultDto> HrProductivityAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var fromDate = DateOnly.FromDateTime(range.From);
        var toDate = DateOnly.FromDateTime(range.To);

        var activeByDept = await dbContext.Employees.AsNoTracking()
            .Where(e => e.IsActive)
            .GroupBy(e => e.Department.Name)
            .Select(g => new { Department = g.Key, Headcount = g.Count() })
            .ToListAsync(ct);

        var shiftsByDept = await dbContext.EmployeeShifts.AsNoTracking()
            .Where(s => s.IsActive && s.ShiftDate >= fromDate && s.ShiftDate <= toDate)
            .GroupBy(s => s.Department.Name)
            .Select(g => new { Department = g.Key, Shifts = g.Count() })
            .ToListAsync(ct);

        var deptNames = activeByDept.Select(d => d.Department)
            .Union(shiftsByDept.Select(d => d.Department))
            .Distinct()
            .OrderBy(d => d);

        var rows = new List<Dictionary<string, object?>>();
        foreach (var dept in deptNames)
        {
            var headcount = activeByDept.FirstOrDefault(d => d.Department == dept)?.Headcount ?? 0;
            var shifts = shiftsByDept.FirstOrDefault(d => d.Department == dept)?.Shifts ?? 0;
            var ratio = headcount == 0 ? 0 : Math.Round((decimal)shifts / headcount, 1);
            rows.Add(new Dictionary<string, object?>
            {
                ["department"] = dept,
                ["headcount"] = headcount,
                ["shifts"] = shifts,
                ["shiftsPerEmployee"] = ratio,
            });
        }

        var completedAppts = await dbContext.Appointments.AsNoTracking()
            .CountAsync(a => a.IsActive
                && a.Status == AppointmentStatus.Completed
                && a.ScheduledAt >= range.From
                && a.ScheduledAt <= range.To, ct);

        return Result(def, "Turnos por colaborador ativo e atendimentos concluídos no período", [
            new("department", "Setor"),
            new("headcount", "Colaboradores"),
            new("shifts", "Turnos"),
            new("shiftsPerEmployee", "Turnos / colaborador"),
        ], rows, [
            new ReportKpiDto("Setores", rows.Count.ToString()),
            new ReportKpiDto("Consultas realizadas", completedAppts.ToString(), "success"),
        ]);
    }

    private async Task<ReportResultDto> ReceptionProductivityAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.Appointments.AsNoTracking()
            .Where(a => a.IsActive && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To)
            .Select(a => new { a.ScheduledAt, a.Status })
            .ToListAsync(ct);

        var grouped = items
            .GroupBy(a => a.ScheduledAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key,
                Scheduled = g.Count(),
                Completed = g.Count(x => x.Status == AppointmentStatus.Completed),
                Cancelled = g.Count(x => x.Status == AppointmentStatus.Cancelled),
                NoShow = g.Count(x => x.Status == AppointmentStatus.NoShow),
                InProgress = g.Count(x => x.Status is AppointmentStatus.InProgress or AppointmentStatus.Confirmed),
            })
            .ToList();

        var rows = ReportRowBuilder.From(grouped, g =>
        {
            var rate = g.Scheduled == 0 ? 0 : Math.Round(g.Completed * 100m / g.Scheduled, 1);
            return ReportRowBuilder.Row(
                ("date", g.Date.ToString("dd/MM/yyyy")),
                ("scheduled", g.Scheduled),
                ("completed", g.Completed),
                ("cancelled", g.Cancelled),
                ("noShow", g.NoShow),
                ("inProgress", g.InProgress),
                ("completionRate", $"{rate}%"));
        });

        var totalScheduled = grouped.Sum(g => g.Scheduled);
        var totalCompleted = grouped.Sum(g => g.Completed);
        var overallRate = totalScheduled == 0 ? 0 : Math.Round(totalCompleted * 100m / totalScheduled, 1);

        return Result(def, "Produtividade diária da recepção — agendamentos e comparecimento", [
            new("date", "Data"),
            new("scheduled", "Agendados"),
            new("completed", "Realizados"),
            new("cancelled", "Cancelados"),
            new("noShow", "Faltas"),
            new("inProgress", "Em andamento"),
            new("completionRate", "Taxa realização"),
        ], rows, [
            new ReportKpiDto("Agendamentos", totalScheduled.ToString(), "primary"),
            new ReportKpiDto("Realizados", totalCompleted.ToString(), "success"),
            new ReportKpiDto("Taxa geral", $"{overallRate}%"),
        ]);
    }

    private async Task<ReportResultDto> PatientFallsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.SecurityIncidents.AsNoTracking()
            .Where(s => s.CreatedAt >= range.From && s.CreatedAt <= range.To
                && (s.Type == SecurityIncidentType.PatientFall
                    || EF.Functions.ILike(s.Description, "%queda%")
                    || EF.Functions.ILike(s.Description, "%fall%")))
            .Select(s => new
            {
                s.CreatedAt,
                s.Location,
                s.Description,
                Patient = s.Patient != null ? s.Patient.FullName : null,
                s.Severity,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, s => ReportRowBuilder.Row(
            ("date", FormatDate(s.CreatedAt)),
            ("patient", s.Patient ?? "—"),
            ("severity", s.Severity?.ToString() ?? "—"),
            ("location", s.Location),
            ("description", s.Description)));

        return Result(def, "Quedas de pacientes — NPSG", [
            new("date", "Data"),
            new("patient", "Paciente"),
            new("severity", "Gravidade"),
            new("location", "Local"),
            new("description", "Descrição"),
        ], rows, [new ReportKpiDto("Quedas", rows.Count.ToString(), "warning")]);
    }

    private async Task<ReportResultDto> QualityIndicatorsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var infections = await dbContext.InfectionSurveillances
            .CountAsync(i => i.CreatedAt >= range.From && i.CreatedAt <= range.To, ct);
        var hospitalizations = await dbContext.Hospitalizations
            .CountAsync(h => h.AdmittedAt >= range.From && h.AdmittedAt <= range.To, ct);
        var adverse = await dbContext.SecurityIncidents
            .CountAsync(s => s.CreatedAt >= range.From && s.CreatedAt <= range.To, ct);
        var deaths = await dbContext.Hospitalizations
            .CountAsync(h => h.DischargedAt >= range.From && h.DischargedAt <= range.To
                && h.Patient.IsDeceased, ct);
        var rate = hospitalizations == 0 ? 0 : Math.Round(infections * 100m / hospitalizations, 2);

        return Result(def, null, [
            new("indicator", "Indicador"),
            new("value", "Valor"),
        ], [
            new() { ["indicator"] = "Infecções hospitalares", ["value"] = infections },
            new() { ["indicator"] = "Taxa de infecção (%)", ["value"] = rate },
            new() { ["indicator"] = "Eventos adversos / segurança", ["value"] = adverse },
            new() { ["indicator"] = "Óbitos (paciente falecido)", ["value"] = deaths },
            new() { ["indicator"] = "Internações no período", ["value"] = hospitalizations },
        ]);
    }

    private async Task<ReportResultDto> AntibioticsUsageAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.PharmacyDispensings.AsNoTracking()
            .Where(d => d.DispensedAt >= range.From && d.DispensedAt <= range.To
                && (EF.Functions.ILike(d.Product.Name, "%cilin%")
                    || EF.Functions.ILike(d.Product.Name, "%micina%")
                    || EF.Functions.ILike(d.Product.Name, "%oxacil%")
                    || EF.Functions.ILike(d.Product.Name, "%antibio%")
                    || EF.Functions.ILike(d.Product.Category ?? "", "%antibio%")))
            .Select(d => new
            {
                d.DispensedAt,
                Product = d.Product.Name,
                Patient = d.Patient.FullName,
                d.Quantity,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, d => ReportRowBuilder.Row(
            ("date", FormatDate(d.DispensedAt)),
            ("product", d.Product),
            ("patient", d.Patient),
            ("quantity", d.Quantity)));

        return Result(def, "Dispensações de antimicrobianos (heurística por nome)", [
            new("date", "Data"),
            new("product", "Medicamento"),
            new("patient", "Paciente"),
            new("quantity", "Qtd."),
        ], rows, [new ReportKpiDto("Dispensações", rows.Count.ToString(), "warning")]);
    }

    private async Task<ReportResultDto> UnauthorizedAccessAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.SecurityIncidents.AsNoTracking()
            .Where(s => s.CreatedAt >= range.From && s.CreatedAt <= range.To
                && s.Type == SecurityIncidentType.AccessDenied)
            .Select(s => new { s.CreatedAt, s.Location, s.Description })
            .ToListAsync(ct);

        var auditItems = await dbContext.AuditLogs.AsNoTracking()
            .Where(a => a.CreatedAt >= range.From && a.CreatedAt <= range.To
                && EF.Functions.ILike(a.Action, "%denied%"))
            .Select(a => new { a.CreatedAt, Location = a.EntityType, Description = a.Details })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(
            items.Select(i => (i.CreatedAt, i.Location, i.Description))
                .Concat(auditItems.Select(a => (a.CreatedAt, a.Location, a.Description))),
            s => ReportRowBuilder.Row(
                ("date", FormatDate(s.CreatedAt)),
                ("location", s.Location),
                ("description", s.Description)));

        return Result(def, null, [
            new("date", "Data"),
            new("location", "Origem"),
            new("description", "Detalhe"),
        ], rows, [new ReportKpiDto("Tentativas", rows.Count.ToString(), "danger")]);
    }

    private async Task<ReportResultDto> ClinicalIndicatorsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var appointments = await dbContext.Appointments.CountAsync(
            a => a.IsActive && a.Status == AppointmentStatus.Completed
                && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To, ct);
        var er = await dbContext.EmergencyVisits.CountAsync(
            e => e.IsActive && e.ArrivedAt >= range.From && e.ArrivedAt <= range.To, ct);
        var hosp = await dbContext.Hospitalizations.CountAsync(
            h => h.AdmittedAt >= range.From && h.AdmittedAt <= range.To, ct);
        var surgeries = await dbContext.Surgeries.CountAsync(
            s => s.IsActive && s.Status == SurgeryStatus.Completed
                && s.ScheduledAt >= range.From && s.ScheduledAt <= range.To, ct);
        var beds = await dbContext.Beds.CountAsync(b => b.IsActive, ct);
        var occupied = await dbContext.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Occupied, ct);
        var occupancy = beds == 0 ? 0 : Math.Round(occupied * 100m / beds, 1);

        return Result(def, null, [
            new("indicator", "Indicador"),
            new("value", "Valor"),
        ], [
            new() { ["indicator"] = "Consultas realizadas", ["value"] = appointments },
            new() { ["indicator"] = "Atendimentos PS", ["value"] = er },
            new() { ["indicator"] = "Internações", ["value"] = hosp },
            new() { ["indicator"] = "Cirurgias realizadas", ["value"] = surgeries },
            new() { ["indicator"] = "Taxa de ocupação (%)", ["value"] = occupancy },
        ]);
    }

    private async Task<ReportResultDto> BpaProductionAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.Appointments.AsNoTracking()
            .Where(a => a.IsActive && a.Status == AppointmentStatus.Completed
                && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To)
            .Select(a => new
            {
                a.ScheduledAt,
                Patient = a.Patient.FullName,
                PatientCns = a.Patient.Cns,
                Doctor = a.Professional.FullName,
                Specialty = a.Professional.Specialty.Name,
            })
            .Take(500)
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, a => ReportRowBuilder.Row(
            ("date", a.ScheduledAt.ToString("dd/MM/yyyy")),
            ("patient", a.Patient),
            ("cns", a.PatientCns ?? "—"),
            ("professional", a.Doctor),
            ("specialty", a.Specialty),
            ("procedure", "0301010072")));

        return Result(def, "Produção ambulatorial BPA (amostra)", [
            new("date", "Data"),
            new("patient", "Paciente"),
            new("cns", "CNS"),
            new("professional", "Profissional"),
            new("specialty", "Especialidade"),
            new("procedure", "Proc. SIGTAP"),
        ], rows, [new ReportKpiDto("Atendimentos", items.Count.ToString(), "success")]);
    }

    private async Task<ReportResultDto> AihProductionAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.Hospitalizations.AsNoTracking()
            .Where(h => h.IsActive && h.AdmittedAt >= range.From && h.AdmittedAt <= range.To)
            .Select(h => new
            {
                h.AdmittedAt,
                Patient = h.Patient.FullName,
                Cns = h.Patient.Cns,
                h.AihNumber,
                h.PrimaryCid10Code,
                h.PrimarySigtapProcedureCode,
                h.SusCompetence,
                Ward = h.Bed.Ward.Name,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, h => ReportRowBuilder.Row(
            ("admitted", h.AdmittedAt.ToString("dd/MM/yyyy")),
            ("patient", h.Patient),
            ("cns", h.Cns ?? "—"),
            ("aih", h.AihNumber ?? "Pendente"),
            ("cid", h.PrimaryCid10Code ?? "—"),
            ("procedure", h.PrimarySigtapProcedureCode ?? "—"),
            ("competence", h.SusCompetence ?? "—"),
            ("ward", h.Ward)));

        return Result(def, "AIH / SIH-SUS — internações no período", [
            new("admitted", "Internação"),
            new("patient", "Paciente"),
            new("cns", "CNS"),
            new("aih", "Nº AIH"),
            new("cid", "CID"),
            new("procedure", "SIGTAP"),
            new("competence", "Competência"),
            new("ward", "Ala"),
        ], rows, [new ReportKpiDto("AIHs", rows.Count.ToString(), "primary")]);
    }

    private async Task<ReportResultDto> SiaProductionAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var labs = await dbContext.LabOrders.CountAsync(
            o => o.IsActive && o.Status == LabOrderStatus.Completed
                && o.UpdatedAt >= range.From && o.UpdatedAt <= range.To, ct);
        var imaging = await dbContext.ImagingStudies.CountAsync(
            o => o.IsActive && o.Status == ImagingStudyStatus.Completed
                && o.CompletedAt >= range.From && o.CompletedAt <= range.To, ct);

        return Result(def, "Produção SIA — procedimentos ambulatoriais de média/alta complexidade", [
            new("type", "Tipo"),
            new("count", "Quantidade"),
            new("estimated", "Estimativa (R$)"),
        ], [
            new() { ["type"] = "Exames laboratoriais", ["count"] = labs, ["estimated"] = FormatMoney(labs * 12.5m) },
            new() { ["type"] = "Exames de imagem", ["count"] = imaging, ["estimated"] = FormatMoney(imaging * 85m) },
        ], [new ReportKpiDto("Procedimentos", (labs + imaging).ToString(), "success")]);
    }

    private async Task<ReportResultDto> HospitalProductionAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var hosp = await dbContext.Hospitalizations.CountAsync(
            h => h.AdmittedAt >= range.From && h.AdmittedAt <= range.To, ct);
        var surgeries = await dbContext.Surgeries.CountAsync(
            s => s.IsActive && s.Status == SurgeryStatus.Completed
                && s.ScheduledAt >= range.From && s.ScheduledAt <= range.To, ct);
        var chemo = await dbContext.ChemotherapySessions.CountAsync(
            c => c.ScheduledAt >= range.From && c.ScheduledAt <= range.To, ct);

        return Result(def, null, [
            new("item", "Produção"),
            new("count", "Quantidade"),
        ], [
            new() { ["item"] = "Internações", ["count"] = hosp },
            new() { ["item"] = "Cirurgias", ["count"] = surgeries },
            new() { ["item"] = "Sessões quimioterapia", ["count"] = chemo },
        ]);
    }

    private async Task<ReportResultDto> AmbulanceOperationsAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var items = await dbContext.AmbulanceDispatches.AsNoTracking()
            .Where(x => x.IsActive && x.RequestedAt >= range.From && x.RequestedAt <= range.To)
            .Select(x => new
            {
                x.RequestedAt,
                x.PatientName,
                x.Destination,
                x.Status,
                Ambulance = x.Ambulance != null ? x.Ambulance.Code : "Não atribuída",
            })
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, i => ReportRowBuilder.Row(
            ("date", i.RequestedAt.ToString("dd/MM/yyyy HH:mm")),
            ("patient", i.PatientName),
            ("ambulance", i.Ambulance),
            ("destination", i.Destination),
            ("status", i.Status.ToString())));

        return Result(def, "Despachos de ambulância por período", [
            new("date", "Data"),
            new("patient", "Paciente"),
            new("ambulance", "Ambulância"),
            new("destination", "Destino"),
            new("status", "Status"),
        ], rows, [
            new ReportKpiDto("Chamados", items.Count.ToString(), "primary"),
            new ReportKpiDto("Em andamento", items.Count(i => i.Status == AmbulanceDispatchStatus.Dispatched).ToString()),
        ]);
    }

    private async Task<ReportResultDto> CnesReportAsync(ReportDefinition def, CancellationToken ct)
    {
        var professionals = await dbContext.Professionals.AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new { p.FullName, p.Crm, Specialty = p.Specialty.Name, p.CouncilUf })
            .ToListAsync(ct);

        var employees = await dbContext.Employees.AsNoTracking()
            .Where(e => e.IsActive)
            .Select(e => new { e.FullName, Role = e.Role.ToString(), Department = e.Department.Name })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(professionals, p => ReportRowBuilder.Row(
            ("name", p.FullName),
            ("council", p.Crm ?? "—"),
            ("specialty", p.Specialty),
            ("uf", p.CouncilUf ?? "—"),
            ("type", "Profissional assistencial")));

        foreach (var e in employees)
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["name"] = e.FullName,
                ["council"] = "—",
                ["specialty"] = e.Department,
                ["uf"] = "—",
                ["type"] = e.Role,
            });
        }

        return Result(def, "Profissionais e funcionários (referência CNES)", [
            new("name", "Nome"),
            new("council", "Conselho"),
            new("specialty", "Especialidade/Setor"),
            new("uf", "UF"),
            new("type", "Tipo"),
        ], rows, [new ReportKpiDto("Cadastros", rows.Count.ToString(), "info")]);
    }

    private async Task<ReportResultDto> EsusProductionAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var vaccinations = await dbContext.PatientVaccinations.CountAsync(
            v => v.IsActive && v.AdministeredAt >= range.From && v.AdministeredAt <= range.To, ct);
        var appointments = await dbContext.Appointments.CountAsync(
            a => a.IsActive && a.Status == AppointmentStatus.Completed
                && a.ScheduledAt >= range.From && a.ScheduledAt <= range.To, ct);
        var vitals = await dbContext.VitalSignRecords.CountAsync(
            v => v.CreatedAt >= range.From && v.CreatedAt <= range.To, ct);

        return Result(def, "Fichas e-SUS APS (agregado)", [
            new("ficha", "Ficha e-SUS"),
            new("records", "Registros"),
        ], [
            new() { ["ficha"] = "Vacinação", ["records"] = vaccinations },
            new() { ["ficha"] = "Atendimento individual", ["records"] = appointments },
            new() { ["ficha"] = "Procedimentos / sinais vitais", ["records"] = vitals },
        ], [new ReportKpiDto("Total registros", (vaccinations + appointments + vitals).ToString(), "success")]);
    }

    private async Task<ReportResultDto> CihaProductionAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var chemo = await dbContext.ChemotherapySessions.AsNoTracking()
            .Where(c => c.IsActive && c.ScheduledAt >= range.From && c.ScheduledAt <= range.To)
            .Select(c => new
            {
                c.ScheduledAt,
                Patient = c.Patient.FullName,
                Cns = c.Patient.Cns,
                c.ProtocolName,
                c.Status,
                c.CycleNumber,
                c.TotalCycles,
            })
            .ToListAsync(ct);

        var dialysis = await dbContext.DialysisSessions.AsNoTracking()
            .Where(d => d.IsActive && d.ScheduledAt >= range.From && d.ScheduledAt <= range.To)
            .Select(d => new
            {
                d.ScheduledAt,
                Patient = d.Patient.FullName,
                Cns = d.Patient.Cns,
                d.Status,
                d.MachineNumber,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(chemo, c => ReportRowBuilder.Row(
            ("date", c.ScheduledAt.ToString("dd/MM/yyyy")),
            ("patient", c.Patient),
            ("cns", c.Cns ?? "—"),
            ("procedure", "Quimioterapia"),
            ("protocol", c.ProtocolName ?? "—"),
            ("cycle", $"{c.CycleNumber}/{c.TotalCycles}"),
            ("status", c.Status.ToString()),
            ("sigtap", "0304030188")));

        foreach (var d in dialysis)
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["date"] = d.ScheduledAt.ToString("dd/MM/yyyy"),
                ["patient"] = d.Patient,
                ["cns"] = d.Cns ?? "—",
                ["procedure"] = "Diálise",
                ["protocol"] = d.MachineNumber,
                ["cycle"] = "—",
                ["status"] = d.Status.ToString(),
                ["sigtap"] = "0304050079",
            });
        }

        return Result(def, "CIHA — quimioterapia e diálise (alta complexidade ambulatorial)", [
            new("date", "Data"),
            new("patient", "Paciente"),
            new("cns", "CNS"),
            new("procedure", "Procedimento"),
            new("protocol", "Protocolo/Máquina"),
            new("cycle", "Ciclo"),
            new("status", "Situação"),
            new("sigtap", "Proc. SIGTAP"),
        ], rows, [
            new ReportKpiDto("Sessões quimio", chemo.Count.ToString(), "primary"),
            new ReportKpiDto("Sessões diálise", dialysis.Count.ToString(), "info"),
            new ReportKpiDto("Total CIHA", (chemo.Count + dialysis.Count).ToString(), "success"),
        ]);
    }

    private async Task<ReportResultDto> ApacProductionAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, CancellationToken ct)
    {
        var competence = range.To.ToString("yyyyMM");

        var chemo = await dbContext.ChemotherapySessions.AsNoTracking()
            .Where(c => c.IsActive && c.ScheduledAt >= range.From && c.ScheduledAt <= range.To
                && c.Status != ChemotherapySessionStatus.Cancelled)
            .Select(c => new
            {
                c.Id,
                c.ScheduledAt,
                Patient = c.Patient.FullName,
                Cns = c.Patient.Cns,
                c.ProtocolName,
                c.Status,
                Professional = c.Professional.FullName,
            })
            .ToListAsync(ct);

        var dialysis = await dbContext.DialysisSessions.AsNoTracking()
            .Where(d => d.IsActive && d.ScheduledAt >= range.From && d.ScheduledAt <= range.To
                && d.Status != DialysisSessionStatus.Cancelled)
            .Select(d => new
            {
                d.Id,
                d.ScheduledAt,
                Patient = d.Patient.FullName,
                Cns = d.Patient.Cns,
                d.Status,
                d.NurseName,
            })
            .ToListAsync(ct);

        var rows = new List<Dictionary<string, object?>>();

        foreach (var c in chemo)
        {
            var apacNum = $"APAC{competence}{c.Id.ToString("N")[..6].ToUpperInvariant()}";
            rows.Add(new Dictionary<string, object?>
            {
                ["apac"] = apacNum,
                ["date"] = c.ScheduledAt.ToString("dd/MM/yyyy"),
                ["patient"] = c.Patient,
                ["cns"] = c.Cns ?? "—",
                ["procedure"] = "0304030188",
                ["label"] = $"Quimioterapia — {c.ProtocolName}",
                ["cid"] = "C50.9",
                ["professional"] = c.Professional,
                ["validity"] = $"{c.ScheduledAt:MM/yyyy} — {c.ScheduledAt.AddMonths(3):MM/yyyy}",
                ["status"] = c.Status.ToString(),
            });
        }

        foreach (var d in dialysis)
        {
            var apacNum = $"APAC{competence}{d.Id.ToString("N")[..6].ToUpperInvariant()}";
            rows.Add(new Dictionary<string, object?>
            {
                ["apac"] = apacNum,
                ["date"] = d.ScheduledAt.ToString("dd/MM/yyyy"),
                ["patient"] = d.Patient,
                ["cns"] = d.Cns ?? "—",
                ["procedure"] = "0304050079",
                ["label"] = "Hemodiálise",
                ["cid"] = "N18.9",
                ["professional"] = d.NurseName ?? "Enfermagem",
                ["validity"] = $"{d.ScheduledAt:MM/yyyy} — {d.ScheduledAt.AddMonths(6):MM/yyyy}",
                ["status"] = d.Status.ToString(),
            });
        }

        var estimated = rows.Count * 735m;
        return Result(def, "APAC — autorização de procedimentos de alta complexidade (oncologia e diálise)", [
            new("apac", "Nº APAC"),
            new("date", "Data"),
            new("patient", "Paciente"),
            new("cns", "CNS"),
            new("procedure", "SIGTAP"),
            new("label", "Procedimento"),
            new("cid", "CID-10"),
            new("professional", "Profissional"),
            new("validity", "Validade"),
            new("status", "Situação"),
        ], rows, [
            new ReportKpiDto("APACs quimio", chemo.Count.ToString(), "primary"),
            new ReportKpiDto("APACs diálise", dialysis.Count.ToString(), "info"),
            new ReportKpiDto("Valor estimado", FormatMoney(estimated), "success"),
        ]);
    }

    private async Task<ReportResultDto> AbcCurveAsync(
        ReportDefinition def, (DateTime From, DateTime To) range, ProductType productType, CancellationToken ct)
    {
        var productValues = new Dictionary<Guid, (string Name, string Sku, decimal Qty, decimal Value)>();

        if (productType == ProductType.Medication)
        {
            var dispensings = await dbContext.PharmacyDispensings.AsNoTracking()
                .Where(d => d.IsActive && d.DispensedAt >= range.From && d.DispensedAt <= range.To)
                .Select(d => new
                {
                    d.ProductId,
                    Name = d.Product.Name,
                    Sku = d.Product.Sku,
                    d.Quantity,
                    Price = d.Product.AverageSalePrice > 0 ? d.Product.AverageSalePrice : d.Product.AveragePurchasePrice,
                })
                .ToListAsync(ct);

            foreach (var d in dispensings)
            {
                var netQty = d.Quantity;
                var value = netQty * d.Price;
                if (productValues.TryGetValue(d.ProductId, out var existing))
                {
                    productValues[d.ProductId] = (existing.Name, existing.Sku, existing.Qty + netQty, existing.Value + value);
                }
                else
                {
                    productValues[d.ProductId] = (d.Name, d.Sku, netQty, value);
                }
            }
        }
        else
        {
            var movements = await dbContext.StockMovements.AsNoTracking()
                .Where(m => m.IsActive && m.Type == StockMovementType.Outbound
                    && m.CreatedAt >= range.From && m.CreatedAt <= range.To
                    && m.Product.Type == ProductType.Supply)
                .Select(m => new
                {
                    m.ProductId,
                    Name = m.Product.Name,
                    Sku = m.Product.Sku,
                    m.Quantity,
                    Price = m.UnitPrice ?? (m.Product.AveragePurchasePrice > 0 ? m.Product.AveragePurchasePrice : 1m),
                })
                .ToListAsync(ct);

            foreach (var m in movements)
            {
                var value = m.Quantity * PriceOrDefault(m.Price);
                if (productValues.TryGetValue(m.ProductId, out var existing))
                {
                    productValues[m.ProductId] = (existing.Name, existing.Sku, existing.Qty + m.Quantity, existing.Value + value);
                }
                else
                {
                    productValues[m.ProductId] = (m.Name, m.Sku, m.Quantity, value);
                }
            }
        }

        var ranked = productValues.Values
            .OrderByDescending(p => p.Value)
            .ToList();

        var totalValue = ranked.Sum(p => p.Value);
        decimal cumulative = 0;
        var rows = new List<Dictionary<string, object?>>();

        foreach (var (name, sku, qty, value) in ranked)
        {
            cumulative += value;
            var pct = totalValue > 0 ? Math.Round(value / totalValue * 100, 2) : 0m;
            var cumPct = totalValue > 0 ? Math.Round(cumulative / totalValue * 100, 2) : 0m;
            var cls = cumPct <= 80 ? "A" : cumPct <= 95 ? "B" : "C";

            rows.Add(new Dictionary<string, object?>
            {
                ["product"] = name,
                ["sku"] = sku,
                ["qty"] = qty,
                ["value"] = FormatMoney(value),
                ["pct"] = $"{pct}%",
                ["cumulative"] = $"{cumPct}%",
                ["class"] = cls,
                ["count"] = value,
            });
        }

        var classA = rows.Count(r => r["class"]?.ToString() == "A");
        var classB = rows.Count(r => r["class"]?.ToString() == "B");
        var classC = rows.Count(r => r["class"]?.ToString() == "C");

        var moduleLabel = productType == ProductType.Medication ? "Farmácia" : "Almoxarifado";
        return Result(def, $"Curva ABC — {moduleLabel} (consumo por valor no período)", [
            new("product", "Produto"),
            new("sku", "SKU"),
            new("qty", "Quantidade"),
            new("value", "Valor (R$)"),
            new("pct", "% individual"),
            new("cumulative", "% acumulado"),
            new("class", "Classe"),
        ], rows, [
            new ReportKpiDto("Itens analisados", rows.Count.ToString(), "primary"),
            new ReportKpiDto("Classe A", classA.ToString(), "success"),
            new ReportKpiDto("Classe B", classB.ToString(), "info"),
            new ReportKpiDto("Classe C", classC.ToString(), "warning"),
            new ReportKpiDto("Valor total", FormatMoney(totalValue), "primary"),
        ]);
    }

    private static decimal PriceOrDefault(decimal price) => price > 0 ? price : 1m;

    private async Task<ReportResultDto> FinancialCashSessionsAsync(ReportDefinition def, CancellationToken ct)
    {
        var items = await dbContext.FinancialCashSessions.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.OpenedAt)
            .Take(100)
            .Select(s => new
            {
                s.Label,
                s.OpenedAt,
                s.ClosedAt,
                s.OpeningBalance,
                s.ExpectedBalance,
                s.ClosingBalance,
                s.Status,
            })
            .ToListAsync(ct);

        var rows = ReportRowBuilder.From(items, s => ReportRowBuilder.Row(
            ("label", s.Label),
            ("opened", s.OpenedAt.ToString("dd/MM/yyyy HH:mm")),
            ("closed", s.ClosedAt.HasValue ? s.ClosedAt.Value.ToString("dd/MM/yyyy HH:mm") : "Aberto"),
            ("opening", s.OpeningBalance),
            ("expected", s.ExpectedBalance),
            ("closing", s.ClosingBalance ?? 0m),
            ("status", s.Status.ToString())));

        return Result(def, "Sessões de caixa", [
            new("label", "Caixa"),
            new("opened", "Abertura"),
            new("closed", "Fechamento"),
            new("opening", "Saldo inicial"),
            new("expected", "Saldo esperado"),
            new("closing", "Saldo informado"),
            new("status", "Situação"),
        ], rows);
    }
}
