using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class ComparisonService : IComparisonService
{
    public const int DefaultImageCount = 2;
    public const int MinimumImageCount = 2;
    public const int MaximumImageCount = 4;

    public Result<IReadOnlyList<int>> CalculateComparisonIndices(
        int currentIndex,
        int imageCount,
        int playlistSize)
    {
        if (playlistSize < MinimumImageCount)
        {
            return Result<IReadOnlyList<int>>.Failure(
                ViewerError.ComparisonError("CalculateComparisonIndices", "Comparison mode requires at least two images"));
        }

        var clampedImageCount = Math.Clamp(imageCount, MinimumImageCount, Math.Min(MaximumImageCount, playlistSize));
        var lastStartIndex = playlistSize - clampedImageCount;
        var startIndex = Math.Clamp(currentIndex, 0, lastStartIndex);
        var indices = Enumerable.Range(startIndex, clampedImageCount).ToArray();

        return Result<IReadOnlyList<int>>.Success(indices);
    }

    public Result<ComparisonLayoutPlan> CalculateSideBySideLayout(int imageCount)
    {
        if (imageCount is < MinimumImageCount or > MaximumImageCount)
        {
            return Result<ComparisonLayoutPlan>.Failure(
                ViewerError.ComparisonError("CalculateSideBySideLayout", "Comparison mode supports two to four images"));
        }

        var rows = imageCount == MaximumImageCount ? 2 : 1;
        var columns = imageCount == MaximumImageCount ? 2 : imageCount;
        var cells = new List<ComparisonCell>(imageCount);

        for (var index = 0; index < imageCount; index++)
        {
            cells.Add(new ComparisonCell(
                Row: index / columns,
                Column: index % columns,
                WidthWeight: 1,
                HeightWeight: 1,
                PreservesAspectRatio: true));
        }

        return Result<ComparisonLayoutPlan>.Success(
            new ComparisonLayoutPlan(ComparisonLayout.SideBySide, rows, columns, cells));
    }

    public Result<RelativePosition> ConvertToRelativePosition(
        ImagePoint absolutePosition,
        ImageDimensions imageSize)
    {
        var normalizedSize = imageSize.Normalize();
        var relativeX = Math.Clamp(absolutePosition.X / normalizedSize.Width, 0, 1);
        var relativeY = Math.Clamp(absolutePosition.Y / normalizedSize.Height, 0, 1);

        return Result<RelativePosition>.Success(new RelativePosition(relativeX, relativeY));
    }

    public Result<IReadOnlyList<ComparisonImageViewport>> SynchronizeZoom(
        IReadOnlyList<ComparisonImageViewport> viewports,
        int sourceIndex,
        double zoomLevel,
        ImagePoint panOffset)
    {
        if (viewports.Count == 0 || sourceIndex < 0 || sourceIndex >= viewports.Count)
        {
            return Result<IReadOnlyList<ComparisonImageViewport>>.Failure(
                ViewerError.ComparisonError("SynchronizeZoom", "Unable to synchronize comparison zoom."));
        }

        var relativePosition = ConvertToRelativePosition(panOffset, viewports[sourceIndex].Dimensions);
        if (relativePosition.IsFailure)
        {
            return Result<IReadOnlyList<ComparisonImageViewport>>.Failure(relativePosition.Error);
        }

        var synchronized = viewports
            .Select(viewport =>
            {
                var dimensions = viewport.Dimensions.Normalize();
                var synchronizedPan = new ImagePoint(
                    relativePosition.Value.X * dimensions.Width,
                    relativePosition.Value.Y * dimensions.Height);

                return viewport
                    .WithDimensions(dimensions)
                    .WithZoom(zoomLevel)
                    .WithPan(synchronizedPan);
            })
            .ToArray();

        return Result<IReadOnlyList<ComparisonImageViewport>>.Success(synchronized);
    }
}
