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

        var exitItem = new ToolStripMenuItem("終了");
        exitItem.Click += (s, e) => Exit();
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

    public void ShowWindow()
    {
        // Window management will be handled by the app
    }

    public void HideWindow()
    {
        // Window management will be handled by the app
    }

    public void Exit()
    {
        _notifyIcon?.Dispose();
        _contextMenu?.Dispose();
        Microsoft.Maui.Controls.Application.Current?.Quit();
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
