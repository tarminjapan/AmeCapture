using Microsoft.Extensions.DependencyInjection;

namespace AmeCapture.App;

public partial class App : global::Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}