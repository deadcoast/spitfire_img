using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class MetadataService : IMetadataService
{
    public Task<Result<ExifMetadata>> ExtractExifAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return Task.FromResult(Result<ExifMetadata>.Failure(
                ViewerError.MetadataExtractionError("ExtractExif", filePath)));
        }

        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Task.FromResult(Result<ExifMetadata>.Success(ExifMetadata.Empty));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result<ExifMetadata>.Failure(
                ViewerError.MetadataExtractionError("ExtractExif", filePath, ex)));
        }
    }
}
