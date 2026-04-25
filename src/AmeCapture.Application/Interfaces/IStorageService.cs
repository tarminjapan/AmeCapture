namespace AmeCapture.Application.Interfaces;

public interface IStorageService
{
    Task EnsureDirectoriesAsync();
    string GetBasePath();
    string ResolveOriginalPath(string filename);
    string ResolveEditedPath(string filename);
    string ResolveThumbnailPath(string originalFilename);
    string ResolveVideoPath(string filename);
    string GetOriginalsDir();
    string GetEditedDir();
    string GetThumbnailsDir();
    string GetVideosDir();
    string GenerateThumbnailFilename(string originalFilename);
}
