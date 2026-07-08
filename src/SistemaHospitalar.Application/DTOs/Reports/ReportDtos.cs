namespace SistemaHospitalar.Application.DTOs.Reports;

public record ReportCatalogItemDto(
    string Code,
    string Name,
    string Module,
    string ModuleLabel,
    string Description,
    bool IsEssential,
    bool IsImplemented,
    int Phase);

public record ReportCatalogSummaryDto(
    int TotalReports,
    int EssentialReports,
    int ImplementedReports,
    IReadOnlyList<ReportModuleSummaryDto> Modules);

public record ReportModuleSummaryDto(
    string Module,
    string Label,
    int Total,
    int Essential,
    int Implemented);

public record ReportFilterDto(
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    Guid? ProfessionalId = null,
    Guid? SpecialtyId = null,
    Guid? HealthInsuranceId = null,
    Guid? PatientId = null,
    Guid? TpaAdministratorId = null,
    int? Year = null,
    int? Month = null,
    string? Department = null);

public record ReportColumnDto(string Key, string Label, string? Format = null);

public record ReportKpiDto(string Label, string Value, string? Variant = null);

public record ReportResultDto(
    string Code,
    string Title,
    string? Subtitle,
    bool IsImplemented,
    IReadOnlyList<ReportColumnDto> Columns,
    IReadOnlyList<Dictionary<string, object?>> Rows,
    IReadOnlyList<ReportKpiDto> Kpis,
    DateTime GeneratedAt);

public record ReportDefinition(
    string Code,
    string Name,
    Domain.Enums.ReportModule Module,
    string Description,
    bool IsEssential,
    bool IsImplemented,
    int Phase = 1);
