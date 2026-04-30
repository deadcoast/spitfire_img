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

        var requestedVisibleCount = Math.Clamp(imageCount, 2, Math.Min(4, playlist.Items.Count));
        var normalizedStart = Mod(startIndex, playlist.Items.Count);

        for (var visibleCount = requestedVisibleCount; visibleCount >= 2; visibleCount--)
        {
            var decodeResults = await DecodeVisibleImagesAsync(
                    playlist,
                    normalizedStart,
                    visibleCount,
                    cancellationToken)
                .ConfigureAwait(false);
            var failedDecode = decodeResults.FirstOrDefault(result => result.Decode.IsFailure);
            if (failedDecode is not null)
            {
                if (visibleCount > 2 && IsMemoryFailure(failedDecode.Decode.Error))
                {
                    continue;
                }

                return Result<ComparisonSet>.Failure(
                    ViewerError.ComparisonError("EnterComparison", failedDecode.FilePath));
            }

            var images = decodeResults
                .Select(result => new ComparisonImage(result.PlaylistIndex, result.Decode.Value))
                .ToArray();

            return Result<ComparisonSet>.Success(new ComparisonSet(layout, images));
        }

        return Result<ComparisonSet>.Failure(ViewerError.ComparisonError("EnterComparison", playlist.FolderPath));
    }

    private Task<ComparisonDecodeResult[]> DecodeVisibleImagesAsync(
        Playlist playlist,
        int normalizedStart,
        int visibleCount,
        CancellationToken cancellationToken)
    {
        var decodeTasks = Enumerable.Range(0, visibleCount)
            .Select(offset => DecodeComparisonImageAsync(playlist, normalizedStart, offset, cancellationToken))
            .ToArray();

        return Task.WhenAll(decodeTasks);
    }

    private async Task<ComparisonDecodeResult> DecodeComparisonImageAsync(
        Playlist playlist,
        int normalizedStart,
        int offset,
        CancellationToken cancellationToken)
    {
        var playlistIndex = Mod(normalizedStart + offset, playlist.Items.Count);
        var item = playlist.Items[playlistIndex];
        var decode = await _imageDecodeService.DecodeImageAsync(item.FilePath, cancellationToken)
            .ConfigureAwait(false);
        return new ComparisonDecodeResult(playlistIndex, item.FilePath, decode);
    }

    private static int Mod(int value, int divisor)
    {
        return ((value % divisor) + divisor) % divisor;
    }

    private static bool IsMemoryFailure(ViewerError error)
    {
        return error.Category == ErrorCategory.DecodeError
            && (string.Equals(error.ExceptionType, nameof(OutOfMemoryException), StringComparison.Ordinal)
                || string.Equals(error.UserMessage, "This image is too large to display.", StringComparison.Ordinal));
    }

    private sealed record ComparisonDecodeResult(
        int PlaylistIndex,
        string FilePath,
        Result<DecodedImage> Decode);
}
