using System.Runtime.InteropServices;
using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;
using SkiaSharp;

namespace AmeCapture.Infrastructure.Services
{
    public class SkiaSharpEditorService : IEditorService
    {
        public async Task ApplyAnnotationsAsync(
            string sourcePath, string outputPath, IReadOnlyList<Annotation> annotations)
        {
            Serilog.Log.Debug("SkiaSharpEditorService.ApplyAnnotationsAsync: source={Source}, output={Output}, annotations={Count}", sourcePath, outputPath, annotations.Count);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            await Task.Run(() =>
            {
                using SKBitmap original = SKBitmap.Decode(sourcePath) ?? throw new InvalidOperationException($"Failed to load image: {sourcePath}");

                Serilog.Log.Debug("SkiaSharpEditorService: source image decoded, {Width}x{Height}", original.Width, original.Height);

                CropAnnotation? cropAnnotation = annotations.OfType<CropAnnotation>().FirstOrDefault();

                SKBitmap workingBitmap;
                double offsetX = 0, offsetY = 0;

                if (cropAnnotation != null)
                {
                    Serilog.Log.Debug("SkiaSharpEditorService: applying crop ({X},{Y},{W},{H})", cropAnnotation.X, cropAnnotation.Y, cropAnnotation.Width, cropAnnotation.Height);
                    workingBitmap = ApplyCrop(original, cropAnnotation);
                    offsetX = Math.Max(0, cropAnnotation.X);
                    offsetY = Math.Max(0, cropAnnotation.Y);
                }
                else
                {
                    workingBitmap = original.Copy()!;
                }

                using var surface = SKSurface.Create(new SKImageInfo(workingBitmap.Width, workingBitmap.Height));
                SKCanvas canvas = surface.Canvas;

                using (var paint = new SKPaint())
                {
                    paint.IsAntialias = true;
                    canvas.DrawBitmap(workingBitmap, 0, 0, paint);
                }

                foreach (Annotation annotation in annotations)
                {
                    Serilog.Log.Debug("SkiaSharpEditorService: applying annotation type={Type}", annotation.GetType().Name);
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
                        default:
                            break;
                    }
                }

                using SKImage image = surface.Snapshot();
                using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
                string? dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    _ = Directory.CreateDirectory(dir);
                }

                using FileStream stream = File.Create(outputPath);
                data.SaveTo(stream);

                workingBitmap.Dispose();

                sw.Stop();
                Serilog.Log.Debug("SkiaSharpEditorService.ApplyAnnotationsAsync completed in {Elapsed}ms", sw.ElapsedMilliseconds);
            });
        }

        private static SKBitmap ApplyCrop(SKBitmap source, CropAnnotation crop)
        {
            int x = (int)Math.Max(0, Math.Round(crop.X));
            int y = (int)Math.Max(0, Math.Round(crop.Y));
            int x2 = (int)Math.Min(source.Width, Math.Round(crop.X + crop.Width));
            int y2 = (int)Math.Min(source.Height, Math.Round(crop.Y + crop.Height));
            int w = x2 - x;
            int h = y2 - y;
            if (w <= 0 || h <= 0)
            {
                return source.Copy()!;
            }

            var subset = new SKBitmap();
            _ = source.ExtractSubset(subset, new SKRectI(x, y, x2, y2));
            return subset;
        }

        private static void DrawArrow(SKCanvas canvas, ArrowAnnotation arrow)
        {
            SKColor color = ParseColor(arrow.StrokeColor);
            int width = Math.Max(1, arrow.StrokeWidth);

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

            double angle = Math.Atan2(arrow.EndY - arrow.StartY, arrow.EndX - arrow.StartX);
            double headLength = width * 4.0;
            DrawArrowhead(canvas, arrow.EndX, arrow.EndY, angle, headLength, color);
        }

        private static void DrawArrowhead(SKCanvas canvas, double tipX, double tipY,
            double angle, double size, SKColor color)
        {
            double angle1 = angle + (Math.PI * 5.0 / 6.0);
            double angle2 = angle - (Math.PI * 5.0 / 6.0);

            using var path = new SKPath();
            path.MoveTo((float)tipX, (float)tipY);
            path.LineTo((float)(tipX + (size * Math.Cos(angle1))), (float)(tipY + (size * Math.Sin(angle1))));
            path.LineTo((float)(tipX + (size * Math.Cos(angle2))), (float)(tipY + (size * Math.Sin(angle2))));
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
            SKColor color = ParseColor(rect.StrokeColor);
            int width = Math.Max(1, rect.StrokeWidth);

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
            int x0 = (int)Math.Max(0, Math.Round(mosaic.X));
            int y0 = (int)Math.Max(0, Math.Round(mosaic.Y));
            int x1 = (int)Math.Min(bitmap.Width, Math.Round(mosaic.X + mosaic.Width));
            int y1 = (int)Math.Min(bitmap.Height, Math.Round(mosaic.Y + mosaic.Height));

            if (x1 <= x0 || y1 <= y0)
            {
                return;
            }

            int blockSize = Math.Max(1, mosaic.Strength);
            int bpp = bitmap.BytesPerPixel;
            if (bpp == 0)
            {
                return;
            }

            nint basePtr = bitmap.GetPixels();
            if (basePtr == IntPtr.Zero)
            {
                return;
            }

            int rowBytes = bitmap.RowBytes;

            for (int by = y0; by < y1; by += blockSize)
            {
                for (int bx = x0; bx < x1; bx += blockSize)
                {
                    int bw = Math.Min(blockSize, x1 - bx);
                    int bh = Math.Min(blockSize, y1 - by);

                    long rSum = 0, gSum = 0, bSum = 0, aSum = 0;
                    int count = bw * bh;

                    for (int py = by; py < by + bh; py++)
                    {
                        int rowOff = (py * rowBytes) + (bx * bpp);
                        for (int px = 0; px < bw; px++)
                        {
                            int off = rowOff + (px * bpp);
                            bSum += Marshal.ReadByte(basePtr, off);
                            gSum += Marshal.ReadByte(basePtr, off + 1);
                            rSum += Marshal.ReadByte(basePtr, off + 2);
                            aSum += Marshal.ReadByte(basePtr, off + 3);
                        }
                    }

                    byte avgB = (byte)(bSum / count);
                    byte avgG = (byte)(gSum / count);
                    byte avgR = (byte)(rSum / count);
                    byte avgA = (byte)(aSum / count);

                    for (int py = by; py < by + bh; py++)
                    {
                        int rowOff = (py * rowBytes) + (bx * bpp);
                        for (int px = 0; px < bw; px++)
                        {
                            int off = rowOff + (px * bpp);
                            Marshal.WriteByte(basePtr, off, avgB);
                            Marshal.WriteByte(basePtr, off + 1, avgG);
                            Marshal.WriteByte(basePtr, off + 2, avgR);
                            Marshal.WriteByte(basePtr, off + 3, avgA);
                        }
                    }
                }
            }
        }

        private static void DrawText(SKCanvas canvas, TextAnnotation text)
        {
            SKColor color = ParseColor(text.StrokeColor);

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

            float yOff = 0f;
            foreach (string line in text.Text.Split('\n'))
            {
                canvas.DrawText(line, (float)text.X, (float)(text.Y - text.FontSize) + yOff, font, paint);
                yOff += (float)text.FontSize;
            }
        }

        private static SKColor ParseColor(string hex)
        {
            string h = hex.TrimStart('#');
            if (h.Length < 6)
            {
                return new SKColor(255, 0, 0, 255);
            }

            byte r = byte.Parse(h[..2], System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(h[2..4], System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(h[4..6], System.Globalization.NumberStyles.HexNumber);
            return new SKColor(r, g, b, 255);
        }
    }
}
