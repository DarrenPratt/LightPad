using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using LightPad.App.Models;
using LightPad.App.Services;
using Microsoft.Maui.Controls;

namespace LightPad.App.ViewModels;

public sealed class TraceViewModel : BaseViewModel
{
    private const double MinZoom = 0.5;
    private const double MaxZoom = 4.0;
    private readonly IImagePickerService _imagePickerService;
    private readonly IScreenWakeService _screenWakeService;
    private readonly TraceImageState _activeImage;
    private double _offsetX;
    private double _offsetY;
    private double _zoom;
    private double _imageOpacity;
    private bool _isImageLocked;
    private string? _imagePath;
    private string _statusMessage;
    private bool _isBusy;

    public TraceViewModel(
        TraceSessionState sessionState,
        IScreenWakeService screenWakeService,
        IImagePickerService imagePickerService)
    {
        SessionState = sessionState;
        _screenWakeService = screenWakeService;
        _imagePickerService = imagePickerService;
        _activeImage = sessionState.ActiveImage;
        _imagePath = _activeImage.FilePath;
        _offsetX = _activeImage.OffsetX;
        _offsetY = _activeImage.OffsetY;
        _zoom = Clamp(_activeImage.Zoom, MinZoom, MaxZoom);
        _imageOpacity = Clamp(_activeImage.Opacity, 0.1, 1.0);
        _isImageLocked = _activeImage.IsLocked;
        _statusMessage = CreateStatusMessage();

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        ImportImageCommand = new Command(async () => await ImportImageAsync(), () => !IsBusy);
        ResetViewCommand = new Command(ResetView, () => HasImage && !IsBusy);
        ToggleImageLockCommand = new Command(ToggleImageLock, () => HasImage && !IsBusy);
    }

    public TraceSessionState SessionState { get; }

    public ICommand BackCommand { get; }

    public Command ImportImageCommand { get; }

    public Command ResetViewCommand { get; }

    public Command ToggleImageLockCommand { get; }

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
            var clampedValue = Clamp(value, MinZoom, MaxZoom);
            if (!SetProperty(ref _zoom, clampedValue))
            {
                return;
            }

            _activeImage.Zoom = clampedValue;
            UpdateStatusMessage();
        }
    }

    public double ImageOpacity
    {
        get => _imageOpacity;
        set
        {
            var clampedValue = Clamp(value, 0.1, 1.0);
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

    public string LockButtonText => IsImageLocked ? "Unlock Image" : "Lock Image";

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

        return $"{ImageName}: zoom {Zoom:0.00}x, opacity {ImageOpacity:P0}, offset ({OffsetX:0}, {OffsetY:0}).";
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        return Math.Min(maximum, Math.Max(minimum, value));
    }
}
