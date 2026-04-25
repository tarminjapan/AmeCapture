using AmeCapture.Application.Interfaces;

namespace AmeCapture.Infrastructure.Services
{
    public class ClipboardService : IClipboardService
    {
        public Task SetImageAsync(System.Drawing.Image image)
        {
            Serilog.Log.Debug("ClipboardService.SetImageAsync started");
            var tcs = new TaskCompletionSource();
            var thread = new Thread(() =>
            {
                try
                {
                    System.Windows.Forms.Clipboard.SetImage(image);
                    Serilog.Log.Debug("ClipboardService.SetImageAsync: image set successfully");
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Debug(ex, "ClipboardService.SetImageAsync: failed to set image");
                    tcs.SetException(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }

        public Task SetTextAsync(string text)
        {
            Serilog.Log.Debug("ClipboardService.SetTextAsync started");
            var tcs = new TaskCompletionSource();
            var thread = new Thread(() =>
            {
                try
                {
                    System.Windows.Forms.Clipboard.SetText(text);
                    Serilog.Log.Debug("ClipboardService.SetTextAsync: text set successfully");
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Debug(ex, "ClipboardService.SetTextAsync: failed to set text");
                    tcs.SetException(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }
    }
}
