namespace SistemaHospitalar.Application.DTOs.ClinicalIntelligence;

public record ClinicalAlertDto(
    string Code,
    string Severity,
    string Title,
    string Message,
    string? RuleId = null);

public record PatientClinicalAlertsDto(
    Guid PatientId,
    string PatientName,
    IReadOnlyList<ClinicalAlertDto> Alerts);

public record StockReplenishmentSuggestionDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal QuantityOnHand,
    decimal MinimumStock,
    decimal? AvgDailyConsumption,
    int? DaysUntilStockout,
    string Recommendation);

public record OperationalInsightDto(
    string Code,
    string Label,
    string Value,
    string? Severity = null);

public record OperationalInsightsDto(
    IReadOnlyList<OperationalInsightDto> Insights,
    DateTime GeneratedAt);

public record PrescriptionSafetyResultDto(
    bool IsSafe,
    IReadOnlyList<ClinicalAlertDto> Alerts,
    string Summary);
