namespace AmeCapture.Domain.Entities;

public class AppSettings
{
    public string SavePath { get; set; } = string.Empty;
    public string ImageFormat { get; set; } = "png";
    public string HotkeyCapture { get; set; } = "Ctrl+Shift+S";
    public string HotkeyRegionCapture { get; set; } = "Ctrl+Shift+F";
    public string HotkeyWindowCapture { get; set; } = "Ctrl+Shift+W";
}
