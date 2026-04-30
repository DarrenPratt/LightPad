namespace LightPad.App.Models;

public sealed class TraceImageState
{
    public string? FilePath { get; set; }

    public double Zoom { get; set; } = 1.0;

    public double Rotation { get; set; }

    public double Opacity { get; set; } = 1.0;

    public bool IsLocked { get; set; }
}
