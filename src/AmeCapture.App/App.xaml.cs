using AmeCapture.Application.Interfaces;
using AmeCapture.Application.Messages;
using AmeCapture.Infrastructure.Database;
using AmeCapture.Infrastructure.Services;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace AmeCapture.App;

public partial class App : global::Microsoft.Maui.Controls.Application
{
    private ITrayService? _trayService;
    private IGlobalShortcutService? _shortcutService;
    private ISettingsRepository? _settingsRepository;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;

    private bool _isExiting;

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Created += (s, e) =>
        {
            var platformWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (platformWindow != null)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(platformWindow);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                
                appWindow.Closing += (sender, args) =>
                {
                    if (!_isExiting)
                    {
                        args.Cancel = true;
                        ShowWindow(hWnd, SW_HIDE);
                    }
                };
            }
        };
        return window;
    }

    protected override async void OnStart()
    {
        base.OnStart();
        Serilog.Log.Debug("App.OnStart called");

        try
        {
            var services = Handler?.MauiContext?.Services;
            if (services == null)
            {
                Serilog.Log.Warning("App.OnStart: MauiContext.Services is null");
                return;
            }

            Serilog.Log.Debug("App.OnStart: registering ExitRequestedMessage");
            var messenger = services.GetRequiredService<IMessenger>();
            messenger.Register<AmeCapture.Application.Messages.ExitRequestedMessage>(this, (r, m) =>
            {
                Serilog.Log.Debug("App: ExitRequestedMessage received");
                MainThread.BeginInvokeOnMainThread(Exit);
            });

            Serilog.Log.Debug("App.OnStart: initializing database");
            var dbFactory = services.GetRequiredService<IDbConnectionFactory>();
            await DatabaseInitializer.InitializeAsync(dbFactory);

            Serilog.Log.Debug("App.OnStart: ensuring storage directories");
            var storageService = services.GetRequiredService<IStorageService>();
            await storageService.EnsureDirectoriesAsync();

            Serilog.Log.Debug("App.OnStart: initializing tray and shortcuts");
            _trayService = services.GetRequiredService<ITrayService>();
            _shortcutService = services.GetRequiredService<IGlobalShortcutService>();
            await _shortcutService.InitializeAsync();
            _settingsRepository = services.GetRequiredService<ISettingsRepository>();

            _trayService.Initialize();

            Serilog.Log.Debug("App.OnStart: loading settings and registering shortcuts");
            var settings = await _settingsRepository.GetAsync();
            await RegisterShortcutsAsync(settings);

            Serilog.Log.Debug("App.OnStart: initialization complete");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to initialize database or storage during startup");
        }
    }

    private async Task RegisterShortcutsAsync(Domain.Entities.AppSettings settings)
    {
        if (_shortcutService == null) return;

        Serilog.Log.Debug("App.RegisterShortcutsAsync: region={Region}, fullscreen={Fullscreen}, window={Window}", settings.HotkeyCaptureRegion, settings.HotkeyCaptureFullscreen, settings.HotkeyCaptureWindow);

        var notificationService = Handler?.MauiContext?.Services.GetService<INotificationService>();

        async Task TryRegister(string name, string shortcut, string label, string type)
        {
            if (string.IsNullOrEmpty(shortcut)) return;

            Serilog.Log.Debug("App.RegisterShortcutsAsync: registering {Name}={Shortcut}", name, shortcut);
            bool success = _shortcutService.RegisterHotKey(name, shortcut, () =>
            {
                Serilog.Log.Debug("App: hotkey {Name} triggered, sending CaptureRequestedMessage({Type})", name, type);
                Dispatcher.Dispatch(() =>
                {
                    var messenger = Handler?.MauiContext?.Services.GetService<IMessenger>();
                    messenger?.Send(new CaptureRequestedMessage(type));
                });
            });

            if (!success)
            {
                Serilog.Log.Warning("App: failed to register hotkey {Name}={Shortcut}", name, shortcut);
                if (notificationService != null)
                {
                    await notificationService.ShowNotificationAsync(
                        "ショートカット登録失敗",
                        $"{label}のショートカット ({shortcut}) は既に他のアプリで使用されています。");
                }
            }
        }

        try
        {
            await TryRegister("CaptureRegion", settings.HotkeyCaptureRegion, "範囲キャプチャ", "region");
            await TryRegister("CaptureFullscreen", settings.HotkeyCaptureFullscreen, "全画面キャプチャ", "fullscreen");
            await TryRegister("CaptureWindow", settings.HotkeyCaptureWindow, "ウィンドウキャプチャ", "window");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to register global shortcuts");
        }
    }

    public void Exit()
    {
        _isExiting = true;
        _trayService?.Exit();
        Quit();
    }
}
