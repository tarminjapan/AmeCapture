using AmeCapture.Application.Interfaces;

namespace AmeCapture.Infrastructure.Services;

public class ClipboardService : IClipboardService
{
    public Task SetImageAsync(System.Drawing.Image image)
    {
        if (!OperatingSystem.IsWindows())
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                System.Windows.Forms.Clipboard.SetImage(image);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs.Task;
    }

    public Task SetTextAsync(string text)
    {
        if (!OperatingSystem.IsWindows())
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                System.Windows.Forms.Clipboard.SetText(text);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs.Task;
    }
}
