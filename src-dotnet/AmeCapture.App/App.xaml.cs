using AmeCapture.Application.Interfaces;
using AmeCapture.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace AmeCapture.App;

public partial class App : global::Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    protected override async void OnStart()
    {
        base.OnStart();

        try
        {
            var services = Handler?.MauiContext?.Services;
            if (services == null) return;

            var dbFactory = services.GetRequiredService<IDbConnectionFactory>();
            await DatabaseInitializer.InitializeAsync(dbFactory);

            var storageService = services.GetRequiredService<IStorageService>();
            await storageService.EnsureDirectoriesAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to initialize database or storage during startup");
        }
    }
}
