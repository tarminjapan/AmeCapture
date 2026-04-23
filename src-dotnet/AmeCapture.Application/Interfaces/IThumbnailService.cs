namespace AmeCapture.Application.Interfaces;

public interface IThumbnailService
{
    Task<string> GenerateThumbnailAsync(string sourcePath, string thumbnailPath);
}
