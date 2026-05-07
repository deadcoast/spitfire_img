using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IPlatformImageDecodeService
{
    Task<Result<DecodedImage>> DecodeImageAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
