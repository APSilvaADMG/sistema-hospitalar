using SistemaHospitalar.Application.DTOs.Sync;

namespace SistemaHospitalar.Application.Interfaces;

public interface ISyncService
{
    Task<SyncPushResponse> PushAsync(SyncPushRequest request, Guid? userId, CancellationToken cancellationToken = default);
    Task<SyncPullResponse> PullAsync(SyncPullRequest request, CancellationToken cancellationToken = default);
}
