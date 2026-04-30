using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using LightPad.App.Models;
using LightPad.App.Services;
using LightPad.App.Utilities;
using Microsoft.Maui.Graphics;

namespace LightPad.App.ViewModels;

public sealed class LightboxViewModel : BaseViewModel
{
    private const double MinBrightness = 0.05;
    private const double MaxBrightness = 1.0;
    private const double MinColorTemperature = 2700.0;
    private const double MaxColorTemperature = 9000.0;
    private const int UnlockHoldDurationMs = 1200;
    private static readonly Color ActivePresetBackground = Color.FromArgb("#F5E7A1");
    private static readonly Color InactivePresetBackground = Color.FromArgb("#2A2A2A");
    private static readonly Color ActivePresetText = Colors.Black;
    private static readonly Color InactivePresetText = Colors.White;
    private readonly ISettingsService _settingsService;
    private readonly IScreenWakeService _screenWakeService;
    private double _brightness;
    private double _colorTemperature;
    private LightColorPreset _selectedPreset;
    private string _customColorHexInput;
    private string _appliedCustomColorHex;
    private bool _isLocked;
    private bool _isImmersiveChromeVisible = true;
    private bool _isUnlockHoldInProgress;
    private double _unlockProgress;
    private CancellationTokenSource? _unlockHoldCts;

    public LightboxViewModel(ISettingsService settingsService, IScreenWakeService screenWakeService)
    {
        _settingsService = settingsService;
        _screenWakeService = screenWakeService;
        _brightness = LightSurfaceStyleCalculator.Clamp(settingsService.Brightness, MinBrightness, MaxBrightness);
        _colorTemperature = LightSurfaceStyleCalculator.Clamp(settingsService.ColorTemperature, MinColorTemperature, MaxColorTemperature);
        _selectedPreset = settingsService.SelectedPreset;
        _appliedCustomColorHex = LightSurfaceStyleCalculator.NormalizeHexOrDefault(settingsService.CustomColorHex, "#FFFFFF");
        _customColorHexInput = _appliedCustomColorHex;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        ToggleLockCommand = new Command(ToggleLock);
        ApplyCustomColorCommand = new Command(ApplyCustomColor, () => !IsLocked);
        SelectPresetCommand = new Command<string>(SelectPreset, _ => !IsLocked);
    }

    public ICommand BackCommand { get; }

    public ICommand ToggleLockCommand { get; }

    public Command ApplyCustomColorCommand { get; }

    public Command<string> SelectPresetCommand { get; }

    public double Brightness
    {
        get => _brightness;
        set
        {
            if (IsLocked)
            {
                return;
            }

            var clampedValue = LightSurfaceStyleCalculator.Clamp(value, MinBrightness, MaxBrightness);
            if (!SetProperty(ref _brightness, clampedValue))
            {
                return;
            }

            _settingsService.Brightness = clampedValue;
            RaiseVisualStateChanged();
        }
    }

    public double ColorTemperature
    {
        get => _colorTemperature;
        set
        {
            if (IsLocked)
            {
                return;
            }

            var clampedValue = LightSurfaceStyleCalculator.Clamp(value, MinColorTemperature, MaxColorTemperature);
            if (!SetProperty(ref _colorTemperature, clampedValue))
            {
                return;
            }

            _settingsService.ColorTemperature = clampedValue;
            RaiseVisualStateChanged();
        }
    }

    public LightColorPreset SelectedPreset
    {
        get => _selectedPreset;
        private set
        {
            if (!SetProperty(ref _selectedPreset, value))
            {
                return;
            }

            _settingsService.SelectedPreset = value;
            RaiseVisualStateChanged();
        }
    }

