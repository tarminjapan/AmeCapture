using AmeCapture.Application.Interfaces;
using AmeCapture.App.Messages;
using AmeCapture.Infrastructure.Database;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace AmeCapture.App;

public partial class App : global::Microsoft.Maui.Controls.Application
{
    private ITrayService? _trayService;
    private IGlobalShortcutService? _shortcutService;
    private ISettingsRepository? _settingsRepository;

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

#if WINDOWS
            _trayService = services.GetRequiredService<ITrayService>();
            _shortcutService = services.GetRequiredService<IGlobalShortcutService>();
            _settingsRepository = services.GetRequiredService<ISettingsRepository>();

            _trayService.Initialize();

            var settings = await _settingsRepository.GetAsync();
            RegisterShortcuts(settings);
#endif
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to initialize database or storage during startup");
        }
    }

#if WINDOWS
    private void RegisterShortcuts(Domain.Entities.AppSettings settings)
    {
        if (_shortcutService == null) return;

        try
        {
            _shortcutService.RegisterHotKey("CaptureRegion", settings.HotkeyCaptureRegion, () =>
            {
                Dispatcher.Dispatch(async () =>
                {
                    if (MainPage != null)
                    {
                        var messenger = Handler?.MauiContext?.Services.GetService<IMessenger>();
                        messenger?.Send(new CaptureRequestedMessage("region"));
                    }
                });
            });

            _shortcutService.RegisterHotKey("CaptureFullscreen", settings.HotkeyCaptureFullscreen, () =>
            {
                Dispatcher.Dispatch(async () =>
                {
                    if (MainPage != null)
                    {
                        var messenger = Handler?.MauiContext?.Services.GetService<IMessenger>();
                        messenger?.Send(new CaptureRequestedMessage("fullscreen"));
                    }
                });
            });

            _shortcutService.RegisterHotKey("CaptureWindow", settings.HotkeyCaptureWindow, () =>
            {
                Dispatcher.Dispatch(async () =>
                {
                    if (MainPage != null)
                    {
                        var messenger = Handler?.MauiContext?.Services.GetService<IMessenger>();
                        messenger?.Send(new CaptureRequestedMessage("window"));
                    }
                });
            });
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to register global shortcuts");
        }
    }
#endif
}
