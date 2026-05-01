using System.ComponentModel;
using LightPad.App.Infrastructure;
using LightPad.App.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LightPad.App.Views;

public partial class TracePage : ContentPage
{
    private readonly TraceViewModel _viewModel;
    private double _panStartX;
    private double _panStartY;
    private double _pinchStartScale;
    private SKBitmap? _traceBitmap;
    private string? _loadedBitmapPath;

    public TracePage()
        : this(ServiceProviderHelper.GetRequiredService<TraceViewModel>())
    {
    }

    public TracePage(TraceViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
        LoadBitmapIfNeeded();
        TraceCanvas.InvalidateSurface();
    }

    protected override async void OnDisappearing()
    {
        await _viewModel.OnDisappearingAsync();
        base.OnDisappearing();
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.NewHandler is null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            DisposeBitmap();
        }

        base.OnHandlerChanging(args);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TraceViewModel.ImagePath))
        {
            LoadBitmapIfNeeded();
        }

        if (e.PropertyName is nameof(TraceViewModel.ImagePath)
            or nameof(TraceViewModel.HasImage)
            or nameof(TraceViewModel.OffsetX)
            or nameof(TraceViewModel.OffsetY)
            or nameof(TraceViewModel.Zoom)
            or nameof(TraceViewModel.RotationAngle)
            or nameof(TraceViewModel.ImageOpacity)
            or nameof(TraceViewModel.IsImageLocked))
        {
            TraceCanvas.InvalidateSurface();
        }
    }

    private void OnTraceCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColor.Parse("#1A1A1A"));

        using var paperPaint = new SKPaint
        {
            Color = SKColor.Parse("#F3F1E8"),
            IsAntialias = true
        };

        var paperBounds = new SKRoundRect(
            new SKRect(20f, 20f, e.Info.Width - 20f, e.Info.Height - 20f),
            24f,
            24f);
        canvas.DrawRoundRect(paperBounds, paperPaint);

        if (_traceBitmap is null || !_viewModel.HasImage)
        {
            return;
        }

        var fitScale = Math.Min(
            (float)(e.Info.Width - 80) / _traceBitmap.Width,
            (float)(e.Info.Height - 80) / _traceBitmap.Height);

        fitScale = Math.Max(0.05f, fitScale);
        var renderScale = fitScale * (float)_viewModel.Zoom;
        var drawWidth = _traceBitmap.Width * renderScale;
        var drawHeight = _traceBitmap.Height * renderScale;
        var left = ((float)e.Info.Width - drawWidth) / 2f + (float)_viewModel.OffsetX;
        var top = ((float)e.Info.Height - drawHeight) / 2f + (float)_viewModel.OffsetY;
        var targetRect = new SKRect(left, top, left + drawWidth, top + drawHeight);

        using var bitmapPaint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.White.WithAlpha((byte)Math.Round(_viewModel.ImageOpacity * 255.0))
        };
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
        using var image = SKImage.FromBitmap(_traceBitmap);
        var centerX = targetRect.MidX;
        var centerY = targetRect.MidY;

        canvas.Save();
        canvas.Translate(centerX, centerY);
        canvas.RotateDegrees((float)_viewModel.RotationAngle);
        canvas.Translate(-centerX, -centerY);
        canvas.DrawImage(image, targetRect, sampling, bitmapPaint);
        canvas.Restore();
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

    private void LoadBitmapIfNeeded()
    {
        var imagePath = _viewModel.ImagePath;
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            DisposeBitmap();
            return;
        }

        if (string.Equals(_loadedBitmapPath, imagePath, StringComparison.OrdinalIgnoreCase) && _traceBitmap is not null)
        {
            return;
        }

        try
        {
            using var stream = File.OpenRead(imagePath);
            var decodedBitmap = SKBitmap.Decode(stream);
            DisposeBitmap();
            _traceBitmap = decodedBitmap;
            _loadedBitmapPath = imagePath;
        }
        catch
        {
            DisposeBitmap();
        }
    }

    private void DisposeBitmap()
    {
        _traceBitmap?.Dispose();
        _traceBitmap = null;
        _loadedBitmapPath = null;
    }
}
