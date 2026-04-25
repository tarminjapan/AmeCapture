using System.Runtime.Versioning;
using AmeCapture.Application.Interfaces;
using AmeCapture.Application.Models;
using AmeCapture.Domain.Entities;

namespace AmeCapture.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public class CaptureOrchestrator : ICaptureOrchestrator
{
    private readonly ICaptureService _captureService;
    private readonly IThumbnailService _thumbnailService;
    private readonly IWindowEnumerationService _windowEnumerationService;
    private readonly IStorageService _storageService;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IClipboardService? _clipboardService;

    public CaptureOrchestrator(
        ICaptureService captureService,
        IThumbnailService thumbnailService,
        IWindowEnumerationService windowEnumerationService,
        IStorageService storageService,
        IWorkspaceRepository workspaceRepository,
        IClipboardService? clipboardService = null)
    {
        _captureService = captureService;
        _thumbnailService = thumbnailService;
        _windowEnumerationService = windowEnumerationService;
        _storageService = storageService;
        _workspaceRepository = workspaceRepository;
        _clipboardService = clipboardService;
    }

    public async Task<WorkspaceItem> CaptureFullScreenAsync()
    {
        await _storageService.EnsureDirectoriesAsync();

        var filename = $"capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        var originalPath = _storageService.ResolveOriginalPath(filename);
        var editedPath = _storageService.ResolveEditedPath(filename);
        var thumbPath = _storageService.ResolveThumbnailPath(filename);

        var captureResult = await _captureService.CaptureFullScreenAsync(originalPath);
        File.Copy(originalPath, editedPath, overwrite: true);
        await _thumbnailService.GenerateThumbnailAsync(originalPath, thumbPath);

        var now = DateTime.UtcNow.ToString("o");
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
        await CopyToClipboardAsync(originalPath);
        return item;
    }

    public async Task<WorkspaceItem> CaptureWindowAsync(nint hwnd)
    {
        await _storageService.EnsureDirectoriesAsync();

        var filename = $"capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        var originalPath = _storageService.ResolveOriginalPath(filename);
        var editedPath = _storageService.ResolveEditedPath(filename);
        var thumbPath = _storageService.ResolveThumbnailPath(filename);

        var captureResult = await _captureService.CaptureWindowAsync(hwnd, originalPath);
        File.Copy(originalPath, editedPath, overwrite: true);
        await _thumbnailService.GenerateThumbnailAsync(originalPath, thumbPath);

        var now = DateTime.UtcNow.ToString("o");
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
        return item;
    }

    public async Task<RegionCaptureInfo> PrepareRegionCaptureAsync()
    {
        var tempDir = Path.GetTempPath();
        var tempPath = Path.Combine(tempDir, $"amecapture_region_{Guid.NewGuid():N}.png");
        var captureResult = await _captureService.CaptureFullScreenAsync(tempPath);

        return new RegionCaptureInfo
        {
            TempPath = tempPath,
            ScreenWidth = captureResult.Width,
            ScreenHeight = captureResult.Height
        };
    }

    public async Task<WorkspaceItem> FinalizeRegionCaptureAsync(string sourcePath, CaptureRegion region)
    {
        ValidateTempPath(sourcePath);
        await _storageService.EnsureDirectoriesAsync();

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source screenshot file not found.", sourcePath);

        using var img = System.Drawing.Image.FromFile(sourcePath);
        int x = Math.Max(0, Math.Min(region.X, img.Width - 1));
        int y = Math.Max(0, Math.Min(region.Y, img.Height - 1));
        int maxW = img.Width - x;
        int maxH = img.Height - y;
        int w = (int)Math.Min(region.Width, maxW);
        int h = (int)Math.Min(region.Height, maxH);

        if (w <= 0 || h <= 0)
        {
            TryDeleteFile(sourcePath);
            throw new InvalidOperationException("Selected region is too small.");
        }

        using var cropped = new System.Drawing.Bitmap(w, h);
        using var g = System.Drawing.Graphics.FromImage(cropped);
        g.DrawImage(img, 0, 0, new System.Drawing.Rectangle(x, y, w, h), System.Drawing.GraphicsUnit.Pixel);

        var filename = $"capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        var originalPath = _storageService.ResolveOriginalPath(filename);
        var editedPath = _storageService.ResolveEditedPath(filename);
        var thumbPath = _storageService.ResolveThumbnailPath(filename);

        cropped.Save(originalPath, System.Drawing.Imaging.ImageFormat.Png);
        File.Copy(originalPath, editedPath, overwrite: true);
        await _thumbnailService.GenerateThumbnailAsync(originalPath, thumbPath);

        var now = DateTime.UtcNow.ToString("o");
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
        return item;
    }

    public async Task CancelRegionCaptureAsync(string sourcePath)
    {
        try
        {
            ValidateTempPath(sourcePath);
        }
        catch
        {
            return;
        }
        TryDeleteFile(sourcePath);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<WindowInfo>> PrepareWindowCaptureAsync()
    {
        return await _windowEnumerationService.EnumerateWindowsAsync();
    }

    private static void ValidateTempPath(string sourcePath)
    {
        var path = Path.GetFullPath(sourcePath);
        var tempDir = Path.GetTempPath();

        if (!path.StartsWith(tempDir, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Source path is outside the temp directory.");

        var fileName = Path.GetFileName(path);
        if (!fileName.StartsWith("amecapture_region_") || !fileName.EndsWith(".png"))
            throw new InvalidOperationException("Invalid temp file name pattern.");
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }

    private async Task CopyToClipboardAsync(string imagePath)
    {
        if (_clipboardService == null || !File.Exists(imagePath)) return;

        try
        {
            using var image = System.Drawing.Image.FromFile(imagePath);
            await _clipboardService.SetImageAsync(image);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to copy captured image to clipboard");
        }
    }
}
