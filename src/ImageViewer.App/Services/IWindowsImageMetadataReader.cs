using ImageViewer.Core.Models;

namespace ImageViewer.App.Services;

public interface IWindowsImageMetadataReader
{
    Task<Result<ImageMetadata>> ReadMetadataAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
