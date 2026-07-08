using SistemaHospitalar.Application.DTOs.Hospitalization;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.MedicalRecords;

public record MedicalRecordEntryDto(
    Guid Id,
    MedicalRecordEntryType EntryType,
    string Content,
    string? Cid10Code,
    string? ProfessionalName,
    Guid? HospitalizationId,
    DateTime CreatedAt,
    bool IsSigned,
    DateTime? SignedAt,
    string? SignedByProfessionalName,
    string? SignatureHash,
    bool HasSignatureImage);

public record CreateMedicalRecordEntryRequest(
    MedicalRecordEntryType EntryType,
    string Content,
    string? Cid10Code,
    Guid? ProfessionalId,
    Guid? AppointmentId,
    Guid? HospitalizationId,
    string? ClientRequestId,
    string? SignatureImage,
    string? Password = null,
    ClinicalSignatureType SignatureType = ClinicalSignatureType.Simple);

public record SignMedicalRecordEntryRequest(
    Guid ProfessionalId,
    string SignatureImage,
    string Password,
    ClinicalSignatureType SignatureType = ClinicalSignatureType.Simple);

public record PendingSignatureEntryDto(
    Guid EntryId,
    Guid PatientId,
    string PatientName,
    string? RecordNumber,
    MedicalRecordEntryType EntryType,
    string ContentPreview,
    DateTime CreatedAt,
    string? ProfessionalName);

public record UpdateMedicalRecordEntryRequest(
    MedicalRecordEntryType EntryType,
    string Content,
    string? Cid10Code);

public record MedicalRecordSummaryDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string RecordNumber,
    IReadOnlyList<MedicalRecordEntryDto> Entries);

public record DigitalRecordSummaryDto(
    MedicalRecordSummaryDto Record,
    HospitalizationDto? ActiveHospitalization,
    IReadOnlyList<HospitalizationDto> HospitalizationHistory,
    IReadOnlyList<TissGuideDto> TissGuides);
