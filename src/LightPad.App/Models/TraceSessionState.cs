namespace LightPad.App.Models;

public sealed class TraceSessionState
{
    public TraceImageState ActiveImage { get; } = new();

    public bool IsSessionLocked { get; set; }
}
