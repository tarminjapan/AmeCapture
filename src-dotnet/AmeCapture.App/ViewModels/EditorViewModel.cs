using System.Collections.ObjectModel;
using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AmeCapture.App.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    private readonly IEditorService? _editorService;
    private readonly IStorageService? _storageService;
    private readonly IWorkspaceRepository? _workspaceRepository;
    private readonly IThumbnailService? _thumbnailService;

    private WorkspaceItem? _item;
    private readonly List<IReadOnlyList<Annotation>> _undoStack = [];
    private readonly List<IReadOnlyList<Annotation>> _redoStack = [];

    public ObservableCollection<Annotation> Annotations { get; } = [];

    [ObservableProperty]
    public partial EditorTool CurrentTool { get; set; } = EditorTool.Select;

    [ObservableProperty]
    public partial string StrokeColor { get; set; } = "#FF0000";

    [ObservableProperty]
    public partial int StrokeWidth { get; set; } = 3;

    [ObservableProperty]
    public partial double Zoom { get; set; } = 1.0;

    [ObservableProperty]
    public partial double PanX { get; set; }

    [ObservableProperty]
    public partial double PanY { get; set; }

    [ObservableProperty]
    public partial bool IsDirty { get; set; }

    [ObservableProperty]
    public partial string ImagePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    [ObservableProperty]
    public partial Annotation? PreviewAnnotation { get; set; }

    [ObservableProperty]
    public partial bool CanUndo { get; set; }

    [ObservableProperty]
    public partial bool CanRedo { get; set; }

    public event EventHandler? AnnotationsChanged;
    public event EventHandler? RequestCanvasInvalidate;

    public EditorViewModel() { }

    public EditorViewModel(
        IEditorService? editorService,
        IStorageService? storageService,
        IWorkspaceRepository? workspaceRepository,
        IThumbnailService? thumbnailService)
    {
        _editorService = editorService;
        _storageService = storageService;
        _workspaceRepository = workspaceRepository;
        _thumbnailService = thumbnailService;
    }

    public void LoadItem(WorkspaceItem item)
    {
        _item = item;
        ImagePath = item.CurrentPath;
        Annotations.Clear();
        _undoStack.Clear();
        _redoStack.Clear();
        IsDirty = false;
        UpdateUndoRedoState();
    }

    public void BeginDraw(double x, double y)
    {
        if (CurrentTool == EditorTool.Select) return;

        PreviewAnnotation = CurrentTool switch
        {
            EditorTool.Arrow => new ArrowAnnotation
            {
                StartX = x, StartY = y, EndX = x, EndY = y,
                StrokeColor = StrokeColor, StrokeWidth = StrokeWidth,
            },
            EditorTool.Rectangle => new RectangleAnnotation
            {
                X = x, Y = y, Width = 0, Height = 0,
                StrokeColor = StrokeColor, StrokeWidth = StrokeWidth,
            },
            EditorTool.Mosaic => new MosaicAnnotation
            {
                X = x, Y = y, Width = 0, Height = 0, Strength = 20,
            },
            EditorTool.Text => new TextAnnotation
            {
                X = x, Y = y,
                Text = "Text",
                FontSize = 24,
                StrokeColor = StrokeColor,
            },
            EditorTool.Crop => new CropAnnotation
            {
                X = x, Y = y, Width = 0, Height = 0,
            },
            _ => null,
        };

        RequestCanvasInvalidate?.Invoke(this, EventArgs.Empty);
    }

    public void MoveDraw(double x, double y)
    {
        if (PreviewAnnotation == null) return;

        switch (PreviewAnnotation)
        {
            case ArrowAnnotation arrow:
                PreviewAnnotation = arrow with { EndX = x, EndY = y };
                break;
            case RectangleAnnotation rect:
                PreviewAnnotation = rect with
                {
                    Width = x - rect.X, Height = y - rect.Y,
                };
                break;
            case MosaicAnnotation mosaic:
                PreviewAnnotation = mosaic with
                {
                    Width = x - mosaic.X, Height = y - mosaic.Y,
                };
                break;
            case CropAnnotation crop:
                PreviewAnnotation = crop with
                {
                    Width = x - crop.X, Height = y - crop.Y,
                };
                break;
        }

        RequestCanvasInvalidate?.Invoke(this, EventArgs.Empty);
    }

    public void EndDraw()
    {
        if (PreviewAnnotation == null) return;

        PushUndoState();
        _redoStack.Clear();

        if (PreviewAnnotation is CropAnnotation)
        {
            var existingCrop = Annotations.OfType<CropAnnotation>().FirstOrDefault();
            if (existingCrop != null)
                Annotations.Remove(existingCrop);
        }

        Annotations.Add(PreviewAnnotation);
        PreviewAnnotation = null;
        IsDirty = true;
        UpdateUndoRedoState();
        AnnotationsChanged?.Invoke(this, EventArgs.Empty);
        RequestCanvasInvalidate?.Invoke(this, EventArgs.Empty);
    }

    public void AddTextAnnotation(string text)
    {
        if (PreviewAnnotation is not TextAnnotation textAnn) return;

        PushUndoState();
        _redoStack.Clear();

        var annotation = textAnn with { Text = text };
        Annotations.Add(annotation);
        PreviewAnnotation = null;
        IsDirty = true;
        UpdateUndoRedoState();
        AnnotationsChanged?.Invoke(this, EventArgs.Empty);
        RequestCanvasInvalidate?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveAnnotation(string id)
    {
        var annotation = Annotations.FirstOrDefault(a => a.Id == id);
        if (annotation == null) return;

        PushUndoState();
        _redoStack.Clear();
        Annotations.Remove(annotation);
        IsDirty = true;
        UpdateUndoRedoState();
        AnnotationsChanged?.Invoke(this, EventArgs.Empty);
        RequestCanvasInvalidate?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Undo()
    {
        if (_undoStack.Count == 0) return;

        _redoStack.Add([.. Annotations]);
        var lastIndex = _undoStack.Count - 1;
        var previous = _undoStack[lastIndex];
        _undoStack.RemoveAt(lastIndex);
        Annotations.Clear();
        foreach (var a in previous)
            Annotations.Add(a);
        IsDirty = true;
        UpdateUndoRedoState();
        AnnotationsChanged?.Invoke(this, EventArgs.Empty);
        RequestCanvasInvalidate?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Redo()
    {
        if (_redoStack.Count == 0) return;

        _undoStack.Add([.. Annotations]);
        var lastIndex = _redoStack.Count - 1;
        var next = _redoStack[lastIndex];
        _redoStack.RemoveAt(lastIndex);
        Annotations.Clear();
        foreach (var a in next)
            Annotations.Add(a);
        IsDirty = true;
        UpdateUndoRedoState();
        AnnotationsChanged?.Invoke(this, EventArgs.Empty);
        RequestCanvasInvalidate?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SetTool(string toolName)
    {
        CurrentTool = toolName switch
        {
            "Select" => EditorTool.Select,
            "Arrow" => EditorTool.Arrow,
            "Rectangle" => EditorTool.Rectangle,
            "Text" => EditorTool.Text,
            "Mosaic" => EditorTool.Mosaic,
            "Crop" => EditorTool.Crop,
            _ => EditorTool.Select,
        };
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_editorService == null || _item == null || _storageService == null) return;

        IsSaving = true;
        try
        {
            var filename = Path.GetFileName(_item.OriginalPath);
            var outputPath = _storageService.ResolveEditedPath(filename);

            await _editorService.ApplyAnnotationsAsync(
                _item.CurrentPath, outputPath, [.. Annotations]);

            _item.CurrentPath = outputPath;
            _item.UpdatedAt = DateTime.UtcNow.ToString("o");
            await _workspaceRepository!.UpdateAsync(_item);

            if (_thumbnailService != null && _item.ThumbnailPath != null)
            {
                await _thumbnailService.GenerateThumbnailAsync(outputPath, _item.ThumbnailPath);
            }

            IsDirty = false;
            ImagePath = outputPath;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (_editorService == null || _item == null || _storageService == null) return;

        IsSaving = true;
        try
        {
            var filename = $"capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            var outputPath = _storageService.ResolveEditedPath(filename);
            var originalPath = _storageService.ResolveOriginalPath(filename);
            var thumbPath = _storageService.ResolveThumbnailPath(filename);

            await _editorService.ApplyAnnotationsAsync(
                _item.CurrentPath, outputPath, [.. Annotations]);

            File.Copy(outputPath, originalPath, overwrite: true);

            if (_thumbnailService != null)
            {
                await _thumbnailService.GenerateThumbnailAsync(outputPath, thumbPath);
            }

            var now = DateTime.UtcNow.ToString("o");
            var newItem = new WorkspaceItem
            {
                Id = Guid.NewGuid().ToString(),
                ItemType = WorkspaceItemType.Image,
                OriginalPath = originalPath,
                CurrentPath = outputPath,
                ThumbnailPath = thumbPath,
                Title = $"Edited {_item.Title}",
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _workspaceRepository!.AddAsync(newItem);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void PushUndoState()
    {
        _undoStack.Add([.. Annotations]);
        if (_undoStack.Count > 50)
            _undoStack.RemoveAt(0);
    }

    private void UpdateUndoRedoState()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
    }
}
