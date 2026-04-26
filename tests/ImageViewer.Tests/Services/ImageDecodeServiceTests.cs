using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class ImageDecodeServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ImageDecodeService _service = new();

    public ImageDecodeServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "spitfire-decode-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".JPG")]
    [InlineData(".jpeg")]
    [InlineData(".BMP")]
    [InlineData(".webp")]
    public void IsSupportedFormat_DelegatesToCentralCatalog(string extension)
    {
        Assert.True(_service.IsSupportedFormat(extension));
    }

    [Theory]
    [InlineData(".gif")]
    [InlineData(".tiff")]
    [InlineData(".tif")]
    [InlineData(".heic")]
    [InlineData(".avif")]
    public void IsDeferredFormat_DelegatesToCentralCatalog(string extension)
    {
        Assert.True(_service.IsDeferredFormat(extension));
    }

    [Fact]
    public async Task DecodeImageAsync_PngHeader_ReturnsMetadata()
    {
        var path = Path.Combine(_tempDirectory, "sample.png");
        await File.WriteAllBytesAsync(path, CreatePngHeader(width: 320, height: 240));

        var result = await _service.DecodeImageAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(path, result.Value.FilePath);
        Assert.Equal(320, result.Value.Metadata.PixelWidth);
        Assert.Equal(240, result.Value.Metadata.PixelHeight);
        Assert.True(result.Value.Metadata.FileSizeBytes > 0);
    }

    [Fact]
    public async Task DecodeImageAsync_CorruptSupportedFile_ReturnsDecodeError()
    {
        var path = Path.Combine(_tempDirectory, "corrupt.png");
        await File.WriteAllBytesAsync(path, [0x01, 0x02, 0x03]);

        var result = await _service.DecodeImageAsync(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.DecodeError, result.Error.Category);
    }

    [Fact]
    public async Task DecodeImageAsync_DeferredFormat_ReturnsUnsupportedFormatError()
    {
        var path = Path.Combine(_tempDirectory, "animation.gif");
        await File.WriteAllBytesAsync(path, [0x47, 0x49, 0x46]);

        var result = await _service.DecodeImageAsync(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.UnsupportedFormatError, result.Error.Category);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static byte[] CreatePngHeader(int width, int height)
    {
        var bytes = new byte[24];
        var signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        signature.CopyTo(bytes, 0);
        bytes[12] = 0x49;
        bytes[13] = 0x48;
        bytes[14] = 0x44;
        bytes[15] = 0x52;
        WriteBigEndian(bytes, 16, width);
        WriteBigEndian(bytes, 20, height);
        return bytes;
    }

    private static void WriteBigEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 24) & 0xFF);
        bytes[offset + 1] = (byte)((value >> 16) & 0xFF);
        bytes[offset + 2] = (byte)((value >> 8) & 0xFF);
        bytes[offset + 3] = (byte)(value & 0xFF);
    }
}
