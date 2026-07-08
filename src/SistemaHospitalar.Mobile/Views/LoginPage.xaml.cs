using SistemaHospitalar.Mobile.Services;

namespace SistemaHospitalar.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly SyncEngine _sync;

    public LoginPage(ApiClient api, SyncEngine sync)
    {
        InitializeComponent();
        _api = api;
        _sync = sync;

        ApiUrlEntry.Text = Preferences.Default.Get("apiUrl", GetDefaultApiUrl());
        EmailEntry.Text = Preferences.Default.Get("email", "admin@hospital.local");
    }

    private static string GetDefaultApiUrl()
    {
#if ANDROID
        return "http://10.0.2.2:8080";
#else
        return "http://localhost:8080";
#endif
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = string.Empty;
        var apiUrl = ApiUrlEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(email))
        {
            StatusLabel.Text = "Informe API e e-mail.";
            return;
        }

        try
        {
            _api.SetBaseUrl(apiUrl);
            var login = await _api.LoginAsync(email, password);
            if (login is null)
            {
                StatusLabel.Text = "Credenciais inválidas ou API indisponível.";
                return;
            }

            Preferences.Default.Set("apiUrl", apiUrl);
            Preferences.Default.Set("email", email);
            Preferences.Default.Set("token", login.Token);
            Preferences.Default.Set("userName", login.FullName);

            await _sync.InitializeAsync();
            await Shell.Current.GoToAsync("//Transport");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }
}
