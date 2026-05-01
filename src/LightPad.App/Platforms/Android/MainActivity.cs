using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace LightPad.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ApplyImmersiveMode();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);

        if (hasFocus)
        {
            ApplyImmersiveMode();
        }
    }

    private void ApplyImmersiveMode()
    {
        if (Window is null)
        {
            return;
        }

        WindowCompat.SetDecorFitsSystemWindows(Window, false);

        var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (controller is null)
        {
            return;
        }

        controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
        controller.Hide(WindowInsetsCompat.Type.SystemBars());
    }
}
