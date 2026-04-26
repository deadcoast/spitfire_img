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
    public async Task DecodeImageAsync_JpegHeader_ReturnsMetadata()
    {
        var path = Path.Combine(_tempDirectory, "sample.jpg");
        await File.WriteAllBytesAsync(path, CreateJpegHeader(width: 640, height: 480));

        var result = await _service.DecodeImageAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(640, result.Value.Metadata.PixelWidth);
        Assert.Equal(480, result.Value.Metadata.PixelHeight);
    }

    [Fact]
    public async Task DecodeImageAsync_BmpHeader_ReturnsMetadata()
    {
        var path = Path.Combine(_tempDirectory, "sample.bmp");
        await File.WriteAllBytesAsync(path, CreateBmpHeader(width: 800, height: 600));

        var result = await _service.DecodeImageAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(800, result.Value.Metadata.PixelWidth);
        Assert.Equal(600, result.Value.Metadata.PixelHeight);
    }

    [Fact]
    public async Task DecodeImageAsync_WebpVp8XHeader_ReturnsMetadata()
    {
        var path = Path.Combine(_tempDirectory, "sample.webp");
        await File.WriteAllBytesAsync(path, CreateWebpVp8XHeader(width: 1024, height: 768));

        var result = await _service.DecodeImageAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(1024, result.Value.Metadata.PixelWidth);
        Assert.Equal(768, result.Value.Metadata.PixelHeight);
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

    private static byte[] CreateJpegHeader(int width, int height)
    {
        return
        [
            0xFF, 0xD8,
            0xFF, 0xE0, 0x00, 0x04, 0x00, 0x00,
            0xFF, 0xC0, 0x00, 0x11, 0x08,
            (byte)((height >> 8) & 0xFF), (byte)(height & 0xFF),
            (byte)((width >> 8) & 0xFF), (byte)(width & 0xFF),
            0x03, 0x01, 0x11, 0x00, 0x02, 0x11, 0x00, 0x03, 0x11, 0x00
        ];
    }

    private static byte[] CreateBmpHeader(int width, int height)
    {
        var bytes = new byte[26];
        bytes[0] = 0x42;
        bytes[1] = 0x4D;
        WriteLittleEndian(bytes, 18, width);
        WriteLittleEndian(bytes, 22, height);
        return bytes;
    }

    private static byte[] CreateWebpVp8XHeader(int width, int height)
    {
        var bytes = new byte[30];
        WriteAscii(bytes, 0, "RIFF");
        WriteAscii(bytes, 8, "WEBP");
        WriteAscii(bytes, 12, "VP8X");
        WriteUInt24LittleEndian(bytes, 24, width - 1);
        WriteUInt24LittleEndian(bytes, 27, height - 1);
        return bytes;
    }

    private static void WriteLittleEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value & 0xFF);
        bytes[offset + 1] = (byte)((value >> 8) & 0xFF);
        bytes[offset + 2] = (byte)((value >> 16) & 0xFF);
        bytes[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static void WriteUInt24LittleEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value & 0xFF);
        bytes[offset + 1] = (byte)((value >> 8) & 0xFF);
        bytes[offset + 2] = (byte)((value >> 16) & 0xFF);
    }

    private static void WriteAscii(byte[] bytes, int offset, string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            bytes[offset + index] = (byte)value[index];
        }
    }
}
