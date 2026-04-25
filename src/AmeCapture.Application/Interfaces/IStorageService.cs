namespace AmeCapture.Application.Interfaces
{
    public interface IStorageService
    {
        public Task EnsureDirectoriesAsync();
        public string GetBasePath();
        public string ResolveOriginalPath(string filename);
        public string ResolveEditedPath(string filename);
        public string ResolveThumbnailPath(string originalFilename);
        public string ResolveVideoPath(string filename);
        public string GetOriginalsDir();
        public string GetEditedDir();
        public string GetThumbnailsDir();
        public string GetVideosDir();
        public string GenerateThumbnailFilename(string originalFilename);
    }
}
