using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Hemotherapy;

public record BloodUnitDto(
    Guid Id,
    string UnitCode,
    BloodType BloodType,
    BloodComponent Component,
    int VolumeMl,
    DateTime CollectedAt,
    DateTime ExpiresAt,
    BloodUnitStatus Status);

public record TransfusionRequestDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string? WardName,
    string? BedNumber,
    string RequestingProfessionalName,
    BloodType BloodTypeRequired,
    BloodComponent Component,
    int UnitsRequested,
    TransfusionRequestStatus Status,
    string? BloodUnitCode,
    string? Notes,
    DateTime CreatedAt,
    DateTime? TransfusedAt);

public record CreateBloodUnitRequest(
    string UnitCode,
    BloodType BloodType,
    BloodComponent Component,
    int VolumeMl,
    DateTime CollectedAt,
    DateTime ExpiresAt);

public record CreateTransfusionRequestRequest(
    Guid PatientId,
    Guid RequestingProfessionalId,
    Guid? HospitalizationId,
    BloodType BloodTypeRequired,
    BloodComponent Component,
    int UnitsRequested,
    string? Notes);

public record MatchTransfusionRequest(Guid BloodUnitId);
