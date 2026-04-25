using AmeCapture.Application.Models;

namespace AmeCapture.Application.Interfaces;

public interface ICaptureService
{
    Task<CaptureResult> CaptureFullScreenAsync(string savePath);
    Task<CaptureResult> CaptureWindowAsync(nint hwnd, string savePath);
}
