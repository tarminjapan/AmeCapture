namespace AmeCapture.Application.Interfaces
{
    public interface IThumbnailService
    {
        public Task<string> GenerateThumbnailAsync(string sourcePath, string thumbnailPath);
    }
}
