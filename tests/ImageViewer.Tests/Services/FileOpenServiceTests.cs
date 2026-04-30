using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class FileOpenServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly FileOpenService _service = new();

    public FileOpenServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "spitfire-file-open-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Theory]
    [InlineData("photo.png")]
    [InlineData("photo.JPG")]
    [InlineData("photo.jpeg")]
    [InlineData("photo.bmp")]
    [InlineData("photo.WEBP")]
    [InlineData("animated.gif")]
    [InlineData("scan.tiff")]
    [InlineData("scan.tif")]
    public void ValidateFilePath_ExistingSupportedFile_ReturnsSuccess(string filename)
    {
        var path = CreateFile(filename);

        var result = _service.ValidateFilePath(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(path, result.Value);
    }

    [Fact]
    public void ValidateFilePath_MissingFile_ReturnsFileAccessError()
    {
        var path = Path.Combine(_tempDirectory, "missing.png");

        var result = _service.ValidateFilePath(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.FileAccessError, result.Error.Category);
    }

    [Fact]
    public void ValidateFilePath_DeferredFormatWithoutCodecContext_ReturnsUnsupportedFormatError()
    {
        var path = CreateFile("modern.avif");

        var result = _service.ValidateFilePath(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.UnsupportedFormatError, result.Error.Category);
    }

    [Fact]
    public void ValidateFilePath_HeicWithCodecAvailable_ReturnsSuccess()
    {
        var path = CreateFile("phone.heic");
        var service = new FileOpenService(new StubCodecDetectionService(heicSupported: true));

        var result = service.ValidateFilePath(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(path, result.Value);
    }

    [Fact]
    public void ValidateFilePath_HeicWithoutCodec_ReturnsCodecUnavailableError()
    {
        var path = CreateFile("phone.heic");
        var service = new FileOpenService(new StubCodecDetectionService(heicSupported: false));

        var result = service.ValidateFilePath(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.CodecUnavailableError, result.Error.Category);
        Assert.Equal(
            "HEIC format requires the HEIF Image Extensions from the Microsoft Store. Would you like to open the Store page?",
            result.Error.UserMessage);
    }

    [Fact]
    public void ValidateFilePath_AvifWithCodecAvailable_ReturnsSuccess()
    {
        var path = CreateFile("modern.avif");
        var service = new FileOpenService(new StubCodecDetectionService(avifSupported: true));

        var result = service.ValidateFilePath(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(path, result.Value);
    }

    [Fact]
    public void ValidateFilePath_AvifWithoutCodec_ReturnsCodecUnavailableError()
    {
        var path = CreateFile("modern.avif");
        var service = new FileOpenService(new StubCodecDetectionService(avifSupported: false));

        var result = service.ValidateFilePath(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.CodecUnavailableError, result.Error.Category);
        Assert.Equal(
            "AVIF format requires the AV1 Video Extension from the Microsoft Store. Would you like to open the Store page?",
            result.Error.UserMessage);
    }

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("archive.zip")]
    [InlineData("noextension")]
    public void ValidateFilePath_UnknownFormat_ReturnsUnsupportedFormatError(string filename)
    {
        var path = CreateFile(filename);

        var result = _service.ValidateFilePath(path);

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

    private string CreateFile(string filename)
    {
        var path = Path.Combine(_tempDirectory, filename);
        File.WriteAllText(path, "test");
        return path;
    }

    private sealed class StubCodecDetectionService : ICodecDetectionService
    {
        private readonly bool _heicSupported;
        private readonly bool _avifSupported;

        public StubCodecDetectionService(bool heicSupported = false, bool avifSupported = false)
        {
            _heicSupported = heicSupported;
            _avifSupported = avifSupported;
        }

        public Task<Result<CodecAvailability>> DetectCodecsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<CodecAvailability>.Success(
                new CodecAvailability(_heicSupported, _avifSupported, DateTimeOffset.UtcNow)));
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
}
