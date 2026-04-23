using AmeCapture.Application.Models;
using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces;

public interface ICaptureOrchestrator
{
    Task<WorkspaceItem> CaptureFullScreenAsync();
    Task<WorkspaceItem> CaptureWindowAsync(nint hwnd);
    Task<RegionCaptureInfo> PrepareRegionCaptureAsync();
    Task<WorkspaceItem> FinalizeRegionCaptureAsync(string sourcePath, CaptureRegion region);
    Task CancelRegionCaptureAsync(string sourcePath);
    Task<IReadOnlyList<WindowInfo>> PrepareWindowCaptureAsync();
}
