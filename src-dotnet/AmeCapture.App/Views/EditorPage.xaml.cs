using AmeCapture.App.ViewModels;
using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace AmeCapture.App.Views;

public partial class EditorPage : ContentPage, IQueryAttributable
{
    private readonly EditorViewModel _viewModel;
    private readonly IWorkspaceRepository _workspaceRepository;
    private SKBitmap? _sourceBitmap;
    private bool _isDragging;

    public EditorPage(EditorViewModel viewModel, IWorkspaceRepository workspaceRepository)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _workspaceRepository = workspaceRepository;
        BindingContext = viewModel;

        _viewModel.AnnotationsChanged += (_, _) => CanvasView.InvalidateSurface();
        _viewModel.RequestCanvasInvalidate += (_, _) => CanvasView.InvalidateSurface();
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EditorViewModel.ImagePath))
                LoadImage();
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("itemId", out var itemIdObj) && itemIdObj is string itemId)
        {
            _ = LoadItemAsync(itemId);
        }
    }

    private async Task LoadItemAsync(string itemId)
    {
        var item = await _workspaceRepository.GetByIdAsync(itemId);
        if (item != null)
            _viewModel.LoadItem(item);
    }

    private void LoadImage()
    {
        _sourceBitmap?.Dispose();
        _sourceBitmap = null;

        if (string.IsNullOrEmpty(_viewModel.ImagePath) || !File.Exists(_viewModel.ImagePath))
            return;

        _sourceBitmap = SKBitmap.Decode(_viewModel.ImagePath);
        CanvasView.InvalidateSurface();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        if (_sourceBitmap == null) return;

        var scaleX = (float)e.Info.Width / _sourceBitmap.Width;
        var scaleY = (float)e.Info.Height / _sourceBitmap.Height;
        var scale = Math.Min(scaleX, scaleY);

        canvas.Save();
        canvas.Scale(scale, scale);

        using (var paint = new SKPaint { IsAntialias = true })
        {
            canvas.DrawBitmap(_sourceBitmap, 0, 0, paint);
        }

        foreach (var annotation in _viewModel.Annotations)
        {
            DrawAnnotation(canvas, annotation);
        }

        if (_viewModel.PreviewAnnotation != null)
        {
            DrawAnnotation(canvas, _viewModel.PreviewAnnotation);
        }

        canvas.Restore();
    }

    private static void DrawAnnotation(SKCanvas canvas, Annotation annotation)
    {
        switch (annotation)
        {
            case ArrowAnnotation arrow:
                DrawArrow(canvas, arrow);
                break;
            case RectangleAnnotation rect:
                DrawRectangle(canvas, rect);
                break;
            case MosaicAnnotation mosaic:
                DrawMosaic(canvas, mosaic);
                break;
            case TextAnnotation text:
                DrawText(canvas, text);
                break;
            case CropAnnotation crop:
                DrawCrop(canvas, crop);
                break;
        }
    }

    private static void DrawArrow(SKCanvas canvas, ArrowAnnotation arrow)
    {
        var color = ParseColor(arrow.StrokeColor);
        using var paint = new SKPaint
        {
            Color = color,
            StrokeWidth = Math.Max(1, arrow.StrokeWidth),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
        };

        canvas.DrawLine(
            (float)arrow.StartX, (float)arrow.StartY,
            (float)arrow.EndX, (float)arrow.EndY,
            paint);

        var angle = Math.Atan2(arrow.EndY - arrow.StartY, arrow.EndX - arrow.StartX);
        var headLength = arrow.StrokeWidth * 4.0;
        var angle1 = angle + Math.PI * 5.0 / 6.0;
        var angle2 = angle - Math.PI * 5.0 / 6.0;

        using var path = new SKPath();
        path.MoveTo((float)arrow.EndX, (float)arrow.EndY);
        path.LineTo((float)(arrow.EndX + headLength * Math.Cos(angle1)),
                     (float)(arrow.EndY + headLength * Math.Sin(angle1)));
        path.LineTo((float)(arrow.EndX + headLength * Math.Cos(angle2)),
                     (float)(arrow.EndY + headLength * Math.Sin(angle2)));
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
        using var paint = new SKPaint
        {
            Color = color,
            StrokeWidth = Math.Max(1, rect.StrokeWidth),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
        };

        canvas.DrawRect(
            (float)rect.X, (float)rect.Y,
            (float)rect.Width, (float)rect.Height,
            paint);
    }

    private static void DrawMosaic(SKCanvas canvas, MosaicAnnotation mosaic)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(128, 128, 128, 160),
            IsAntialias = false,
            Style = SKPaintStyle.Fill,
        };
        canvas.DrawRect(
            (float)mosaic.X, (float)mosaic.Y,
            (float)mosaic.Width, (float)mosaic.Height,
            paint);

        using var strokePaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 200),
            StrokeWidth = 1,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash([4, 4], 0),
        };
        canvas.DrawRect(
            (float)mosaic.X, (float)mosaic.Y,
            (float)mosaic.Width, (float)mosaic.Height,
            strokePaint);
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

    private static void DrawCrop(SKCanvas canvas, CropAnnotation crop)
    {
        using var dimPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 100),
            Style = SKPaintStyle.Fill,
        };

        canvas.DrawRect(0, 0, 99999, (float)crop.Y, dimPaint);
        canvas.DrawRect(0, (float)(crop.Y + crop.Height), 99999, 99999, dimPaint);
        canvas.DrawRect(0, (float)crop.Y, (float)crop.X, (float)crop.Height, dimPaint);
        canvas.DrawRect((float)(crop.X + crop.Width), (float)crop.Y, 99999, (float)crop.Height, dimPaint);

        using var borderPaint = new SKPaint
        {
            Color = SKColors.White,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
        };
        canvas.DrawRect(
            (float)crop.X, (float)crop.Y,
            (float)crop.Width, (float)crop.Height,
            borderPaint);
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

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (_sourceBitmap == null) return;

        var scaleX = (float)CanvasView.CanvasSize.Width / _sourceBitmap.Width;
        var scaleY = (float)CanvasView.CanvasSize.Height / _sourceBitmap.Height;
        var scale = Math.Min(scaleX, scaleY);

        var x = e.Location.X / scale;
        var y = e.Location.Y / scale;

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _isDragging = true;
                _viewModel.BeginDraw(x, y);
                e.Handled = true;
                break;
            case SKTouchAction.Moved when _isDragging:
                _viewModel.MoveDraw(x, y);
                e.Handled = true;
                break;
            case SKTouchAction.Released when _isDragging:
                _isDragging = false;
                _viewModel.EndDraw();
                e.Handled = true;
                break;
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        _sourceBitmap?.Dispose();
        _sourceBitmap = null;
        base.OnNavigatedFrom(args);
    }
}
