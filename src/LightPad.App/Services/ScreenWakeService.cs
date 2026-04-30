using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

#if ANDROID
using Android.Views;
#endif

#if WINDOWS
using Windows.System.Display;
#endif

namespace LightPad.App.Services;

public sealed class ScreenWakeService : IScreenWakeService
{
    private readonly HashSet<string> _activeOwners = [];
    private readonly object _sync = new();

#if WINDOWS
    private readonly DisplayRequest _displayRequest = new();
    private bool _displayRequestActive;
#endif

    public bool IsActive
    {
        get
        {
            lock (_sync)
            {
                return _activeOwners.Count > 0;
            }
        }
    }

    public async Task ActivateAsync(string owner, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        cancellationToken.ThrowIfCancellationRequested();

        var shouldEnable = false;

        lock (_sync)
        {
            if (_activeOwners.Add(owner) && _activeOwners.Count == 1)
            {
                shouldEnable = true;
            }
        }

        if (shouldEnable)
        {
            await ApplyKeepAwakeAsync(true).ConfigureAwait(false);
        }
    }

    public async Task DeactivateAsync(string owner, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        cancellationToken.ThrowIfCancellationRequested();

        var shouldDisable = false;

        lock (_sync)
        {
            if (_activeOwners.Remove(owner) && _activeOwners.Count == 0)
            {
                shouldDisable = true;
            }
        }

        if (shouldDisable)
        {
            await ApplyKeepAwakeAsync(false).ConfigureAwait(false);
        }
    }

    private async Task ApplyKeepAwakeAsync(bool enabled)
    {
#if ANDROID
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var activity = Platform.CurrentActivity;
            if (activity?.Window is null)
            {
                DeviceDisplay.Current.KeepScreenOn = enabled;
                return;
            }

            if (enabled)
            {
                activity.Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            }
            else
            {
                activity.Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
            }
        });
#elif WINDOWS
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (enabled && !_displayRequestActive)
            {
                _displayRequest.RequestActive();
                _displayRequestActive = true;
            }
            else if (!enabled && _displayRequestActive)
            {
                _displayRequest.RequestRelease();
                _displayRequestActive = false;
            }
        });
#else
        await MainThread.InvokeOnMainThreadAsync(() => DeviceDisplay.Current.KeepScreenOn = enabled);
#endif
    }
}
