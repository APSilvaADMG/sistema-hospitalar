using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Nutrition;

public record DietOrderDto(
    Guid Id,
    Guid HospitalizationId,
    string PatientName,
    string WardName,
    string BedNumber,
    DietType DietType,
    MealPeriod MealPeriod,
    DietOrderStatus Status,
    DateOnly MealDate,
    string? Notes,
    DateTime? DeliveredAt);

public record CreateDietOrderRequest(
    Guid HospitalizationId,
    DietType DietType,
    MealPeriod MealPeriod,
    DateOnly MealDate,
    string? Notes);

public record UpdateDietOrderStatusRequest(DietOrderStatus Status);
