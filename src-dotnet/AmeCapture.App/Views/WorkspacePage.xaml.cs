using AmeCapture.App.ViewModels;

namespace AmeCapture.App.Views;

public partial class WorkspacePage : ContentPage
{
    public WorkspacePage(WorkspaceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
