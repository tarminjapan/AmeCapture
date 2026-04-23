using System.Runtime.Versioning;
using AmeCapture.Application.Interfaces;

namespace AmeCapture.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public class ThumbnailService : IThumbnailService
{
    public async Task<string> GenerateThumbnailAsync(string sourcePath, string thumbnailPath)
    {
        return await Task.Run(() =>
        {
            using var img = System.Drawing.Image.FromFile(sourcePath);

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

            if (thumbW <= 0) thumbW = 1;
            if (thumbH <= 0) thumbH = 1;

            using var thumb = new System.Drawing.Bitmap(thumbW, thumbH);
            using var g = System.Drawing.Graphics.FromImage(thumb);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, 0, 0, thumbW, thumbH);

            var dir = Path.GetDirectoryName(thumbnailPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            thumb.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Png);
            return thumbnailPath;
        });
    }
}
