using System;
using System.Windows.Input;
using LightPad.App.Models;
using LightPad.App.Services;
using Microsoft.Maui.Graphics;

namespace LightPad.App.ViewModels;

public sealed class LightboxViewModel : BaseViewModel
{
    private const double MinBrightness = 0.05;
    private const double MaxBrightness = 1.0;
    private const double MinColorTemperature = 2700.0;
    private const double MaxColorTemperature = 9000.0;
    private static readonly Color ActivePresetBackground = Color.FromArgb("#F5E7A1");
    private static readonly Color InactivePresetBackground = Color.FromArgb("#2A2A2A");
    private static readonly Color ActivePresetText = Colors.Black;
    private static readonly Color InactivePresetText = Colors.White;
    private readonly ISettingsService _settingsService;
    private double _brightness;
    private double _colorTemperature;
    private LightColorPreset _selectedPreset;
    private string _customColorHexInput;
    private string _appliedCustomColorHex;
    private bool _isLocked;

    public LightboxViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _brightness = Clamp(settingsService.Brightness, MinBrightness, MaxBrightness);
        _colorTemperature = Clamp(settingsService.ColorTemperature, MinColorTemperature, MaxColorTemperature);
        _selectedPreset = settingsService.SelectedPreset;
        _appliedCustomColorHex = NormalizeHexOrDefault(settingsService.CustomColorHex, "#FFFFFF");
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

            var clampedValue = Clamp(value, MinBrightness, MaxBrightness);
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

            var clampedValue = Clamp(value, MinColorTemperature, MaxColorTemperature);
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

            OnPropertyChanged(nameof(IsControlsVisible));
            OnPropertyChanged(nameof(LockButtonText));
            OnPropertyChanged(nameof(LockStatusText));
            OnPropertyChanged(nameof(CanEditCustomColor));
            OnPropertyChanged(nameof(CanAdjustColorTemperature));
            ApplyCustomColorCommand.ChangeCanExecute();
            SelectPresetCommand.ChangeCanExecute();
        }
    }

    public bool IsControlsVisible => !IsLocked;

    public bool CanAdjustColorTemperature => !IsLocked && SelectedPreset == LightColorPreset.White;

    public bool CanEditCustomColor => !IsLocked && SelectedPreset == LightColorPreset.Custom;

    public string LockButtonText => IsLocked ? "Unlock Controls" : "Lock Controls";

    public string LockStatusText => IsLocked
        ? "Locked. Brightness and colour controls are hidden until you unlock."
        : "Unlocked. Adjust the light surface, then lock it before tracing.";

    public Color LightColor => SelectedPreset switch
    {
        LightColorPreset.Warm => Color.FromArgb("#FFD6A3"),
        LightColorPreset.Cool => Color.FromArgb("#DDF1FF"),
        LightColorPreset.Custom => ParseColorOrDefault(_appliedCustomColorHex, Colors.White),
        _ => FromColorTemperature(ColorTemperature)
    };

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

        var normalizedColor = NormalizeHexOrDefault(CustomColorHexInput, _appliedCustomColorHex);
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
        IsLocked = !IsLocked;
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

    private static double Clamp(double value, double minimum, double maximum)
    {
        return Math.Min(maximum, Math.Max(minimum, value));
    }

    private static string NormalizeHexOrDefault(string? value, string fallback)
    {
        if (TryNormalizeHex(value, out var normalized))
        {
            return normalized;
        }

        return fallback;
    }

    private static bool TryNormalizeHex(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith('#'))
        {
            trimmed = $"#{trimmed}";
        }

        if (trimmed.Length != 7)
        {
            return false;
        }

        for (var index = 1; index < trimmed.Length; index++)
        {
            if (!Uri.IsHexDigit(trimmed[index]))
            {
                return false;
            }
        }

        normalized = trimmed.ToUpperInvariant();
        return true;
    }

    private static Color ParseColorOrDefault(string? colorHex, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(colorHex) && Color.TryParse(colorHex, out var parsedColor))
        {
            return parsedColor;
        }

        return fallback;
    }

    private static Color FromColorTemperature(double kelvin)
    {
        var temperature = kelvin / 100.0;
        double red;
        double green;
        double blue;

        if (temperature <= 66.0)
        {
            red = 255.0;
            green = 99.4708025861 * Math.Log(temperature) - 161.1195681661;
            blue = temperature <= 19.0
                ? 0.0
                : 138.5177312231 * Math.Log(temperature - 10.0) - 305.0447927307;
        }
        else
        {
            red = 329.698727446 * Math.Pow(temperature - 60.0, -0.1332047592);
            green = 288.1221695283 * Math.Pow(temperature - 60.0, -0.0755148492);
            blue = 255.0;
        }

        return Color.FromRgb(
            ClampToByte(red),
            ClampToByte(green),
            ClampToByte(blue));
    }

    private static int ClampToByte(double value)
    {
        return (int)Math.Round(Math.Min(255.0, Math.Max(0.0, value)));
    }
}
