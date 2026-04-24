using AmeCapture.Application.Interfaces;

namespace AmeCapture.Infrastructure.Services;

public class ClipboardService : IClipboardService
{
    public Task SetImageAsync(System.Drawing.Image image)
    {
        if (OperatingSystem.IsWindows())
        {
            var thread = new Thread(() =>
            {
                System.Windows.Forms.Clipboard.SetImage(image);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        return Task.CompletedTask;
    }

    public Task SetTextAsync(string text)
    {
        if (OperatingSystem.IsWindows())
        {
            var thread = new Thread(() =>
            {
                System.Windows.Forms.Clipboard.SetText(text);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        return Task.CompletedTask;
    }
}
