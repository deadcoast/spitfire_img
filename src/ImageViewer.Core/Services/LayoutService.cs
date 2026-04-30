using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class LayoutService : ILayoutService
{
    private readonly IImageDecodeService _imageDecodeService;

    public LayoutService(IImageDecodeService imageDecodeService)
    {
        _imageDecodeService = imageDecodeService;
    }

    public async Task<Result<LayoutSet>> EnterGridAsync(
        Playlist playlist,
        LayoutConfig config,
        int? startIndex = null,
        CancellationToken cancellationToken = default)
    {
        if (playlist.Items.Count == 0)
        {
            return Result<LayoutSet>.Failure(ViewerError.UnexpectedViewerError("EnterGrid", playlist.FolderPath));
        }

        var currentConfig = NormalizeGridConfig(config);
        var windowStartIndex = Mod(startIndex ?? playlist.CurrentIndex, playlist.Items.Count);
        while (true)
        {
            var visibleCount = Math.Min(currentConfig.VisibleCellCount, playlist.Items.Count);
            var decodeResults = await DecodeVisibleImagesAsync(
                    playlist,
                    windowStartIndex,
                    visibleCount,
                    wrap: true,
                    cancellationToken)
                .ConfigureAwait(false);
            var failedDecode = decodeResults.FirstOrDefault(result => result.Decode.IsFailure);
            if (failedDecode is not null)
            {
                if (CanReduceGridDensity(currentConfig) && IsMemoryFailure(failedDecode.Decode.Error))
                {
                    currentConfig = ReduceGridDensity(currentConfig);
                    continue;
                }

                return Result<LayoutSet>.Failure(ViewerError.LayoutError("EnterGrid", failedDecode.FilePath));
            }

            var images = decodeResults
                .Select(result => new LayoutImage(result.PlaylistIndex, result.Decode.Value))
                .ToArray();

            return Result<LayoutSet>.Success(new LayoutSet(
                LayoutMode.Grid,
                currentConfig,
                images,
                windowStartIndex,
                ActiveImageIndex: 0,
                playlist.Items.Count));
        }
    }

    public async Task<Result<LayoutSet>> EnterFilmstripAsync(
        Playlist playlist,
        int? focusIndex = null,
        CancellationToken cancellationToken = default)
    {
        if (playlist.Items.Count == 0)
        {
            return Result<LayoutSet>.Failure(ViewerError.UnexpectedViewerError("EnterFilmstrip", playlist.FolderPath));
        }

        var visibleCount = Math.Min(LayoutConfig.FilmstripThumbnailCount, playlist.Items.Count);
        var normalizedCurrentIndex = Math.Clamp(focusIndex ?? playlist.CurrentIndex, 0, playlist.Items.Count - 1);
        var halfWindow = visibleCount / 2;
        var windowStartIndex = Math.Clamp(
            normalizedCurrentIndex - halfWindow,
            0,
            Math.Max(0, playlist.Items.Count - visibleCount));
        var activeImageIndex = normalizedCurrentIndex - windowStartIndex;
        var decodeResults = await DecodeVisibleImagesAsync(
                playlist,
                windowStartIndex,
                visibleCount,
                wrap: false,
                cancellationToken)
            .ConfigureAwait(false);
        var failedDecode = decodeResults.FirstOrDefault(result => result.Decode.IsFailure);
        if (failedDecode is not null)
        {
            return Result<LayoutSet>.Failure(ViewerError.DecodeError("EnterFilmstrip", failedDecode.FilePath));
        }

        var images = decodeResults
            .Select(result => new LayoutImage(result.PlaylistIndex, result.Decode.Value))
            .ToArray();

        return Result<LayoutSet>.Success(new LayoutSet(
            LayoutMode.Filmstrip,
            LayoutConfig.Filmstrip,
            images,
            windowStartIndex,
            activeImageIndex,
            playlist.Items.Count));
    }

    public IReadOnlyList<GridLayoutRegion> CalculateGridRegions(
        LayoutConfig config,
        double canvasWidth,
        double canvasHeight)
    {
        var normalizedConfig = NormalizeGridConfig(config);
        var rows = normalizedConfig.Rows;
        var columns = normalizedConfig.Columns;
        var spacing = Math.Max(0, normalizedConfig.CellSpacing);
        var cellWidth = Math.Max(0, (canvasWidth - (columns - 1) * spacing) / columns);
        var cellHeight = Math.Max(0, (canvasHeight - (rows - 1) * spacing) / rows);
        var regions = new List<GridLayoutRegion>(rows * columns);

        for (var index = 0; index < rows * columns; index++)
        {
            var row = index / columns;
            var column = index % columns;
            regions.Add(new GridLayoutRegion(
                index,
                row,
                column,
                column * (cellWidth + spacing),
                row * (cellHeight + spacing),
                cellWidth,
                cellHeight));
        }

        return regions;
    }

    public LayoutImageFit CalculateAspectPreservingFit(
        ImageMetadata imageMetadata,
        GridLayoutRegion region)
    {
        if (imageMetadata.PixelWidth <= 0 ||
            imageMetadata.PixelHeight <= 0 ||
            region.Width <= 0 ||
            region.Height <= 0)
        {
            return new LayoutImageFit(0, 0);
        }

        var scale = Math.Min(
            region.Width / imageMetadata.PixelWidth,
            region.Height / imageMetadata.PixelHeight);

        return new LayoutImageFit(
            imageMetadata.PixelWidth * scale,
            imageMetadata.PixelHeight * scale);
    }

    private static LayoutConfig NormalizeGridConfig(LayoutConfig config)
    {
        return config with
        {
            Mode = LayoutMode.Grid,
            Rows = Math.Clamp(config.Rows, LayoutConfig.MinGridSize, LayoutConfig.MaxGridSize),
            Columns = Math.Clamp(config.Columns, LayoutConfig.MinGridSize, LayoutConfig.MaxGridSize),
            CellSpacing = Math.Max(0, config.CellSpacing)
        };
    }

    private Task<LayoutDecodeResult[]> DecodeVisibleImagesAsync(
        Playlist playlist,
        int windowStartIndex,
        int visibleCount,
        bool wrap,
        CancellationToken cancellationToken)
    {
        var decodeTasks = Enumerable.Range(0, visibleCount)
            .Select(offset => DecodeLayoutImageAsync(playlist, windowStartIndex, offset, wrap, cancellationToken))
            .ToArray();

        return Task.WhenAll(decodeTasks);
    }

    private async Task<LayoutDecodeResult> DecodeLayoutImageAsync(
        Playlist playlist,
        int windowStartIndex,
        int offset,
        bool wrap,
        CancellationToken cancellationToken)
    {
        var playlistIndex = wrap
            ? Mod(windowStartIndex + offset, playlist.Items.Count)
            : windowStartIndex + offset;
        var item = playlist.Items[playlistIndex];
        var decode = await _imageDecodeService.DecodeImageAsync(item.FilePath, cancellationToken)
            .ConfigureAwait(false);
        return new LayoutDecodeResult(playlistIndex, item.FilePath, decode);
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

    private static bool CanReduceGridDensity(LayoutConfig config)
    {
        return config.Rows > LayoutConfig.MinGridSize || config.Columns > LayoutConfig.MinGridSize;
    }

    private static LayoutConfig ReduceGridDensity(LayoutConfig config)
    {
        return config with
        {
            Rows = config.Rows > LayoutConfig.MinGridSize ? config.Rows - 1 : config.Rows,
            Columns = config.Columns > LayoutConfig.MinGridSize ? config.Columns - 1 : config.Columns
        };
    }

    private sealed record LayoutDecodeResult(
        int PlaylistIndex,
        string FilePath,
        Result<DecodedImage> Decode);
}
