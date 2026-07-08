using SistemaHospitalar.Application.DTOs.CommandCenter;

namespace SistemaHospitalar.Application.Interfaces;

public interface ICommandCenterService
{
    Task<CommandCenterDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<OperationsQueueSnapshotDto> GetQueueSnapshotAsync(CancellationToken cancellationToken = default);
}
