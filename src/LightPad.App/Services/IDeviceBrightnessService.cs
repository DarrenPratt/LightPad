using System.Threading;
using System.Threading.Tasks;

namespace LightPad.App.Services;

public interface IDeviceBrightnessService
{
    bool IsHardwareControlSupported { get; }

    bool IsHardwareOverrideActive { get; }

    bool UseOverlayFallback { get; }

    double CurrentBrightness { get; }

    Task<double> GetBrightnessAsync(CancellationToken cancellationToken = default);

    Task ApplyBrightnessAsync(double brightness, CancellationToken cancellationToken = default);

    Task RestoreDefaultAsync(CancellationToken cancellationToken = default);
}
