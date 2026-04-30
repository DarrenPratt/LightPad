using System.ComponentModel;
using LightPad.App.Infrastructure;
using LightPad.App.ViewModels;

namespace LightPad.App.Views;

public partial class LightboxPage : ContentPage
{
    private readonly LightboxViewModel _viewModel;
    private IDispatcherTimer? _immersiveHideTimer;

    public LightboxPage()
        : this(ServiceProviderHelper.GetRequiredService<LightboxViewModel>())
    {
    }

    public LightboxPage(LightboxViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        EnsureImmersiveHideTimer();
        await _viewModel.OnAppearingAsync();
        SyncImmersiveTimerWithState();
    }

    protected override async void OnDisappearing()
    {
        StopImmersiveHideTimer();
        await _viewModel.OnDisappearingAsync();
        base.OnDisappearing();
    }

    private async void OnUnlockButtonPressed(object? sender, EventArgs e)
    {
        await _viewModel.BeginUnlockHoldAsync();
        SyncImmersiveTimerWithState();
    }

    private void OnUnlockButtonReleased(object? sender, EventArgs e)
    {
        _viewModel.CancelUnlockHold();
        SyncImmersiveTimerWithState();
    }

    private void OnUnlockButtonUnloaded(object? sender, EventArgs e)
    {
        _viewModel.CancelUnlockHold();
        StopImmersiveHideTimer();
    }

    private void OnLightSurfaceTapped(object? sender, TappedEventArgs e)
    {
        _viewModel.OnLightSurfaceTapped();
        SyncImmersiveTimerWithState();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LightboxViewModel.IsLocked) or nameof(LightboxViewModel.IsImmersiveChromeVisible))
        {
            SyncImmersiveTimerWithState();
        }
    }

    private void EnsureImmersiveHideTimer()
    {
        if (_immersiveHideTimer is not null)
        {
            return;
        }

        _immersiveHideTimer = Dispatcher.CreateTimer();
        _immersiveHideTimer.Interval = TimeSpan.FromSeconds(3.5);
        _immersiveHideTimer.IsRepeating = false;
        _immersiveHideTimer.Tick += OnImmersiveHideTimerTick;
    }

    private void OnImmersiveHideTimerTick(object? sender, EventArgs e)
    {
        _viewModel.HideImmersiveChrome();
        StopImmersiveHideTimer();
    }

    private void SyncImmersiveTimerWithState()
    {
        if (_viewModel.IsLocked && _viewModel.IsImmersiveChromeVisible)
        {
            RestartImmersiveHideTimer();
            return;
        }

        StopImmersiveHideTimer();
    }

    private void RestartImmersiveHideTimer()
    {
        EnsureImmersiveHideTimer();
        _immersiveHideTimer!.Stop();
        _immersiveHideTimer.Start();
    }

    private void StopImmersiveHideTimer()
    {
        _immersiveHideTimer?.Stop();
    }
}
