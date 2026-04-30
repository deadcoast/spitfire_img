using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IComparisonService
{
    Task<Result<ComparisonSet>> EnterComparisonAsync(
        Playlist playlist,
        ComparisonLayout layout,
        int imageCount,
        CancellationToken cancellationToken = default);

    Task<Result<ComparisonSet>> ShiftComparisonAsync(
        Playlist playlist,
        IReadOnlyList<int> currentIndices,
        int offset,
        ComparisonLayout layout,
        CancellationToken cancellationToken = default);

    IReadOnlyList<ComparisonLayoutRegion> CalculateSideBySideLayout(
        int imageCount,
        double canvasWidth,
        double canvasHeight);

    ComparisonImageFit CalculateAspectPreservingFit(
        ImageMetadata imageMetadata,
        ComparisonLayoutRegion region);
}
