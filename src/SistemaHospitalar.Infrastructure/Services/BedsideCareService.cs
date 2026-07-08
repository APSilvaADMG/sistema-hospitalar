using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Audit;
using SistemaHospitalar.Application.DTOs.Bedside;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class BedsideCareService(
    AppDbContext dbContext,
    IPatientIdentityService patientIdentityService,
    IAuditService auditService) : IBedsideCareService
{
    public async Task<BedsideCareResultDto?> RegisterVitalsAsync(
        Guid patientId,
        BedsideVitalsRequest request,
        Guid userId,
        string userEmail,
        Guid? professionalId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        await EnsureIdentityMatchesPatientAsync(patientId, request.IdentityCode, cancellationToken);

        var record = await dbContext.MedicalRecords
            .FirstOrDefaultAsync(m => m.PatientId == patientId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var content = $"""
            [Leito — Sinais Vitais]
            PA: {request.BloodPressure ?? "—"} | FC: {request.HeartRate ?? "—"} | FR: {request.RespiratoryRate ?? "—"} | Temp: {request.Temperature ?? "—"}°C | SpO2: {request.SpO2 ?? "—"}%
            Paciente validado: {NormalizeCode(request.IdentityCode)}
            """;

        var entry = new MedicalRecordEntry
        {
            MedicalRecordId = record.Id,
            EntryType = MedicalRecordEntryType.Evolution,
            Content = content.Trim(),
            ProfessionalId = professionalId,
        };

        dbContext.MedicalRecordEntries.Add(entry);
        record.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await LogBedsideAsync(userId, userEmail, entry.Id, patientId, "RegistrarSinaisVitaisLeito", ipAddress, userAgent, cancellationToken);

        return new BedsideCareResultDto(entry.Id, false, entry.CreatedAt, "Sinais vitais registrados no PEP.");
    }

    public async Task<BedsideCareResultDto?> AdministerMedicationAsync(
        Guid patientId,
        BedsideMedicationRequest request,
        Guid userId,
        string userEmail,
        Guid professionalId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        await EnsureIdentityMatchesPatientAsync(patientId, request.IdentityCode, cancellationToken);
        NursingRules.ValidateFiveRights(
            request.MedicationName,
            request.Dose,
            request.Route);

        if (request.PrescriptionEntryId.HasValue)
        {
            var prescription = await dbContext.MedicalRecordEntries
                .AsNoTracking()
                .Include(e => e.MedicalRecord)
                .FirstOrDefaultAsync(
                    e => e.Id == request.PrescriptionEntryId.Value
                        && e.MedicalRecord.PatientId == patientId
                        && e.EntryType == MedicalRecordEntryType.Prescription,
                    cancellationToken);

            if (prescription is null)
            {
                throw new InvalidOperationException("Prescrição não encontrada para este paciente.");
            }

            if (!prescription.IsSigned)
            {
                throw new InvalidOperationException("Prescrição ainda não assinada — administração bloqueada.");
            }
        }

        var record = await dbContext.MedicalRecords
            .FirstOrDefaultAsync(m => m.PatientId == patientId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var prescriptionRef = request.PrescriptionEntryId.HasValue
            ? $"\nPrescrição vinculada: {request.PrescriptionEntryId}"
            : string.Empty;

        var content = $"""
            [Leito — Medicação Administrada]
            Medicamento: {request.MedicationName.Trim()}
            Dose: {request.Dose.Trim()}
            Via: {request.Route.Trim()}
            Paciente validado: {NormalizeCode(request.IdentityCode)}{prescriptionRef}
            """;

        var entry = new MedicalRecordEntry
        {
            MedicalRecordId = record.Id,
            EntryType = MedicalRecordEntryType.Evolution,
            Content = content.Trim(),
            ProfessionalId = professionalId,
        };

        var signedAt = DateTime.UtcNow;
        entry.IsSigned = true;
        entry.SignedAt = signedAt;
        entry.SignedByProfessionalId = professionalId;
        entry.SignatureImage = BuildBedsideSignatureToken(userId, request.Password, content);
        dbContext.MedicalRecordEntries.Add(entry);
        record.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        entry.SignatureHash = MedicalRecordService.ComputeSignatureHash(
            entry.Id, entry.Content, professionalId, signedAt);
        await dbContext.SaveChangesAsync(cancellationToken);

        await LogBedsideAsync(userId, userEmail, entry.Id, patientId, "AdministrarMedicacaoLeito", ipAddress, userAgent, cancellationToken);

        return new BedsideCareResultDto(entry.Id, true, entry.CreatedAt, "Administração registrada e assinada.");
    }

    private async Task EnsureIdentityMatchesPatientAsync(
        Guid patientId, string identityCode, CancellationToken cancellationToken)
    {
        var resolved = await patientIdentityService.ResolveAsync(identityCode, cancellationToken);
        if (resolved is null || resolved.PatientId != patientId)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.MedicationPatientId}] Código de identificação não confere com o paciente.");
        }
    }

    private static string NormalizeCode(string code)
    {
        var trimmed = code.Trim().ToUpperInvariant();
        if (trimmed.StartsWith("GTH:", StringComparison.Ordinal))
        {
            trimmed = trimmed[4..];
        }

        return trimmed;
    }

    private static string BuildBedsideSignatureToken(Guid userId, string password, string content)
    {
        var payload = $"{userId}|{password}|{content.Trim()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return $"BEDSIDE:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private Task LogBedsideAsync(
        Guid userId,
        string userEmail,
        Guid entryId,
        Guid patientId,
        string action,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken) =>
        auditService.LogAsync(new CreateAuditLogRequest(
            UserId: userId,
            UserEmail: userEmail,
            Action: action,
            EntityType: "MedicalRecordEntry",
            EntityId: entryId,
            Details: $"Point of Care — paciente {patientId}",
            IpAddress: ipAddress,
            UserAgent: userAgent,
            ActionCategory: "ClinicalSignature",
            IsSensitive: true),
            cancellationToken);
}
