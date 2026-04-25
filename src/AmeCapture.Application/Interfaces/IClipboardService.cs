namespace AmeCapture.Application.Interfaces;

public interface IClipboardService
{
    Task SetImageAsync(System.Drawing.Image image);
    Task SetTextAsync(string text);
}
