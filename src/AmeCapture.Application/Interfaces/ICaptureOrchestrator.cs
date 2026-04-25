using AmeCapture.Application.Models;
using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces
{
    public interface ICaptureOrchestrator
    {
        public Task<WorkspaceItem> CaptureFullScreenAsync();
        public Task<WorkspaceItem> CaptureWindowAsync(nint hwnd);
        public Task<RegionCaptureInfo> PrepareRegionCaptureAsync();
        public Task<WorkspaceItem> FinalizeRegionCaptureAsync(string sourcePath, CaptureRegion region);
        public Task CancelRegionCaptureAsync(string sourcePath);
        public Task<IReadOnlyList<WindowInfo>> PrepareWindowCaptureAsync();
    }
}
