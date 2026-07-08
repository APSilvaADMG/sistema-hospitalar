using SistemaHospitalar.Application.DTOs.Tasks;

namespace SistemaHospitalar.Application.Interfaces;

public interface ITaskEngineService
{
    Task<UserMissionsDto> GenerateTasksForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteTaskAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
