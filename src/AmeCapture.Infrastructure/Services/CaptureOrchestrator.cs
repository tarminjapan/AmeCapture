using AmeCapture.Application.Interfaces;
using AmeCapture.Application.Models;
using AmeCapture.Domain.Entities;

namespace AmeCapture.Infrastructure.Services
{
    public class CaptureOrchestrator(
        ICaptureService captureService,
        IThumbnailService thumbnailService,
        IWindowEnumerationService windowEnumerationService,
        IStorageService storageService,
        IWorkspaceRepository workspaceRepository,
        IClipboardService? clipboardService = null) : ICaptureOrchestrator
    {
        private readonly ICaptureService _captureService = captureService;
        private readonly IThumbnailService _thumbnailService = thumbnailService;
        private readonly IWindowEnumerationService _windowEnumerationService = windowEnumerationService;
        private readonly IStorageService _storageService = storageService;
        private readonly IWorkspaceRepository _workspaceRepository = workspaceRepository;
        private readonly IClipboardService? _clipboardService = clipboardService;

        public async Task<WorkspaceItem> CaptureFullScreenAsync()
        {
            Serilog.Log.Debug("CaptureOrchestrator.CaptureFullScreenAsync started");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            await _storageService.EnsureDirectoriesAsync();

            string filename = $"capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            string originalPath = _storageService.ResolveOriginalPath(filename);
            string editedPath = _storageService.ResolveEditedPath(filename);
            string thumbPath = _storageService.ResolveThumbnailPath(filename);
            Serilog.Log.Debug("Paths resolved - original={OriginalPath}, edited={EditedPath}, thumb={ThumbPath}", originalPath, editedPath, thumbPath);

            var captureResult = await _captureService.CaptureFullScreenAsync(originalPath);
            Serilog.Log.Debug("Capture result: {Width}x{Height}", captureResult.Width, captureResult.Height);

            File.Copy(originalPath, editedPath, overwrite: true);
            _ = await _thumbnailService.GenerateThumbnailAsync(originalPath, thumbPath);
            Serilog.Log.Debug("Thumbnail generated at {ThumbPath}", thumbPath);

            string now = DateTime.UtcNow.ToString("o");
            var item = new WorkspaceItem
            {
                Id = Guid.NewGuid().ToString(),
                ItemType = WorkspaceItemType.Image,
                OriginalPath = originalPath,
                CurrentPath = editedPath,
                ThumbnailPath = thumbPath,
                Title = $"Capture {DateTime.Now:yyyy/MM/dd HH:mm:ss}",
                CreatedAt = now,
                UpdatedAt = now,
                IsFavorite = false,
                MetadataJson = null
            };

            await _workspaceRepository.AddAsync(item);
            Serilog.Log.Debug("WorkspaceItem added to repository: {ItemId}", item.Id);
            await CopyToClipboardAsync(originalPath);

            sw.Stop();
            Serilog.Log.Debug("CaptureOrchestrator.CaptureFullScreenAsync completed in {Elapsed}ms, ItemId={ItemId}", sw.ElapsedMilliseconds, item.Id);
            return item;
        }

        public async Task<WorkspaceItem> CaptureWindowAsync(nint hwnd)
        {
            Serilog.Log.Debug("CaptureOrchestrator.CaptureWindowAsync started, hwnd={Hwnd}", hwnd);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            await _storageService.EnsureDirectoriesAsync();

            string filename = $"capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            string originalPath = _storageService.ResolveOriginalPath(filename);
            string editedPath = _storageService.ResolveEditedPath(filename);
            string thumbPath = _storageService.ResolveThumbnailPath(filename);

            var captureResult = await _captureService.CaptureWindowAsync(hwnd, originalPath);
            Serilog.Log.Debug("Window capture result: {Width}x{Height}", captureResult.Width, captureResult.Height);

            File.Copy(originalPath, editedPath, overwrite: true);
            _ = await _thumbnailService.GenerateThumbnailAsync(originalPath, thumbPath);

            string now = DateTime.UtcNow.ToString("o");
            var item = new WorkspaceItem
            {
                Id = Guid.NewGuid().ToString(),
                ItemType = WorkspaceItemType.Image,
                OriginalPath = originalPath,
                CurrentPath = editedPath,
                ThumbnailPath = thumbPath,
                Title = $"Window Capture {DateTime.Now:yyyy/MM/dd HH:mm:ss}",
                CreatedAt = now,
                UpdatedAt = now,
                IsFavorite = false,
                MetadataJson = null
            };

            await _workspaceRepository.AddAsync(item);
            await CopyToClipboardAsync(originalPath);

            sw.Stop();
            Serilog.Log.Debug("CaptureOrchestrator.CaptureWindowAsync completed in {Elapsed}ms, ItemId={ItemId}", sw.ElapsedMilliseconds, item.Id);
            return item;
        }

        public async Task<RegionCaptureInfo> PrepareRegionCaptureAsync()
        {
            Serilog.Log.Debug("CaptureOrchestrator.PrepareRegionCaptureAsync started");
            string tempDir = Path.GetTempPath();
            string tempPath = Path.Combine(tempDir, $"amecapture_region_{Guid.NewGuid():N}.png");
            Serilog.Log.Debug("Region temp path: {TempPath}", tempPath);

            var captureResult = await _captureService.CaptureFullScreenAsync(tempPath);
            Serilog.Log.Debug("Region capture prepared: {Width}x{Height}", captureResult.Width, captureResult.Height);

            return new RegionCaptureInfo
            {
                TempPath = tempPath,
                ScreenWidth = captureResult.Width,
                ScreenHeight = captureResult.Height
            };
        }

        public async Task<WorkspaceItem> FinalizeRegionCaptureAsync(string sourcePath, CaptureRegion region)
        {
            Serilog.Log.Debug("CaptureOrchestrator.FinalizeRegionCaptureAsync started, sourcePath={SourcePath}, region=({X},{Y},{W},{H})", sourcePath, region.X, region.Y, region.Width, region.Height);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            ValidateTempPath(sourcePath);
            await _storageService.EnsureDirectoriesAsync();

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Source screenshot file not found.", sourcePath);
            }

            using var img = System.Drawing.Image.FromFile(sourcePath);
            int x = Math.Max(0, Math.Min(region.X, img.Width - 1));
            int y = Math.Max(0, Math.Min(region.Y, img.Height - 1));
            int maxW = img.Width - x;
            int maxH = img.Height - y;
            int w = (int)Math.Min(region.Width, maxW);
            int h = (int)Math.Min(region.Height, maxH);
            Serilog.Log.Debug("Source image: {SrcW}x{SrcH}, Crop region: x={X}, y={Y}, w={W}, h={H}", img.Width, img.Height, x, y, w, h);

            if (w <= 0 || h <= 0)
            {
                TryDeleteFile(sourcePath);
                throw new InvalidOperationException("Selected region is too small.");
            }

            using var cropped = new Bitmap(w, h);
            using var g = Graphics.FromImage(cropped);
            g.DrawImage(img, 0, 0, new Rectangle(x, y, w, h), GraphicsUnit.Pixel);

            string filename = $"capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            string originalPath = _storageService.ResolveOriginalPath(filename);
            string editedPath = _storageService.ResolveEditedPath(filename);
            string thumbPath = _storageService.ResolveThumbnailPath(filename);

            cropped.Save(originalPath, System.Drawing.Imaging.ImageFormat.Png);
            File.Copy(originalPath, editedPath, overwrite: true);
            _ = await _thumbnailService.GenerateThumbnailAsync(originalPath, thumbPath);

            string now = DateTime.UtcNow.ToString("o");
            var item = new WorkspaceItem
            {
                Id = Guid.NewGuid().ToString(),
                ItemType = WorkspaceItemType.Image,
                OriginalPath = originalPath,
                CurrentPath = editedPath,
                ThumbnailPath = thumbPath,
                Title = $"Region Capture {DateTime.Now:yyyy/MM/dd HH:mm:ss}",
                CreatedAt = now,
                UpdatedAt = now,
                IsFavorite = false,
                MetadataJson = null
            };

            await _workspaceRepository.AddAsync(item);
            await CopyToClipboardAsync(originalPath);
            TryDeleteFile(sourcePath);

            sw.Stop();
            Serilog.Log.Debug("CaptureOrchestrator.FinalizeRegionCaptureAsync completed in {Elapsed}ms, ItemId={ItemId}", sw.ElapsedMilliseconds, item.Id);
            return item;
        }

