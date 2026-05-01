using Microsoft.Maui.Devices;

namespace LightPad.App.Utilities;

public static class GestureHintContentFactory
{
    public static GestureHintContent CreateTraceHint()
    {
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            return new GestureHintContent(
                "Trace Controls",
                "These tips disappear after 5 seconds or your first gesture.",
                [
                    "Drag with one finger or pen to pan the reference image.",
                    "Pinch with two fingers to zoom in or out.",
                    "Use Rotate - / Rotate + or the rotation slider to fine-tune alignment."
                ]);
        }

        return new GestureHintContent(
            "Trace Controls",
            "These tips disappear after 5 seconds or your first gesture.",
            [
                "Drag with one finger to pan the reference image.",
                "Pinch with two fingers to zoom in or out.",
                "Use Rotate - / Rotate + or the rotation slider to fine-tune alignment."
            ]);
    }

    public static GestureHintContent CreateAnimationHint()
    {
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            return new GestureHintContent(
                "Animation Controls",
                "These tips disappear after 5 seconds or your first gesture.",
                [
                    "Drag with one finger or pen to pan the current frame.",
                    "Pinch with two fingers to zoom the frame stack.",
                    "Double-tap the canvas to reset the current view."
                ]);
        }

        return new GestureHintContent(
            "Animation Controls",
            "These tips disappear after 5 seconds or your first gesture.",
            [
                "Drag with one finger to pan the current frame.",
                "Pinch with two fingers to zoom the frame stack.",
                "Double-tap the canvas to reset the current view."
            ]);
    }
}

public sealed record GestureHintContent(string Title, string Subtitle, IReadOnlyList<string> Tips);
