using LightPad.App.Views;

namespace LightPad.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(LightboxPage), typeof(LightboxPage));
        Routing.RegisterRoute(nameof(TracePage), typeof(TracePage));
        Routing.RegisterRoute(nameof(AnimationPage), typeof(AnimationPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
    }
}
