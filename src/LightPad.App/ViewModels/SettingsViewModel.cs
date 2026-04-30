using LightPad.App.Models;
using LightPad.App.Services;

namespace LightPad.App.ViewModels;

public sealed class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public double Brightness => _settingsService.Brightness;

    public double ColorTemperature => _settingsService.ColorTemperature;

    public LightColorPreset SelectedPreset => _settingsService.SelectedPreset;

    public string CustomColorHex => _settingsService.CustomColorHex;

    public string StatusText =>
        $"Persisted defaults: brightness {Brightness:0.##}, preset {SelectedPreset}, {ColorTemperature:0}K, custom {CustomColorHex}.";
}
