using AmeCapture.Application.Interfaces;

namespace AmeCapture.Infrastructure.Services;

public class StorageService : IStorageService
{
    private const string DirOriginals = "originals";
    private const string DirEdited = "edited";
    private const string DirThumbnails = "thumbnails";
    private const string DirVideos = "videos";

    private readonly string _basePath;

    public StorageService(string basePath)
    {
        _basePath = basePath;
    }

    public string GetBasePath() => _basePath;

    public async Task EnsureDirectoriesAsync()
    {
        var dirs = new[] { DirOriginals, DirEdited, DirThumbnails, DirVideos };
        foreach (var dir in dirs)
        {
            var path = Path.Combine(_basePath, dir);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        await Task.CompletedTask;
    }

    public string ResolveOriginalPath(string filename) => Path.Combine(_basePath, DirOriginals, filename);
    public string ResolveEditedPath(string filename) => Path.Combine(_basePath, DirEdited, filename);
    public string ResolveThumbnailPath(string originalFilename) =>
        Path.Combine(_basePath, DirThumbnails, GenerateThumbnailFilename(originalFilename));
    public string ResolveVideoPath(string filename) => Path.Combine(_basePath, DirVideos, filename);

    public string GetOriginalsDir() => Path.Combine(_basePath, DirOriginals);
    public string GetEditedDir() => Path.Combine(_basePath, DirEdited);
    public string GetThumbnailsDir() => Path.Combine(_basePath, DirThumbnails);
    public string GetVideosDir() => Path.Combine(_basePath, DirVideos);

    public string GenerateThumbnailFilename(string originalFilename)
    {
        var fileName = Path.GetFileName(originalFilename);
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        if (!string.IsNullOrEmpty(extension))
        {
            return $"{nameWithoutExtension}_thumb{extension}";
        }

        return $"{nameWithoutExtension}_thumb";
    }
}
