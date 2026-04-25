namespace AmeCapture.Application.Interfaces
{
    public interface IClipboardService
    {
        public Task SetImageAsync(System.Drawing.Image image);
        public Task SetTextAsync(string text);
    }
}
