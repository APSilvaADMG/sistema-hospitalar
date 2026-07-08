namespace SistemaHospitalar.Application.Interfaces;

public interface IOperationsRealtimeNotifier
{
    Task NotifyTransportChangedAsync(Guid? requestId = null, CancellationToken cancellationToken = default);
    Task NotifyCleaningChangedAsync(Guid? requestId = null, CancellationToken cancellationToken = default);
    Task NotifyBedsChangedAsync(CancellationToken cancellationToken = default);
}
