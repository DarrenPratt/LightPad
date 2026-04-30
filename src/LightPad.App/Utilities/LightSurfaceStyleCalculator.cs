using System;
using LightPad.App.Models;
using Microsoft.Maui.Graphics;

namespace LightPad.App.Utilities;

public static class LightSurfaceStyleCalculator
{
    public static Color ResolveLightColor(LightColorPreset preset, double colorTemperature, string? customColorHex)
    {
        return preset switch
        {
            LightColorPreset.Warm => Color.FromArgb("#FFD6A3"),
            LightColorPreset.Cool => Color.FromArgb("#DDF1FF"),
            LightColorPreset.Custom => ParseColorOrDefault(customColorHex, Colors.White),
            _ => FromColorTemperature(colorTemperature)
        };
    }

    public static string NormalizeHexOrDefault(string? value, string fallback)
    {
        if (TryNormalizeHex(value, out var normalized))
        {
            return normalized;
        }

        return fallback;
    }

    public static bool TryNormalizeHex(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith('#'))
        {
            trimmed = $"#{trimmed}";
        }

        if (trimmed.Length != 7)
        {
            return false;
        }

        for (var index = 1; index < trimmed.Length; index++)
        {
            if (!Uri.IsHexDigit(trimmed[index]))
            {
                return false;
            }
        }

        normalized = trimmed.ToUpperInvariant();
        return true;
    }

    public static double Clamp(double value, double minimum, double maximum)
    {
        return Math.Min(maximum, Math.Max(minimum, value));
    }

    private static Color ParseColorOrDefault(string? colorHex, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(colorHex) && Color.TryParse(colorHex, out var parsedColor))
        {
            return parsedColor;
        }

        return fallback;
    }

    private static Color FromColorTemperature(double kelvin)
    {
        var temperature = kelvin / 100.0;
        double red;
        double green;
        double blue;

        if (temperature <= 66.0)
        {
            red = 255.0;
            green = 99.4708025861 * Math.Log(temperature) - 161.1195681661;
            blue = temperature <= 19.0
                ? 0.0
                : 138.5177312231 * Math.Log(temperature - 10.0) - 305.0447927307;
        }
        else
        {
            red = 329.698727446 * Math.Pow(temperature - 60.0, -0.1332047592);
            green = 288.1221695283 * Math.Pow(temperature - 60.0, -0.0755148492);
            blue = 255.0;
        }

        return Color.FromRgb(
            ClampToByte(red),
            ClampToByte(green),
            ClampToByte(blue));
    }

    private static int ClampToByte(double value)
    {
        return (int)Math.Round(Math.Min(255.0, Math.Max(0.0, value)));
    }
}
