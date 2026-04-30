using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class ComparisonService : IComparisonService
{
    private readonly IImageDecodeService _imageDecodeService;

    public ComparisonService(IImageDecodeService imageDecodeService)
    {
        _imageDecodeService = imageDecodeService;
    }

    public Task<Result<ComparisonSet>> EnterComparisonAsync(
        Playlist playlist,
        ComparisonLayout layout,
        int imageCount,
        CancellationToken cancellationToken = default)
    {
        return BuildComparisonAsync(playlist, playlist.CurrentIndex, layout, imageCount, cancellationToken);
    }

    public Task<Result<ComparisonSet>> ShiftComparisonAsync(
        Playlist playlist,
        IReadOnlyList<int> currentIndices,
        int offset,
        ComparisonLayout layout,
        CancellationToken cancellationToken = default)
    {
        var startIndex = currentIndices.Count > 0
            ? currentIndices[0] + offset
            : playlist.CurrentIndex + offset;

        return BuildComparisonAsync(playlist, startIndex, layout, currentIndices.Count, cancellationToken);
    }

    public IReadOnlyList<ComparisonLayoutRegion> CalculateSideBySideLayout(
        int imageCount,
        double canvasWidth,
        double canvasHeight)
    {
        var visibleCount = Math.Clamp(imageCount, 2, 4);
        var rows = visibleCount <= 3 ? 1 : 2;
        var columns = visibleCount <= 3 ? visibleCount : 2;
        var cellWidth = canvasWidth / columns;
        var cellHeight = canvasHeight / rows;
        var regions = new List<ComparisonLayoutRegion>(visibleCount);

        for (var index = 0; index < visibleCount; index++)
        {
            var row = index / columns;
            var column = index % columns;
            regions.Add(new ComparisonLayoutRegion(
                index,
                row,
                column,
                column * cellWidth,
                row * cellHeight,
                cellWidth,
                cellHeight));
        }

        return regions;
    }

    public ComparisonImageFit CalculateAspectPreservingFit(
        ImageMetadata imageMetadata,
        ComparisonLayoutRegion region)
    {
        if (imageMetadata.PixelWidth <= 0 ||
            imageMetadata.PixelHeight <= 0 ||
            region.Width <= 0 ||
            region.Height <= 0)
        {
            return new ComparisonImageFit(0, 0);
        }

        var scale = Math.Min(
            region.Width / imageMetadata.PixelWidth,
            region.Height / imageMetadata.PixelHeight);

        return new ComparisonImageFit(
            imageMetadata.PixelWidth * scale,
            imageMetadata.PixelHeight * scale);
    }

    private async Task<Result<ComparisonSet>> BuildComparisonAsync(
        Playlist playlist,
        int startIndex,
        ComparisonLayout layout,
        int imageCount,
        CancellationToken cancellationToken)
    {
        if (playlist.Items.Count < 2)
        {
            return Result<ComparisonSet>.Failure(ViewerError.ComparisonError("EnterComparison", playlist.FolderPath));
        }

        var visibleCount = Math.Clamp(imageCount, 2, Math.Min(4, playlist.Items.Count));
        var normalizedStart = Mod(startIndex, playlist.Items.Count);
        var images = new List<ComparisonImage>(visibleCount);
        for (var offset = 0; offset < visibleCount; offset++)
        {
            var playlistIndex = Mod(normalizedStart + offset, playlist.Items.Count);
            var item = playlist.Items[playlistIndex];
            var decode = await _imageDecodeService.DecodeImageAsync(item.FilePath, cancellationToken)
                .ConfigureAwait(false);
            if (decode.IsFailure)
            {
                return Result<ComparisonSet>.Failure(ViewerError.ComparisonError("EnterComparison", item.FilePath));
            }

            images.Add(new ComparisonImage(playlistIndex, decode.Value));
        }

        return Result<ComparisonSet>.Success(new ComparisonSet(layout, images));
    }

    private static int Mod(int value, int divisor)
    {
        return ((value % divisor) + divisor) % divisor;
    }
}
