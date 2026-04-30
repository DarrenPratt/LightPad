using System.Windows.Input;
using LightPad.App.Models;
using LightPad.App.Services;
using LightPad.App.Utilities;
using Microsoft.Maui.Graphics;

namespace LightPad.App.ViewModels;

public sealed class SettingsViewModel : BaseViewModel
{
    private const double MinBrightness = 0.05;
    private const double MaxBrightness = 1.0;
    private const double MinColorTemperature = 2700.0;
    private const double MaxColorTemperature = 9000.0;
    private readonly ISettingsService _settingsService;
    private double _brightness;
    private double _colorTemperature;
    private LightColorPreset _selectedPreset;
    private string _customColorHexInput;
    private string _appliedCustomColorHex;
    private double _defaultTraceOpacity;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _brightness = LightSurfaceStyleCalculator.Clamp(settingsService.Brightness, MinBrightness, MaxBrightness);
        _colorTemperature = LightSurfaceStyleCalculator.Clamp(settingsService.ColorTemperature, MinColorTemperature, MaxColorTemperature);
        _selectedPreset = settingsService.SelectedPreset;
        _appliedCustomColorHex = LightSurfaceStyleCalculator.NormalizeHexOrDefault(settingsService.CustomColorHex, "#FFFFFF");
        _customColorHexInput = _appliedCustomColorHex;
        _defaultTraceOpacity = LightSurfaceStyleCalculator.Clamp(settingsService.DefaultTraceOpacity, 0.1, 1.0);

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        ApplyCustomColorCommand = new Command(ApplyCustomColor);
        SelectPresetCommand = new Command<string>(SelectPreset);
        ResetDefaultsCommand = new Command(ResetDefaults);
    }

    public ICommand BackCommand { get; }

    public Command ApplyCustomColorCommand { get; }

    public Command<string> SelectPresetCommand { get; }

    public Command ResetDefaultsCommand { get; }

    public double Brightness
    {
        get => _brightness;
        set
        {
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

    public double DefaultTraceOpacity
    {
        get => _defaultTraceOpacity;
        set
        {
            var clampedValue = LightSurfaceStyleCalculator.Clamp(value, 0.1, 1.0);
            if (!SetProperty(ref _defaultTraceOpacity, clampedValue))
            {
                return;
            }

            _settingsService.DefaultTraceOpacity = clampedValue;
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public bool CanAdjustColorTemperature => SelectedPreset == LightColorPreset.White;

    public Color PreviewLightColor => LightSurfaceStyleCalculator.ResolveLightColor(SelectedPreset, ColorTemperature, _appliedCustomColorHex);

    public double PreviewOverlayOpacity => 1.0 - Brightness;

    public Color WhitePresetBackground => GetPresetBackground(LightColorPreset.White);

    public Color WarmPresetBackground => GetPresetBackground(LightColorPreset.Warm);

    public Color CoolPresetBackground => GetPresetBackground(LightColorPreset.Cool);

    public Color CustomPresetBackground => GetPresetBackground(LightColorPreset.Custom);

    public Color WhitePresetTextColor => GetPresetTextColor(LightColorPreset.White);

    public Color WarmPresetTextColor => GetPresetTextColor(LightColorPreset.Warm);

    public Color CoolPresetTextColor => GetPresetTextColor(LightColorPreset.Cool);

    public Color CustomPresetTextColor => GetPresetTextColor(LightColorPreset.Custom);

    public string StatusText =>
        $"Global defaults: {SelectedPreset} light, brightness {Brightness:P0}, {ColorTemperature:0}K, custom {AppliedCustomColorHex}, trace opacity {DefaultTraceOpacity:P0}.";

    public string ScopeSummaryText =>
        "Global defaults: light surface colour, brightness, white temperature, custom colour, and starting trace opacity. Session-local: image file, pan offset, zoom, and image lock.";

    private void SelectPreset(string? presetName)
    {
        if (!Enum.TryParse<LightColorPreset>(presetName, true, out var preset))
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

    private void ResetDefaults()
    {
        Brightness = 1.0;
        ColorTemperature = 6500.0;
        _appliedCustomColorHex = "#FFFFFF";
        _settingsService.CustomColorHex = _appliedCustomColorHex;
        CustomColorHexInput = _appliedCustomColorHex;
        OnPropertyChanged(nameof(AppliedCustomColorHex));
        SelectedPreset = LightColorPreset.White;
        DefaultTraceOpacity = 0.65;
        RaiseVisualStateChanged();
    }

    private Color GetPresetBackground(LightColorPreset preset)
    {
        return SelectedPreset == preset ? Color.FromArgb("#F5E7A1") : Color.FromArgb("#2A2A2A");
    }

    private Color GetPresetTextColor(LightColorPreset preset)
    {
        return SelectedPreset == preset ? Colors.Black : Colors.White;
    }

    private void RaiseVisualStateChanged()
    {
        OnPropertyChanged(nameof(CanAdjustColorTemperature));
        OnPropertyChanged(nameof(PreviewLightColor));
        OnPropertyChanged(nameof(PreviewOverlayOpacity));
        OnPropertyChanged(nameof(WhitePresetBackground));
        OnPropertyChanged(nameof(WarmPresetBackground));
        OnPropertyChanged(nameof(CoolPresetBackground));
        OnPropertyChanged(nameof(CustomPresetBackground));
        OnPropertyChanged(nameof(WhitePresetTextColor));
        OnPropertyChanged(nameof(WarmPresetTextColor));
        OnPropertyChanged(nameof(CoolPresetTextColor));
        OnPropertyChanged(nameof(CustomPresetTextColor));
        OnPropertyChanged(nameof(StatusText));
    }
}
