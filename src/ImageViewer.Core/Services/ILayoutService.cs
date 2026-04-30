using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface ILayoutService
{
    Task<Result<LayoutSet>> EnterGridAsync(
        Playlist playlist,
        LayoutConfig config,
        int? startIndex = null,
        CancellationToken cancellationToken = default);

    Task<Result<LayoutSet>> EnterFilmstripAsync(
        Playlist playlist,
        int? focusIndex = null,
        CancellationToken cancellationToken = default);

    IReadOnlyList<GridLayoutRegion> CalculateGridRegions(
        LayoutConfig config,
        double canvasWidth,
        double canvasHeight);

    LayoutImageFit CalculateAspectPreservingFit(
        ImageMetadata imageMetadata,
        GridLayoutRegion region);
}
