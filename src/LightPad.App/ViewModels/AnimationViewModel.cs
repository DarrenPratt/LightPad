using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using LightPad.App.Models;
using LightPad.App.Services;
using Microsoft.Maui.Graphics;

namespace LightPad.App.ViewModels;

public sealed class AnimationViewModel : BaseViewModel
{
    private const double MinZoom = 0.5;
    private const double MaxZoom = 4.0;
    private readonly AnimationSessionState _sessionState;
    private readonly IImagePickerService _imagePickerService;
    private readonly IScreenWakeService _screenWakeService;
    private readonly ISettingsService _settingsService;
    private readonly ObservableCollection<AnimationFrameItemViewModel> _frames = new();
    private double _offsetX;
    private double _offsetY;
    private double _zoom;
    private double _currentFrameOpacity;
    private double _onionSkinOpacity;
    private bool _isOnionSkinEnabled;
    private bool _isFrameLocked;
    private bool _isControlsExpanded;
    private bool _isBusy;
    private string _statusText;
    private double _surfaceBrightness;
    private double _surfaceColorTemperature;
    private LightColorPreset _surfacePreset;
    private string _surfaceCustomColorHex;

    public AnimationViewModel(
        AnimationSessionState sessionState,
        IScreenWakeService screenWakeService,
        IImagePickerService imagePickerService,
        ISettingsService settingsService)
    {
        _sessionState = sessionState;
        _screenWakeService = screenWakeService;
        _imagePickerService = imagePickerService;
        _settingsService = settingsService;
        _offsetX = sessionState.OffsetX;
        _offsetY = sessionState.OffsetY;
        _zoom = sessionState.Zoom <= 0 ? 1.0 : sessionState.Zoom;
        _currentFrameOpacity = sessionState.CurrentFrameOpacity;
        _onionSkinOpacity = sessionState.OnionSkinOpacity;
        _isOnionSkinEnabled = sessionState.IsOnionSkinEnabled;
        _isFrameLocked = sessionState.IsFrameLocked;
        _isControlsExpanded = sessionState.IsControlsExpanded;
        _surfaceBrightness = settingsService.Brightness;
        _surfaceColorTemperature = settingsService.ColorTemperature;
        _surfacePreset = settingsService.SelectedPreset;
        _surfaceCustomColorHex = settingsService.CustomColorHex;

        foreach (var frame in sessionState.Frames)
        {
            _frames.Add(new AnimationFrameItemViewModel(frame));
        }

        RefreshFrameSelection();
        _statusText = CreateStatusText();

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        ImportFrameCommand = new Command(async () => await ImportFrameAsync(), () => !IsBusy);
        PreviousFrameCommand = new Command(MovePreviousFrame, () => CurrentFrameIndex > 0 && !IsBusy);
        NextFrameCommand = new Command(MoveNextFrame, () => CurrentFrameIndex >= 0 && CurrentFrameIndex < Frames.Count - 1 && !IsBusy);
        SelectFrameCommand = new Command<AnimationFrameItemViewModel>(SelectFrame, frame => frame is not null && !IsBusy);
        RemoveCurrentFrameCommand = new Command(RemoveCurrentFrame, () => HasFrames && !IsBusy);
        ClearFramesCommand = new Command(ClearFrames, () => HasFrames && !IsBusy);
        ResetViewCommand = new Command(ResetView, () => HasFrames && !IsBusy);
        ToggleFrameLockCommand = new Command(ToggleFrameLock, () => HasFrames && !IsBusy);
        ToggleControlsCommand = new Command(ToggleControls);
        ZoomOutCommand = new Command(() => NudgeZoom(-0.15), () => HasFrames && !IsBusy);
        ZoomInCommand = new Command(() => NudgeZoom(0.15), () => HasFrames && !IsBusy);
    }

    public ICommand BackCommand { get; }

    public Command ImportFrameCommand { get; }

    public Command PreviousFrameCommand { get; }

    public Command NextFrameCommand { get; }

    public Command<AnimationFrameItemViewModel> SelectFrameCommand { get; }

    public Command RemoveCurrentFrameCommand { get; }

    public Command ClearFramesCommand { get; }

    public Command ResetViewCommand { get; }

    public Command ToggleFrameLockCommand { get; }

    public Command ToggleControlsCommand { get; }

    public Command ZoomOutCommand { get; }

