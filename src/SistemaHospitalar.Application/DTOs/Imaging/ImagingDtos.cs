using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Imaging;

public record ImagingStudyDto(
    Guid Id, Guid PatientId, string PatientName, string RequestingProfessionalName,
    ImagingModality Modality, string StudyDescription, ImagingStudyStatus Status,
    DateTime ScheduledAt, DateTime? CompletedAt, string? ReportContent, DateTime? ReportedAt,
    string? AccessionNumber);

public record CreateImagingStudyRequest(
    Guid PatientId, Guid RequestingProfessionalId, ImagingModality Modality,
    string StudyDescription, DateTime ScheduledAt);

public record UpdateImagingStudyStatusRequest(ImagingStudyStatus Status);

public record RegisterImagingReportRequest(string ReportContent, Guid? ReportingProfessionalId);
