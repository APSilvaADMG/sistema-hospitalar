using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Financial;

public record PixChargeDto(
    Guid Id,
    Guid FinancialAccountId,
    Guid PatientId,
    string PatientName,
    string TxId,
    decimal Amount,
    PixChargeStatus Status,
    string CopyPasteCode,
    DateTime ExpiresAt,
    DateTime? PaidAt,
    DateTime CreatedAt);

public record PixWebhookRequest(
    string TxId,
    string Status,
    decimal? Amount,
    string? PayerName);

public record PixWebhookResult(
    bool Processed,
    string Message,
    Guid? ChargeId,
    Guid? FinancialAccountId);
