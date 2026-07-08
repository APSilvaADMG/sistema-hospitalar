using SistemaHospitalar.Application.DTOs.MedicalRecords;

namespace SistemaHospitalar.Application.Interfaces;

public interface IMedicalRecordService
{
    Task<MedicalRecordSummaryDto?> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<MedicalRecordEntryDto?> AddEntryAsync(
        Guid patientId,
        CreateMedicalRecordEntryRequest request,
        Guid? signingUserId = null,
        string? signingUserEmail = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
    Task<MedicalRecordEntryDto?> SignEntryAsync(
        Guid patientId,
        Guid entryId,
        SignMedicalRecordEntryRequest request,
        Guid signingUserId,
        string signingUserEmail,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
    Task<MedicalRecordEntryDto?> UpdateEntryAsync(Guid patientId, Guid entryId, UpdateMedicalRecordEntryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingSignatureEntryDto>> GetPendingSignaturesAsync(int limit, CancellationToken cancellationToken = default);
}