        public async Task CancelRegionCaptureAsync(string sourcePath)
        {
            Serilog.Log.Debug("CaptureOrchestrator.CancelRegionCaptureAsync, sourcePath={SourcePath}", sourcePath);
            try
            {
                ValidateTempPath(sourcePath);
            }
            catch
            {
                Serilog.Log.Debug("CancelRegionCaptureAsync: path validation failed, ignoring");
                return;
            }
            TryDeleteFile(sourcePath);
            Serilog.Log.Debug("CancelRegionCaptureAsync: temp file deleted");
            await Task.CompletedTask;
        }

        public async Task<IReadOnlyList<WindowInfo>> PrepareWindowCaptureAsync()
        {
            Serilog.Log.Debug("CaptureOrchestrator.PrepareWindowCaptureAsync started");
            var windows = await _windowEnumerationService.EnumerateWindowsAsync();
            Serilog.Log.Debug("PrepareWindowCaptureAsync: found {Count} windows", windows.Count);
            return windows;
        }

        private static void ValidateTempPath(string sourcePath)
        {
            string path = Path.GetFullPath(sourcePath);
            string tempDir = Path.GetTempPath();

            if (!path.StartsWith(tempDir, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Source path is outside the temp directory.");
            }

            string fileName = Path.GetFileName(path);
            if (!fileName.StartsWith("amecapture_region_", StringComparison.Ordinal) || !fileName.EndsWith(".png", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid temp file name pattern.");
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch { }
        }

        private async Task CopyToClipboardAsync(string imagePath)
        {
            if (_clipboardService == null || !File.Exists(imagePath))
            {
                Serilog.Log.Debug("CopyToClipboardAsync skipped: clipboardService={HasService}, fileExists={FileExists}", _clipboardService != null, File.Exists(imagePath));
                return;
            }

            try
            {
                using var image = System.Drawing.Image.FromFile(imagePath);
                await _clipboardService.SetImageAsync(image);
                Serilog.Log.Debug("Image copied to clipboard: {ImagePath}", imagePath);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to copy captured image to clipboard");
            }
        }
    }
}
