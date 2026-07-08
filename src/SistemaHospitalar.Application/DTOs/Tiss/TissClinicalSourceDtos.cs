using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Tiss;

public record TissClinicalSourceDto(
    Guid Id,
    ClinicalDocumentKind DocumentKind,
    Guid PatientId,
    string PatientName,
    Guid? HealthInsuranceId,
    string? HealthInsuranceName,
    TissGuideType GuideType,
    string? ReportCode,
    Guid? AppointmentId,
    Guid? HospitalizationId,
    Guid? ChemotherapySessionId,
    Guid? SurgeryId,
    Guid? LabOrderId,
    Guid? ImagingStudyId,
    string Label,
    string FormDataJson,
    Guid? GeneratedTissGuideId,
    string? GeneratedGuideNumber,
    string? GeneratedArtifactJson,
    DateTime? GeneratedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record UpsertTissClinicalSourceRequest(
    ClinicalDocumentKind DocumentKind,
    Guid PatientId,
    TissGuideType GuideType,
    string? ReportCode,
    Guid? HealthInsuranceId,
    Guid? AppointmentId,
    Guid? HospitalizationId,
    Guid? ChemotherapySessionId,
    Guid? SurgeryId,
    Guid? LabOrderId,
    Guid? ImagingStudyId,
    string Label,
    string FormDataJson);

public record ClinicalSourceLookupRequest(
    ClinicalDocumentKind DocumentKind,
    Guid PatientId,
    TissGuideType GuideType,
    string? ReportCode = null,
    Guid? AppointmentId = null,
    Guid? HospitalizationId = null,
    Guid? ChemotherapySessionId = null,
    Guid? SurgeryId = null,
    Guid? LabOrderId = null,
    Guid? ImagingStudyId = null);

public record LinkClinicalSourceGuideRequest(Guid TissGuideId);

public record LinkClinicalSourceArtifactRequest(string ArtifactJson);
