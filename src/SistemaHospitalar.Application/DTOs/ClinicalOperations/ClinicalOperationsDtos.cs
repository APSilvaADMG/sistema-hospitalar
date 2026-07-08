using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.ClinicalOperations;

public record FinancialCashSessionDto(
    Guid Id,
    string Label,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal OpeningBalance,
    decimal? ClosingBalance,
    decimal ExpectedBalance,
    FinancialCashSessionStatus Status,
    string? Notes,
    decimal CashReceived,
    decimal CashPaidOut,
    decimal CounterReceived,
    decimal CounterPaidOut,
    decimal DayOperationalReceived,
    decimal DayOperationalPaidOut);

public record OpenFinancialCashSessionRequest(
    string Label,
    decimal OpeningBalance,
    string? Notes = null);

public record CloseFinancialCashSessionRequest(
    decimal ClosingBalance,
    string? Notes = null);

public record WardStockBalanceDto(
    Guid Id,
    Guid WardId,
    string WardName,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    decimal QuantityOnHand,
    decimal MinimumStock,
    string Unit,
    bool IsLowStock);

public record WardStockMovementDto(
    Guid Id,
    Guid WardId,
    string WardName,
    Guid ProductId,
    string ProductName,
    WardStockMovementType MovementType,
    decimal Quantity,
    string Unit,
    Guid? PatientId,
    string? PatientName,
    string? Reference,
    string? Notes,
    DateTime MovementDate);

public record WardStockTransferRequest(
    Guid WardId,
    Guid ProductId,
    decimal Quantity,
    string? Reference = null,
    string? Notes = null);

public record WardStockDispenseRequest(
    Guid WardId,
    Guid ProductId,
    Guid PatientId,
    decimal Quantity,
    string? Notes = null);

public record VaccineCatalogDto(
    Guid Id,
    string Code,
    string Name,
    VaccineScheduleType ScheduleType,
    int DisplayOrder);

public record PatientVaccinationDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid VaccineCatalogId,
    string VaccineName,
    string VaccineCode,
    DateTime AdministeredAt,
    int DoseNumber,
    string? BatchNumber,
    Guid? ProfessionalId,
    string? ProfessionalName,
    string? Notes);

public record CreatePatientVaccinationRequest(
    Guid PatientId,
    Guid VaccineCatalogId,
    DateTime AdministeredAt,
    int DoseNumber = 1,
    string? BatchNumber = null,
    Guid? ProfessionalId = null,
    string? Notes = null);

public record EpidemicDiseaseCatalogDto(
    Guid Id,
    string Code,
    string Name,
    EpidemicDiseaseClass DiseaseClass,
    bool IncludeOpd,
    bool IncludeIpd,
    int DisplayOrder);
