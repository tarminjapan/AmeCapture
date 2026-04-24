using System.Runtime.Versioning;
using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;
using SkiaSharp;

namespace AmeCapture.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public class SkiaSharpEditorService : IEditorService
{
    public async Task ApplyAnnotationsAsync(
        string sourcePath, string outputPath, IReadOnlyList<Annotation> annotations)
    {
        await Task.Run(() =>
        {
            using var original = SKBitmap.Decode(sourcePath);
            if (original == null)
                throw new InvalidOperationException($"Failed to load image: {sourcePath}");

            var cropAnnotation = annotations.OfType<CropAnnotation>().FirstOrDefault();

            SKBitmap workingBitmap;
            double offsetX = 0, offsetY = 0;

            if (cropAnnotation != null)
            {
                workingBitmap = ApplyCrop(original, cropAnnotation);
                offsetX = Math.Max(0, cropAnnotation.X);
                offsetY = Math.Max(0, cropAnnotation.Y);
            }
            else
            {
                workingBitmap = original.Copy()!;
            }

            using var surface = SKSurface.Create(new SKImageInfo(workingBitmap.Width, workingBitmap.Height));
            var canvas = surface.Canvas;

            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                canvas.DrawBitmap(workingBitmap, 0, 0, paint);
            }

            foreach (var annotation in annotations)
            {
                switch (annotation)
                {
                    case ArrowAnnotation arrow:
                        DrawArrow(canvas, arrow with
                        {
                            StartX = arrow.StartX - offsetX,
                            StartY = arrow.StartY - offsetY,
                            EndX = arrow.EndX - offsetX,
                            EndY = arrow.EndY - offsetY,
                        });
                        break;
                    case RectangleAnnotation rect:
                        DrawRectangle(canvas, rect with
                        {
                            X = rect.X - offsetX,
                            Y = rect.Y - offsetY,
                        });
                        break;
                    case MosaicAnnotation mosaic:
                        ApplyMosaic(workingBitmap, mosaic with
                        {
                            X = mosaic.X - offsetX,
                            Y = mosaic.Y - offsetY,
                        });
                        canvas.DrawBitmap(workingBitmap, 0, 0);
                        break;
                    case TextAnnotation text:
                        DrawText(canvas, text with
                        {
                            X = text.X - offsetX,
                            Y = text.Y - offsetY,
                        });
                        break;
                    case CropAnnotation:
                        break;
                }
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            using var stream = File.OpenWrite(outputPath);
            data.SaveTo(stream);

            workingBitmap.Dispose();
        });
    }

    private static SKBitmap ApplyCrop(SKBitmap source, CropAnnotation crop)
    {
        var x = (int)Math.Max(0, Math.Round(crop.X));
        var y = (int)Math.Max(0, Math.Round(crop.Y));
        var x2 = (int)Math.Min(source.Width, Math.Round(crop.X + crop.Width));
        var y2 = (int)Math.Min(source.Height, Math.Round(crop.Y + crop.Height));
        var w = x2 - x;
        var h = y2 - y;
        if (w <= 0 || h <= 0)
            return source.Copy()!;

        var subset = new SKBitmap();
        source.ExtractSubset(subset, new SKRectI(x, y, x2, y2));
        return subset;
    }

    private static void DrawArrow(SKCanvas canvas, ArrowAnnotation arrow)
    {
        var color = ParseColor(arrow.StrokeColor);
        var width = Math.Max(1, arrow.StrokeWidth);

        using var linePaint = new SKPaint
        {
            Color = color,
            StrokeWidth = width,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
        };

        canvas.DrawLine(
            (float)arrow.StartX, (float)arrow.StartY,
            (float)arrow.EndX, (float)arrow.EndY,
            linePaint);

        var angle = Math.Atan2(arrow.EndY - arrow.StartY, arrow.EndX - arrow.StartX);
        var headLength = width * 4.0;
        DrawArrowhead(canvas, arrow.EndX, arrow.EndY, angle, headLength, color);
    }

