using System.Collections.ObjectModel;
using AmeCapture.Application.Interfaces;
using AmeCapture.Application.Messages;
using AmeCapture.Application.Models;
using AmeCapture.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AmeCapture.App.ViewModels;

public partial class WorkspaceViewModel : ObservableObject
{
    private readonly ICaptureOrchestrator? _captureOrchestrator;
    private readonly IWorkspaceRepository? _workspaceRepository;
    private readonly IClipboardService? _clipboardService;
    private readonly INotificationService? _notificationService;
    private readonly IMessenger? _messenger;

    public ObservableCollection<WorkspaceItem> Items { get; } = [];
    public ObservableCollection<WindowInfo> Windows { get; } = [];

    [ObservableProperty]
    public partial bool IsCapturing { get; set; }

    [ObservableProperty]
    public partial RegionCaptureInfo? RegionCaptureInfo { get; set; }

    [ObservableProperty]
    public partial bool IsWindowSelectionMode { get; set; }

    [ObservableProperty]
    public partial WorkspaceItem? SelectedItem { get; set; }

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    public event EventHandler<string>? NavigateToItemRequested;

    partial void OnSelectedItemChanged(WorkspaceItem? value)
    {
        HasSelection = value != null;
    }

    public WorkspaceViewModel() { }

    public WorkspaceViewModel(
        ICaptureOrchestrator? captureOrchestrator,
        IWorkspaceRepository? workspaceRepository,
        IClipboardService? clipboardService,
        INotificationService? notificationService,
        IMessenger? messenger)
    {
        _captureOrchestrator = captureOrchestrator;
        _workspaceRepository = workspaceRepository;
        _clipboardService = clipboardService;
        _notificationService = notificationService;
        _messenger = messenger;

        if (_messenger != null)
        {
            _messenger.Register<CaptureRequestedMessage>(this, (r, m) =>
            {
                _ = ((WorkspaceViewModel)r!).HandleCaptureRequestAsync(m.CaptureType);
            });
        }
    }

    private async Task HandleCaptureRequestAsync(string captureType)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            switch (captureType)
            {
                case "region":
                    await PrepareRegionCaptureCommand.ExecuteAsync(null);
                    break;
                case "fullscreen":
                    await CaptureFullScreenCommand.ExecuteAsync(null);
                    break;
                case "window":
                    await PrepareWindowCaptureCommand.ExecuteAsync(null);
                    break;
            }
        });
    }

    public async Task LoadItemsAsync()
    {
        if (_workspaceRepository == null) return;
        try
        {
            var items = await _workspaceRepository.GetAllAsync();
            Items.Clear();
            foreach (var item in items.OrderByDescending(i => i.CreatedAt))
                Items.Add(item);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to load workspace items");
        }
    }

    [RelayCommand]
    private async Task CaptureFullScreenAsync()
    {
        if (_captureOrchestrator == null) return;
        IsCapturing = true;
        try
        {
            var item = await _captureOrchestrator.CaptureFullScreenAsync();
            Items.Insert(0, item);
            await NotifyCaptureCompleteAsync(item);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Fullscreen capture failed");
        }
        finally
        {
            IsCapturing = false;
        }
    }

    public async Task CaptureWindowAsync(nint hwnd)
    {
        if (_captureOrchestrator == null) return;
        IsCapturing = true;
        try
        {
            var item = await _captureOrchestrator.CaptureWindowAsync(hwnd);
            Items.Insert(0, item);
            IsWindowSelectionMode = false;
            await NotifyCaptureCompleteAsync(item);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Window capture failed");
        }
        finally
        {
            IsCapturing = false;
        }
    }

    [RelayCommand]
    private async Task PrepareRegionCaptureAsync()
    {
        if (_captureOrchestrator == null) return;
        IsCapturing = true;
        try
        {
            RegionCaptureInfo = await _captureOrchestrator.PrepareRegionCaptureAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Region capture preparation failed");
        }
        finally
        {
            IsCapturing = false;
        }
    }

    public async Task FinalizeRegionCaptureAsync(CaptureRegion region)
    {
        if (_captureOrchestrator == null || RegionCaptureInfo == null) return;
        IsCapturing = true;
        try
        {
            var item = await _captureOrchestrator.FinalizeRegionCaptureAsync(
                RegionCaptureInfo.TempPath, region);
            Items.Insert(0, item);
            RegionCaptureInfo = null;
            await NotifyCaptureCompleteAsync(item);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Region capture finalization failed");
        }
        finally
        {
            IsCapturing = false;
        }
    }

    public async Task CancelRegionCaptureAsync()
    {
        if (_captureOrchestrator == null || RegionCaptureInfo == null) return;
        try
        {
            await _captureOrchestrator.CancelRegionCaptureAsync(RegionCaptureInfo.TempPath);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Region capture cancellation failed");
        }
        RegionCaptureInfo = null;
    }

    [RelayCommand]
    private async Task PrepareWindowCaptureAsync()
    {
        if (_captureOrchestrator == null) return;
        IsCapturing = true;
        try
        {
            var windows = await _captureOrchestrator.PrepareWindowCaptureAsync();
            Windows.Clear();
            foreach (var w in windows)
                Windows.Add(w);
            IsWindowSelectionMode = true;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Window capture preparation failed");
        }
        finally
        {
            IsCapturing = false;
        }
    }

    public void CancelWindowCapture()
    {
        IsWindowSelectionMode = false;
    }

    [RelayCommand]
    private async Task CopyToClipboardAsync(WorkspaceItem? item)
    {
        if (_clipboardService == null) return;
        item ??= SelectedItem;
        if (item == null) return;

        try
        {
            var path = item.CurrentPath;
            if (!File.Exists(path)) return;

            using var image = System.Drawing.Image.FromFile(path);
            await _clipboardService.SetImageAsync(image);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to copy image to clipboard for item {ItemId}", item.Id);
        }
    }

    [RelayCommand]
    private async Task DeleteItemAsync(WorkspaceItem? item)
    {
        if (_workspaceRepository == null) return;
        item ??= SelectedItem;
        if (item == null) return;

        try
        {
            await _workspaceRepository.DeleteAsync(item.Id);
            Items.Remove(item);
            if (SelectedItem == item)
                SelectedItem = null;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to delete workspace item {ItemId}", item.Id);
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(WorkspaceItem? item)
    {
        if (_workspaceRepository == null) return;
        item ??= SelectedItem;
        if (item == null) return;

        try
        {
            item.IsFavorite = !item.IsFavorite;
            item.UpdatedAt = DateTime.UtcNow.ToString("o");
            await _workspaceRepository.UpdateAsync(item);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to toggle favorite for item {ItemId}", item.Id);
        }
    }

    private async Task NotifyCaptureCompleteAsync(WorkspaceItem item)
    {
        if (_notificationService == null) return;

        try
        {
            await _notificationService.ShowNotificationAsync(
                "キャプチャ完了",
                $"{item.Title} をクリップボードにコピーしました。",
                () =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        NavigateToItemRequested?.Invoke(this, item.Id);
                    });
                });
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to show capture completion notification");
        }
    }
}
