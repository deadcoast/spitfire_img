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

    [Theory]
    [InlineData("animated.gif")]
    [InlineData("scan.tiff")]
    [InlineData("scan.tif")]
    [InlineData("phone.heic")]
    [InlineData("modern.avif")]
    public void ValidateFilePath_DeferredFormat_ReturnsUnsupportedFormatError(string filename)
    {
        var path = CreateFile(filename);

        var result = _service.ValidateFilePath(path);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.UnsupportedFormatError, result.Error.Category);
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
}