    public string CustomColorHexInput
    {
        get => _customColorHexInput;
        set
        {
            if (!SetProperty(ref _customColorHexInput, value))
            {
                return;
            }

            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string AppliedCustomColorHex => _appliedCustomColorHex;

    public bool IsLocked
    {
        get => _isLocked;
        private set
        {
            if (!SetProperty(ref _isLocked, value))
            {
                return;
            }

            if (!value)
            {
                CancelUnlockHold();
            }

            RaiseInteractionStateChanged();
        }
    }

    public bool IsImmersiveChromeVisible
    {
        get => _isImmersiveChromeVisible;
        private set
        {
            if (!SetProperty(ref _isImmersiveChromeVisible, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsChromeVisible));
            OnPropertyChanged(nameof(IsLockedHintVisible));
            OnPropertyChanged(nameof(IsUnlockPanelVisible));
            OnPropertyChanged(nameof(LockStatusText));
        }
    }

    public bool IsUnlockHoldInProgress
    {
        get => _isUnlockHoldInProgress;
        private set
        {
            if (!SetProperty(ref _isUnlockHoldInProgress, value))
            {
                return;
            }

            OnPropertyChanged(nameof(UnlockButtonText));
        }
    }

    public double UnlockProgress
    {
        get => _unlockProgress;
        private set
        {
            if (!SetProperty(ref _unlockProgress, value))
            {
                return;
            }

            OnPropertyChanged(nameof(UnlockButtonText));
        }
    }

    public bool IsWakeLockActive => _screenWakeService.IsActive;

    public bool IsChromeVisible => !IsLocked || IsImmersiveChromeVisible;

    public bool CanShowEditControls => !IsLocked;

    public bool CanAdjustColorTemperature => !IsLocked && SelectedPreset == LightColorPreset.White;

    public bool CanEditCustomColor => !IsLocked && SelectedPreset == LightColorPreset.Custom;

    public bool IsUnlockPanelVisible => IsLocked && IsImmersiveChromeVisible;

    public bool IsLockedHintVisible => IsLocked && !IsImmersiveChromeVisible;

    public string LockButtonText => "Lock Surface";

    public string LockStatusText
    {
        get
        {
            if (!IsLocked)
            {
                return IsWakeLockActive
                    ? "Screen stay-awake is active. Lock the surface to hide controls during tracing."
                    : "Adjust the light surface, then lock it before tracing.";
            }

            return IsImmersiveChromeVisible
                ? "Locked. Hold the unlock button to re-open editing. A single tap hides this chrome again."
                : "Locked. Tap anywhere to reveal controls.";
        }
    }

    public string UnlockButtonText => IsUnlockHoldInProgress
        ? $"Hold to Unlock {UnlockProgress:P0}"
        : "Hold to Unlock";

    public Color LightColor => LightSurfaceStyleCalculator.ResolveLightColor(SelectedPreset, ColorTemperature, _appliedCustomColorHex);

    public double BrightnessOverlayOpacity => 1.0 - Brightness;

    public Color WhitePresetBackground => GetPresetBackground(LightColorPreset.White);

    public Color WarmPresetBackground => GetPresetBackground(LightColorPreset.Warm);

    public Color CoolPresetBackground => GetPresetBackground(LightColorPreset.Cool);

    public Color CustomPresetBackground => GetPresetBackground(LightColorPreset.Custom);

    public Color WhitePresetTextColor => GetPresetTextColor(LightColorPreset.White);

    public Color WarmPresetTextColor => GetPresetTextColor(LightColorPreset.Warm);

    public Color CoolPresetTextColor => GetPresetTextColor(LightColorPreset.Cool);

    public Color CustomPresetTextColor => GetPresetTextColor(LightColorPreset.Custom);

    public LightboxSettingsState Snapshot =>
        new()
        {
            Brightness = Brightness,
            ColorTemperature = ColorTemperature,
            SelectedPreset = SelectedPreset,
            CustomColorHex = _appliedCustomColorHex
        };

    public string StatusText => SelectedPreset switch
    {
        LightColorPreset.Custom => $"Custom light {_appliedCustomColorHex} at {Brightness:P0} brightness.",
        LightColorPreset.White => $"White light at {ColorTemperature:0}K and {Brightness:P0} brightness.",
        _ => $"{SelectedPreset} light at {Brightness:P0} brightness."
    };

    public async Task OnAppearingAsync()
    {
        await _screenWakeService.ActivateAsync(nameof(LightboxViewModel));
        RevealImmersiveChrome();
        OnPropertyChanged(nameof(IsWakeLockActive));
        OnPropertyChanged(nameof(LockStatusText));
    }

    public async Task OnDisappearingAsync()
    {
        CancelUnlockHold();
        await _screenWakeService.DeactivateAsync(nameof(LightboxViewModel));
        RevealImmersiveChrome();
        OnPropertyChanged(nameof(IsWakeLockActive));
        OnPropertyChanged(nameof(LockStatusText));
    }

    public void OnLightSurfaceTapped()
    {
        if (!IsLocked)
        {
            return;
        }

        if (IsImmersiveChromeVisible)
        {
            HideImmersiveChrome();
            return;
        }

        RevealImmersiveChrome();
    }

    public async Task BeginUnlockHoldAsync()
    {
        if (!IsLocked || IsUnlockHoldInProgress)
        {
            return;
        }

        CancelUnlockHold();
        var cts = new CancellationTokenSource();
        _unlockHoldCts = cts;
        IsUnlockHoldInProgress = true;
        UnlockProgress = 0.0;

        try
        {
            const int stepMs = 100;
            var steps = UnlockHoldDurationMs / stepMs;

            for (var step = 1; step <= steps; step++)
            {
                await Task.Delay(stepMs, cts.Token);
                UnlockProgress = (double)step / steps;
            }

            UnlockSurface();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_unlockHoldCts, cts))
            {
                _unlockHoldCts = null;
            }

            if (IsLocked)
            {
                UnlockProgress = 0.0;
                IsUnlockHoldInProgress = false;
            }

            cts.Dispose();
        }
    }

