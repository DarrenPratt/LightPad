using System.Threading;
using System.Threading.Tasks;

namespace LightPad.App.Services;

public interface IImagePickerService
{
    Task<string?> PickImageAsync(CancellationToken cancellationToken = default);
}
