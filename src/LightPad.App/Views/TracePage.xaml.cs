using LightPad.App.Infrastructure;
using LightPad.App.ViewModels;

namespace LightPad.App.Views;

public partial class TracePage : ContentPage
{
    private readonly TraceViewModel _viewModel;
    private double _panStartX;
    private double _panStartY;
    private double _pinchStartScale;

    public TracePage()
        : this(ServiceProviderHelper.GetRequiredService<TraceViewModel>())
    {
    }

    public TracePage(TraceViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    protected override async void OnDisappearing()
    {
        await _viewModel.OnDisappearingAsync();
        base.OnDisappearing();
    }

    private void OnImagePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!_viewModel.CanManipulateImage)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panStartX = _viewModel.OffsetX;
                _panStartY = _viewModel.OffsetY;
                break;
            case GestureStatus.Running:
                _viewModel.ApplyPan(_panStartX, _panStartY, e.TotalX, e.TotalY);
                break;
        }
    }

    private void OnImagePinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (!_viewModel.CanManipulateImage)
        {
            return;
        }

        switch (e.Status)
        {
            case GestureStatus.Started:
                _pinchStartScale = _viewModel.Zoom;
                break;
            case GestureStatus.Running:
                _viewModel.ApplyZoom(_pinchStartScale * e.Scale);
                break;
        }
    }

    private void OnTraceSurfaceDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.ResetViewCommand.CanExecute(null))
        {
            _viewModel.ResetViewCommand.Execute(null);
        }
    }
}
