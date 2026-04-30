using Microsoft.Maui.Storage;

namespace LightPad.App.Services;

public sealed class SettingsService : ISettingsService
{
    private const string BrightnessKey = "lightpad.brightness";
    private const string ColorTemperatureKey = "lightpad.colorTemperature";

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
}
