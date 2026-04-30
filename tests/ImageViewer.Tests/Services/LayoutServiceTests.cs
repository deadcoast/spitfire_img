using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class LayoutServiceTests
{
    [Fact]
    public async Task EnterGridAsync_DefaultConfig_LoadsCurrentPlaylistPageIntoThreeByThreeGrid()
    {
        var playlist = CreatePlaylist(10, currentIndex: 0);
        var service = new LayoutService(new PathImageDecodeService());

        var result = await service.EnterGridAsync(playlist, LayoutConfig.Default);

        Assert.True(result.IsSuccess);
        Assert.Equal(LayoutMode.Grid, result.Value.Mode);
        Assert.Equal(3, result.Value.Config.Rows);
        Assert.Equal(3, result.Value.Config.Columns);
        Assert.Equal(9, result.Value.VisibleImages.Count);
        Assert.Equal(
            Enumerable.Range(0, 9),
            result.Value.VisibleImages.Select(image => image.PlaylistIndex));
        Assert.All(result.Value.VisibleImages, image => Assert.EndsWith(".png", image.Image.FilePath));
    }

    [Fact]
    public async Task EnterGridAsync_WithStartIndex_LoadsRequestedGridWindow()
    {
        var playlist = CreatePlaylist(12, currentIndex: 0);
        var service = new LayoutService(new PathImageDecodeService());

        var result = await service.EnterGridAsync(playlist, LayoutConfig.Default, startIndex: 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.WindowStartIndex);
        Assert.Equal(
            Enumerable.Range(3, 9),
            result.Value.VisibleImages.Select(image => image.PlaylistIndex));
    }

    [Fact]
    public async Task EnterGridAsync_MemoryFailureAtRequestedDensity_ReducesGridDensity()
    {
        var playlist = CreatePlaylist(20, currentIndex: 0);
        var service = new LayoutService(new MemoryFailingDecodeService(@"C:\Photos\009.png"));

        var result = await service.EnterGridAsync(
            playlist,
            new LayoutConfig(LayoutMode.Grid, Rows: 4, Columns: 4, CellSpacing: 12));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Config.Rows);
        Assert.Equal(3, result.Value.Config.Columns);
        Assert.Equal(
            Enumerable.Range(0, 9),
            result.Value.VisibleImages.Select(image => image.PlaylistIndex));
    }

    [Fact]
    public async Task EnterGridAsync_RectangularConfig_PreservesRowsColumnsAndVisibleCount()
    {
        var playlist = CreatePlaylist(30, currentIndex: 0);
        var service = new LayoutService(new PathImageDecodeService());

        var result = await service.EnterGridAsync(
            playlist,
            new LayoutConfig(LayoutMode.Grid, Rows: 4, Columns: 5, CellSpacing: 12));

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Config.Rows);
        Assert.Equal(5, result.Value.Config.Columns);
        Assert.Equal(20, result.Value.VisibleImages.Count);
        Assert.Equal(
            Enumerable.Range(0, 20),
            result.Value.VisibleImages.Select(image => image.PlaylistIndex));
    }

    [Fact]
    public async Task EnterGridAsync_LoadsVisibleGridWindowInParallel()
    {
        var playlist = CreatePlaylist(20, currentIndex: 0);
        var decoder = new BlockingDecodeService();
        var service = new LayoutService(decoder);

        var layoutTask = service.EnterGridAsync(playlist, LayoutConfig.Default);
        await decoder.WaitForStartedCountAsync(9);

        Assert.Equal(
            Enumerable.Range(0, 9).Select(index => $@"C:\Photos\{index:000}.png"),
            decoder.StartedPaths);

        decoder.Release();
        var result = await layoutTask;

        Assert.True(result.IsSuccess);
        Assert.Equal(9, result.Value.VisibleImages.Count);
        Assert.Equal(
            Enumerable.Range(0, 9),
            result.Value.VisibleImages.Select(image => image.PlaylistIndex));
    }

    [Fact]
    public void CalculateGridRegions_UsesRowsColumnsAndSpacingToCreateStableCells()
    {
        var service = new LayoutService(new PathImageDecodeService());
        var config = new LayoutConfig(LayoutMode.Grid, Rows: 3, Columns: 3, CellSpacing: 15);

        var regions = service.CalculateGridRegions(config, canvasWidth: 330, canvasHeight: 330);

        Assert.Equal(9, regions.Count);
        Assert.Equal(new GridLayoutRegion(0, 0, 0, 0, 0, 100, 100), regions[0]);
        Assert.Equal(new GridLayoutRegion(4, 1, 1, 115, 115, 100, 100), regions[4]);
        Assert.Equal(new GridLayoutRegion(8, 2, 2, 230, 230, 100, 100), regions[8]);
    }

    [Fact]
    public void CalculateAspectPreservingFit_ScalesImageInsideGridCellWithoutCropping()
    {
        var service = new LayoutService(new PathImageDecodeService());
        var region = new GridLayoutRegion(0, 0, 0, 0, 0, 150, 150);

        var fit = service.CalculateAspectPreservingFit(new ImageMetadata(400, 200, 100), region);

        Assert.Equal(150, fit.Width);
        Assert.Equal(75, fit.Height);
    }

    [Theory]
    [InlineData(3, 1, 4)]
    [InlineData(6, 1, 6)]
    [InlineData(3, -1, 2)]
    [InlineData(2, -1, 2)]
    public void StepGridDensity_ChangesRowsAndColumnsWithinTwoToSixRange(
        int currentSize,
        int direction,
        int expectedSize)
    {
        var config = new LayoutConfig(LayoutMode.Grid, currentSize, currentSize, 12);

        var stepped = LayoutConfig.StepGridDensity(config, direction);

        Assert.Equal(expectedSize, stepped.Rows);
        Assert.Equal(expectedSize, stepped.Columns);
        Assert.Equal(LayoutMode.Grid, stepped.Mode);
    }

    [Fact]
    public async Task EnterFilmstripAsync_CentersNineThumbnailsAroundCurrentImage()
    {
        var playlist = CreatePlaylist(12, currentIndex: 5);
        var service = new LayoutService(new PathImageDecodeService());

        var result = await service.EnterFilmstripAsync(playlist);

        Assert.True(result.IsSuccess);
        Assert.Equal(LayoutMode.Filmstrip, result.Value.Mode);
        Assert.Equal(1, result.Value.WindowStartIndex);
        Assert.Equal(4, result.Value.ActiveImageIndex);
        Assert.Equal(5, result.Value.FocusImage?.PlaylistIndex);
        Assert.Equal(
            Enumerable.Range(1, 9),
            result.Value.VisibleImages.Select(image => image.PlaylistIndex));
    }

    [Fact]
    public async Task EnterFilmstripAsync_WithSmallPlaylist_UsesAllImagesForThumbnailStrip()
    {
        var playlist = CreatePlaylist(5, currentIndex: 2);
        var service = new LayoutService(new PathImageDecodeService());

        var result = await service.EnterFilmstripAsync(playlist);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.VisibleImages.Count);
        Assert.Equal(2, result.Value.ActiveImageIndex);
        Assert.Equal(2, result.Value.FocusImage?.PlaylistIndex);
    }

    [Fact]
    public async Task EnterFilmstripAsync_WithFocusIndex_RebuildsCenteredThumbnailWindow()
    {
        var playlist = CreatePlaylist(12, currentIndex: 0);
        var service = new LayoutService(new PathImageDecodeService());

        var result = await service.EnterFilmstripAsync(playlist, focusIndex: 6);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.WindowStartIndex);
        Assert.Equal(4, result.Value.ActiveImageIndex);
        Assert.Equal(6, result.Value.FocusImage?.PlaylistIndex);
        Assert.Equal(
            Enumerable.Range(2, 9),
            result.Value.VisibleImages.Select(image => image.PlaylistIndex));
    }

    private static Playlist CreatePlaylist(int count, int currentIndex)
    {
        var items = Enumerable.Range(0, count)
            .Select(index => new ImageItem($@"C:\Photos\{index:000}.png"))
            .ToArray();

        return new Playlist(items, currentIndex, @"C:\Photos");
    }

    private sealed class PathImageDecodeService : IImageDecodeService
    {
        public Task<Result<DecodedImage>> DecodeImageAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<DecodedImage>.Success(
                new DecodedImage(filePath, new ImageMetadata(100, 100, 10))));
        }

        public bool IsSupportedFormat(string extension) => true;

        public bool IsDeferredFormat(string extension) => false;

        public Task<Result<int>> GetFrameCountAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }

    private sealed class MemoryFailingDecodeService : IImageDecodeService
    {
        private readonly string _failPath;

        public MemoryFailingDecodeService(string failPath)
        {
            _failPath = failPath;
        }

        public Task<Result<DecodedImage>> DecodeImageAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            if (string.Equals(filePath, _failPath, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Result<DecodedImage>.Failure(
                    ViewerError.DecodeError("DecodeImage", filePath, new OutOfMemoryException(), isMemoryError: true)));
            }

            return Task.FromResult(Result<DecodedImage>.Success(
                new DecodedImage(filePath, new ImageMetadata(100, 100, 10))));
        }

        public bool IsSupportedFormat(string extension) => true;

        public bool IsDeferredFormat(string extension) => false;

        public Task<Result<int>> GetFrameCountAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }

    private sealed class BlockingDecodeService : IImageDecodeService
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _gate = new();

        public List<string> StartedPaths { get; } = [];

        public async Task<Result<DecodedImage>> DecodeImageAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                StartedPaths.Add(filePath);
            }

            await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return Result<DecodedImage>.Success(new DecodedImage(filePath, new ImageMetadata(100, 100, 10)));
        }

        public async Task WaitForStartedCountAsync(int count)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            while (true)
            {
                lock (_gate)
                {
                    if (StartedPaths.Count >= count)
                    {
                        return;
                    }
                }

                await Task.Delay(10, timeout.Token).ConfigureAwait(false);
            }
        }

        public void Release()
        {
            _release.TrySetResult();
        }

        public bool IsSupportedFormat(string extension) => true;

        public bool IsDeferredFormat(string extension) => false;

        public Task<Result<int>> GetFrameCountAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }
}
