using SistemaHospitalar.Mobile.Views;

namespace SistemaHospitalar.Mobile;

public partial class AppShell : Shell
{
    public AppShell(LoginPage loginPage, TransportQueuePage transportPage)
    {
        InitializeComponent();

        Items.Add(new ShellContent
        {
            Title = "Login",
            Route = "Login",
            Content = loginPage,
        });

        Items.Add(new ShellContent
        {
            Title = "Transportes",
            Route = "Transport",
            Content = transportPage,
        });

        CurrentItem = Items[0];
    }
}
