using SistemaHospitalar.Application.DTOs.Nutrition;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface INutritionService
{
    Task<IReadOnlyList<DietOrderDto>> GetOrdersAsync(DietOrderStatus? status, DateOnly? mealDate, CancellationToken cancellationToken = default);
    Task<DietOrderDto> CreateOrderAsync(CreateDietOrderRequest request, CancellationToken cancellationToken = default);
    Task<DietOrderDto?> UpdateStatusAsync(Guid id, UpdateDietOrderStatusRequest request, CancellationToken cancellationToken = default);
}
