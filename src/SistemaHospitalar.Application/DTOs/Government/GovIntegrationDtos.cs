using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Government;

public record GovIntegrationProfileDto(
    GovIntegrationSystem System,
    string Name,
    string Description,
    GovIntegrationPriority Priority,
    GovIntegrationCredentialStatus CredentialStatus,
    bool MockEnabled,
    string? OfficialEndpoint,
    string? CredentialNote);

public record CnsLookupResultDto(
    bool Found,
    string? Cns,
    string? FullName,
    DateOnly? BirthDate,
    string? MotherName,
    string? Gender,
    string? AddressCity,
    string? AddressState,
    string? Message);

public record CnesEstablishmentDto(
    string CnesCode,
    string Name,
    string? FantasyName,
    string? Address,
    string? City,
    string? State,
    string? ManagementType,
    IReadOnlyList<CnesProfessionalDto> Professionals);

public record CnesProfessionalDto(
    string Name,
    string? Cns,
    string? CboCode,
    string? Specialty,
    string? Occupation);

public record ApplyCnsToPatientRequest(string Cns);

public record SihAihPreviewDto(
    Guid HospitalizationId,
    string PatientName,
    string? PatientCns,
    string WardName,
    string BedNumber,
    DateTime AdmittedAt,
    int LengthOfStayDays,
    string? PrimaryDiagnosis,
    string? PrimaryCid10Code,
    string? SecondaryCid10Code,
    string? PrimaryProcedureCode,
    string? SecondaryProcedureCode,
    string? Character,
    string? Modality,
    string? CnesCode,
    string? AuthorizationNumber,
    string Competence,
    string AihNumber,
    string PayloadSummary);

public record SiaDocumentPreviewDto(
    SiaDocumentType DocumentType,
    string Competence,
    int RecordCount,
    decimal EstimatedValue,
    string PayloadSummary,
    IReadOnlyList<SiaProductionLineDto>? Lines = null);

public record SiaProductionLineDto(
    string PatientName,
    string? PatientCns,
    string ProcedureCode,
    string ProcedureLabel,
    DateTime ServiceDate,
    int Quantity,
    decimal UnitValue,
    string? ProfessionalCbo);

public record DatasusExportFileDto(
    string FileName,
    string ContentType,
    string Content,
    int RecordCount,
    string ChecksumSha256,
    string LayoutVersion,
    string Competence,
    string DocumentType);

public record RndsPatientSummaryDto(
    Guid PatientId,
    string PatientName,
    bool MockData,
    IReadOnlyList<RndsClinicalItemDto> Items);

public record RndsClinicalItemDto(
    string Category,
    string Title,
    DateTime? OccurredAt,
    string Source);

public record GovIntegrationActionResultDto(
    Guid? MessageId,
    bool Success,
    string Message,
    string? Details);
