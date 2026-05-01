using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LightPad.App.Infrastructure;
using LightPad.App.Services;
using LightPad.App.Utilities;
using LightPad.App.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LightPad.App.Views;

public partial class AnimationPage : ContentPage
{
    private readonly AnimationViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private readonly Dictionary<string, SKBitmap> _bitmapCache = new(StringComparer.OrdinalIgnoreCase);
    private double _panStartX;
    private double _panStartY;
    private double _pinchStartScale;
    private CancellationTokenSource? _gestureHintDismissalCts;
    private bool _isGestureHintVisible;

    public AnimationPage()
        : this(
            ServiceProviderHelper.GetRequiredService<AnimationViewModel>(),
            ServiceProviderHelper.GetRequiredService<ISettingsService>())
    {
    }

    public AnimationPage(AnimationViewModel viewModel, ISettingsService settingsService)
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
        ShowGestureHintIfNeeded();
        AnimationCanvas.InvalidateSurface();
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
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            DisposeBitmaps();
        }

        base.OnHandlerChanging(args);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AnimationViewModel.CurrentFrame)
            or nameof(AnimationViewModel.PreviousFrame)
            or nameof(AnimationViewModel.CurrentFrameIndex)
            or nameof(AnimationViewModel.OffsetX)
            or nameof(AnimationViewModel.OffsetY)
            or nameof(AnimationViewModel.Zoom)
            or nameof(AnimationViewModel.CurrentFrameOpacity)
            or nameof(AnimationViewModel.OnionSkinOpacity)
            or nameof(AnimationViewModel.IsOnionSkinEnabled)
            or nameof(AnimationViewModel.HasFrames))
        {
            AnimationCanvas.InvalidateSurface();
        }
    }

    private void OnAnimationCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
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

        if (!_viewModel.HasFrames || _viewModel.CurrentFrame is null)
        {
            return;
        }

        if (_viewModel.CanShowOnionSkin && _viewModel.PreviousFrame is not null)
        {
            DrawFrame(canvas, e.Info, _viewModel.PreviousFrame.FilePath, _viewModel.OnionSkinOpacity);
        }

        DrawFrame(canvas, e.Info, _viewModel.CurrentFrame.FilePath, _viewModel.CurrentFrameOpacity);
    }

    private void OnFramePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        DismissGestureHintForInteraction();

        if (!_viewModel.CanManipulateFrames)
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

    private void OnFramePinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        DismissGestureHintForInteraction();

        if (!_viewModel.CanManipulateFrames)
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

    private void OnAnimationSurfaceDoubleTapped(object? sender, TappedEventArgs e)
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

    private void DrawFrame(SKCanvas canvas, SKImageInfo info, string imagePath, double opacity)
    {
        var bitmap = GetBitmap(imagePath);
        if (bitmap is null)
        {
            return;
        }

        var fitScale = Math.Min(
            (float)(info.Width - 80) / bitmap.Width,
            (float)(info.Height - 80) / bitmap.Height);

        fitScale = Math.Max(0.05f, fitScale);
        var renderScale = fitScale * (float)_viewModel.Zoom;
        var drawWidth = bitmap.Width * renderScale;
        var drawHeight = bitmap.Height * renderScale;
        var left = ((float)info.Width - drawWidth) / 2f + (float)_viewModel.OffsetX;
        var top = ((float)info.Height - drawHeight) / 2f + (float)_viewModel.OffsetY;
        var targetRect = new SKRect(left, top, left + drawWidth, top + drawHeight);

        using var bitmapPaint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.White.WithAlpha((byte)Math.Round(opacity * 255.0))
        };

        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
        using var image = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(image, targetRect, sampling, bitmapPaint);
    }

    private SKBitmap? GetBitmap(string imagePath)
    {
        if (_bitmapCache.TryGetValue(imagePath, out var cachedBitmap))
        {
            return cachedBitmap;
        }

        try
        {
            using var stream = File.OpenRead(imagePath);
            var decodedBitmap = SKBitmap.Decode(stream);
            if (decodedBitmap is null)
            {
                return null;
            }

            _bitmapCache[imagePath] = decodedBitmap;
            return decodedBitmap;
        }
        catch
        {
            return null;
        }
    }

    private void DisposeBitmaps()
    {
        foreach (var bitmap in _bitmapCache.Values)
        {
            bitmap.Dispose();
        }

        _bitmapCache.Clear();
    }

    private void ConfigureGestureHint()
    {
        var hint = GestureHintContentFactory.CreateAnimationHint();
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
        if (_settingsService.HasSeenAnimationGestureHint || _isGestureHintVisible)
        {
            return;
        }

        _settingsService.HasSeenAnimationGestureHint = true;
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
}
