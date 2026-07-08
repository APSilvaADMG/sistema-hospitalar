namespace SistemaHospitalar.Mobile.Services;

public class BackgroundSyncService : IDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private Func<Task>? _syncAction;

    public void Start(Func<Task> syncAction, TimeSpan? interval = null)
    {
        Stop();
        _syncAction = syncAction;
        _cts = new CancellationTokenSource();
        var period = interval ?? TimeSpan.FromSeconds(45);
        _worker = RunAsync(period, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _worker = null;
    }

    private async Task RunAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (_syncAction is null || Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                continue;
            }

            try
            {
                await _syncAction.Invoke();
            }
            catch
            {
                // Próximo ciclo tenta novamente.
            }
        }
    }

    public void Dispose() => Stop();
}
