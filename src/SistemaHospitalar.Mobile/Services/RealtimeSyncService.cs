using Microsoft.AspNetCore.SignalR.Client;

namespace SistemaHospitalar.Mobile.Services;

public class RealtimeSyncService : IAsyncDisposable
{
    private HubConnection? _connection;
    private string? _apiUrl;
    private string? _token;

    public event Func<Task>? SyncRequested;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public void Configure(string apiUrl, string token)
    {
        _apiUrl = apiUrl.TrimEnd('/');
        _token = token;
    }

    public async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(_apiUrl) || string.IsNullOrWhiteSpace(_token))
        {
            return;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connection = new HubConnectionBuilder()
            .WithUrl($"{_apiUrl}/hubs/operations?access_token={Uri.EscapeDataString(_token)}")
            .WithAutomaticReconnect()
            .Build();

        _connection.On("syncRequired", async () =>
        {
            if (SyncRequested is not null)
            {
                await SyncRequested.Invoke();
            }
        });

        _connection.On("transportChanged", async () =>
        {
            if (SyncRequested is not null)
            {
                await SyncRequested.Invoke();
            }
        });

        await _connection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
