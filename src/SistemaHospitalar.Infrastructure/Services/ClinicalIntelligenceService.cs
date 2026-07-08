using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.ClinicalIntelligence;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class ClinicalIntelligenceService(AppDbContext dbContext) : IClinicalIntelligenceService
{
    public async Task<PatientClinicalAlertsDto?> GetPatientClinicalAlertsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive, cancellationToken);

        if (patient is null)
        {
            return null;
        }

        var alerts = new List<ClinicalAlertDto>();

        var allergies = await LoadAllergyEntriesAsync(patientId, cancellationToken);
        foreach (var allergy in allergies.Take(5))
        {
            var snippet = allergy.Length > 80 ? allergy[..80] + "…" : allergy;
            alerts.Add(new ClinicalAlertDto(
                "ALLERGY",
                "critical",
                "Alergia registrada",
                snippet,
                BusinessRuleCodes.PrescriptionAllergy));
        }

        var activeProblems = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.MedicalRecord.PatientId == patientId
                && e.IsActive
                && (EF.Functions.ILike(e.Content, "%problema ativo%")
                    || EF.Functions.ILike(e.Content, "%diagnóstico principal%")
                    || EF.Functions.ILike(e.Content, "%diagnostico principal%")))
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.Content)
            .Take(3)
            .ToListAsync(cancellationToken);

        foreach (var problem in activeProblems)
        {
            var snippet = problem.Length > 100 ? problem[..100] + "…" : problem;
            alerts.Add(new ClinicalAlertDto("PROBLEM", "warning", "Problema clínico ativo", snippet));
        }

        var latestVitals = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.MedicalRecord.PatientId == patientId
                && e.IsActive
                && EF.Functions.ILike(e.Content, "%Sinais Vitais%"))
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.Content)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestVitals is not null)
        {
            if (ContainsCriticalVital(latestVitals))
            {
                alerts.Add(new ClinicalAlertDto(
                    "VITALS",
                    "critical",
                    "Sinais vitais críticos",
                    "Último registro indica parâmetro fora da faixa segura.",
                    "RN-TRI-004"));
            }
        }

        var unsignedPrescriptions = await dbContext.MedicalRecordEntries.CountAsync(
            e => e.MedicalRecord.PatientId == patientId
                && e.IsActive
                && e.EntryType == MedicalRecordEntryType.Prescription
                && !e.IsSigned,
            cancellationToken);

        if (unsignedPrescriptions > 0)
        {
            alerts.Add(new ClinicalAlertDto(
                "RX-UNSIGNED",
                "warning",
                "Prescrições não assinadas",
                $"{unsignedPrescriptions} prescrição(ões) aguardando assinatura.",
                "RN-011"));
        }

        var overdueMeds = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.MedicalRecord.PatientId == patientId
                && e.IsActive
                && e.EntryType == MedicalRecordEntryType.Prescription
                && e.IsSigned
                && e.CreatedAt < DateTime.UtcNow.AddHours(-24))
            .CountAsync(cancellationToken);

        var hasActiveHospitalization = await dbContext.Hospitalizations.AnyAsync(
            h => h.PatientId == patientId && h.Status == HospitalizationStatus.Active,
            cancellationToken);

        if (overdueMeds > 0 && hasActiveHospitalization)
        {
            alerts.Add(new ClinicalAlertDto(
                "MED-OVERDUE",
                "info",
                "Prescrições antigas",
                $"{overdueMeds} prescrição(ões) com mais de 24h — revisar posologia.",
                "RN-ADM-002"));
        }

        return new PatientClinicalAlertsDto(patientId, patient.FullName, alerts);
    }

    public async Task<IReadOnlyList<StockReplenishmentSuggestionDto>> GetStockReplenishmentSuggestionsAsync(
        CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var outboundByProduct = await dbContext.StockMovements.AsNoTracking()
            .Where(m => m.Type == StockMovementType.Outbound && m.CreatedAt >= since)
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total, cancellationToken);

        var lowProducts = await dbContext.Products.AsNoTracking()
            .Where(p => p.IsActive && p.QuantityOnHand <= p.MinimumStock * 1.25m)
            .OrderBy(p => p.QuantityOnHand - p.MinimumStock)
            .Take(25)
            .ToListAsync(cancellationToken);

        return lowProducts.Select(p =>
        {
            var avgDaily = outboundByProduct.TryGetValue(p.Id, out var total)
                ? Math.Round(total / 30m, 2)
                : 0m;
            int? daysUntil = avgDaily > 0
                ? (int)Math.Floor(p.QuantityOnHand / avgDaily)
                : null;

            var recommendation = p.QuantityOnHand <= 0
                ? "Reposição urgente — estoque zerado."
                : daysUntil is <= 7
                    ? $"Solicitar compra — ruptura estimada em {daysUntil} dia(s)."
                    : $"Monitorar — consumo médio {avgDaily}/dia.";

            return new StockReplenishmentSuggestionDto(
                p.Id,
                p.Name,
                p.Sku,
                p.QuantityOnHand,
                p.MinimumStock,
                avgDaily > 0 ? avgDaily : null,
                daysUntil,
                recommendation);
        }).ToList();
    }

    public async Task<OperationalInsightsDto> GetOperationalInsightsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var waiting = await dbContext.EmergencyVisits.AsNoTracking()
            .Where(v => v.IsActive && v.Status == EmergencyVisitStatus.Waiting)
            .Select(v => new { v.ArrivedAt, v.Urgency })
            .ToListAsync(cancellationToken);

        var avgWait = waiting.Count == 0
            ? 0
            : Math.Round(waiting.Average(v => (now - v.ArrivedAt).TotalMinutes), 1);
        var slaViolations = waiting.Count(v =>
            HospitalBusinessRules.IsEmergencyWaitExceeded(v.ArrivedAt, v.Urgency, now));

        var totalBeds = await dbContext.Beds.CountAsync(b => b.IsActive, cancellationToken);
        var occupied = await dbContext.Beds.CountAsync(
            b => b.IsActive && b.Status == BedStatus.Occupied, cancellationToken);
        var occupancy = totalBeds == 0 ? 0 : Math.Round((decimal)occupied / totalBeds * 100, 1);

        var lowStock = await dbContext.Products.CountAsync(
            p => p.IsActive && p.QuantityOnHand <= p.MinimumStock, cancellationToken);

        var expiringLots = await dbContext.ProductLots.CountAsync(
            l => l.IsActive
                && l.QuantityOnHand > 0
                && l.ExpiryDate != null
                && l.ExpiryDate <= DateOnly.FromDateTime(now).AddDays(30),
            cancellationToken);

        var insights = new List<OperationalInsightDto>
        {
            new("PS-WAIT", "Tempo médio de espera PS (min)", avgWait.ToString(CultureInfo.InvariantCulture),
                slaViolations > 0 ? "critical" : avgWait > 60 ? "warning" : null),
            new("PS-SLA", "Violações de SLA no PS", slaViolations.ToString(),
                slaViolations > 0 ? "critical" : null),
            new("BED-OCC", "Ocupação de leitos (%)", occupancy.ToString(CultureInfo.InvariantCulture),
                occupancy >= HospitalBusinessRules.CriticalBedOccupancyPercent ? "critical" : null),
            new("STOCK-LOW", "Produtos abaixo do mínimo", lowStock.ToString(),
                lowStock > 0 ? "warning" : null),
            new("LOT-EXP", "Lotes a vencer (30d)", expiringLots.ToString(),
                expiringLots > 0 ? "warning" : null),
        };

        return new OperationalInsightsDto(insights, now);
    }

    private static bool ContainsCriticalVital(string content)
    {
        var lower = content.ToLowerInvariant();
        return lower.Contains("spo2") && (lower.Contains("spo2: 8") || lower.Contains("spo2: 9"))
            || lower.Contains("pa:") && lower.Contains("/")
            || lower.Contains("fc:") && (lower.Contains("fc: 1") || lower.Contains("fc: 2"));
    }

    private async Task<IReadOnlyList<string>> LoadAllergyEntriesAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        return await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.MedicalRecord.PatientId == patientId
                && e.IsActive
                && (EF.Functions.ILike(e.Content, "%alerg%")
                    || EF.Functions.ILike(e.Content, "%Alergia%")))
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.Content)
            .Take(10)
            .ToListAsync(cancellationToken);
    }
}