    public void CancelUnlockHold()
    {
        _unlockHoldCts?.Cancel();
        _unlockHoldCts = null;
        UnlockProgress = 0.0;
        IsUnlockHoldInProgress = false;
    }

    public void RevealImmersiveChrome()
    {
        IsImmersiveChromeVisible = true;
    }

    public void HideImmersiveChrome()
    {
        CancelUnlockHold();
        if (IsLocked)
        {
            IsImmersiveChromeVisible = false;
        }
    }

    private void SelectPreset(string? presetName)
    {
        if (IsLocked || !Enum.TryParse<LightColorPreset>(presetName, true, out var preset))
        {
            return;
        }

        if (preset == LightColorPreset.Custom)
        {
            ApplyCustomColor();
        }

        SelectedPreset = preset;
    }

    private void ApplyCustomColor()
    {
        if (IsLocked)
        {
            return;
        }

        var normalizedColor = LightSurfaceStyleCalculator.NormalizeHexOrDefault(CustomColorHexInput, _appliedCustomColorHex);
        _appliedCustomColorHex = normalizedColor;
        _settingsService.CustomColorHex = normalizedColor;

        if (CustomColorHexInput != normalizedColor)
        {
            CustomColorHexInput = normalizedColor;
        }

        OnPropertyChanged(nameof(AppliedCustomColorHex));
        if (SelectedPreset == LightColorPreset.Custom)
        {
            RaiseVisualStateChanged();
        }
    }

    private void ToggleLock()
    {
        if (IsLocked)
        {
            RevealImmersiveChrome();
            return;
        }

        IsLocked = true;
        RevealImmersiveChrome();
    }

    private void UnlockSurface()
    {
        IsLocked = false;
        RevealImmersiveChrome();
        UnlockProgress = 0.0;
        IsUnlockHoldInProgress = false;
    }

    private Color GetPresetBackground(LightColorPreset preset)
    {
        return SelectedPreset == preset ? ActivePresetBackground : InactivePresetBackground;
    }

    private Color GetPresetTextColor(LightColorPreset preset)
    {
        return SelectedPreset == preset ? ActivePresetText : InactivePresetText;
    }

    private void RaiseVisualStateChanged()
    {
        OnPropertyChanged(nameof(LightColor));
        OnPropertyChanged(nameof(BrightnessOverlayOpacity));
        OnPropertyChanged(nameof(WhitePresetBackground));
        OnPropertyChanged(nameof(WarmPresetBackground));
        OnPropertyChanged(nameof(CoolPresetBackground));
        OnPropertyChanged(nameof(CustomPresetBackground));
        OnPropertyChanged(nameof(WhitePresetTextColor));
        OnPropertyChanged(nameof(WarmPresetTextColor));
        OnPropertyChanged(nameof(CoolPresetTextColor));
        OnPropertyChanged(nameof(CustomPresetTextColor));
        OnPropertyChanged(nameof(CanEditCustomColor));
        OnPropertyChanged(nameof(CanAdjustColorTemperature));
        OnPropertyChanged(nameof(StatusText));
    }

    private void RaiseInteractionStateChanged()
    {
        OnPropertyChanged(nameof(IsChromeVisible));
        OnPropertyChanged(nameof(CanShowEditControls));
        OnPropertyChanged(nameof(CanEditCustomColor));
        OnPropertyChanged(nameof(CanAdjustColorTemperature));
        OnPropertyChanged(nameof(IsUnlockPanelVisible));
        OnPropertyChanged(nameof(IsLockedHintVisible));
        OnPropertyChanged(nameof(LockButtonText));
        OnPropertyChanged(nameof(LockStatusText));
        ApplyCustomColorCommand.ChangeCanExecute();
        SelectPresetCommand.ChangeCanExecute();
    }

}
