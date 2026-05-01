namespace LightPad.App.Models;

public sealed class TraceSessionState
{
    private const int MaxHistoryEntries = 10;
    private readonly Stack<TraceImageSnapshot> _undoHistory = new();
    private readonly Stack<TraceImageSnapshot> _redoHistory = new();

    public TraceImageState ActiveImage { get; } = new();

    public bool IsSessionLocked { get; set; }

    public bool GridVisible { get; set; }

    public double GridSpacing { get; set; } = 48.0;

    public bool CanUndo => _undoHistory.Count > 0;

    public bool CanRedo => _redoHistory.Count > 0;

    public void PushUndoState(TraceImageSnapshot snapshot)
    {
        _undoHistory.Push(snapshot);
        TrimHistory(_undoHistory);
        _redoHistory.Clear();
    }

    public bool TryPopUndo(out TraceImageSnapshot snapshot)
    {
        return _undoHistory.TryPop(out snapshot);
    }

    public void PushRedoState(TraceImageSnapshot snapshot)
    {
        _redoHistory.Push(snapshot);
        TrimHistory(_redoHistory);
    }

    public bool TryPopRedo(out TraceImageSnapshot snapshot)
    {
        return _redoHistory.TryPop(out snapshot);
    }

    private static void TrimHistory(Stack<TraceImageSnapshot> history)
    {
        if (history.Count <= MaxHistoryEntries)
        {
            return;
        }

        var snapshots = history.ToArray();
        history.Clear();
        for (var index = MaxHistoryEntries - 1; index >= 0; index--)
        {
            history.Push(snapshots[index]);
        }
    }
}

public readonly record struct TraceImageSnapshot(
    string? FilePath,
    double OffsetX,
    double OffsetY,
    double Zoom,
    double Rotation,
    double Opacity,
    bool IsLocked);
