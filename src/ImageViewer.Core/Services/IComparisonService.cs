using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IComparisonService
{
    Result<IReadOnlyList<int>> CalculateComparisonIndices(
        int currentIndex,
        int imageCount,
        int playlistSize);

    Result<ComparisonLayoutPlan> CalculateSideBySideLayout(int imageCount);

    Result<RelativePosition> ConvertToRelativePosition(
        ImagePoint absolutePosition,
        ImageDimensions imageSize);

    Result<IReadOnlyList<ComparisonImageViewport>> SynchronizeZoom(
        IReadOnlyList<ComparisonImageViewport> viewports,
        int sourceIndex,
        double zoomLevel,
        ImagePoint panOffset);
}
