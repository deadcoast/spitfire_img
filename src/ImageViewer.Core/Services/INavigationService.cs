using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface INavigationService
{
    Task<Result<NavigationResult>> NavigateToIndexAsync(
        Playlist playlist,
        int targetIndex,
        CancellationToken cancellationToken = default);
}
