using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using LightPad.App.Models;
using LightPad.App.Services;
using LightPad.App.Utilities;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace LightPad.App.ViewModels;

public sealed class TraceViewModel : BaseViewModel
{
    private const double MinZoom = 0.5;
    private const double MaxZoom = 4.0;
    private const double RotationStep = 5.0;
    private readonly IImagePickerService _imagePickerService;
    private readonly IScreenWakeService _screenWakeService;
    private readonly ISettingsService _settingsService;
    private readonly TraceImageState _activeImage;
    private double _offsetX;
    private double _offsetY;
    private double _zoom;
    private double _rotation;
    private double _imageOpacity;
    private bool _isImageLocked;
    private string? _imagePath;
    private string _statusMessage;
    private bool _isBusy;
    private bool _isControlsExpanded = true;
    private double _surfaceBrightness;
    private double _surfaceColorTemperature;
    private LightColorPreset _surfacePreset;
    private string _surfaceCustomColorHex;

    public TraceViewModel(
        TraceSessionState sessionState,
        IScreenWakeService screenWakeService,
        IImagePickerService imagePickerService,
        ISettingsService settingsService)
    {
        SessionState = sessionState;
        _screenWakeService = screenWakeService;
        _imagePickerService = imagePickerService;
        _settingsService = settingsService;
        _activeImage = sessionState.ActiveImage;
        _imagePath = _activeImage.FilePath;
        _offsetX = _activeImage.OffsetX;
        _offsetY = _activeImage.OffsetY;
        _zoom = LightSurfaceStyleCalculator.Clamp(_activeImage.Zoom, MinZoom, MaxZoom);
        _rotation = NormalizeRotation(_activeImage.Rotation);
        _imageOpacity = LightSurfaceStyleCalculator.Clamp(
            _activeImage.FilePath is null ? settingsService.DefaultTraceOpacity : _activeImage.Opacity,
            0.1,
            1.0);
        _isImageLocked = _activeImage.IsLocked;
        _surfaceBrightness = LightSurfaceStyleCalculator.Clamp(settingsService.Brightness, 0.05, 1.0);
        _surfaceColorTemperature = LightSurfaceStyleCalculator.Clamp(settingsService.ColorTemperature, 2700.0, 9000.0);
        _surfacePreset = settingsService.SelectedPreset;
        _surfaceCustomColorHex = LightSurfaceStyleCalculator.NormalizeHexOrDefault(settingsService.CustomColorHex, "#FFFFFF");
        _statusMessage = CreateStatusMessage();
        _activeImage.Rotation = _rotation;
        _activeImage.Opacity = _imageOpacity;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        ImportImageCommand = new Command(async () => await ImportImageAsync(), () => !IsBusy);
        ResetViewCommand = new Command(ResetView, () => HasImage && !IsBusy);
        ToggleImageLockCommand = new Command(ToggleImageLock, () => HasImage && !IsBusy);
        ClearImageCommand = new Command(ClearImage, () => HasImage && !IsBusy);
        ZoomOutCommand = new Command(() => NudgeZoom(-0.15), () => HasImage && !IsBusy);
        ZoomInCommand = new Command(() => NudgeZoom(0.15), () => HasImage && !IsBusy);
        RotateLeftCommand = new Command(() => NudgeRotation(-RotationStep), () => CanManipulateImage && !IsBusy);
        RotateRightCommand = new Command(() => NudgeRotation(RotationStep), () => CanManipulateImage && !IsBusy);
        ToggleControlsCommand = new Command(ToggleControls);
    }

    public TraceSessionState SessionState { get; }

    public ICommand BackCommand { get; }

    public Command ImportImageCommand { get; }

    public Command ResetViewCommand { get; }

    public Command ToggleImageLockCommand { get; }

    public Command ClearImageCommand { get; }

    public Command ZoomOutCommand { get; }

    public Command ZoomInCommand { get; }

    public Command RotateLeftCommand { get; }

