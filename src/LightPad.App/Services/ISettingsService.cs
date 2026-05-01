using LightPad.App.Models;

namespace LightPad.App.Services;

public interface ISettingsService
{
    double Brightness { get; set; }

    double ColorTemperature { get; set; }

    LightColorPreset SelectedPreset { get; set; }

    string CustomColorHex { get; set; }

    double DefaultTraceOpacity { get; set; }

    bool HasSeenTraceGestureHint { get; set; }

    bool HasSeenAnimationGestureHint { get; set; }
}
