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
        Assert.Equal("Comparison mode requires at least two images", result.Error.UserMessage);
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
}
