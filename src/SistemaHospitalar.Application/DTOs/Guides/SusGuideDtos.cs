namespace SistemaHospitalar.Application.DTOs.Guides;

public record ServiceUnitDto(
    Guid Id,
    string Name,
    string Code,
    string? Cnes,
    string? Address,
    bool IsDefault,
    bool IsActive);

public record CreateServiceUnitRequest(
    string Name,
    string Code,
    string? Cnes,
    string? Address,
    bool IsDefault = false);

public record UpdateServiceUnitRequest(
    string Name,
    string Code,
    string? Cnes,
    string? Address,
    bool IsDefault,
    bool IsActive);

public record SusGuideDto(
    Guid Id,
    string GuideNumber,
    int GuideType,
    int Status,
    Guid PatientId,
    string PatientName,
    Guid? ProfessionalId,
    string? ProfessionalName,
    Guid? ServiceUnitId,
    string? ServiceUnitName,
    Guid? AppointmentId,
    Guid? HospitalizationId,
    string? Cid10Code,
    string? SigtapProcedureCode,
    string? ProcedureDescription,
    string? Competence,
    string? AuthorizationNumber,
    DateTime? AuthorizedAt,
    DateTime? SubmittedAt,
    decimal? TotalAmount,
    string? Notes,
    DateTime CreatedAt);

public record CreateSusGuideRequest(
    Guid PatientId,
    int GuideType,
    Guid? ProfessionalId,
    Guid? ServiceUnitId,
    Guid? AppointmentId,
    Guid? HospitalizationId,
    string? Cid10Code,
    string? SigtapProcedureCode,
    string? ProcedureDescription,
    string? Competence,
    string? Notes,
    decimal? TotalAmount);

public record UpdateSusGuideRequest(
    Guid? ProfessionalId,
    Guid? ServiceUnitId,
    Guid? AppointmentId,
    Guid? HospitalizationId,
    string? Cid10Code,
    string? SigtapProcedureCode,
    string? ProcedureDescription,
    string? Competence,
    string? Notes,
    decimal? TotalAmount);

public record SusGuideFilterDto(
    DateTime? DateFrom,
    DateTime? DateTo,
    Guid? PatientId,
    Guid? ProfessionalId,
    Guid? ServiceUnitId,
    int? GuideType,
    int? Status,
    string? GuideNumber,
    string? ProcedureSearch,
    int Skip = 0,
    int Take = 50);
