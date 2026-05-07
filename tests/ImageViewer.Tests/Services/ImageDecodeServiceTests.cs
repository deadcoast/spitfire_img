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
    [InlineData(".gif")]
    [InlineData(".TIFF")]
    [InlineData(".tif")]
    public void IsSupportedFormat_DelegatesToCentralCatalog(string extension)
    {
        Assert.True(_service.IsSupportedFormat(extension));
    }

    [Theory]
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
    public async Task DecodeImageAsync_WebpVp8Header_ReturnsMetadata()
    {
        var path = Path.Combine(_tempDirectory, "lossy.webp");
        await File.WriteAllBytesAsync(path, Convert.FromBase64String(
            "UklGRmIAAABXRUJQVlA4IFYAAABwBgCdASqgAFoAPpFIoUylpCMiIIgAsBIJaW7hdJlwAAE9tiLxBUc9sReIKjntiLxBUc9sReIKjntiLYAA/v6YFf/9XbtZnj3//8Q50OdDmSQAAAAAAA=="));

        var result = await _service.DecodeImageAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(160, result.Value.Metadata.PixelWidth);
        Assert.Equal(90, result.Value.Metadata.PixelHeight);
    }

    [Fact]
    public async Task DecodeImageAsync_GifHeader_ReturnsMetadata()
    {
        var path = Path.Combine(_tempDirectory, "sample.gif");
        await File.WriteAllBytesAsync(path, CreateGifHeader(width: 120, height: 80));

        var result = await _service.DecodeImageAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(120, result.Value.Metadata.PixelWidth);
        Assert.Equal(80, result.Value.Metadata.PixelHeight);
    }

    [Theory]
    [InlineData(".tif")]
    [InlineData(".tiff")]
    public async Task DecodeImageAsync_TiffHeader_ReturnsMetadata(string extension)
    {
        var path = Path.Combine(_tempDirectory, $"sample{extension}");
        await File.WriteAllBytesAsync(path, CreateTiffHeader(width: 300, height: 200));

        var result = await _service.DecodeImageAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(300, result.Value.Metadata.PixelWidth);
        Assert.Equal(200, result.Value.Metadata.PixelHeight);
    }

    [Fact]
    public async Task GetFrameCountAsync_Gif_ReturnsOneOrMoreFrames()
    {
        var path = Path.Combine(_tempDirectory, "sample.gif");
        await File.WriteAllBytesAsync(path, CreateGifHeader(width: 120, height: 80));

        var result = await _service.GetFrameCountAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
    }

    [Fact]
    public async Task GetFrameCountAsync_AnimatedGif_ReturnsImageDescriptorCount()
    {
        var path = Path.Combine(_tempDirectory, "animated.gif");
        await File.WriteAllBytesAsync(path, CreateGifWithFrameDescriptors(width: 120, height: 80, frameCount: 3));

        var result = await _service.GetFrameCountAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
    }

    [Fact]
    public async Task DecodeImageAsync_MultiPageTiffHeader_UsesFirstPageDimensions()
    {
        var path = Path.Combine(_tempDirectory, "multipage.tiff");
        await File.WriteAllBytesAsync(path, CreateMultiPageTiffHeader(
            firstWidth: 300,
            firstHeight: 200,
            secondWidth: 900,
            secondHeight: 800));

        var result = await _service.DecodeImageAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(300, result.Value.Metadata.PixelWidth);
        Assert.Equal(200, result.Value.Metadata.PixelHeight);
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
    public async Task DecodeImageAsync_UnknownFormat_ReturnsUnsupportedFormatError()
    {
        var path = Path.Combine(_tempDirectory, "future.jxl");
        await File.WriteAllBytesAsync(path, [0x00, 0x00, 0x00]);

        var result = await _service.DecodeImageAsync(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.UnsupportedFormatError, result.Error.Category);
    }

    [Fact]
    public async Task DecodeImageAsync_HeicWithoutCodec_ReturnsCodecUnavailableError()
    {
        var path = Path.Combine(_tempDirectory, "phone.heic");
        await File.WriteAllBytesAsync(path, [0x00, 0x00, 0x00]);
        var service = new ImageDecodeService(
            new StubCodecDetectionService(heicSupported: false, avifSupported: false),
            new StubPlatformImageDecodeService());

        var result = await service.DecodeImageAsync(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.CodecUnavailableError, result.Error.Category);
        Assert.Equal(
            "HEIC format requires the HEIF Image Extensions from the Microsoft Store. Would you like to open the Store page?",
            result.Error.UserMessage);
    }

    [Fact]
    public async Task DecodeImageAsync_AvifWithoutCodec_ReturnsCodecUnavailableError()
    {
        var path = Path.Combine(_tempDirectory, "modern.avif");
        await File.WriteAllBytesAsync(path, [0x00, 0x00, 0x00]);
        var service = new ImageDecodeService(
            new StubCodecDetectionService(heicSupported: false, avifSupported: false),
            new StubPlatformImageDecodeService());

        var result = await service.DecodeImageAsync(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.CodecUnavailableError, result.Error.Category);
        Assert.Equal(
            "AVIF format requires the AV1 Video Extension from the Microsoft Store. Would you like to open the Store page?",
            result.Error.UserMessage);
    }

    [Fact]
    public async Task DecodeImageAsync_HeicWithCodec_DelegatesToPlatformDecoder()
    {
        var path = Path.Combine(_tempDirectory, "phone.heic");
        await File.WriteAllBytesAsync(path, [0x00, 0x00, 0x00]);
        var platformDecoder = new StubPlatformImageDecodeService();
        var service = new ImageDecodeService(
            new StubCodecDetectionService(heicSupported: true, avifSupported: false),
            platformDecoder);

        var result = await service.DecodeImageAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(path, result.Value.FilePath);
        Assert.Equal(4032, result.Value.Metadata.PixelWidth);
        Assert.Equal(3024, result.Value.Metadata.PixelHeight);
        Assert.Equal([path], platformDecoder.DecodedPaths);
    }

    [Fact]
    public async Task DecodeImageAsync_AvifWithCodec_DelegatesToPlatformDecoder()
    {
        var path = Path.Combine(_tempDirectory, "modern.avif");
        await File.WriteAllBytesAsync(path, [0x00, 0x00, 0x00]);
        var platformDecoder = new StubPlatformImageDecodeService();
        var service = new ImageDecodeService(
            new StubCodecDetectionService(heicSupported: false, avifSupported: true),
            platformDecoder);

        var result = await service.DecodeImageAsync(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(path, result.Value.FilePath);
        Assert.Equal(4032, result.Value.Metadata.PixelWidth);
        Assert.Equal(3024, result.Value.Metadata.PixelHeight);
        Assert.Equal([path], platformDecoder.DecodedPaths);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void IsSupportedFormat_HeicReflectsCodecAvailability(bool heicSupported, bool expected)
    {
        var service = new ImageDecodeService(
            new StubCodecDetectionService(heicSupported, avifSupported: false),
            new StubPlatformImageDecodeService());

        Assert.Equal(expected, service.IsSupportedFormat(".heic"));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void IsSupportedFormat_AvifReflectsCodecAvailability(bool avifSupported, bool expected)
    {
        var service = new ImageDecodeService(
            new StubCodecDetectionService(heicSupported: false, avifSupported),
            new StubPlatformImageDecodeService());

        Assert.Equal(expected, service.IsSupportedFormat(".avif"));
        Assert.False(service.IsSupportedFormat(".heic"));
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

    private static byte[] CreateGifHeader(int width, int height)
    {
        var bytes = new byte[10];
        WriteAscii(bytes, 0, "GIF89a");
        WriteUInt16LittleEndian(bytes, 6, width);
        WriteUInt16LittleEndian(bytes, 8, height);
        return bytes;
    }

    private static byte[] CreateGifWithFrameDescriptors(int width, int height, int frameCount)
    {
        var bytes = new byte[10 + frameCount];
        CreateGifHeader(width, height).CopyTo(bytes, 0);
        for (var index = 0; index < frameCount; index++)
        {
            bytes[10 + index] = 0x2C;
        }

        return bytes;
    }

    private static byte[] CreateTiffHeader(int width, int height)
    {
        var bytes = new byte[38];
        bytes[0] = 0x49;
        bytes[1] = 0x49;
        WriteUInt16LittleEndian(bytes, 2, 42);
        WriteLittleEndian(bytes, 4, 8);
        WriteUInt16LittleEndian(bytes, 8, 2);
        WriteUInt16LittleEndian(bytes, 10, 256);
        WriteUInt16LittleEndian(bytes, 12, 4);
        WriteLittleEndian(bytes, 14, 1);
        WriteLittleEndian(bytes, 18, width);
        WriteUInt16LittleEndian(bytes, 22, 257);
        WriteUInt16LittleEndian(bytes, 24, 4);
        WriteLittleEndian(bytes, 26, 1);
        WriteLittleEndian(bytes, 30, height);
        WriteLittleEndian(bytes, 34, 0);
        return bytes;
    }

    private static byte[] CreateMultiPageTiffHeader(
        int firstWidth,
        int firstHeight,
        int secondWidth,
        int secondHeight)
    {
        var bytes = new byte[68];
        var firstPage = CreateTiffHeader(firstWidth, firstHeight);
        firstPage.CopyTo(bytes, 0);
        WriteLittleEndian(bytes, 34, 38);
        WriteUInt16LittleEndian(bytes, 38, 2);
        WriteUInt16LittleEndian(bytes, 40, 256);
        WriteUInt16LittleEndian(bytes, 42, 4);
        WriteLittleEndian(bytes, 44, 1);
        WriteLittleEndian(bytes, 48, secondWidth);
        WriteUInt16LittleEndian(bytes, 52, 257);
        WriteUInt16LittleEndian(bytes, 54, 4);
        WriteLittleEndian(bytes, 56, 1);
        WriteLittleEndian(bytes, 60, secondHeight);
        WriteLittleEndian(bytes, 64, 0);
        return bytes;
    }

    private static void WriteUInt16LittleEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value & 0xFF);
        bytes[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    private sealed class StubCodecDetectionService : ICodecDetectionService
    {
        private readonly bool _heicSupported;
        private readonly bool _avifSupported;

        public StubCodecDetectionService(bool heicSupported, bool avifSupported)
        {
            _heicSupported = heicSupported;
            _avifSupported = avifSupported;
        }

        public Task<Result<CodecAvailability>> DetectCodecsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<CodecAvailability>.Success(
                new CodecAvailability(_heicSupported, _avifSupported, DateTimeOffset.UnixEpoch)));
        }

        public Result<bool> IsHeicSupported()
        {
            return Result<bool>.Success(_heicSupported);
        }

        public Result<bool> IsAvifSupported()
        {
            return Result<bool>.Success(_avifSupported);
        }

        public Result<string> GetHeicStoreUrl()
        {
            return Result<string>.Success("ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG");
        }

        public Result<string> GetAvifStoreUrl()
        {
            return Result<string>.Success("ms-windows-store://pdp/?ProductId=9MVZQVXJBQ9V");
        }
    }

    private sealed class StubPlatformImageDecodeService : IPlatformImageDecodeService
    {
        public List<string> DecodedPaths { get; } = [];

        public Task<Result<DecodedImage>> DecodeImageAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            DecodedPaths.Add(filePath);
            return Task.FromResult(Result<DecodedImage>.Success(
                new DecodedImage(filePath, new ImageMetadata(4032, 3024, new FileInfo(filePath).Length))));
        }
    }
}
