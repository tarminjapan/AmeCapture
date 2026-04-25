using AmeCapture.App.ViewModels;
using AmeCapture.Domain.Entities;

namespace AmeCapture.App.Views;

public partial class WorkspacePage : ContentPage
{
    private readonly WorkspaceViewModel _viewModel;

    public WorkspacePage(WorkspaceViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        _viewModel.NavigateToItemRequested += OnNavigateToItemRequested;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        Serilog.Log.Debug("WorkspacePage.OnPageLoaded");
        await _viewModel.LoadItemsAsync();
    }

    private async void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable) return;
        if (bindable.BindingContext is not WorkspaceItem item) return;

        Serilog.Log.Debug("WorkspacePage.OnItemTapped: ItemId={ItemId}", item.Id);
        _viewModel.SelectedItem = item;

        var parameters = new Dictionary<string, object>
        {
            { "itemId", item.Id },
        };

        await Shell.Current.GoToAsync(nameof(EditorPage), parameters);
    }

    private async void OnNavigateToItemRequested(object? sender, string itemId)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "itemId", itemId },
                };
                await Shell.Current.GoToAsync(nameof(EditorPage), parameters);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to navigate to item from notification");
            }
        });
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        Serilog.Log.Debug("WorkspacePage.OnNavigatedTo");
        _ = _viewModel.LoadItemsAsync();
    }
}