    public Command ZoomInCommand { get; }

    public ObservableCollection<AnimationFrameItemViewModel> Frames => _frames;

    public int CurrentFrameIndex
    {
        get => _sessionState.CurrentFrameIndex;
        private set
        {
            if (_sessionState.CurrentFrameIndex == value)
            {
                return;
            }

            _sessionState.CurrentFrameIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentFrame));
            OnPropertyChanged(nameof(PreviousFrame));
            OnPropertyChanged(nameof(CurrentFrameName));
            OnPropertyChanged(nameof(CanShowOnionSkin));
            RefreshFrameSelection();
            RaiseCommandStateChanged();
            UpdateStatusText();
        }
    }

    public AnimationFrameItemViewModel? CurrentFrame =>
        CurrentFrameIndex >= 0 && CurrentFrameIndex < _frames.Count ? _frames[CurrentFrameIndex] : null;

    public AnimationFrameItemViewModel? PreviousFrame =>
        CurrentFrameIndex > 0 && CurrentFrameIndex - 1 < _frames.Count ? _frames[CurrentFrameIndex - 1] : null;

    public bool HasFrames => _frames.Count > 0;

    public bool IsEmptyStateVisible => !HasFrames;

    public string CurrentFrameName => CurrentFrame?.DisplayName ?? "No frames loaded";

    public string StatusText => _statusText;

    public double OffsetX
    {
        get => _offsetX;
        private set
        {
            if (!SetProperty(ref _offsetX, value))
            {
                return;
            }

            _sessionState.OffsetX = value;
            UpdateStatusText();
        }
    }

    public double OffsetY
    {
        get => _offsetY;
        private set
        {
            if (!SetProperty(ref _offsetY, value))
            {
                return;
            }

            _sessionState.OffsetY = value;
            UpdateStatusText();
        }
    }

    public double Zoom
    {
        get => _zoom;
        set
        {
            var clampedValue = Math.Clamp(value, MinZoom, MaxZoom);
            if (!SetProperty(ref _zoom, clampedValue))
            {
                return;
            }

            _sessionState.Zoom = clampedValue;
            UpdateStatusText();
        }
    }

    public double CurrentFrameOpacity
    {
        get => _currentFrameOpacity;
        set
        {
            var clampedValue = Math.Clamp(value, 0.1, 1.0);
            if (!SetProperty(ref _currentFrameOpacity, clampedValue))
            {
                return;
            }

            _sessionState.CurrentFrameOpacity = clampedValue;
            UpdateStatusText();
        }
    }

    public double OnionSkinOpacity
    {
        get => _onionSkinOpacity;
        set
        {
            var clampedValue = Math.Clamp(value, 0.0, 1.0);
            if (!SetProperty(ref _onionSkinOpacity, clampedValue))
            {
                return;
            }

            _sessionState.OnionSkinOpacity = clampedValue;
            OnPropertyChanged(nameof(CanShowOnionSkin));
            UpdateStatusText();
        }
    }

    public bool IsOnionSkinEnabled
    {
        get => _isOnionSkinEnabled;
        set
        {
            if (!SetProperty(ref _isOnionSkinEnabled, value))
            {
                return;
            }

            _sessionState.IsOnionSkinEnabled = value;
            OnPropertyChanged(nameof(CanShowOnionSkin));
            UpdateStatusText();
        }
    }

    public bool IsFrameLocked
    {
        get => _isFrameLocked;
        private set
        {
            if (!SetProperty(ref _isFrameLocked, value))
            {
                return;
            }

            _sessionState.IsFrameLocked = value;
            OnPropertyChanged(nameof(FrameLockButtonText));
            OnPropertyChanged(nameof(CanManipulateFrames));
            UpdateStatusText();
        }
    }

    public bool IsControlsExpanded
    {
        get => _isControlsExpanded;
        private set
        {
            if (!SetProperty(ref _isControlsExpanded, value))
            {
                return;
            }

            _sessionState.IsControlsExpanded = value;
            OnPropertyChanged(nameof(ControlsToggleText));
            OnPropertyChanged(nameof(IsFloatingShowToolsVisible));
        }
    }

    public bool IsFloatingShowToolsVisible => !IsControlsExpanded;

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

    public bool CanManipulateFrames => HasFrames && !IsFrameLocked;

    public bool CanShowOnionSkin => IsOnionSkinEnabled && PreviousFrame is not null && OnionSkinOpacity > 0.0;

    public string ControlsToggleText => IsControlsExpanded ? "Hide Tools" : "Show Tools";

    public string FrameLockButtonText => IsFrameLocked ? "Unlock Frame" : "Lock Frame";

    public string FrameCounterText => HasFrames ? $"Frame {CurrentFrameIndex + 1} / {_frames.Count}" : "No frames";

    public Color TraceBackdropColor => Utilities.LightSurfaceStyleCalculator.ResolveLightColor(
        _surfacePreset,
        _surfaceColorTemperature,
        _surfaceCustomColorHex);

    public double TraceBackdropOverlayOpacity => 1.0 - _surfaceBrightness;

    public string TraceBackdropStatusText =>
        $"Animation background reuses the global {_surfacePreset} light defaults at {_surfaceBrightness:P0} brightness.";

    public async Task OnAppearingAsync()
    {
        await _screenWakeService.ActivateAsync(nameof(AnimationViewModel));
    }

    public async Task OnDisappearingAsync()
    {
        await _screenWakeService.DeactivateAsync(nameof(AnimationViewModel));
    }

    public void ApplyPan(double startX, double startY, double deltaX, double deltaY)
    {
        if (!CanManipulateFrames)
        {
            return;
        }

        OffsetX = startX + deltaX;
        OffsetY = startY + deltaY;
    }

    public void ApplyZoom(double scale)
    {
        if (!CanManipulateFrames)
        {
            return;
        }

        Zoom = scale;
    }

    public async Task ImportFrameAsync()
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
                UpdateStatusText("Frame import cancelled.");
                return;
            }

            var frame = new AnimationFrameState
            {
                FilePath = selectedImagePath,
                DisplayName = Path.GetFileName(selectedImagePath)
            };

            _sessionState.Frames.Add(frame);
            _frames.Add(new AnimationFrameItemViewModel(frame));
            CurrentFrameIndex = _frames.Count - 1;
            OnPropertyChanged(nameof(HasFrames));
            OnPropertyChanged(nameof(IsEmptyStateVisible));
            OnPropertyChanged(nameof(FrameCounterText));
            OnPropertyChanged(nameof(CanManipulateFrames));
            RaiseCommandStateChanged();
            UpdateStatusText($"Added {frame.DisplayName} to the animation sequence.");
        }
        catch (Exception exception)
        {
            UpdateStatusText($"Frame import failed: {exception.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void MovePreviousFrame()
    {
        if (CurrentFrameIndex <= 0)
        {
            return;
        }

        CurrentFrameIndex--;
        OnPropertyChanged(nameof(FrameCounterText));
    }

    private void MoveNextFrame()
    {
        if (CurrentFrameIndex < 0 || CurrentFrameIndex >= _frames.Count - 1)
        {
            return;
        }

        CurrentFrameIndex++;
        OnPropertyChanged(nameof(FrameCounterText));
    }

    private void SelectFrame(AnimationFrameItemViewModel? frame)
    {
        if (frame is null)
        {
            return;
        }

        var index = _frames.IndexOf(frame);
        if (index < 0)
        {
            return;
        }

        CurrentFrameIndex = index;
        OnPropertyChanged(nameof(FrameCounterText));
    }

    private void RemoveCurrentFrame()
    {
        if (!HasFrames || CurrentFrameIndex < 0)
        {
            return;
        }

        _sessionState.Frames.RemoveAt(CurrentFrameIndex);
        _frames.RemoveAt(CurrentFrameIndex);

        if (_frames.Count == 0)
        {
            _sessionState.CurrentFrameIndex = -1;
            OnPropertyChanged(nameof(CurrentFrameIndex));
            OnPropertyChanged(nameof(CurrentFrame));
            OnPropertyChanged(nameof(PreviousFrame));
            OnPropertyChanged(nameof(CurrentFrameName));
            OnPropertyChanged(nameof(CanShowOnionSkin));
        }
        else if (CurrentFrameIndex >= _frames.Count)
        {
            CurrentFrameIndex = _frames.Count - 1;
        }
        else
        {
            OnPropertyChanged(nameof(CurrentFrame));
            OnPropertyChanged(nameof(PreviousFrame));
            OnPropertyChanged(nameof(CurrentFrameName));
            OnPropertyChanged(nameof(CanShowOnionSkin));
            RefreshFrameSelection();
        }

        OnPropertyChanged(nameof(HasFrames));
        OnPropertyChanged(nameof(IsEmptyStateVisible));
        OnPropertyChanged(nameof(FrameCounterText));
        OnPropertyChanged(nameof(CanManipulateFrames));
        RaiseCommandStateChanged();
        UpdateStatusText(_frames.Count == 0 ? "Removed the last frame." : $"Removed the current frame. {_frames.Count} frame(s) remain.");
    }

    private void ClearFrames()
    {
        _sessionState.Frames.Clear();
        _frames.Clear();
        _sessionState.CurrentFrameIndex = -1;
        OffsetX = 0.0;
        OffsetY = 0.0;
        Zoom = 1.0;
        OnPropertyChanged(nameof(CurrentFrameIndex));
        OnPropertyChanged(nameof(CurrentFrame));
        OnPropertyChanged(nameof(PreviousFrame));
        OnPropertyChanged(nameof(CurrentFrameName));
        OnPropertyChanged(nameof(HasFrames));
        OnPropertyChanged(nameof(IsEmptyStateVisible));
        OnPropertyChanged(nameof(CanShowOnionSkin));
        OnPropertyChanged(nameof(FrameCounterText));
        OnPropertyChanged(nameof(CanManipulateFrames));
        RaiseCommandStateChanged();
        UpdateStatusText("Cleared the animation sequence.");
    }

    private void ResetView()
    {
        OffsetX = 0.0;
        OffsetY = 0.0;
        Zoom = 1.0;
        UpdateStatusText(HasFrames ? $"Reset view for {CurrentFrameName}." : "View reset.");
    }

    private void ToggleFrameLock()
    {
        if (!HasFrames)
        {
            return;
        }

        IsFrameLocked = !IsFrameLocked;
    }

    private void ToggleControls()
    {
        IsControlsExpanded = !IsControlsExpanded;
    }

    private void NudgeZoom(double delta)
    {
        if (!HasFrames)
        {
            return;
        }

        Zoom += delta;
    }

    private void RefreshFrameSelection()
    {
        for (var i = 0; i < _frames.Count; i++)
        {
            _frames[i].IsSelected = i == CurrentFrameIndex;
        }

        OnPropertyChanged(nameof(FrameCounterText));
    }

    private void RaiseCommandStateChanged()
    {
        ImportFrameCommand.ChangeCanExecute();
        PreviousFrameCommand.ChangeCanExecute();
        NextFrameCommand.ChangeCanExecute();
        SelectFrameCommand.ChangeCanExecute();
        RemoveCurrentFrameCommand.ChangeCanExecute();
        ClearFramesCommand.ChangeCanExecute();
        ResetViewCommand.ChangeCanExecute();
        ToggleFrameLockCommand.ChangeCanExecute();
        ZoomOutCommand.ChangeCanExecute();
        ZoomInCommand.ChangeCanExecute();
    }

    private void UpdateStatusText(string? overrideText = null)
    {
        _statusText = overrideText ?? CreateStatusText();
        OnPropertyChanged(nameof(StatusText));
    }

    private string CreateStatusText()
    {
        if (!HasFrames)
        {
            return "Import frames one at a time to build an onion-skin sequence.";
        }

        var onionText = CanShowOnionSkin ? $"onion {OnionSkinOpacity:P0}" : "onion off";
        return $"{CurrentFrameName}: {FrameCounterText.ToLowerInvariant()}, zoom {Zoom:0.00}x, current {CurrentFrameOpacity:P0}, {onionText}.";
    }
}

public sealed class AnimationFrameItemViewModel : BaseViewModel
{
    private bool _isSelected;

    public AnimationFrameItemViewModel(AnimationFrameState state)
    {
        State = state;
    }

    public AnimationFrameState State { get; }

    public string FilePath => State.FilePath;

    public string DisplayName => State.DisplayName;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetProperty(ref _isSelected, value))
            {
                return;
            }

            OnPropertyChanged(nameof(FrameChipBackground));
            OnPropertyChanged(nameof(FrameChipTextColor));
        }
    }

    public Color FrameChipBackground => IsSelected ? Color.FromArgb("#F5E7A1") : Color.FromArgb("#252525");

    public Color FrameChipTextColor => IsSelected ? Colors.Black : Colors.White;
}
