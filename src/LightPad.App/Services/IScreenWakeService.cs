using System.Threading;
using System.Threading.Tasks;

namespace LightPad.App.Services;

public interface IScreenWakeService
{
    bool IsActive { get; }

    Task ActivateAsync(string owner, CancellationToken cancellationToken = default);

    Task DeactivateAsync(string owner, CancellationToken cancellationToken = default);
}
