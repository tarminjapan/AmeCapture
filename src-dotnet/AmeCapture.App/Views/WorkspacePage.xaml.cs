using AmeCapture.App.ViewModels;

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
}
