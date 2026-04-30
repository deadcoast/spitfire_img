using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class ComparisonServiceTests
{
    [Fact]
    public async Task EnterComparisonAsync_WithTwoOrMoreImages_ReturnsCurrentAndNextImages()
    {
        var service = new ComparisonService(new PathImageDecodeService());
        var playlist = CreatePlaylist(count: 3, currentIndex: 1);

        var result = await service.EnterComparisonAsync(playlist, ComparisonLayout.SideBySide, imageCount: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(ComparisonLayout.SideBySide, result.Value.Layout);
        Assert.Equal([1, 2], result.Value.Images.Select(image => image.PlaylistIndex).ToArray());
        Assert.Equal(@"C:\Photos\1.png", result.Value.Images[0].Image.FilePath);
        Assert.Equal(@"C:\Photos\2.png", result.Value.Images[1].Image.FilePath);
    }

    [Fact]
    public async Task EnterComparisonAsync_WithOneImage_ReturnsComparisonError()
    {
        var service = new ComparisonService(new PathImageDecodeService());
        var playlist = CreatePlaylist(count: 1, currentIndex: 0);

        var result = await service.EnterComparisonAsync(playlist, ComparisonLayout.SideBySide, imageCount: 2);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.ComparisonError, result.Error.Category);
        Assert.Equal("Unable to load images for comparison.", result.Error.UserMessage);
    }

    [Fact]
    public async Task ShiftComparisonAsync_MovesVisibleImagesTogetherThroughPlaylist()
    {
        var service = new ComparisonService(new PathImageDecodeService());
        var playlist = CreatePlaylist(count: 4, currentIndex: 1);
        var current = await service.EnterComparisonAsync(playlist, ComparisonLayout.SideBySide, imageCount: 2);

        var shifted = await service.ShiftComparisonAsync(
            playlist,
            current.Value.Images.Select(image => image.PlaylistIndex).ToArray(),
            offset: 1,
            ComparisonLayout.SideBySide);

        Assert.True(shifted.IsSuccess);
        Assert.Equal([2, 3], shifted.Value.Images.Select(image => image.PlaylistIndex).ToArray());
    }

    [Fact]
    public async Task EnterComparisonAsync_MemoryFailureAtRequestedCount_ReducesVisibleImageCount()
    {
        var service = new ComparisonService(new MemoryFailingDecodeService(@"C:\Photos\3.png"));
        var playlist = CreatePlaylist(count: 5, currentIndex: 0);

        var result = await service.EnterComparisonAsync(playlist, ComparisonLayout.SideBySide, imageCount: 4);

        Assert.True(result.IsSuccess);
        Assert.Equal([0, 1, 2], result.Value.Images.Select(image => image.PlaylistIndex).ToArray());
    }

    [Fact]
    public async Task EnterComparisonAsync_LoadsRequestedImagesInParallel()
    {
        var decoder = new BlockingDecodeService();
        var service = new ComparisonService(decoder);
        var playlist = CreatePlaylist(count: 4, currentIndex: 0);

        var comparisonTask = service.EnterComparisonAsync(playlist, ComparisonLayout.SideBySide, imageCount: 3);
        await decoder.WaitForStartedCountAsync(3);

        Assert.Equal(
            [@"C:\Photos\0.png", @"C:\Photos\1.png", @"C:\Photos\2.png"],
            decoder.StartedPaths);

        decoder.Release();
        var result = await comparisonTask;

        Assert.True(result.IsSuccess);
        Assert.Equal([0, 1, 2], result.Value.Images.Select(image => image.PlaylistIndex).ToArray());
    }

    [Theory]
    [InlineData(2, 2, 1)]
    [InlineData(3, 3, 1)]
    [InlineData(4, 2, 2)]
    public void CalculateSideBySideLayout_UsesEqualSectionsForSupportedImageCounts(
        int imageCount,
        int expectedColumns,
        int expectedRows)
    {
        var service = new ComparisonService(new PathImageDecodeService());

        var regions = service.CalculateSideBySideLayout(imageCount, canvasWidth: 1200, canvasHeight: 800);

        Assert.Equal(imageCount, regions.Count);
        Assert.All(regions, region =>
        {
            Assert.Equal(1200d / expectedColumns, region.Width, precision: 5);
            Assert.Equal(800d / expectedRows, region.Height, precision: 5);
        });
    }

    private static Playlist CreatePlaylist(int count, int currentIndex)
    {
        var items = Enumerable.Range(0, count)
            .Select(index => new ImageItem($@"C:\Photos\{index}.png"))
            .ToArray();

        return new Playlist(items, currentIndex, @"C:\Photos");
    }

    private sealed class PathImageDecodeService : IImageDecodeService
    {
        public Task<Result<DecodedImage>> DecodeImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<DecodedImage>.Success(
                new DecodedImage(filePath, new ImageMetadata(100, 100, 10))));
        }

        public bool IsSupportedFormat(string extension) => true;

        public bool IsDeferredFormat(string extension) => false;

        public Task<Result<int>> GetFrameCountAsync(string filePath, CancellationToken cancellationToken = default)
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
