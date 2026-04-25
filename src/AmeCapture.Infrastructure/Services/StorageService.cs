using AmeCapture.Application.Interfaces;

namespace AmeCapture.Infrastructure.Services
{
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
            Serilog.Log.Debug("StorageService initialized with basePath={BasePath}", basePath);
        }

        public string GetBasePath()
        {
            return _basePath;
        }

        public async Task EnsureDirectoriesAsync()
        {
            Serilog.Log.Debug("StorageService.EnsureDirectoriesAsync: basePath={BasePath}", _basePath);
            string[] dirs = new[] { DirOriginals, DirEdited, DirThumbnails, DirVideos };
            foreach (string? dir in dirs)
            {
                string path = Path.Combine(_basePath, dir);
                if (!Directory.Exists(path))
                {
                    Serilog.Log.Debug("StorageService: creating directory {Path}", path);
                    _ = Directory.CreateDirectory(path);
                }
            }

            await Task.CompletedTask;
        }

        public string ResolveOriginalPath(string filename)
        {
            return Path.Combine(_basePath, DirOriginals, filename);
        }

        public string ResolveEditedPath(string filename)
        {
            return Path.Combine(_basePath, DirEdited, filename);
        }

        public string ResolveThumbnailPath(string originalFilename)
        {
            return Path.Combine(_basePath, DirThumbnails, GenerateThumbnailFilename(originalFilename));
        }

        public string ResolveVideoPath(string filename)
        {
            return Path.Combine(_basePath, DirVideos, filename);
        }

        public string GetOriginalsDir()
        {
            return Path.Combine(_basePath, DirOriginals);
        }

        public string GetEditedDir()
        {
            return Path.Combine(_basePath, DirEdited);
        }

        public string GetThumbnailsDir()
        {
            return Path.Combine(_basePath, DirThumbnails);
        }

        public string GetVideosDir()
        {
            return Path.Combine(_basePath, DirVideos);
        }

        public string GenerateThumbnailFilename(string originalFilename)
        {
            string fileName = Path.GetFileName(originalFilename);
            string extension = Path.GetExtension(fileName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            return !string.IsNullOrEmpty(extension) ? $"{nameWithoutExtension}_thumb{extension}" : $"{nameWithoutExtension}_thumb";
        }
    }
}
