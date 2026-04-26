namespace AmeCapture.Domain.Entities
{
    public class AppSettings
    {
        public string SavePath { get; set; } = string.Empty;
        public string ImageFormat { get; set; } = "png";
        public bool StartMinimized { get; set; }
        public string HotkeyCaptureRegion { get; set; } = "Ctrl+Shift+S";
    }
}
