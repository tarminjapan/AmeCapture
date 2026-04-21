using AmeCapture.Infrastructure.Services;

namespace AmeCapture.Tests.Integration;

public class StorageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly StorageService _service;

    public StorageServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"test_storage_{Guid.NewGuid():N}");
        _service = new StorageService(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public async Task EnsureDirectoriesAsync_CreatesAllDirectories()
    {
        await _service.EnsureDirectoriesAsync();

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "originals")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "edited")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "thumbnails")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "videos")));
    }

    [Fact]
    public async Task EnsureDirectoriesAsync_IsIdempotent()
    {
        await _service.EnsureDirectoriesAsync();
        await _service.EnsureDirectoriesAsync();

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "originals")));
    }

    [Fact]
    public void ResolveOriginalPath_ReturnsCorrectPath()
    {
        var path = _service.ResolveOriginalPath("a.png");
        Assert.Equal(Path.Combine(_tempDir, "originals", "a.png"), path);
    }

    [Fact]
    public void ResolveEditedPath_ReturnsCorrectPath()
    {
        var path = _service.ResolveEditedPath("a.png");
        Assert.Equal(Path.Combine(_tempDir, "edited", "a.png"), path);
    }

    [Fact]
    public void ResolveThumbnailPath_ReturnsCorrectPath()
    {
        var path = _service.ResolveThumbnailPath("a.png");
        Assert.Equal(Path.Combine(_tempDir, "thumbnails", "a_thumb.png"), path);
    }

    [Fact]
    public void ResolveVideoPath_ReturnsCorrectPath()
    {
        var path = _service.ResolveVideoPath("b.mp4");
        Assert.Equal(Path.Combine(_tempDir, "videos", "b.mp4"), path);
    }

    [Fact]
    public void GenerateThumbnailFilename_WithExtension()
    {
        Assert.Equal("capture_001_thumb.png", _service.GenerateThumbnailFilename("capture_001.png"));
    }

    [Fact]
    public void GenerateThumbnailFilename_Jpg()
    {
        Assert.Equal("screenshot_thumb.jpg", _service.GenerateThumbnailFilename("screenshot.jpg"));
    }

    [Fact]
    public void GenerateThumbnailFilename_NoExtension()
    {
        Assert.Equal("noext_thumb", _service.GenerateThumbnailFilename("noext"));
    }

    [Fact]
    public void GenerateThumbnailFilename_WithSubdirectory_StripsDirectory()
    {
        Assert.Equal("capture_001_thumb.png", _service.GenerateThumbnailFilename("subdir/capture_001.png"));
    }

    [Fact]
    public void GetDirectoryPaths_ReturnCorrectPaths()
    {
        Assert.Equal(Path.Combine(_tempDir, "originals"), _service.GetOriginalsDir());
        Assert.Equal(Path.Combine(_tempDir, "edited"), _service.GetEditedDir());
        Assert.Equal(Path.Combine(_tempDir, "thumbnails"), _service.GetThumbnailsDir());
        Assert.Equal(Path.Combine(_tempDir, "videos"), _service.GetVideosDir());
    }
}
