using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Tiss;

public record TussCatalogDto(
    Guid Id,
    string Code,
    string Description,
    TussTableType TableType,
    string? Unit,
    decimal? ReferencePrice,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil);

public record ImportTussRequest(IReadOnlyList<TussCatalogImportItem> Items);

public record ImportTussCsvRequest(string CsvContent);

public record ImportTussResultDto(int Imported, int TotalInFile, string Message);

public record ImportSigtapResultDto(
    int Imported,
    int TotalInFile,
    string Competence,
    string Message);

public record SyncSigtapOfficialResultDto(
    bool Success,
    string Competence,
    string RemoteCompetence,
    string SourceUrl,
    string? FileHash,
    long FileSizeBytes,
    int Inserted,
    int Updated,
    int Skipped,
    int TotalInFile,
    string Message,
    DateTime SyncedAtUtc);

public record TussCatalogImportItem(
    string Code,
    string Description,
    TussTableType TableType,
    string? Unit,
    decimal? ReferencePrice);

public record SigtapProcedureDto(
    Guid Id,
    string Code,
    string Competence,
    string Description,
    string? GroupName,
    string? Complexity,
    decimal? HospitalAmount,
    decimal? ProfessionalAmount);

public record SigtapCatalogSummaryDto(
    int TotalCount,
    string? LastCompetence,
    DateTime? LastImportAt);

public record TissDemonstrativoDto(
    Guid Id,
    string DemonstrativoNumber,
    Guid HealthInsuranceId,
    string HealthInsuranceName,
    string Competence,
    TissDemonstrativoStatus Status,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal TotalGlosa,
    int ItemCount,
    int MatchedCount,
    DateTime CreatedAt,
    DateTime? ProcessedAt);

public record TissDemonstrativoDetailDto(
    Guid Id,
    string DemonstrativoNumber,
    Guid HealthInsuranceId,
    string HealthInsuranceName,
    string Competence,
    TissDemonstrativoStatus Status,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal TotalGlosa,
    string? SourceFileName,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    IReadOnlyList<TissDemonstrativoItemDto> Items);

public record TissDemonstrativoItemDto(
    Guid Id,
    string GuideNumber,
    string? TussCode,
    decimal BilledAmount,
    decimal PaidAmount,
    decimal GlosaAmount,
    string? GlosaReason,
    string? AnsGlosaCode,
    bool IsMatched,
    Guid? TissGuideId);

public record ImportDemonstrativoRequest(
    Guid HealthInsuranceId,
    string Competence,
    string? SourceFileName,
    string CsvContent);

public record TissGuideAnnexDto(
    Guid Id,
    Guid TissGuideId,
    TissAnnexType AnnexType,
    string? Cid10Code,
    string? ClinicalIndication,
    string? CycleInfo,
    string? Notes,
    IReadOnlyList<TissOpmeItemDto> OpmeItems);

public record TissOpmeItemDto(
    Guid Id,
    string TussCode,
    string Description,
    string? Manufacturer,
    string? AuthorizationNumber,
    int Quantity,
    decimal UnitPrice,
    decimal Total);

public record CreateTissGuideAnnexRequest(
    Guid TissGuideId,
    TissAnnexType AnnexType,
    string? Cid10Code,
    string? ClinicalIndication,
    string? CycleInfo,
    string? Notes,
    IReadOnlyList<TissOpmeItemRequest>? OpmeItems);

public record TissOpmeItemRequest(
    string TussCode,
    string Description,
    string? Manufacturer,
    string? AuthorizationNumber,
    int Quantity,
    decimal UnitPrice);

public record HealthInsuranceIntegrationDto(
    Guid Id,
    string Name,
    string? AnsRegistration,
    string? TissVersion,
    string? OperatorCode,
    string? PortalUrl,
    string? WebServiceUrl,
    string? IntegrationUser,
    bool UseMockIntegration,
    bool IsActive);

public record UpdateHealthInsuranceIntegrationRequest(
    string? TissVersion,
    string? OperatorCode,
    string? PortalUrl,
    string? WebServiceUrl,
    string? IntegrationUser,
    string? IntegrationSecret,
    bool UseMockIntegration);

public record OperatorTransactionLogDto(
    Guid Id,
    Guid HealthInsuranceId,
    string HealthInsuranceName,
    OperatorTransactionType TransactionType,
    OperatorTransactionStatus Status,
    string? ReferenceId,
    string? ErrorMessage,
    int? DurationMs,
    DateTime CreatedAt);

public record FetchOperatorDemonstrativoRequest(Guid HealthInsuranceId, Guid? TissBatchId);

public record TissReconciliationSummaryDto(
    int GuidesWithReceivable,
    int GuidesPaidInFinance,
    decimal TotalReceivableOpen,
    decimal TotalReceivablePaid);
