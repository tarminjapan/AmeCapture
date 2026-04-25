using System.Runtime.Versioning;
using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;
using AmeCapture.Infrastructure.Services;

namespace AmeCapture.Tests.Integration;

[SupportedOSPlatform("windows")]
public class EditorServiceTests : IAsyncLifetime
{
    private readonly string _tempDir;
    private readonly IEditorService _editorService;

    public EditorServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"amecapture_editor_test_{Guid.NewGuid():N}");
        _editorService = new SkiaSharpEditorService();
    }

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_tempDir);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
        return Task.CompletedTask;
    }

    private string CreateTestImage(int width = 100, int height = 100)
    {
        var path = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.png");
        using var bitmap = new SkiaSharp.SKBitmap(width, height);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        canvas.Clear(new SkiaSharp.SKColor(200, 200, 200, 255));
        canvas.Flush();

        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);

        return path;
    }

    [Fact]
    public async Task ApplyAnnotationsAsync_WithNoAnnotations_CopiesImage()
    {
        var sourcePath = CreateTestImage();
        var outputPath = Path.Combine(_tempDir, "output.png");

        await _editorService.ApplyAnnotationsAsync(sourcePath, outputPath, []);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ApplyAnnotationsAsync_WithArrow_CreatesOutput()
    {
        var sourcePath = CreateTestImage();
        var outputPath = Path.Combine(_tempDir, "output_arrow.png");

        var annotations = new List<Annotation>
        {
            new ArrowAnnotation
            {
                StartX = 10, StartY = 10,
                EndX = 80, EndY = 80,
                StrokeColor = "#FF0000", StrokeWidth = 3,
            },
        };

        await _editorService.ApplyAnnotationsAsync(sourcePath, outputPath, annotations);

        Assert.True(File.Exists(outputPath));
        using var result = SkiaSharp.SKBitmap.Decode(outputPath);
        Assert.NotNull(result);
        Assert.Equal(100, result.Width);
        Assert.Equal(100, result.Height);
    }

    [Fact]
    public async Task ApplyAnnotationsAsync_WithRectangle_CreatesOutput()
    {
        var sourcePath = CreateTestImage();
        var outputPath = Path.Combine(_tempDir, "output_rect.png");

        var annotations = new List<Annotation>
        {
            new RectangleAnnotation
            {
                X = 10, Y = 10, Width = 80, Height = 80,
                StrokeColor = "#00FF00", StrokeWidth = 2,
            },
        };

        await _editorService.ApplyAnnotationsAsync(sourcePath, outputPath, annotations);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ApplyAnnotationsAsync_WithMosaic_CreatesOutput()
    {
        var sourcePath = CreateTestImage();
        var outputPath = Path.Combine(_tempDir, "output_mosaic.png");

        var annotations = new List<Annotation>
        {
            new MosaicAnnotation
            {
                X = 10, Y = 10, Width = 50, Height = 50, Strength = 10,
            },
        };

        await _editorService.ApplyAnnotationsAsync(sourcePath, outputPath, annotations);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ApplyAnnotationsAsync_WithText_CreatesOutput()
    {
        var sourcePath = CreateTestImage();
        var outputPath = Path.Combine(_tempDir, "output_text.png");

        var annotations = new List<Annotation>
        {
            new TextAnnotation
            {
                X = 10, Y = 50,
                Text = "Hello",
                FontSize = 16,
                StrokeColor = "#000000",
            },
        };

        await _editorService.ApplyAnnotationsAsync(sourcePath, outputPath, annotations);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ApplyAnnotationsAsync_WithCrop_CreatesCroppedOutput()
    {
        var sourcePath = CreateTestImage(200, 200);
        var outputPath = Path.Combine(_tempDir, "output_crop.png");

        var annotations = new List<Annotation>
        {
            new CropAnnotation
            {
                X = 50, Y = 50, Width = 100, Height = 100,
            },
        };

        await _editorService.ApplyAnnotationsAsync(sourcePath, outputPath, annotations);

        Assert.True(File.Exists(outputPath));
        using var result = SkiaSharp.SKBitmap.Decode(outputPath);
        Assert.NotNull(result);
        Assert.Equal(100, result.Width);
        Assert.Equal(100, result.Height);
    }

    [Fact]
    public async Task ApplyAnnotationsAsync_WithCropAndArrow_AdjustsCoordinates()
    {
        var sourcePath = CreateTestImage(200, 200);
        var outputPath = Path.Combine(_tempDir, "output_crop_arrow.png");

        var annotations = new List<Annotation>
        {
            new CropAnnotation
            {
                X = 50, Y = 50, Width = 100, Height = 100,
            },
            new ArrowAnnotation
            {
                StartX = 60, StartY = 60,
                EndX = 140, EndY = 140,
                StrokeColor = "#FF0000", StrokeWidth = 2,
            },
        };

        await _editorService.ApplyAnnotationsAsync(sourcePath, outputPath, annotations);

        Assert.True(File.Exists(outputPath));
        using var result = SkiaSharp.SKBitmap.Decode(outputPath);
        Assert.Equal(100, result.Width);
        Assert.Equal(100, result.Height);
    }

    [Fact]
    public async Task ApplyAnnotationsAsync_MultipleAnnotations_CreatesOutput()
    {
        var sourcePath = CreateTestImage();
        var outputPath = Path.Combine(_tempDir, "output_multi.png");

        var annotations = new List<Annotation>
        {
            new ArrowAnnotation
            {
                StartX = 5, StartY = 5, EndX = 50, EndY = 50,
                StrokeColor = "#FF0000", StrokeWidth = 2,
            },
            new RectangleAnnotation
            {
                X = 20, Y = 20, Width = 60, Height = 60,
                StrokeColor = "#0000FF", StrokeWidth = 2,
            },
            new TextAnnotation
            {
                X = 30, Y = 30, Text = "Test", FontSize = 12,
                StrokeColor = "#000000",
            },
        };

        await _editorService.ApplyAnnotationsAsync(sourcePath, outputPath, annotations);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ApplyAnnotationsAsync_InvalidSourcePath_ThrowsException()
    {
        var outputPath = Path.Combine(_tempDir, "output.png");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _editorService.ApplyAnnotationsAsync("nonexistent.png", outputPath, []));
    }
}
