using System.Collections.ObjectModel;
using AmeCapture.Application.Interfaces;
using AmeCapture.Application.Models;
using AmeCapture.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AmeCapture.App.ViewModels;

public partial class WorkspaceViewModel : ObservableObject
{
    private readonly ICaptureOrchestrator? _captureOrchestrator;
    private readonly IWorkspaceRepository? _workspaceRepository;

    public ObservableCollection<WorkspaceItem> Items { get; } = [];
    public ObservableCollection<WindowInfo> Windows { get; } = [];

    [ObservableProperty]
    public partial bool IsCapturing { get; set; }

    [ObservableProperty]
    public partial RegionCaptureInfo? RegionCaptureInfo { get; set; }

    [ObservableProperty]
    public partial bool IsWindowSelectionMode { get; set; }

    public WorkspaceViewModel() { }

    public WorkspaceViewModel(
        ICaptureOrchestrator? captureOrchestrator,
        IWorkspaceRepository? workspaceRepository)
    {
        _captureOrchestrator = captureOrchestrator;
        _workspaceRepository = workspaceRepository;
    }

    public async Task LoadItemsAsync()
    {
        if (_workspaceRepository == null) return;
        var items = await _workspaceRepository.GetAllAsync();
        Items.Clear();
        foreach (var item in items.OrderByDescending(i => i.CreatedAt))
            Items.Add(item);
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
        }
        finally
        {
            IsCapturing = false;
        }
    }

    public async Task CancelRegionCaptureAsync()
    {
        if (_captureOrchestrator == null || RegionCaptureInfo == null) return;
        await _captureOrchestrator.CancelRegionCaptureAsync(RegionCaptureInfo.TempPath);
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
        finally
        {
            IsCapturing = false;
        }
    }

    public void CancelWindowCapture()
    {
        IsWindowSelectionMode = false;
    }
}
