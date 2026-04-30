using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IRecentFilesService
{
    Task<Result<IReadOnlyList<RecentFile>>> LoadRecentFilesAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RecentFile>>> AddRecentFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RecentFile>>> RemoveRecentFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task<Result> ClearRecentFilesAsync(CancellationToken cancellationToken = default);
}
