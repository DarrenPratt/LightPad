using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace LightPad.App.Services;

public sealed class ImagePickerService : IImagePickerService
{
    public async Task<string?> PickImageAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Choose a reference image",
            FileTypes = FilePickerFileType.Images
        });

        if (result is null)
        {
            return null;
        }

        return result.FullPath;
    }
}
