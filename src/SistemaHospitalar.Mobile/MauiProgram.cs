using Microsoft.Extensions.Logging;
using SistemaHospitalar.Mobile.Services;
using SistemaHospitalar.Mobile.Views;

namespace SistemaHospitalar.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<SecureDatabaseService>();
        builder.Services.AddSingleton<LocalDatabase>();
        builder.Services.AddSingleton<ConnectivityService>();
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<SyncEngine>();
        builder.Services.AddSingleton<RealtimeSyncService>();
        builder.Services.AddSingleton<BackgroundSyncService>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<TransportQueuePage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
