using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IMetadataService
{
    Task<Result<ExifMetadata>> ExtractExifAsync(string filePath, CancellationToken cancellationToken = default);
}
