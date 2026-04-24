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
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        await _viewModel.LoadItemsAsync();
    }

    private async void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable) return;
        if (bindable.BindingContext is not WorkspaceItem item) return;

        var parameters = new Dictionary<string, object>
        {
            { "itemId", item.Id },
        };

        await Shell.Current.GoToAsync(nameof(EditorPage), parameters);
    }
}
