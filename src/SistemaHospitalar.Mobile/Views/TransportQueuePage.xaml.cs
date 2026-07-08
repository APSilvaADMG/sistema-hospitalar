using System.Collections.ObjectModel;
using SistemaHospitalar.Mobile.Models;
using SistemaHospitalar.Mobile.Services;

namespace SistemaHospitalar.Mobile.Views;

public partial class TransportQueuePage : ContentPage
{
    private readonly LocalDatabase _db;
    private readonly ApiClient _api;
    private readonly SyncEngine _sync;
    private readonly ConnectivityService _connectivity;
    private readonly RealtimeSyncService _realtime;
    private readonly BackgroundSyncService _background;
    private readonly ObservableCollection<TransportItemVm> _items = [];

    public TransportQueuePage(
        LocalDatabase db,
        ApiClient api,
        SyncEngine sync,
        ConnectivityService connectivity,
        RealtimeSyncService realtime,
        BackgroundSyncService background)
    {
        InitializeComponent();
        _db = db;
        _api = api;
        _sync = sync;
        _connectivity = connectivity;
        _realtime = realtime;
        _background = background;
        QueueView.ItemsSource = _items;

        Connectivity.ConnectivityChanged += async (_, e) =>
        {
            await RefreshUiAsync();
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                await TrySyncInBackgroundAsync();
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var token = Preferences.Default.Get("token", string.Empty);
        var apiUrl = Preferences.Default.Get("apiUrl", string.Empty);
        if (string.IsNullOrWhiteSpace(token))
        {
            await Shell.Current.GoToAsync("//Login");
            return;
        }

        _api.SetBaseUrl(apiUrl);
        _api.SetToken(token);
        await _sync.InitializeAsync();
        await RefreshUiAsync();

        _realtime.SyncRequested -= OnRealtimeSyncRequested;
        _realtime.SyncRequested += OnRealtimeSyncRequested;
        _realtime.Configure(apiUrl, token);

        try
        {
            await _realtime.ConnectAsync();
        }
        catch (Exception ex)
        {
            SyncStatusLabel.Text = $"SignalR: {ex.Message}";
        }

        _background.Start(async () => await TrySyncInBackgroundAsync());
        await TrySyncInBackgroundAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _background.Stop();
        _realtime.SyncRequested -= OnRealtimeSyncRequested;
        await _realtime.DisconnectAsync();
    }

    private Task OnRealtimeSyncRequested() => MainThread.InvokeOnMainThreadAsync(TrySyncInBackgroundAsync);

    private async Task RefreshUiAsync()
    {
        UserLabel.Text = $"Olá, {Preferences.Default.Get("userName", "Maqueiro")}";
        var online = _connectivity.IsOnline;
        var realtime = _realtime.IsConnected ? " · tempo real ativo" : string.Empty;
        NetworkLabel.Text = online
            ? $"Online — sync automática{realtime}"
            : "Offline — ações na fila local (SQLite criptografado)";
        NetworkLabel.TextColor = online ? Colors.Green : Colors.OrangeRed;

        var transports = await _db.GetActiveTransportsAsync();
        _items.Clear();
        foreach (var t in transports)
        {
            _items.Add(TransportItemVm.From(t));
        }
    }

    private async Task TrySyncInBackgroundAsync()
    {
        try
        {
            if (await _sync.SyncAsync())
            {
                SyncStatusLabel.Text = $"Última sync: {DateTime.Now:HH:mm:ss}";
                await RefreshUiAsync();
            }
        }
        catch (Exception ex)
        {
            SyncStatusLabel.Text = ex.Message;
        }
    }

    private async void OnSyncClicked(object? sender, EventArgs e) => await TrySyncInBackgroundAsync();

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        _background.Stop();
        await _realtime.DisconnectAsync();
        Preferences.Default.Remove("token");
        await Shell.Current.GoToAsync("//Login");
    }

    private async void OnAcceptClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not Guid id)
        {
            return;
        }

        var porterRaw = await _db.GetMetaAsync("defaultPorterId");
        if (!Guid.TryParse(porterRaw, out var employeeId))
        {
            SyncStatusLabel.Text = "Sincronize primeiro para carregar maqueiros.";
            return;
        }

        await _sync.EnqueueTransportAcceptAsync(id, employeeId, null);
        await RefreshUiAsync();
        await TrySyncInBackgroundAsync();
    }

    private async void OnTransitClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not Guid id)
        {
            return;
        }

        await _sync.EnqueueTransportAdvanceAsync(id, "InTransit");
        await RefreshUiAsync();
        await TrySyncInBackgroundAsync();
    }

    private async void OnCompleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not Guid id)
        {
            return;
        }

        await _sync.EnqueueTransportAdvanceAsync(id, "Completed");
        await RefreshUiAsync();
        await TrySyncInBackgroundAsync();
    }
}

public class TransportItemVm
{
    public Guid Id { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string RouteText { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool CanAccept { get; init; }
    public bool CanTransit { get; init; }
    public bool CanComplete { get; init; }

    public static TransportItemVm From(CachedTransportRequest t)
    {
        var origin = string.IsNullOrWhiteSpace(t.OriginDetail) ? t.OriginType : $"{t.OriginType} — {t.OriginDetail}";
        var dest = string.IsNullOrWhiteSpace(t.DestinationDetail) ? t.DestinationType : $"{t.DestinationType} — {t.DestinationDetail}";
        return new TransportItemVm
        {
            Id = t.Id,
            PatientName = t.PatientName,
            RouteText = $"{origin} → {dest}",
            Status = t.Status,
            CanAccept = t.Status == "Queued",
            CanTransit = t.Status == "Accepted",
            CanComplete = t.Status == "InTransit",
        };
    }
}
