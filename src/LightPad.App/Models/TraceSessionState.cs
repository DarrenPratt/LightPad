namespace LightPad.App.Models;

public sealed class TraceSessionState
{
    public TraceImageState ActiveImage { get; } = new();

    public bool IsSessionLocked { get; set; }

    public bool GridVisible { get; set; }

    public double GridSpacing { get; set; } = 48.0;
}
