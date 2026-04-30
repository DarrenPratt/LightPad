using System;
using LightPad.App.Models;
using Microsoft.Maui.Storage;

namespace LightPad.App.Services;

public sealed class SettingsService : ISettingsService
{
    private const string BrightnessKey = "lightpad.brightness";
    private const string ColorTemperatureKey = "lightpad.colorTemperature";
    private const string SelectedPresetKey = "lightpad.selectedPreset";
    private const string CustomColorHexKey = "lightpad.customColorHex";
    private const string DefaultTraceOpacityKey = "lightpad.defaultTraceOpacity";

    public double Brightness
    {
        get => Preferences.Default.Get(BrightnessKey, 1.0);
        set => Preferences.Default.Set(BrightnessKey, value);
    }

    public double ColorTemperature
    {
        get => Preferences.Default.Get(ColorTemperatureKey, 6500.0);
        set => Preferences.Default.Set(ColorTemperatureKey, value);
    }

    public LightColorPreset SelectedPreset
    {
        get
        {
            var storedValue = Preferences.Default.Get(SelectedPresetKey, LightColorPreset.White.ToString());
            return Enum.TryParse<LightColorPreset>(storedValue, true, out var preset)
                ? preset
                : LightColorPreset.White;
        }
        set => Preferences.Default.Set(SelectedPresetKey, value.ToString());
    }

    public string CustomColorHex
    {
        get => Preferences.Default.Get(CustomColorHexKey, "#FFFFFF");
        set => Preferences.Default.Set(CustomColorHexKey, value);
    }

    public double DefaultTraceOpacity
    {
        get => Preferences.Default.Get(DefaultTraceOpacityKey, 0.65);
        set => Preferences.Default.Set(DefaultTraceOpacityKey, value);
    }
}
