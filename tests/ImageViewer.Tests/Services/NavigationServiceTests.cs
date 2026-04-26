using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class NavigationServiceTests
{
    [Fact]
    public async Task NavigateToIndexAsync_ValidIndex_DecodesTargetImage()
    {
        var playlist = CreatePlaylist(3, currentIndex: 0);
        var decoder = new FakeImageDecodeService();
        var service = new NavigationService(decoder);

        var result = await service.NavigateToIndexAsync(playlist, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Index);
        Assert.Equal(playlist.Items[2].FilePath, result.Value.Image.FilePath);
    }

    [Fact]
    public async Task NavigateToIndexAsync_PastLast_StaysOnLastImage()
    {
        var playlist = CreatePlaylist(3, currentIndex: 1);
        var service = new NavigationService(new FakeImageDecodeService());

        var result = await service.NavigateToIndexAsync(playlist, 99);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Index);
    }

    [Fact]
    public async Task NavigateToIndexAsync_BeforeFirst_StaysOnFirstImage()
    {
        var playlist = CreatePlaylist(3, currentIndex: 1);
        var service = new NavigationService(new FakeImageDecodeService());

        var result = await service.NavigateToIndexAsync(playlist, -20);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Index);
    }

    [Fact]
    public async Task NavigateToIndexAsync_DecodeFailure_ReturnsNavigationError()
    {
        var playlist = CreatePlaylist(2, currentIndex: 0);
        var decoder = new FakeImageDecodeService(failPath: playlist.Items[1].FilePath);
        var service = new NavigationService(decoder);

        var result = await service.NavigateToIndexAsync(playlist, 1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.NavigationError, result.Error.Category);
    }

    private static Playlist CreatePlaylist(int count, int currentIndex)
    {
        var items = Enumerable.Range(1, count)
            .Select(index => new ImageItem($@"C:\Photos\img{index}.png"))
            .ToList();

        return new Playlist(items, currentIndex, @"C:\Photos");
    }

    private sealed class FakeImageDecodeService : IImageDecodeService
    {
        private readonly string? _failPath;

        public FakeImageDecodeService(string? failPath = null)
        {
            _failPath = failPath;
        }

        public Task<Result<DecodedImage>> DecodeImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.Equals(filePath, _failPath, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Result<DecodedImage>.Failure(ViewerError.DecodeError("DecodeImage", filePath)));
            }

            var image = new DecodedImage(filePath, new ImageMetadata(100, 100, 10));
            return Task.FromResult(Result<DecodedImage>.Success(image));
        }

        public bool IsSupportedFormat(string extension) => ImageFormatCatalog.IsSupportedFormat(extension);

        public bool IsDeferredFormat(string extension) => ImageFormatCatalog.IsDeferredFormat(extension);
    }
}
