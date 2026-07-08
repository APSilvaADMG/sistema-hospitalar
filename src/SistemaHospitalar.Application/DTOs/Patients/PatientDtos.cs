using SistemaHospitalar.Application.DTOs.Hospitalization;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Patients;

public record PatientInsuranceDto(
    Guid Id,
    Guid HealthInsuranceId,
    string HealthInsuranceName,
    string CardNumber,
    string? PlanName,
    string? CardHolderName,
    string? ProductCode,
    string? CnsNumber,
    string? AccommodationType,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil,
    bool IsPrimary);

public record PatientInsuranceInput(
    Guid HealthInsuranceId,
    string CardNumber,
    string? PlanName,
    string? CardHolderName,
    string? ProductCode,
    string? CnsNumber,
    string? AccommodationType,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil,
    bool IsPrimary);

public record PatientDto(
    Guid Id,
    string FullName,
    string? SocialName,
    string Cpf,
    string? Cns,
    DateOnly BirthDate,
    Gender Gender,
    string? Email,
    string? Phone,
    string? MobilePhone,
    string? AddressCity,
    string? AddressState,
    string? PrimaryInsuranceName,
    bool HasPhoto,
    bool IsActive,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? MotherName,
    string? EmergencyContactRelationship,
    DateTime CreatedAt,
    bool UsesResponsibleCpf,
    LegalResponsibleDto? LegalResponsible,
    int OpenReceivableCount = 0,
    DateTime? LastAppointmentAt = null,
    DateTime? NextAppointmentAt = null);

public record PatientDetailDto(
    Guid Id,
    string FullName,
    string? SocialName,
    string Cpf,
    string? Cns,
    DateOnly BirthDate,
    Gender Gender,
    string? Email,
    string? Phone,
    string? MobilePhone,
    string? AddressStreet,
    string? AddressNumber,
    string? AddressComplement,
    string? AddressNeighborhood,
    string? AddressCity,
    string? AddressState,
    string? AddressZipCode,
    string? MotherName,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelationship,
    string? Notes,
    string? PhotoData,
    string? Rg,
    string? Nationality,
    string? BloodType,
    string? Occupation,
    string? MaritalStatus,
    string? BirthPlace,
    bool IsActive,
    DateTime CreatedAt,
    Guid? MedicalRecordId,
    string? MedicalRecordNumber,
    IReadOnlyList<PatientInsuranceDto> Insurances,
    bool UsesResponsibleCpf,
    LegalResponsibleDto? LegalResponsible);

public record PatientInitialAdmissionInput(
    Guid BedId,
    Guid ProfessionalId,
    string Reason,
    string? Diagnosis,
    string? Notes);

public record CreatePatientResult(
    PatientDetailDto Patient,
    HospitalizationDto? InitialHospitalization);

public record CpfAvailabilityResult(bool Available, string? Message);

public record LegalResponsibleInput(
    string Name,
    string Cpf,
    DateOnly BirthDate,
    LegalResponsibleRelationship Relationship,
    string Rg,
    LegalAuthorizationDocumentType? AuthorizationDocumentType,
    string? AuthorizationDocumentReference);

public record LegalResponsibleDto(
    string Name,
    DateOnly BirthDate,
    LegalResponsibleRelationship Relationship,
    string Rg,
    LegalAuthorizationDocumentType? AuthorizationDocumentType,
    string? AuthorizationDocumentReference);

public record CreatePatientRequest(
    string FullName,
    string? SocialName,
    string Cpf,
    DateOnly BirthDate,
    Gender Gender,
    string? Email,
    string? Phone,
    string? MobilePhone,
    string? AddressStreet,
    string? AddressNumber,
    string? AddressComplement,
    string? AddressNeighborhood,
    string? AddressCity,
    string? AddressState,
    string? AddressZipCode,
    string? MotherName,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelationship,
    string? Notes,
    string? PhotoData,
    string? Rg,
    string? Nationality,
    string? BloodType,
    string? Occupation,
    string? MaritalStatus,
    string? BirthPlace,
    IReadOnlyList<PatientInsuranceInput>? Insurances,
    bool UsesResponsibleCpf = false,
    LegalResponsibleInput? LegalResponsible = null,
    PatientInitialAdmissionInput? InitialAdmission = null);

public record UpdatePatientRequest(
    string FullName,
    string? SocialName,
    DateOnly BirthDate,
    Gender Gender,
    string? Email,
    string? Phone,
    string? MobilePhone,
    string? AddressStreet,
    string? AddressNumber,
    string? AddressComplement,
    string? AddressNeighborhood,
    string? AddressCity,
    string? AddressState,
    string? AddressZipCode,
    string? MotherName,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelationship,
    string? Notes,
    string? PhotoData,
    string? Rg,
    string? Nationality,
    string? BloodType,
    string? Occupation,
    string? MaritalStatus,
    string? BirthPlace,
    bool IsActive,
    string? InactivationReason = null,
    IReadOnlyList<PatientInsuranceInput>? Insurances = null);

public record PotentialDuplicatePatientDto(
    Guid Id,
    string FullName,
    DateOnly BirthDate,
    string? Cpf,
    string? MedicalRecordNumber,
    string MatchReason);

public record PatientDuplicateCheckRequest(
    string FullName,
    DateOnly BirthDate,
    string? Cpf = null,
    Guid? ExcludePatientId = null);
