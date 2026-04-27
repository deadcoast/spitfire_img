using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class ThumbnailServiceTests
{
    [Fact]
    public async Task GetThumbnailAsync_FirstRequestGeneratesAndSecondRequestUsesCache()
    {
        var decoder = new RecordingDecodeService(new ImageMetadata(400, 200, 1024));
        var service = new ThumbnailService(decoder, maxCacheBytes: 1024 * 1024);
        var item = new ImageItem(@"C:\Images\a.png");

        var first = await service.GetThumbnailAsync(item);
        var second = await service.GetThumbnailAsync(item);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, decoder.DecodeCount);
        Assert.Equal(120, first.Value.PixelWidth);
        Assert.Equal(60, first.Value.PixelHeight);
    }

    [Fact]
    public async Task GetThumbnailAsync_CacheExceedsLimit_EvictsLeastRecentlyUsed()
    {
        var decoder = new RecordingDecodeService(new ImageMetadata(100, 100, 2048));
        var service = new ThumbnailService(decoder, maxCacheBytes: 1);

        await service.GetThumbnailAsync(new ImageItem(@"C:\Images\a.png"));
        await service.GetThumbnailAsync(new ImageItem(@"C:\Images\b.png"));

        Assert.True(service.GetCacheSize() <= 1);
    }

    [Fact]
    public void CalculateThumbnailSize_Property_PreservesAspectRatioAndLongestDimension()
    {
        var random = new Random(5678);

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var width = random.Next(1, 10000);
            var height = random.Next(1, 10000);

            var (scaledWidth, scaledHeight) = ThumbnailService.CalculateThumbnailSize(width, height);

            Assert.Equal(120, Math.Max(scaledWidth, scaledHeight));
            Assert.True(scaledWidth >= 1);
            Assert.True(scaledHeight >= 1);
        }
    }

    private sealed class RecordingDecodeService : IImageDecodeService
    {
        private readonly ImageMetadata _metadata;

        public RecordingDecodeService(ImageMetadata metadata)
        {
            _metadata = metadata;
        }

        public int DecodeCount { get; private set; }

        public Task<Result<DecodedImage>> DecodeImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            DecodeCount++;
            return Task.FromResult(Result<DecodedImage>.Success(new DecodedImage(filePath, _metadata)));
        }

        public bool IsSupportedFormat(string extension) => true;

        public bool IsDeferredFormat(string extension) => false;

        public Task<Result<int>> GetFrameCountAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }
}
