using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Icu;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class IcuService(AppDbContext dbContext) : IIcuService
{
    public async Task<IcuDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var icuHospitalizations = await dbContext.Hospitalizations
            .AsNoTracking()
            .Where(h => h.IsActive && h.Status == HospitalizationStatus.Active
                && h.Bed.Ward.Category == WardCategory.Uti)
            .Select(h => new
            {
                h.Id,
                h.PatientId,
                PatientName = h.Patient.FullName,
                h.Bed.BedNumber,
                WardName = h.Bed.Ward.Name
            })
            .ToListAsync(cancellationToken);

        var totalBeds = await dbContext.Beds
            .CountAsync(b => b.IsActive && b.Ward.Category == WardCategory.Uti, cancellationToken);

        var patients = new List<IcuPatientDto>();
        var criticalCount = 0;

        foreach (var h in icuHospitalizations)
        {
            var latest = await GetLatestVitalAsync(h.Id, cancellationToken);
            var alert = ComputeAlertLevel(latest);
            if (alert == "Critical") criticalCount++;

            patients.Add(new IcuPatientDto(
                h.Id, h.PatientId, h.PatientName, h.BedNumber, h.WardName, latest, alert));
        }

        patients = patients
            .OrderByDescending(p => p.AlertLevel == "Critical")
            .ThenByDescending(p => p.AlertLevel == "Warning")
            .ToList();

        return new IcuDashboardDto(
            totalBeds,
            icuHospitalizations.Count,
            criticalCount,
            patients);
    }

    public Task<VitalSignDto> UpdateVitalSignsAsync(
        Guid vitalSignId,
        RecordVitalSignsRequest request,
        CancellationToken cancellationToken = default)
    {
        HospitalBusinessRules.ValidateVitalSignCorrectionAsNewEntry(isUpdateAttempt: true);
        throw new NotSupportedException("Use RecordVitalSignsAsync para registrar uma nova aferição.");
    }

    public async Task<VitalSignDto> RecordVitalSignsAsync(
        RecordVitalSignsRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Hospitalizations
            .AnyAsync(h => h.Id == request.HospitalizationId && h.IsActive, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Internação não encontrada.");
        }

        var record = new VitalSignRecord
        {
            HospitalizationId = request.HospitalizationId,
            HeartRate = request.HeartRate,
            SystolicBp = request.SystolicBp,
            DiastolicBp = request.DiastolicBp,
            SpO2 = request.SpO2,
            Temperature = request.Temperature,
            RespiratoryRate = request.RespiratoryRate,
            RecordedByProfessionalId = request.RecordedByProfessionalId,
            Notes = request.Notes?.Trim()
        };

        dbContext.VitalSignRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await MapVitalAsync(record.Id, cancellationToken))!;
    }

    public async Task<VitalSignDto> RecordVitalSignCorrectionAsync(
        RecordVitalSignCorrectionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CorrectionReason))
        {
            throw new InvalidOperationException("Informe o motivo da retificação dos sinais vitais.");
        }

        var original = await dbContext.VitalSignRecords
            .FirstOrDefaultAsync(v => v.Id == request.OriginalRecordId && v.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Registro original de sinais vitais não encontrado.");

        var record = new VitalSignRecord
        {
            HospitalizationId = original.HospitalizationId,
            HeartRate = request.HeartRate,
            SystolicBp = request.SystolicBp,
            DiastolicBp = request.DiastolicBp,
            SpO2 = request.SpO2,
            Temperature = request.Temperature,
            RespiratoryRate = request.RespiratoryRate,
            RecordedByProfessionalId = request.RecordedByProfessionalId,
            Notes = $"[{BusinessRuleCodes.VitalSignImmutable}] Retificação do registro {original.Id} " +
                    $"(aferido em {original.RecordedAt:O}). Motivo: {request.CorrectionReason.Trim()}"
        };

        dbContext.VitalSignRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await MapVitalAsync(record.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<VitalSignDto>> GetVitalHistoryAsync(
        Guid hospitalizationId, int limit, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        return await dbContext.VitalSignRecords
            .AsNoTracking()
            .Where(v => v.HospitalizationId == hospitalizationId)
            .OrderByDescending(v => v.RecordedAt)
            .Take(limit)
            .Select(v => new VitalSignDto(
                v.Id, v.HeartRate, v.SystolicBp, v.DiastolicBp, v.SpO2,
                v.Temperature, v.RespiratoryRate, v.RecordedAt,
                v.RecordedByProfessional != null ? v.RecordedByProfessional.FullName : null))
            .ToListAsync(cancellationToken);
    }

    private async Task<VitalSignDto?> GetLatestVitalAsync(Guid hospitalizationId, CancellationToken cancellationToken)
    {
        return await dbContext.VitalSignRecords
            .AsNoTracking()
            .Where(v => v.HospitalizationId == hospitalizationId)
            .OrderByDescending(v => v.RecordedAt)
            .Select(v => new VitalSignDto(
                v.Id, v.HeartRate, v.SystolicBp, v.DiastolicBp, v.SpO2,
                v.Temperature, v.RespiratoryRate, v.RecordedAt,
                v.RecordedByProfessional != null ? v.RecordedByProfessional.FullName : null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<VitalSignDto?> MapVitalAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.VitalSignRecords
            .AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new VitalSignDto(
                v.Id, v.HeartRate, v.SystolicBp, v.DiastolicBp, v.SpO2,
                v.Temperature, v.RespiratoryRate, v.RecordedAt,
                v.RecordedByProfessional != null ? v.RecordedByProfessional.FullName : null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    internal static string ComputeAlertLevel(VitalSignDto? v)
    {
        if (v is null) return "Unknown";
        if (v.SpO2 < 90 || v.HeartRate is < 40 or > 130 || v.SystolicBp is < 90 or > 180)
            return "Critical";
        if (v.SpO2 < 94 || v.HeartRate > 100 || v.Temperature > 38.5m)
            return "Warning";
        return "Normal";
    }
}