    private static void DrawArrowhead(SKCanvas canvas, double tipX, double tipY,
        double angle, double size, SKColor color)
    {
        var angle1 = angle + Math.PI * 5.0 / 6.0;
        var angle2 = angle - Math.PI * 5.0 / 6.0;

        using var path = new SKPath();
        path.MoveTo((float)tipX, (float)tipY);
        path.LineTo((float)(tipX + size * Math.Cos(angle1)), (float)(tipY + size * Math.Sin(angle1)));
        path.LineTo((float)(tipX + size * Math.Cos(angle2)), (float)(tipY + size * Math.Sin(angle2)));
        path.Close();

        using var fillPaint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        canvas.DrawPath(path, fillPaint);
    }

    private static void DrawRectangle(SKCanvas canvas, RectangleAnnotation rect)
    {
        var color = ParseColor(rect.StrokeColor);
        var width = Math.Max(1, rect.StrokeWidth);

        using var paint = new SKPaint
        {
            Color = color,
            StrokeWidth = width,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Miter,
        };

        canvas.DrawRect(
            (float)rect.X, (float)rect.Y,
            (float)rect.Width, (float)rect.Height,
            paint);
    }

    private static void ApplyMosaic(SKBitmap bitmap, MosaicAnnotation mosaic)
    {
        var x0 = (int)Math.Max(0, Math.Round(mosaic.X));
        var y0 = (int)Math.Max(0, Math.Round(mosaic.Y));
        var x1 = (int)Math.Min(bitmap.Width, Math.Round(mosaic.X + mosaic.Width));
        var y1 = (int)Math.Min(bitmap.Height, Math.Round(mosaic.Y + mosaic.Height));

        if (x1 <= x0 || y1 <= y0) return;

        var blockSize = Math.Max(1, mosaic.Strength);

        for (var by = y0; by < y1; by += blockSize)
        for (var bx = x0; bx < x1; bx += blockSize)
        {
            var bw = Math.Min(blockSize, x1 - bx);
            var bh = Math.Min(blockSize, y1 - by);

            long rSum = 0, gSum = 0, bSum = 0, aSum = 0;
            var count = bw * bh;

            for (var py = by; py < by + bh; py++)
            for (var px = bx; px < bx + bw; px++)
            {
                var pixel = bitmap.GetPixel(px, py);
                rSum += pixel.Red;
                gSum += pixel.Green;
                bSum += pixel.Blue;
                aSum += pixel.Alpha;
            }

            var avgColor = new SKColor(
                (byte)(rSum / count),
                (byte)(gSum / count),
                (byte)(bSum / count),
                (byte)(aSum / count));

            for (var py = by; py < by + bh; py++)
            for (var px = bx; px < bx + bw; px++)
            {
                bitmap.SetPixel(px, py, avgColor);
            }
        }
    }

    private static void DrawText(SKCanvas canvas, TextAnnotation text)
    {
        var color = ParseColor(text.StrokeColor);

        using var font = new SKFont
        {
            Size = (float)text.FontSize,
            Typeface = SKTypeface.FromFamilyName(null),
        };

        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };

        var yOff = 0f;
        foreach (var line in text.Text.Split('\n'))
        {
            canvas.DrawText(line, (float)text.X, (float)(text.Y - text.FontSize) + yOff, font, paint);
            yOff += (float)text.FontSize;
        }
    }

    private static SKColor ParseColor(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length < 6)
            return new SKColor(255, 0, 0, 255);

        var r = byte.Parse(h[..2], System.Globalization.NumberStyles.HexNumber);
        var g = byte.Parse(h[2..4], System.Globalization.NumberStyles.HexNumber);
        var b = byte.Parse(h[4..6], System.Globalization.NumberStyles.HexNumber);
        return new SKColor(r, g, b, 255);
    }
}
