using AmeCapture.Application.Models;
using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces
{
    public interface ICaptureOrchestrator
    {
        public Task<RegionCaptureInfo> PrepareRegionCaptureAsync();
        public Task<WorkspaceItem> FinalizeRegionCaptureAsync(string sourcePath, CaptureRegion region);
        public Task CancelRegionCaptureAsync(string sourcePath);
    }
}
