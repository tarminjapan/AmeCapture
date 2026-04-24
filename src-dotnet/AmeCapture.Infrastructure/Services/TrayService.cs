using System.Linq;
using System.Runtime.InteropServices;
using AmeCapture.Application.Interfaces;
using CommunityToolkit.Mvvm.Messaging;

namespace AmeCapture.Infrastructure.Services;

public class TrayService : ITrayService
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private readonly IMessenger _messenger;

    public TrayService(IMessenger messenger)
    {
        _messenger = messenger;
    }

    public void Initialize()
    {
        if (!OperatingSystem.IsWindows())
            return;

        _contextMenu = new ContextMenuStrip();

        var showItem = new ToolStripMenuItem("ウィンドウを表示");
        showItem.Click += (s, e) => ShowWindow();
        _contextMenu.Items.Add(showItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        var captureMenu = new ToolStripMenuItem("キャプチャ");
        var regionItem = new ToolStripMenuItem("範囲キャプチャ");
        regionItem.Click += (s, e) => TriggerCapture("region");
        var fullscreenItem = new ToolStripMenuItem("全画面キャプチャ");
        fullscreenItem.Click += (s, e) => TriggerCapture("fullscreen");
        var windowItem = new ToolStripMenuItem("ウィンドウキャプチャ");
        windowItem.Click += (s, e) => TriggerCapture("window");
        captureMenu.DropDownItems.Add(regionItem);
        captureMenu.DropDownItems.Add(fullscreenItem);
        captureMenu.DropDownItems.Add(windowItem);
        _contextMenu.Items.Add(captureMenu);

        _contextMenu.Items.Add(new ToolStripSeparator());

        var settingsItem = new ToolStripMenuItem("設定");
        settingsItem.Click += (s, e) => ShowSettings();
        _contextMenu.Items.Add(settingsItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("終了");
        exitItem.Click += (s, e) => _messenger.Send(new AmeCapture.Application.Messages.ExitRequestedMessage());
        _contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "AmeCapture",
            ContextMenuStrip = _contextMenu
        };

        _notifyIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowWindow();
            }
        };
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    public void ShowWindow()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
            if (window != null)
            {
#if WINDOWS
                var platformWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (platformWindow != null)
                {
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(platformWindow);
                    ShowWindow(hWnd, SW_SHOW);
                    platformWindow.Activate();
                }
#endif
            }
        });
    }

    public void HideWindow()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
            if (window != null)
            {
#if WINDOWS
                var platformWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (platformWindow != null)
                {
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(platformWindow);
                    ShowWindow(hWnd, SW_HIDE);
                }
#endif
            }
        });
    }

    public void Exit()
    {
        _notifyIcon?.Dispose();
        _contextMenu?.Dispose();
    }

    private void ShowSettings()
    {
        ShowWindow();
        // TODO: Navigate to SettingsPage once implemented
        // MainThread.BeginInvokeOnMainThread(async () =>
        // {
        //     await Shell.Current.GoToAsync("SettingsPage");
        // });
    }

    private void TriggerCapture(string captureType)
    {
        _messenger.Send(new CaptureRequestedMessage(captureType));
    }
}

public class CaptureRequestedMessage(string captureType)
{
    public string CaptureType { get; } = captureType;
}
