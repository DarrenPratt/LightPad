namespace LightPad.App.Models;

public sealed class LightboxSettingsState
{
    public double Brightness { get; set; } = 1.0;

    public double ColorTemperature { get; set; } = 6500.0;

    public LightColorPreset SelectedPreset { get; set; } = LightColorPreset.White;

    public string CustomColorHex { get; set; } = "#FFFFFF";
}
