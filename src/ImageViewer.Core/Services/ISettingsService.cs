using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface ISettingsService
{
    Task<Result<ViewerSettings>> LoadSettingsAsync(CancellationToken cancellationToken = default);

    Task<Result> SaveSettingsAsync(ViewerSettings settings, CancellationToken cancellationToken = default);
}
