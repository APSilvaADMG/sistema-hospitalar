using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Audit;
using SistemaHospitalar.Application.DTOs.MedicalRecords;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class MedicalRecordService(AppDbContext dbContext, IAuditService auditService) : IMedicalRecordService
{
    public async Task<MedicalRecordSummaryDto?> GetByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.MedicalRecords
            .AsNoTracking()
            .Where(m => m.PatientId == patientId)
            .Select(m => new MedicalRecordSummaryDto(
                m.Id,
                m.PatientId,
                m.Patient.FullName,
                m.RecordNumber,
                m.Entries
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e => new MedicalRecordEntryDto(
                        e.Id,
                        e.EntryType,
                        e.Content,
                        e.Cid10Code,
                        e.Professional != null ? e.Professional.FullName : null,
                        e.HospitalizationId,
                        e.CreatedAt,
                        e.IsSigned,
                        e.SignedAt,
                        e.SignedByProfessional != null ? e.SignedByProfessional.FullName : null,
                        e.SignatureHash,
                        e.SignatureImage != null && e.SignatureImage != ""))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendingSignatureEntryDto>> GetPendingSignaturesAsync(
        int limit, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var rows = await dbContext.MedicalRecordEntries
            .AsNoTracking()
            .Where(e => !e.IsSigned)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .Select(e => new
            {
                e.Id,
                PatientId = e.MedicalRecord.PatientId,
                PatientName = e.MedicalRecord.Patient.FullName,
                RecordNumber = e.MedicalRecord.RecordNumber,
                e.EntryType,
                e.Content,
                e.CreatedAt,
                ProfessionalName = e.Professional != null ? e.Professional.FullName : null,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(e => new PendingSignatureEntryDto(
                e.Id,
                e.PatientId,
                e.PatientName,
                e.RecordNumber,
                e.EntryType,
                e.Content.Length > 120 ? e.Content[..120] + "…" : e.Content,
                e.CreatedAt,
                e.ProfessionalName))
            .ToList();
    }

    public async Task<MedicalRecordEntryDto?> AddEntryAsync(
        Guid patientId,
        CreateMedicalRecordEntryRequest request,
        Guid? signingUserId = null,
        string? signingUserEmail = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.ClientRequestId))
        {
            var existing = await dbContext.MedicalRecordEntries
                .AsNoTracking()
                .Where(e => e.ClientRequestId == request.ClientRequestId)
                .Select(e => new MedicalRecordEntryDto(
                    e.Id,
                    e.EntryType,
                    e.Content,
                    e.Cid10Code,
                    e.Professional != null ? e.Professional.FullName : null,
                    e.HospitalizationId,
                    e.CreatedAt,
                    e.IsSigned,
                    e.SignedAt,
                    e.SignedByProfessional != null ? e.SignedByProfessional.FullName : null,
                    e.SignatureHash,
                    e.SignatureImage != null && e.SignatureImage != ""))
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                return existing;
            }
        }

        var record = await dbContext.MedicalRecords
            .FirstOrDefaultAsync(m => m.PatientId == patientId, cancellationToken);

        if (record is null)
        {
            return null;
        }

        if (request.EntryType == MedicalRecordEntryType.Prescription)
        {
            var allergies = await LoadAllergyEntriesAsync(record.Id, cancellationToken);
            PrescriptionRules.ValidateNoAllergyConflict(request.Content, allergies);
        }

        var entry = new MedicalRecordEntry
        {
            MedicalRecordId = record.Id,
            EntryType = request.EntryType,
            Content = request.Content.Trim(),
            Cid10Code = request.Cid10Code?.Trim(),
            ProfessionalId = request.ProfessionalId,
            AppointmentId = request.AppointmentId,
            HospitalizationId = request.HospitalizationId,
            ClientRequestId = request.ClientRequestId?.Trim()
        };

        var signOnCreate = !string.IsNullOrWhiteSpace(request.SignatureImage) && request.ProfessionalId.HasValue;
        if (signOnCreate)
        {
            ApplySignature(entry, request.ProfessionalId!.Value, request.SignatureImage!.Trim(), computeHash: false);
        }

        dbContext.MedicalRecordEntries.Add(entry);
        record.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (signOnCreate && entry.SignedAt.HasValue)
        {
            entry.SignatureHash = ComputeSignatureHash(
                entry.Id, entry.Content, entry.SignedByProfessionalId!.Value, entry.SignedAt.Value);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (signingUserId.HasValue && !string.IsNullOrWhiteSpace(signingUserEmail))
            {
                await LogSignatureAuditAsync(
                    signingUserId.Value,
                    signingUserEmail,
                    entry.Id,
                    record.PatientId,
                    request.SignatureType,
                    "create-and-sign",
                    ipAddress,
                    userAgent,
                    cancellationToken);
            }
        }

        return await GetEntryDtoAsync(entry.Id, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> LoadAllergyEntriesAsync(
        Guid medicalRecordId,
        CancellationToken cancellationToken)
    {
        return await dbContext.MedicalRecordEntries
            .AsNoTracking()
            .Where(e => e.MedicalRecordId == medicalRecordId
                && (EF.Functions.ILike(e.Content, "%alerg%")
                    || EF.Functions.ILike(e.Content, "%Alergia%")))
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.Content)
            .Take(10)
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicalRecordEntryDto?> SignEntryAsync(
        Guid patientId,
        Guid entryId,
        SignMedicalRecordEntryRequest request,
        Guid signingUserId,
        string signingUserEmail,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.MedicalRecordEntries
            .Include(e => e.MedicalRecord)
            .FirstOrDefaultAsync(e => e.Id == entryId && e.MedicalRecord.PatientId == patientId, cancellationToken);

        if (entry is null)
        {
            return null;
        }

        if (entry.IsSigned)
        {
            throw new InvalidOperationException("Este registro já foi assinado digitalmente.");
        }

        if (string.IsNullOrWhiteSpace(request.SignatureImage))
        {
            throw new InvalidOperationException("A imagem da assinatura é obrigatória.");
        }

        ApplySignature(entry, request.ProfessionalId, request.SignatureImage.Trim());
        entry.MedicalRecord.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await LogSignatureAuditAsync(
            signingUserId,
            signingUserEmail,
            entry.Id,
            patientId,
            request.SignatureType,
            "sign",
            ipAddress,
            userAgent,
            cancellationToken);

        return await GetEntryDtoAsync(entry.Id, cancellationToken);
    }

    public async Task<MedicalRecordEntryDto?> UpdateEntryAsync(
        Guid patientId,
        Guid entryId,
        UpdateMedicalRecordEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.MedicalRecordEntries
            .Include(e => e.MedicalRecord)
            .FirstOrDefaultAsync(e => e.Id == entryId && e.MedicalRecord.PatientId == patientId, cancellationToken);

        if (entry is null)
        {
            return null;
        }

        if (entry.IsSigned)
        {
            throw new InvalidOperationException("Registro assinado não pode ser alterado.");
        }

        entry.EntryType = request.EntryType;
        entry.Content = request.Content.Trim();
        entry.Cid10Code = string.IsNullOrWhiteSpace(request.Cid10Code) ? null : request.Cid10Code.Trim();
        entry.UpdatedAt = DateTime.UtcNow;
        entry.MedicalRecord.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetEntryDtoAsync(entry.Id, cancellationToken);
    }

    private async Task LogSignatureAuditAsync(
        Guid userId,
        string userEmail,
        Guid entryId,
        Guid patientId,
        Domain.Enums.ClinicalSignatureType signatureType,
        string action,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        await auditService.LogAsync(new CreateAuditLogRequest(
            UserId: userId,
            UserEmail: userEmail,
            Action: action == "sign" ? "AssinarRegistroClinico" : "AssinarRegistroClinicoCriacao",
            EntityType: "MedicalRecordEntry",
            EntityId: entryId,
            Details: $"Assinatura {signatureType} — paciente {patientId}",
            IpAddress: ipAddress,
            UserAgent: userAgent,
            ActionCategory: "ClinicalSignature",
            IsSensitive: true),
            cancellationToken);
    }

    private static void ApplySignature(
        MedicalRecordEntry entry,
        Guid professionalId,
        string signatureImage,
        bool computeHash = true)
    {
        var signedAt = DateTime.UtcNow;
        entry.IsSigned = true;
        entry.SignedAt = signedAt;
        entry.SignedByProfessionalId = professionalId;
        entry.SignatureImage = signatureImage;
        if (computeHash && entry.Id != Guid.Empty)
        {
            entry.SignatureHash = ComputeSignatureHash(entry.Id, entry.Content, professionalId, signedAt);
        }
    }

    internal static string ComputeSignatureHash(Guid entryId, string content, Guid professionalId, DateTime signedAt)
    {
        var payload = $"{entryId}|{content.Trim()}|{professionalId}|{signedAt:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task<IReadOnlyList<string>> GetAllergyEntriesAsync(
        Guid medicalRecordId,
        CancellationToken cancellationToken)
    {
        return await dbContext.MedicalRecordEntries
            .AsNoTracking()
            .Where(e => e.MedicalRecordId == medicalRecordId
                && (EF.Functions.ILike(e.Content, "%alerg%")
                    || EF.Functions.ILike(e.Content, "%Alergia%")))
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.Content)
            .ToListAsync(cancellationToken);
    }

    private async Task<MedicalRecordEntryDto?> GetEntryDtoAsync(Guid entryId, CancellationToken cancellationToken) =>
        await dbContext.MedicalRecordEntries
            .AsNoTracking()
            .Where(e => e.Id == entryId)
            .Select(e => new MedicalRecordEntryDto(
                e.Id,
                e.EntryType,
                e.Content,
                e.Cid10Code,
                e.Professional != null ? e.Professional.FullName : null,
                e.HospitalizationId,
                e.CreatedAt,
                e.IsSigned,
                e.SignedAt,
                e.SignedByProfessional != null ? e.SignedByProfessional.FullName : null,
                e.SignatureHash,
                e.SignatureImage != null && e.SignatureImage != ""))
            .FirstOrDefaultAsync(cancellationToken);
}
