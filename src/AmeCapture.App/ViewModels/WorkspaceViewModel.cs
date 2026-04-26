using System.Collections.ObjectModel;
using AmeCapture.Application.Interfaces;
using AmeCapture.Application.Messages;
using AmeCapture.Application.Models;
using AmeCapture.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AmeCapture.App.ViewModels
{
    public partial class WorkspaceViewModel : ObservableObject
    {
        private readonly ICaptureOrchestrator? _captureOrchestrator;
        private readonly IWorkspaceRepository? _workspaceRepository;
        private readonly IClipboardService? _clipboardService;
        private readonly INotificationService? _notificationService;
        private readonly IMessenger? _messenger;

        public ObservableCollection<WorkspaceItem> Items { get; } = [];
        [ObservableProperty]
        public partial bool IsCapturing { get; set; }

        [ObservableProperty]
        public partial RegionCaptureInfo? RegionCaptureInfo { get; set; }

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

            _messenger?.Register<CaptureRequestedMessage>(this, (r, m) => _ = ((WorkspaceViewModel)r!).HandleCaptureRequestAsync(m.CaptureType));
        }

        private async Task HandleCaptureRequestAsync(string captureType)
        {
            Serilog.Log.Debug("WorkspaceViewModel.HandleCaptureRequestAsync: captureType={CaptureType}", captureType);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (captureType == "region")
                {
                    await PrepareRegionCaptureCommand.ExecuteAsync(null);
                }
            });
        }

        public async Task LoadItemsAsync()
        {
            if (_workspaceRepository == null)
            {
                return;
            }

            Serilog.Log.Debug("WorkspaceViewModel.LoadItemsAsync started");
            try
            {
                IReadOnlyList<WorkspaceItem> items = await _workspaceRepository.GetAllAsync();
                Items.Clear();
                foreach (WorkspaceItem item in items.OrderByDescending(i => i.CreatedAt))
                {
                    Items.Add(item);
                }

                Serilog.Log.Debug("WorkspaceViewModel.LoadItemsAsync: loaded {Count} items", Items.Count);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to load workspace items");
            }
        }

        [RelayCommand]
        private async Task PrepareRegionCaptureAsync()
        {
            if (_captureOrchestrator == null)
            {
                return;
            }

            Serilog.Log.Debug("WorkspaceViewModel.PrepareRegionCaptureAsync started");
            IsCapturing = true;
            try
            {
                RegionCaptureInfo = await _captureOrchestrator.PrepareRegionCaptureAsync();
                Serilog.Log.Debug("WorkspaceViewModel: region capture prepared, ScreenSize={Width}x{Height}", RegionCaptureInfo.ScreenWidth, RegionCaptureInfo.ScreenHeight);
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
            if (_captureOrchestrator == null || RegionCaptureInfo == null)
            {
                return;
            }

            Serilog.Log.Debug("WorkspaceViewModel.FinalizeRegionCaptureAsync started, region=({X},{Y},{W},{H})", region.X, region.Y, region.Width, region.Height);
            IsCapturing = true;
            try
            {
                WorkspaceItem item = await _captureOrchestrator.FinalizeRegionCaptureAsync(
                    RegionCaptureInfo.TempPath, region);
                Items.Insert(0, item);
                RegionCaptureInfo = null;
                Serilog.Log.Debug("WorkspaceViewModel: region capture finalized, ItemId={ItemId}", item.Id);
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
            if (_captureOrchestrator == null || RegionCaptureInfo == null)
            {
                return;
            }

            Serilog.Log.Debug("WorkspaceViewModel.CancelRegionCaptureAsync");
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
        private async Task CopyToClipboardAsync(WorkspaceItem? item)
        {
            if (_clipboardService == null)
            {
                return;
            }

            item ??= SelectedItem;
            if (item == null)
            {
                return;
            }

            Serilog.Log.Debug("WorkspaceViewModel.CopyToClipboardAsync: ItemId={ItemId}", item.Id);
            try
            {
                string path = item.CurrentPath;
                if (!File.Exists(path))
                {
                    return;
                }

                using var image = System.Drawing.Image.FromFile(path);
                await _clipboardService.SetImageAsync(image);
                Serilog.Log.Debug("WorkspaceViewModel: image copied to clipboard for ItemId={ItemId}", item.Id);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to copy image to clipboard for item {ItemId}", item.Id);
            }
        }

        [RelayCommand]
        private async Task DeleteItemAsync(WorkspaceItem? item)
        {
            if (_workspaceRepository == null)
            {
                return;
            }

            item ??= SelectedItem;
            if (item == null)
            {
                return;
            }

            Serilog.Log.Debug("WorkspaceViewModel.DeleteItemAsync: ItemId={ItemId}", item.Id);
            try
            {
                await _workspaceRepository.DeleteAsync(item.Id);
                _ = Items.Remove(item);
                if (SelectedItem == item)
                {
                    SelectedItem = null;
                }

                Serilog.Log.Debug("WorkspaceViewModel: item deleted, ItemId={ItemId}", item.Id);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to delete workspace item {ItemId}", item.Id);
            }
        }

        [RelayCommand]
        private async Task ToggleFavoriteAsync(WorkspaceItem? item)
        {
            if (_workspaceRepository == null)
            {
                return;
            }

            item ??= SelectedItem;
            if (item == null)
            {
                return;
            }

            Serilog.Log.Debug("WorkspaceViewModel.ToggleFavoriteAsync: ItemId={ItemId}, newFavorite={NewFavorite}", item.Id, !item.IsFavorite);
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
            if (_notificationService == null)
            {
                return;
            }

            try
            {
                await _notificationService.ShowNotificationAsync(
                    "キャプチャ完了",
                    $"{item.Title} をクリップボードにコピーしました。",
                    () => MainThread.BeginInvokeOnMainThread(() => NavigateToItemRequested?.Invoke(this, item.Id)));
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to show capture completion notification");
            }
        }
    }
}
