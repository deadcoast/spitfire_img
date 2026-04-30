using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class ExportService : IExportService
{
    public async Task<Result> ExportImageAsync(
        string sourceFilePath,
        string destinationFilePath,
        ExportOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(sourceFilePath))
            {
                return Result.Failure(ViewerError.ExportError("ExportImage", sourceFilePath));
            }

            if (File.Exists(destinationFilePath) && !options.OverwriteExisting)
            {
                return Result.Failure(ViewerError.ExportError("ExportImage", destinationFilePath));
            }

            var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            await using var source = File.OpenRead(sourceFilePath);
            await using var destination = File.Create(destinationFilePath);
            await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failure(ViewerError.ExportError("ExportImage", destinationFilePath, ex));
        }
    }
}
