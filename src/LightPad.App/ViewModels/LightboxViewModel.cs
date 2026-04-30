using LightPad.App.Models;
using LightPad.App.Services;

namespace LightPad.App.ViewModels;

public sealed class LightboxViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private double _brightness;
    private double _colorTemperature;
    private LightColorPreset _selectedPreset;
    private string _customColorHex;

    public LightboxViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _brightness = settingsService.Brightness;
        _colorTemperature = settingsService.ColorTemperature;
        _selectedPreset = settingsService.SelectedPreset;
        _customColorHex = settingsService.CustomColorHex;
    }

    public double Brightness
    {
        get => _brightness;
        set
        {
            if (!SetProperty(ref _brightness, value))
            {
                return;
            }

            _settingsService.Brightness = value;
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public double ColorTemperature
    {
        get => _colorTemperature;
        set
        {
            if (!SetProperty(ref _colorTemperature, value))
            {
                return;
            }

            _settingsService.ColorTemperature = value;
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public LightColorPreset SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (!SetProperty(ref _selectedPreset, value))
            {
                return;
            }

            _settingsService.SelectedPreset = value;
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string CustomColorHex
    {
        get => _customColorHex;
        set
        {
            if (!SetProperty(ref _customColorHex, value))
            {
                return;
            }

            _settingsService.CustomColorHex = value;
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public LightboxSettingsState Snapshot =>
        new()
        {
            Brightness = Brightness,
            ColorTemperature = ColorTemperature,
            SelectedPreset = SelectedPreset,
            CustomColorHex = CustomColorHex
        };

    public string StatusText =>
        $"Brightness {Brightness:0.##}, {SelectedPreset} preset, {ColorTemperature:0}K, custom {CustomColorHex}.";
}
