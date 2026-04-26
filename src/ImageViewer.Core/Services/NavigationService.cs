using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IImageDecodeService _imageDecodeService;

    public NavigationService(IImageDecodeService imageDecodeService)
    {
        _imageDecodeService = imageDecodeService;
    }

    public async Task<Result<NavigationResult>> NavigateToIndexAsync(
        Playlist playlist,
        int targetIndex,
        CancellationToken cancellationToken = default)
    {
        if (playlist.Items.Count == 0)
        {
            return Result<NavigationResult>.Failure(
                ViewerError.NavigationError("NavigateToIndex", playlist.FolderPath));
        }

        var resolvedIndex = Math.Clamp(targetIndex, 0, playlist.Items.Count - 1);
        var targetItem = playlist.Items[resolvedIndex];
        var decodeResult = await _imageDecodeService.DecodeImageAsync(targetItem.FilePath, cancellationToken)
            .ConfigureAwait(false);

        if (decodeResult.IsFailure)
        {
            return Result<NavigationResult>.Failure(
                ViewerError.NavigationError("NavigateToIndex", targetItem.FilePath));
        }

        return Result<NavigationResult>.Success(new NavigationResult(resolvedIndex, decodeResult.Value));
    }
}
