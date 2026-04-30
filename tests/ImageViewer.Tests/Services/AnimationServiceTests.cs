using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class AnimationServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly AnimationService _service = new();

    public AnimationServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "spitfire-animation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task IsAnimatedGifAsync_WithMultipleFrames_ReturnsTrue()
    {
        var path = Path.Combine(_tempDirectory, "animated.gif");
        await File.WriteAllBytesAsync(path, GifFixture.CreateGif(width: 4, height: 3, frameDelaysMs: [40, 120]));

        var result = await _service.IsAnimatedGifAsync(path);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task LoadAnimatedGifAsync_WithAnimatedGif_CachesAllFramesAndDelays()
    {
        var path = Path.Combine(_tempDirectory, "animated.gif");
        await File.WriteAllBytesAsync(path, GifFixture.CreateGif(width: 5, height: 4, frameDelaysMs: [40, 120, 250]));

        var result = await _service.LoadAnimatedGifAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(path, result.Value.FilePath);
        Assert.True(result.Value.IsAnimated);
        Assert.Equal(3, result.Value.TotalFrames);
        Assert.Equal(5, result.Value.Metadata.PixelWidth);
        Assert.Equal(4, result.Value.Metadata.PixelHeight);
        Assert.Equal(
            [TimeSpan.FromMilliseconds(40), TimeSpan.FromMilliseconds(120), TimeSpan.FromMilliseconds(250)],
            result.Value.Frames.Select(frame => frame.Delay).ToArray());
        Assert.All(result.Value.Frames, frame => Assert.NotEmpty(frame.EncodedBytes));
        Assert.Equal([0, 1, 2], result.Value.Frames.Select(frame => frame.Index).ToArray());
    }

    [Fact]
    public async Task LoadAnimatedGifAsync_WithSingleFrameGif_ReturnsStaticAnimationModel()
    {
        var path = Path.Combine(_tempDirectory, "static.gif");
        await File.WriteAllBytesAsync(path, GifFixture.CreateGif(width: 6, height: 7, frameDelaysMs: [90]));

        var result = await _service.LoadAnimatedGifAsync(path);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsAnimated);
        Assert.Equal(1, result.Value.TotalFrames);
        Assert.Equal(TimeSpan.FromMilliseconds(90), result.Value.Frames[0].Delay);
    }

    [Fact]
    public async Task LoadAnimatedGifAsync_WithNonGif_ReturnsUnsupportedFormatError()
    {
        var path = Path.Combine(_tempDirectory, "not-gif.png");
        await File.WriteAllBytesAsync(path, [0x89, 0x50, 0x4E, 0x47]);

        var result = await _service.LoadAnimatedGifAsync(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.UnsupportedFormatError, result.Error.Category);
    }

    [Fact]
    public async Task LoadAnimatedGifAsync_WithMalformedGif_ReturnsAnimationError()
    {
        var path = Path.Combine(_tempDirectory, "corrupt.gif");
        await File.WriteAllBytesAsync(path, "GIF89a"u8.ToArray());

        var result = await _service.LoadAnimatedGifAsync(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.AnimationError, result.Error.Category);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
