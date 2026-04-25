using AmeCapture.App.Views;

namespace AmeCapture.App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(EditorPage), typeof(EditorPage));
        }
    }
}
