using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using LightPad.App.Infrastructure;
using LightPad.App.Services;
using LightPad.App.Utilities;
using LightPad.App.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
#if WINDOWS
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
#endif

namespace LightPad.App.Views;

public partial class TracePage : ContentPage
{
    private readonly TraceViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private double _panStartX;
    private double _panStartY;
    private double _pinchStartScale;
    private SKBitmap? _traceBitmap;
    private string? _loadedBitmapPath;
    private CancellationTokenSource? _gestureHintDismissalCts;
    private bool _isGestureHintVisible;
#if WINDOWS
    private FrameworkElement? _windowsKeyboardElement;
#endif

    public TracePage()
        : this(
            ServiceProviderHelper.GetRequiredService<TraceViewModel>(),
            ServiceProviderHelper.GetRequiredService<ISettingsService>())
    {
    }

    public TracePage(TraceViewModel viewModel, ISettingsService settingsService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _settingsService = settingsService;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        BindingContext = viewModel;
        ConfigureGestureHint();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
        RegisterWindowsKeyboardShortcuts();
        ShowGestureHintIfNeeded();
        LoadBitmapIfNeeded();
        TraceCanvas.InvalidateSurface();
    }

    protected override async void OnDisappearing()
    {
        CancelGestureHintDismissal();
        await _viewModel.OnDisappearingAsync();
        base.OnDisappearing();
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.NewHandler is null)
        {
            CancelGestureHintDismissal();
            UnregisterWindowsKeyboardShortcuts();
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
            or nameof(TraceViewModel.IsGridVisible)
            or nameof(TraceViewModel.GridSpacing)
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

        DrawGrid(canvas, paperBounds);

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
        DismissGestureHintForInteraction();

        if (!_viewModel.CanManipulateImage)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _viewModel.BeginInteractionHistoryCapture();
                _panStartX = _viewModel.OffsetX;
                _panStartY = _viewModel.OffsetY;
                break;
            case GestureStatus.Running:
                _viewModel.ApplyPan(_panStartX, _panStartY, e.TotalX, e.TotalY);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _viewModel.CommitInteractionHistoryCapture();
                break;
        }
    }

    private void OnImagePinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        DismissGestureHintForInteraction();

        if (!_viewModel.CanManipulateImage)
        {
            return;
        }

        switch (e.Status)
        {
            case GestureStatus.Started:
                _viewModel.BeginInteractionHistoryCapture();
                _pinchStartScale = _viewModel.Zoom;
                break;
            case GestureStatus.Running:
                _viewModel.ApplyZoom(_pinchStartScale * e.Scale);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _viewModel.CommitInteractionHistoryCapture();
                break;
        }
    }

    private void OnTraceSurfaceDoubleTapped(object? sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        DismissGestureHintForInteraction();

        if (_viewModel.ResetViewCommand.CanExecute(null))
        {
            _viewModel.ResetViewCommand.Execute(null);
        }
    }

    private void OnGestureHintDismissed(object? sender, EventArgs e)
    {
        DismissGestureHint();
    }

    private void OnManipulationSliderDragStarted(object? sender, EventArgs e)
    {
        DismissGestureHintForInteraction();
        _viewModel.BeginInteractionHistoryCapture();
    }

    private void OnManipulationSliderDragCompleted(object? sender, EventArgs e)
    {
        _viewModel.CommitInteractionHistoryCapture();
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

    private void DrawGrid(SKCanvas canvas, SKRoundRect paperBounds)
    {
        if (!_viewModel.IsGridVisible)
        {
            return;
        }

        var paperRect = paperBounds.Rect;
        var majorSpacing = (float)_viewModel.GridSpacing;
        var minorSpacing = Math.Max(majorSpacing / 2f, 8f);
        var lineColor = ResolveGridColor();
        var accentColor = lineColor.WithAlpha((byte)Math.Min(255, lineColor.Alpha + 35));

        canvas.Save();
        canvas.ClipRoundRect(paperBounds, antialias: true);

        using var minorPaint = new SKPaint
        {
            Color = lineColor,
            StrokeWidth = 1f,
            IsAntialias = true
        };

        using var majorPaint = new SKPaint
        {
            Color = accentColor,
            StrokeWidth = 1.4f,
            IsAntialias = true
        };

        DrawGridLines(canvas, paperRect, minorSpacing, minorPaint);
        DrawGridLines(canvas, paperRect, majorSpacing, majorPaint);
        canvas.Restore();
    }

    private static void DrawGridLines(SKCanvas canvas, SKRect bounds, float spacing, SKPaint paint)
    {
        for (var x = bounds.Left + spacing; x < bounds.Right; x += spacing)
        {
            canvas.DrawLine(x, bounds.Top, x, bounds.Bottom, paint);
        }

        for (var y = bounds.Top + spacing; y < bounds.Bottom; y += spacing)
        {
            canvas.DrawLine(bounds.Left, y, bounds.Right, y, paint);
        }
    }

    private SKColor ResolveGridColor()
    {
        return _viewModel.TraceBackdropOverlayOpacity > 0.45
            ? SKColor.Parse("#88FFF7CF")
            : SKColor.Parse("#884A4332");
    }

    private void ConfigureGestureHint()
    {
        var hint = GestureHintContentFactory.CreateTraceHint();
        GestureHintTitleLabel.Text = hint.Title;
        GestureHintSubtitleLabel.Text = hint.Subtitle;

        foreach (var tip in hint.Tips)
        {
            GestureHintTipsLayout.Children.Add(new Label
            {
                Text = $"• {tip}",
                TextColor = Colors.White,
                LineBreakMode = LineBreakMode.WordWrap
            });
        }
    }

    private void ShowGestureHintIfNeeded()
    {
        if (_settingsService.HasSeenTraceGestureHint || _isGestureHintVisible)
        {
            return;
        }

        _settingsService.HasSeenTraceGestureHint = true;
        _isGestureHintVisible = true;
        GestureHintOverlay.IsVisible = true;
        StartGestureHintDismissalTimer();
    }

    private void DismissGestureHintForInteraction()
    {
        if (!_isGestureHintVisible)
        {
            return;
        }

        DismissGestureHint();
    }

    private void DismissGestureHint()
    {
        if (!_isGestureHintVisible)
        {
            return;
        }

        CancelGestureHintDismissal();
        _isGestureHintVisible = false;
        GestureHintOverlay.IsVisible = false;
    }

    private void StartGestureHintDismissalTimer()
    {
        CancelGestureHintDismissal();
        _gestureHintDismissalCts = new CancellationTokenSource();
        _ = DismissGestureHintAfterDelayAsync(_gestureHintDismissalCts.Token);
    }

    private async Task DismissGestureHintAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            await MainThread.InvokeOnMainThreadAsync(DismissGestureHint);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CancelGestureHintDismissal()
    {
        _gestureHintDismissalCts?.Cancel();
        _gestureHintDismissalCts?.Dispose();
        _gestureHintDismissalCts = null;
    }

    private void RegisterWindowsKeyboardShortcuts()
    {
#if WINDOWS
        if (_windowsKeyboardElement is not null)
        {
            return;
        }

        if (Handler?.PlatformView is FrameworkElement element)
        {
            _windowsKeyboardElement = element;
            element.KeyDown += OnWindowsKeyDown;
        }
#endif
    }

    private void UnregisterWindowsKeyboardShortcuts()
    {
#if WINDOWS
        if (_windowsKeyboardElement is null)
        {
            return;
        }

        _windowsKeyboardElement.KeyDown -= OnWindowsKeyDown;
        _windowsKeyboardElement = null;
#endif
    }

#if WINDOWS
    private void OnWindowsKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var controlPressed = InputKeyboardSource
            .GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(CoreVirtualKeyStates.Down);
        if (!controlPressed || e.Key is not VirtualKey.Z)
        {
            return;
        }

        var shiftPressed = InputKeyboardSource
            .GetKeyStateForCurrentThread(VirtualKey.Shift)
            .HasFlag(CoreVirtualKeyStates.Down);

        if (shiftPressed)
        {
            if (_viewModel.RedoCommand.CanExecute(null))
            {
                _viewModel.RedoCommand.Execute(null);
                e.Handled = true;
            }

            return;
        }

        if (_viewModel.UndoCommand.CanExecute(null))
        {
            _viewModel.UndoCommand.Execute(null);
            e.Handled = true;
        }
    }
#endif
}
