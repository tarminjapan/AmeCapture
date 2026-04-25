using AmeCapture.Application.Interfaces;

namespace AmeCapture.Infrastructure.Services
{
    public class ThumbnailService : IThumbnailService
    {
        public async Task<string> GenerateThumbnailAsync(string sourcePath, string thumbnailPath)
        {
            Serilog.Log.Debug("ThumbnailService.GenerateThumbnailAsync: source={Source}, thumb={Thumb}", sourcePath, thumbnailPath);
            return await Task.Run(() =>
            {
                using var img = System.Drawing.Image.FromFile(sourcePath);
                Serilog.Log.Debug("ThumbnailService: source image {Width}x{Height}", img.Width, img.Height);

                int maxDim = 256;
                int thumbW, thumbH;
                if (img.Width > img.Height)
                {
                    thumbW = maxDim;
                    thumbH = (int)(img.Height * (double)maxDim / img.Width);
                }
                else
                {
                    thumbH = maxDim;
                    thumbW = (int)(img.Width * (double)maxDim / img.Height);
                }

                if (thumbW <= 0)
                {
                    thumbW = 1;
                }

                if (thumbH <= 0)
                {
                    thumbH = 1;
                }

                using var thumb = new Bitmap(thumbW, thumbH);
                using var g = Graphics.FromImage(thumb);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, thumbW, thumbH);

                string? dir = Path.GetDirectoryName(thumbnailPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    _ = Directory.CreateDirectory(dir);
                }

                thumb.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Png);
                Serilog.Log.Debug("ThumbnailService: thumbnail saved {ThumbW}x{ThumbH} to {ThumbPath}", thumbW, thumbH, thumbnailPath);
                return thumbnailPath;
            });
        }
    }
}
