using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class ExportTransformMetadataServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public ExportTransformMetadataServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "spitfire-phase2-service-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task ExportImageAsync_CopiesSourceWithoutOverwritingUnlessAllowed()
    {
        var source = CreateFile("source.png", "image");
        var destination = Path.Combine(_tempDirectory, "copy.png");
        var service = new ExportService();

        var first = await service.ExportImageAsync(source, destination, ExportOptions.Default);
        var second = await service.ExportImageAsync(source, destination, ExportOptions.Default);
        var overwrite = await service.ExportImageAsync(source, destination, ExportOptions.Default with { OverwriteExisting = true });

        Assert.True(first.IsSuccess);
        Assert.True(File.Exists(destination));
        Assert.True(second.IsFailure);
        Assert.Equal(ErrorCategory.ExportError, second.Error.Category);
        Assert.True(overwrite.IsSuccess);
    }

    [Theory]
    [InlineData(TransformOperation.RotateLeft, 200, 100)]
    [InlineData(TransformOperation.RotateRight, 200, 100)]
    [InlineData(TransformOperation.FlipHorizontal, 100, 200)]
    [InlineData(TransformOperation.FlipVertical, 100, 200)]
    public async Task ApplyTransformAsync_ReturnsExpectedPreviewDimensions(
        TransformOperation operation,
        int expectedWidth,
        int expectedHeight)
    {
        var service = new TransformService();
        var image = new DecodedImage(@"C:\Images\a.png", new ImageMetadata(100, 200, 10));

        var result = await service.ApplyTransformAsync(image, [operation]);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedWidth, result.Value.Metadata.PixelWidth);
        Assert.Equal(expectedHeight, result.Value.Metadata.PixelHeight);
    }

    [Fact]
    public async Task SaveTransformsAsync_MissingFile_ReturnsTransformSaveError()
    {
        var result = await new TransformService().SaveTransformsAsync(
            Path.Combine(_tempDirectory, "missing.png"),
            [TransformOperation.RotateRight]);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.TransformSaveError, result.Error.Category);
    }

    [Fact]
    public async Task ExtractExifAsync_MissingFile_ReturnsMetadataExtractionError()
    {
        var result = await new MetadataService().ExtractExifAsync(Path.Combine(_tempDirectory, "missing.jpg"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.MetadataExtractionError, result.Error.Category);
    }

    [Fact]
    public async Task ExtractExifAsync_FileWithoutExif_ReturnsEmptyMetadata()
    {
        var source = CreateFile("plain.jpg", "not a real photo");

        var result = await new MetadataService().ExtractExifAsync(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExifMetadata.Empty, result.Value);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private string CreateFile(string filename, string content)
    {
        var path = Path.Combine(_tempDirectory, filename);
        File.WriteAllText(path, content);
        return path;
    }
}