    public Command RotateRightCommand { get; }

    public Command ToggleControlsCommand { get; }

    public string? ImagePath
    {
        get => _imagePath;
        private set
        {
            if (!SetProperty(ref _imagePath, value))
            {
                return;
            }

            _activeImage.FilePath = value;
            OnPropertyChanged(nameof(HasImage));
            OnPropertyChanged(nameof(IsEmptyStateVisible));
            OnPropertyChanged(nameof(ImageName));
            RaiseCommandStateChanged();
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    public bool IsEmptyStateVisible => !HasImage;

    public string ImageName => HasImage ? Path.GetFileName(ImagePath!) ?? ImagePath! : "No image loaded";

    public double OffsetX
    {
        get => _offsetX;
        set
        {
            if (!SetProperty(ref _offsetX, value))
            {
                return;
            }

            _activeImage.OffsetX = value;
            UpdateStatusMessage();
        }
    }

    public double OffsetY
    {
        get => _offsetY;
        set
        {
            if (!SetProperty(ref _offsetY, value))
            {
                return;
            }

            _activeImage.OffsetY = value;
            UpdateStatusMessage();
        }
    }

    public double Zoom
    {
        get => _zoom;
        set
        {
            var clampedValue = LightSurfaceStyleCalculator.Clamp(value, MinZoom, MaxZoom);
            if (!SetProperty(ref _zoom, clampedValue))
            {
                return;
            }

            _activeImage.Zoom = clampedValue;
            UpdateStatusMessage();
        }
    }

    public double RotationAngle
    {
        get => _rotation;
        set
        {
            var normalizedValue = NormalizeRotation(value);
            if (!SetProperty(ref _rotation, normalizedValue))
            {
                return;
            }

            _activeImage.Rotation = normalizedValue;
            UpdateStatusMessage();
        }
    }

    public double ImageOpacity
    {
        get => _imageOpacity;
        set
        {
            var clampedValue = LightSurfaceStyleCalculator.Clamp(value, 0.1, 1.0);
            if (!SetProperty(ref _imageOpacity, clampedValue))
            {
                return;
            }

            _activeImage.Opacity = clampedValue;
            UpdateStatusMessage();
        }
    }

    public bool IsImageLocked
    {
        get => _isImageLocked;
        private set
        {
            if (!SetProperty(ref _isImageLocked, value))
            {
                return;
            }

            _activeImage.IsLocked = value;
            SessionState.IsSessionLocked = value;
            OnPropertyChanged(nameof(LockButtonText));
            OnPropertyChanged(nameof(LockStatusText));
            OnPropertyChanged(nameof(CanManipulateImage));
            RaiseCommandStateChanged();
            UpdateStatusMessage();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            RaiseCommandStateChanged();
        }
    }

    public bool CanManipulateImage => HasImage && !IsImageLocked;

    public bool IsControlsExpanded
    {
        get => _isControlsExpanded;
        private set
        {
            if (!SetProperty(ref _isControlsExpanded, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ControlsToggleText));
            OnPropertyChanged(nameof(IsFloatingShowToolsVisible));
        }
    }

    public bool IsFloatingShowToolsVisible => !IsControlsExpanded;

    public string LockButtonText => IsImageLocked ? "Unlock Image" : "Lock Image";

    public string ControlsToggleText => IsControlsExpanded ? "Hide Tools" : "Show Tools";

    public string LockStatusText
    {
        get
        {
            if (!HasImage)
            {
                return "Import a single reference image to begin tracing.";
            }

            return IsImageLocked
                ? "Image movement is locked. Pan and pinch are disabled until you unlock."
                : "Image movement is unlocked. Drag to pan, pinch or use the zoom slider to scale.";
        }
    }

    public Color TraceBackdropColor => LightSurfaceStyleCalculator.ResolveLightColor(_surfacePreset, _surfaceColorTemperature, _surfaceCustomColorHex);

    public double TraceBackdropOverlayOpacity => 1.0 - _surfaceBrightness;

    public string TraceBackdropStatusText =>
        $"Trace background reuses the global {_surfacePreset} light defaults at {_surfaceBrightness:P0} brightness.";

    public string StatusText => _statusMessage;

    public async Task OnAppearingAsync()
    {
        await _screenWakeService.ActivateAsync(nameof(TraceViewModel));
    }

    public async Task OnDisappearingAsync()
    {
        await _screenWakeService.DeactivateAsync(nameof(TraceViewModel));
    }

    public void ApplyPan(double startX, double startY, double deltaX, double deltaY)
    {
        if (!CanManipulateImage)
        {
            return;
        }

        OffsetX = startX + deltaX;
        OffsetY = startY + deltaY;
    }

    public void ApplyZoom(double scale)
    {
        if (!CanManipulateImage)
        {
            return;
        }

        Zoom = scale;
    }

    public async Task ImportImageAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var selectedImagePath = await _imagePickerService.PickImageAsync();
            if (string.IsNullOrWhiteSpace(selectedImagePath))
            {
                UpdateStatusMessage("Image import cancelled.");
                return;
            }

            ImagePath = selectedImagePath;
            ImageOpacity = _settingsService.DefaultTraceOpacity;
            ResetView();
            UpdateStatusMessage($"Loaded {Path.GetFileName(selectedImagePath)} for tracing.");
        }
        catch (Exception exception)
        {
            UpdateStatusMessage($"Image import failed: {exception.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetView()
    {
        OffsetX = 0.0;
        OffsetY = 0.0;
        Zoom = 1.0;
        RotationAngle = 0.0;
        if (HasImage)
        {
            UpdateStatusMessage($"Reset view for {ImageName}.");
        }
    }

    private void ToggleImageLock()
    {
        if (!HasImage)
        {
            return;
        }

        IsImageLocked = !IsImageLocked;
    }

    private void RaiseCommandStateChanged()
    {
        ImportImageCommand.ChangeCanExecute();
        ResetViewCommand.ChangeCanExecute();
        ToggleImageLockCommand.ChangeCanExecute();
        ClearImageCommand.ChangeCanExecute();
        ZoomOutCommand.ChangeCanExecute();
        ZoomInCommand.ChangeCanExecute();
        RotateLeftCommand.ChangeCanExecute();
        RotateRightCommand.ChangeCanExecute();
    }

    private void ClearImage()
    {
        if (!HasImage)
        {
            return;
        }

        ImagePath = null;
        OffsetX = 0.0;
        OffsetY = 0.0;
        Zoom = 1.0;
        RotationAngle = 0.0;
        ImageOpacity = _settingsService.DefaultTraceOpacity;
        IsImageLocked = false;
        UpdateStatusMessage("Trace image cleared. Import a new reference image to continue.");
    }

    private void NudgeZoom(double delta)
    {
        if (!HasImage)
        {
            return;
        }

        Zoom += delta;
    }

    private void NudgeRotation(double delta)
    {
        if (!CanManipulateImage)
        {
            return;
        }

        RotationAngle = _rotation + delta;
    }

    private void ToggleControls()
    {
        IsControlsExpanded = !IsControlsExpanded;
    }

    private void UpdateStatusMessage(string? overrideMessage = null)
    {
        _statusMessage = overrideMessage ?? CreateStatusMessage();
        OnPropertyChanged(nameof(StatusText));
    }

    private string CreateStatusMessage()
    {
        if (!HasImage)
        {
            return "No trace image loaded yet. Import a single image to start panning and zooming.";
        }

        return $"{ImageName}: zoom {Zoom:0.00}x, rotation {RotationAngle:0}°, opacity {ImageOpacity:P0}, offset ({OffsetX:0}, {OffsetY:0}).";
    }

    private static double NormalizeRotation(double value)
    {
        var normalized = value % 360.0;
        if (normalized < 0)
        {
            normalized += 360.0;
        }

        return normalized;
    }
}
