using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Billing;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class BillingDashboardService(
    AppDbContext dbContext,
    IFinancialAccountService financialAccountService,
    IInsuranceIntegrationService insuranceIntegrationService) : IBillingDashboardService
{
    public async Task<BillingDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var financial = await financialAccountService.GetSummaryAsync(cancellationToken);
        var tiss = await insuranceIntegrationService.GetConvenioDashboardAsync(cancellationToken);

        var openAccounts = await dbContext.FinancialAccounts
            .Where(f => f.IsActive
                && f.Direction == FinancialAccountDirection.Receivable
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid))
            .Select(f => new { f.Amount, f.PaidAmount })
            .ToListAsync(cancellationToken);

        var guidesDraft = await dbContext.TissGuides.CountAsync(
            g => g.IsActive && g.Status == TissGuideStatus.Draft, cancellationToken);
        var guidesSent = await dbContext.TissGuides.CountAsync(
            g => g.IsActive && g.Status == TissGuideStatus.Sent, cancellationToken);
        var guidesPaid = await dbContext.TissGuides.CountAsync(
            g => g.IsActive && g.Status == TissGuideStatus.Paid, cancellationToken);
        var guidesGlosa = await dbContext.TissGuides.CountAsync(
            g => g.IsActive && g.Status == TissGuideStatus.Glosa, cancellationToken);

        var activeHosp = await dbContext.Hospitalizations
            .Include(h => h.Patient).ThenInclude(p => p.Insurances).ThenInclude(i => i.HealthInsurance)
            .Include(h => h.Bed).ThenInclude(b => b.Ward)
            .Where(h => h.IsActive && h.Status == HospitalizationStatus.Active)
            .ToListAsync(cancellationToken);

        var activeSus = activeHosp.Count(h =>
            h.Bed.Ward.CoverageModality == WardCoverageModality.Sus
            || h.Patient.Insurances.Any(i =>
                i.IsActive && i.HealthInsurance.Name.Contains("SUS", StringComparison.OrdinalIgnoreCase)));

        var aihReady = activeHosp.Count(h =>
            !string.IsNullOrWhiteSpace(h.PrimaryCid10Code ?? h.Diagnosis)
            && !string.IsNullOrWhiteSpace(h.PrimarySigtapProcedureCode));

        var susExports = await dbContext.IntegrationMessages.CountAsync(
            m => m.IsActive
                && (m.Type == IntegrationMessageType.SihExport || m.Type == IntegrationMessageType.SiaExport)
                && m.CreatedAt >= startOfMonth,
            cancellationToken);

        var alerts = BuildAlerts(
            openAccounts.Count,
            guidesDraft,
            tiss.GuidesSentOver30Days,
            tiss.TotalGlosaOpen,
            tiss.GlosaRatePercent,
            activeSus,
            aihReady);

        return new BillingDashboardDto(
            openAccounts.Count,
            openAccounts.Sum(f => f.Amount - f.PaidAmount),
            financial.ReceivableOpen,
            financial.ReceivedThisMonth,
            guidesDraft,
            guidesSent,
            guidesPaid,
            guidesGlosa,
            tiss.TotalBilled,
            tiss.TotalPaid,
            tiss.TotalGlosaOpen,
            tiss.GlosaRatePercent,
            tiss.GuidesSentOver30Days,
            activeSus,
            aihReady,
            susExports,
            alerts,
            DateTime.UtcNow);
    }

    private static List<BillingAlertDto> BuildAlerts(
        int openAccounts,
        int draftGuides,
        int guidesOver30,
        decimal glosaOpen,
        decimal glosaRate,
        int activeSus,
        int aihReady)
    {
        var alerts = new List<BillingAlertDto>();

        if (glosaRate >= 5)
        {
            alerts.Add(new BillingAlertDto(
                "GLOSA-RATE",
                "critical",
                "Taxa de glosa elevada",
                $"Glosas em aberto: R$ {glosaOpen:N2} ({glosaRate}% do faturado TISS).",
                "/faturamento-tiss/glosas"));
        }

        if (guidesOver30 > 0)
        {
            alerts.Add(new BillingAlertDto(
                "TISS-AGING",
                "warning",
                "Guias TISS sem retorno",
                $"{guidesOver30} guia(s) enviada(s) há mais de 30 dias.",
                "/faturamento-tiss/lotes"));
        }

        if (draftGuides > 5)
        {
            alerts.Add(new BillingAlertDto(
                "TISS-DRAFT",
                "info",
                "Guias em rascunho",
                $"{draftGuides} guia(s) TISS aguardando fechamento.",
                "/faturamento-tiss"));
        }

        if (openAccounts > 10)
        {
            alerts.Add(new BillingAlertDto(
                "ACC-OPEN",
                "warning",
                "Contas hospitalares abertas",
                $"{openAccounts} conta(s) a receber pendentes de fechamento.",
                "/financeiro"));
        }

        if (activeSus > aihReady)
        {
            alerts.Add(new BillingAlertDto(
                "AIH-PENDING",
                "info",
                "Internações SUS sem AIH completa",
                $"{activeSus - aihReady} internação(ões) SUS sem CID/procedimento SIGTAP.",
                "/faturamento/sus/aih"));
        }

        return alerts;
    }
}
