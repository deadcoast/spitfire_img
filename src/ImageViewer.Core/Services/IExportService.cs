using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IExportService
{
    Task<Result> ExportImageAsync(
        string sourceFilePath,
        string destinationFilePath,
        ExportOptions options,
        CancellationToken cancellationToken = default);
}
