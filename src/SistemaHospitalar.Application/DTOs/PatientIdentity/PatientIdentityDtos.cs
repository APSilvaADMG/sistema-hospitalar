using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.PatientIdentity;

public record PatientIdentityDto(
    Guid Id,
    Guid PatientId,
    Guid? HospitalizationId,
    PatientIdentityType IdentityType,
    string Code,
    string? LabelContext,
    DateTime IssuedAt,
    bool IsActive);

public record GenerateBraceletRequest(
    Guid? HospitalizationId);

public record GenerateLabelRequest(
    PatientIdentityType LabelType,
    string? LabelContext,
    Guid? HospitalizationId);

public record PatientIdentityResolveDto(
    Guid PatientId,
    string PatientName,
    string? MedicalRecordNumber,
    string? SocialName,
    DateOnly BirthDate,
    string? BloodType,
    string Code,
    PatientIdentityType IdentityType,
    string? LabelContext,
    Guid? HospitalizationId,
    string? BedNumber,
    string? WardName,
    string[]? AllergyWarnings);
