namespace AmeCapture.Application.Models
{
    public class WindowInfo
    {
        public nint Hwnd { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public (int X, int Y, int Width, int Height) Bounds { get; set; }
    }
}
