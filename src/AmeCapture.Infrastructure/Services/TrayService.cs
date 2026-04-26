using System.Runtime.InteropServices;
using AmeCapture.Application.Interfaces;
using AmeCapture.Application.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace AmeCapture.Infrastructure.Services
{
    public class TrayService(IMessenger messenger) : ITrayService, IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private bool _disposed;
        private readonly IMessenger _messenger = messenger;

        public void Initialize()
        {
            try
            {
                _contextMenu = new ContextMenuStrip();

                var showItem = new ToolStripMenuItem("ウィンドウを表示");
                showItem.Click += (s, e) => ShowWindow();
                _ = _contextMenu.Items.Add(showItem);

                _ = _contextMenu.Items.Add(new ToolStripSeparator());

                var captureMenu = new ToolStripMenuItem("キャプチャ");
                var regionItem = new ToolStripMenuItem("範囲キャプチャ");
                regionItem.Click += (s, e) => TriggerCapture("region");
                _ = captureMenu.DropDownItems.Add(regionItem);
                _ = _contextMenu.Items.Add(captureMenu);

                _ = _contextMenu.Items.Add(new ToolStripSeparator());

                var settingsItem = new ToolStripMenuItem("設定");
                settingsItem.Click += (s, e) => ShowSettings();
                _ = _contextMenu.Items.Add(settingsItem);

                _ = _contextMenu.Items.Add(new ToolStripSeparator());

                var exitItem = new ToolStripMenuItem("終了");
                exitItem.Click += (s, e) => _messenger.Send(new ExitRequestedMessage());
                _ = _contextMenu.Items.Add(exitItem);

                _notifyIcon = new NotifyIcon
                {
                    Icon = SystemIcons.Application,
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

                Serilog.Log.Information("TrayService initialized successfully");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to initialize TrayService");
            }
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        public void ShowWindow()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Window? window = Microsoft.Maui.Controls.Application.Current?.Windows[0];
                if (window != null)
                {
                    var platformWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                    if (platformWindow != null)
                    {
                        nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(platformWindow);
                        _ = ShowWindow(hWnd, SW_SHOW);
                        platformWindow.Activate();
                    }
                }
            });
        }

        public void HideWindow()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Window? window = Microsoft.Maui.Controls.Application.Current?.Windows[0];
                if (window != null)
                {
                    var platformWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                    if (platformWindow != null)
                    {
                        nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(platformWindow);
                        _ = ShowWindow(hWnd, SW_HIDE);
                    }
                }
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();
            }

            _disposed = true;
        }

        public void Exit()
        {
            try
            {
                Dispose();
                Serilog.Log.Information("TrayService exited");
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Error during TrayService cleanup");
            }
        }

        private void ShowSettings()
        {
            ShowWindow();
        }

        private void TriggerCapture(string captureType)
        {
            Serilog.Log.Debug("TrayService.TriggerCapture: {CaptureType}", captureType);
            _ = _messenger.Send(new CaptureRequestedMessage(captureType));
        }
    }
}
