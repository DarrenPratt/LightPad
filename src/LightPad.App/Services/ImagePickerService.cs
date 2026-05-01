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

        if (!string.IsNullOrWhiteSpace(result.FullPath) && File.Exists(result.FullPath))
        {
            return result.FullPath;
        }

        await using var sourceStream = await result.OpenReadAsync();
        var extension = Path.GetExtension(result.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".img";
        }

        var safeFileName = $"trace-{Guid.NewGuid():N}{extension}";
        var localPath = Path.Combine(FileSystem.CacheDirectory, safeFileName);

        await using (var destinationStream = File.Create(localPath))
        {
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);
        }

        return localPath;
    }
}
