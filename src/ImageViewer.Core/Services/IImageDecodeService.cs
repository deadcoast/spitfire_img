using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IImageDecodeService
{
    Task<Result<DecodedImage>> DecodeImageAsync(string filePath, CancellationToken cancellationToken = default);

    bool IsSupportedFormat(string extension);

    bool IsDeferredFormat(string extension);
}
