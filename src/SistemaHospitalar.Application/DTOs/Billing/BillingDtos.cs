namespace SistemaHospitalar.Application.DTOs.Billing;

public record BillingDashboardDto(
    int OpenAccountsCount,
    decimal OpenAccountsAmount,
    decimal ReceivableOpen,
    decimal ReceivedThisMonth,
    int TissGuidesDraft,
    int TissGuidesSent,
    int TissGuidesPaid,
    int TissGuidesGlosa,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal TotalGlosaOpen,
    decimal GlosaRatePercent,
    int GuidesPendingOver30Days,
    int ActiveSusHospitalizations,
    int AihReadyCount,
    int SusExportsThisMonth,
    IReadOnlyList<BillingAlertDto> Alerts,
    DateTime GeneratedAt);

public record BillingAlertDto(
    string Code,
    string Severity,
    string Title,
    string Message,
    string? LinkPath);
