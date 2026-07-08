using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.PatientIdentity;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class PatientIdentityService(AppDbContext dbContext) : IPatientIdentityService
{
    public async Task<PatientIdentityDto> GenerateBraceletAsync(
        Guid patientId,
        GenerateBraceletRequest request,
        Guid? issuedByUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePatientExistsAsync(patientId, cancellationToken);

        if (request.HospitalizationId.HasValue)
        {
            await EnsureHospitalizationBelongsToPatientAsync(
                patientId, request.HospitalizationId.Value, cancellationToken);
        }

        await RevokeActiveIdentitiesAsync(
            patientId,
            PatientIdentityType.Bracelet,
            request.HospitalizationId,
            cancellationToken);

        var identity = new PatientIdentity
        {
            PatientId = patientId,
            HospitalizationId = request.HospitalizationId,
            IdentityType = PatientIdentityType.Bracelet,
            Code = GenerateCode(),
            IssuedByUserId = issuedByUserId,
        };

        dbContext.PatientIdentities.Add(identity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapDto(identity);
    }

    public async Task<PatientIdentityDto> GenerateLabelAsync(
        Guid patientId,
        GenerateLabelRequest request,
        Guid? issuedByUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePatientExistsAsync(patientId, cancellationToken);

        if (request.HospitalizationId.HasValue)
        {
            await EnsureHospitalizationBelongsToPatientAsync(
                patientId, request.HospitalizationId.Value, cancellationToken);
        }

        var labelType = request.LabelType switch
        {
            PatientIdentityType.ExamLabel => PatientIdentityType.ExamLabel,
            PatientIdentityType.MedicationLabel => PatientIdentityType.MedicationLabel,
            PatientIdentityType.SampleLabel => PatientIdentityType.SampleLabel,
            _ => throw new InvalidOperationException("Tipo de etiqueta inválido."),
        };

        var identity = new PatientIdentity
        {
            PatientId = patientId,
            HospitalizationId = request.HospitalizationId,
            IdentityType = labelType,
            Code = GenerateCode(),
            LabelContext = request.LabelContext?.Trim(),
            IssuedByUserId = issuedByUserId,
        };

        dbContext.PatientIdentities.Add(identity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapDto(identity);
    }

    public async Task<PatientIdentityResolveDto?> ResolveAsync(
        string code, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCode(code);
        if (normalized is null)
        {
            return null;
        }

        var identity = await dbContext.PatientIdentities
            .AsNoTracking()
            .Include(i => i.Patient).ThenInclude(p => p.MedicalRecord)
            .Include(i => i.Hospitalization).ThenInclude(h => h!.Bed).ThenInclude(b => b.Ward)
            .FirstOrDefaultAsync(
                i => i.Code == normalized && i.IsActive && i.RevokedAt == null,
                cancellationToken);

        if (identity is null)
        {
            return null;
        }

        var allergies = await ExtractAllergyWarningsAsync(identity.PatientId, cancellationToken);

        return new PatientIdentityResolveDto(
            identity.PatientId,
            identity.Patient.FullName,
            identity.Patient.MedicalRecord?.RecordNumber,
            identity.Patient.SocialName,
            identity.Patient.BirthDate,
            identity.Patient.BloodType,
            identity.Code,
            identity.IdentityType,
            identity.LabelContext,
            identity.HospitalizationId,
            identity.Hospitalization?.Bed?.BedNumber,
            identity.Hospitalization?.Bed?.Ward?.Name,
            allergies);
    }

    public async Task<IReadOnlyList<PatientIdentityDto>> ListActiveAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PatientIdentities
            .AsNoTracking()
            .Where(i => i.PatientId == patientId && i.IsActive && i.RevokedAt == null)
            .OrderByDescending(i => i.IssuedAt)
            .Select(i => new PatientIdentityDto(
                i.Id,
                i.PatientId,
                i.HospitalizationId,
                i.IdentityType,
                i.Code,
                i.LabelContext,
                i.IssuedAt,
                i.IsActive))
            .ToListAsync(cancellationToken);
    }

    private async Task EnsurePatientExistsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Patients
            .AnyAsync(p => p.Id == patientId && p.IsActive, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Paciente não encontrado.");
        }
    }

    private async Task EnsureHospitalizationBelongsToPatientAsync(
        Guid patientId, Guid hospitalizationId, CancellationToken cancellationToken)
    {
        var valid = await dbContext.Hospitalizations
            .AnyAsync(h => h.Id == hospitalizationId && h.PatientId == patientId, cancellationToken);

        if (!valid)
        {
            throw new InvalidOperationException("Internação não pertence ao paciente informado.");
        }
    }

    private async Task RevokeActiveIdentitiesAsync(
        Guid patientId,
        PatientIdentityType type,
        Guid? hospitalizationId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PatientIdentities
            .Where(i => i.PatientId == patientId
                && i.IdentityType == type
                && i.IsActive
                && i.RevokedAt == null);

        if (hospitalizationId.HasValue)
        {
            query = query.Where(i => i.HospitalizationId == hospitalizationId);
        }

        var active = await query.ToListAsync(cancellationToken);
        foreach (var item in active)
        {
            item.IsActive = false;
            item.RevokedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<string[]> ExtractAllergyWarningsAsync(
        Guid patientId, CancellationToken cancellationToken)
    {
        var record = await dbContext.MedicalRecords
            .AsNoTracking()
            .Where(m => m.PatientId == patientId)
            .Select(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (record == Guid.Empty)
        {
            return [];
        }

        var entries = await dbContext.MedicalRecordEntries
            .AsNoTracking()
            .Where(e => e.MedicalRecordId == record
                && (EF.Functions.ILike(e.Content, "%alerg%")
                    || EF.Functions.ILike(e.Content, "%Alergia%")))
            .OrderByDescending(e => e.CreatedAt)
            .Take(3)
            .Select(e => e.Content)
            .ToListAsync(cancellationToken);

        return entries
            .Select(c => c.Length > 120 ? c.Substring(0, 120) + "…" : c)
            .ToArray();
    }

    private static string GenerateCode()
    {
        var raw = $"GTH-{Guid.NewGuid():N}";
        return raw.Substring(0, Math.Min(16, raw.Length)).ToUpperInvariant();
    }

    private static string? NormalizeCode(string code)
    {
        var trimmed = code.Trim().ToUpperInvariant();
        if (trimmed.StartsWith("GTH:", StringComparison.Ordinal))
        {
            trimmed = trimmed[4..];
        }

        if (trimmed.StartsWith("GTH-", StringComparison.Ordinal))
        {
            return trimmed;
        }

        return trimmed.Length >= 8 ? $"GTH-{trimmed}" : null;
    }

    private static PatientIdentityDto MapDto(PatientIdentity identity) =>
        new(
            identity.Id,
            identity.PatientId,
            identity.HospitalizationId,
            identity.IdentityType,
            identity.Code,
            identity.LabelContext,
            identity.IssuedAt,
            identity.IsActive);
}
