using System.Windows.Input;
using LightPad.App.Views;

namespace LightPad.App.ViewModels;

public sealed class MainViewModel : BaseViewModel
{
    public ICommand OpenLightboxCommand { get; }

    public ICommand OpenTraceImageCommand { get; }

    public ICommand OpenAnimationFramesCommand { get; }

    public ICommand OpenSettingsCommand { get; }

    public MainViewModel()
    {
        OpenLightboxCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(LightboxPage)));
        OpenTraceImageCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(TracePage)));
        OpenAnimationFramesCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(AnimationPage)));
        OpenSettingsCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(SettingsPage)));
    }
}
