using System.Collections.Generic;

namespace LightPad.App.Models;

public sealed class AnimationSessionState
{
    public List<AnimationFrameState> Frames { get; } = new();

    public int CurrentFrameIndex { get; set; } = -1;

    public double OffsetX { get; set; }

    public double OffsetY { get; set; }

    public double Zoom { get; set; } = 1.0;

    public double CurrentFrameOpacity { get; set; } = 1.0;

    public double OnionSkinOpacity { get; set; } = 0.35;

    public bool IsOnionSkinEnabled { get; set; } = true;

    public bool IsFrameLocked { get; set; }

    public bool IsControlsExpanded { get; set; } = true;
}
