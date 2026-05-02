using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

#if ANDROID
using Android.Provider;
#endif

#if WINDOWS
using Windows.Graphics.Display;
#endif

namespace LightPad.App.Services;

public sealed class DeviceBrightnessService : IDeviceBrightnessService
{
    private readonly object _sync = new();
    private double _currentBrightness = 1.0;
    private bool _isHardwareControlSupported;
    private bool _isHardwareOverrideActive;

    public bool IsHardwareControlSupported
    {
        get
        {
            lock (_sync)
            {
                return _isHardwareControlSupported;
            }
        }
    }

    public bool IsHardwareOverrideActive
    {
        get
        {
            lock (_sync)
            {
                return _isHardwareOverrideActive;
            }
        }
    }

    public bool UseOverlayFallback => !IsHardwareControlSupported || !IsHardwareOverrideActive;

    public double CurrentBrightness
    {
        get
        {
            lock (_sync)
            {
                return _currentBrightness;
            }
        }
    }

    public async Task<double> GetBrightnessAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

#if ANDROID
        return await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var brightness = GetAndroidBrightness();
            UpdateState(brightness, isSupported: true, isOverrideActive: true);
            return CurrentBrightness;
        });
#elif WINDOWS
        return await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var brightnessOverride = TryGetWindowsBrightnessOverride();
            if (brightnessOverride?.IsSupported == true)
            {
                var resolvedBrightness = brightnessOverride.IsOverrideActive
                    ? Clamp(brightnessOverride.BrightnessLevel)
                    : CurrentBrightness;
                UpdateState(resolvedBrightness, isSupported: true, isOverrideActive: brightnessOverride.IsOverrideActive);
                return CurrentBrightness;
            }

            UpdateState(CurrentBrightness, isSupported: false, isOverrideActive: false);
            return CurrentBrightness;
        });
#else
        UpdateState(CurrentBrightness, isSupported: false, isOverrideActive: false);
        return CurrentBrightness;
#endif
    }

    public async Task ApplyBrightnessAsync(double brightness, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var clampedBrightness = Clamp(brightness);

#if ANDROID
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var window = Platform.CurrentActivity?.Window;
            if (window?.Attributes is null)
            {
                UpdateState(clampedBrightness, isSupported: false, isOverrideActive: false);
                return;
            }

            var layoutParameters = window.Attributes;
            layoutParameters.ScreenBrightness = (float)clampedBrightness;
            window.Attributes = layoutParameters;
            UpdateState(clampedBrightness, isSupported: true, isOverrideActive: true);
        });
#elif WINDOWS
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var brightnessOverride = TryGetWindowsBrightnessOverride();
            if (brightnessOverride?.IsSupported != true)
            {
                UpdateState(clampedBrightness, isSupported: false, isOverrideActive: false);
                return;
            }

            try
            {
                // Windows brightness override is device- and app-model-dependent.
                // If the runtime refuses or ignores the request, callers keep using
                // the overlay fallback via UseOverlayFallback.
                brightnessOverride.SetBrightnessLevel(clampedBrightness, DisplayBrightnessOverrideOptions.None);
                brightnessOverride.StartOverride();
                UpdateState(clampedBrightness, isSupported: true, isOverrideActive: brightnessOverride.IsOverrideActive);
            }
            catch
            {
                UpdateState(clampedBrightness, isSupported: false, isOverrideActive: false);
            }
        });
#else
        UpdateState(clampedBrightness, isSupported: false, isOverrideActive: false);
        await Task.CompletedTask;
#endif
    }

    public async Task RestoreDefaultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

#if ANDROID
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var window = Platform.CurrentActivity?.Window;
            if (window?.Attributes is null)
            {
                UpdateState(CurrentBrightness, isSupported: false, isOverrideActive: false);
                return;
            }

            var layoutParameters = window.Attributes;
            layoutParameters.ScreenBrightness = -1f;
            window.Attributes = layoutParameters;
            UpdateState(GetAndroidBrightness(), isSupported: true, isOverrideActive: false);
        });
#elif WINDOWS
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var brightnessOverride = TryGetWindowsBrightnessOverride();
            if (brightnessOverride?.IsSupported != true)
            {
                UpdateState(CurrentBrightness, isSupported: false, isOverrideActive: false);
                return;
            }

            try
            {
                brightnessOverride.StopOverride();
                UpdateState(CurrentBrightness, isSupported: true, isOverrideActive: false);
            }
            catch
            {
                UpdateState(CurrentBrightness, isSupported: false, isOverrideActive: false);
            }
        });
#else
        UpdateState(CurrentBrightness, isSupported: false, isOverrideActive: false);
        await Task.CompletedTask;
#endif
    }

#if ANDROID
    private double GetAndroidBrightness()
    {
        var window = Platform.CurrentActivity?.Window;
        var overrideBrightness = window?.Attributes?.ScreenBrightness ?? -1f;
        if (overrideBrightness >= 0f)
        {
            return Clamp(overrideBrightness);
        }

        try
        {
            var contentResolver = Platform.CurrentActivity?.ContentResolver;
            if (contentResolver is not null)
            {
                var brightness = Settings.System.GetInt(contentResolver, Settings.System.ScreenBrightness);
                return Clamp(brightness / 255.0);
            }
        }
        catch
        {
        }

        return CurrentBrightness;
    }
#endif

#if WINDOWS
    private static BrightnessOverride? TryGetWindowsBrightnessOverride()
    {
        try
        {
            return BrightnessOverride.GetForCurrentView();
        }
        catch
        {
            return null;
        }
    }
#endif

    private void UpdateState(double brightness, bool isSupported, bool isOverrideActive)
    {
        lock (_sync)
        {
            _currentBrightness = Clamp(brightness);
            _isHardwareControlSupported = isSupported;
            _isHardwareOverrideActive = isOverrideActive;
        }
    }

    private static double Clamp(double brightness)
    {
        return Math.Clamp(brightness, 0.0, 1.0);
    }
}
