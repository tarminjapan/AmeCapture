namespace AmeCapture.Application.Models;

public class CaptureResult
{
    public string FilePath { get; set; } = string.Empty;
    public uint Width { get; set; }
    public uint Height { get; set; }
}
