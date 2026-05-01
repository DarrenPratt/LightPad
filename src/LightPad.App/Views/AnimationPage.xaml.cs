using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using LightPad.App.Infrastructure;
using LightPad.App.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LightPad.App.Views;

public partial class AnimationPage : ContentPage
{
    private readonly AnimationViewModel _viewModel;
    private readonly Dictionary<string, SKBitmap> _bitmapCache = new(StringComparer.OrdinalIgnoreCase);
    private double _panStartX;
    private double _panStartY;
    private double _pinchStartScale;

    public AnimationPage()
        : this(ServiceProviderHelper.GetRequiredService<AnimationViewModel>())
    {
    }

    public AnimationPage(AnimationViewModel viewModel)
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
        AnimationCanvas.InvalidateSurface();
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
        if (_viewModel.ResetViewCommand.CanExecute(null))
        {
            _viewModel.ResetViewCommand.Execute(null);
        }
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
}
