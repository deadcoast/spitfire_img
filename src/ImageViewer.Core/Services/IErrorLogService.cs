using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IErrorLogService
{
    Task<Result> LogErrorAsync(ViewerError error, CancellationToken cancellationToken = default);

    Task<Result> CleanupOldLogsAsync(CancellationToken cancellationToken = default);
}
