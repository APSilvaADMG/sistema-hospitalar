using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Financial;

public record MiscellaneousReceiptDto(
    Guid Id,
    string ReceiptNumber,
    DateTime ReceiptDate,
    string PayerName,
    string ReceiverName,
    decimal Amount,
    string Description,
    PaymentMethod PaymentMethod,
    string? Reference,
    DateTime CreatedAt);

public record CreateMiscellaneousReceiptRequest(
    DateTime ReceiptDate,
    string PayerName,
    string ReceiverName,
    decimal Amount,
    string Description,
    PaymentMethod PaymentMethod,
    string? Reference);

public record UpdateMiscellaneousReceiptRequest(
    DateTime ReceiptDate,
    string PayerName,
    string ReceiverName,
    decimal Amount,
    string Description,
    PaymentMethod PaymentMethod,
    string? Reference);
