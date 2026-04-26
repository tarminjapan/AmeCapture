using AmeCapture.Application.Models;

namespace AmeCapture.Application.Interfaces
{
    public interface ICaptureService
    {
        public Task<CaptureResult> CaptureFullScreenAsync(string savePath);
    }
}
