using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.App.Services;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using Windows.Graphics.Imaging;

namespace ImageViewer.App.Tests;

public sealed class WindowsImagingServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public WindowsImagingServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "spitfire-windows-imaging-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task ThumbnailService_GeneratesRealPngThumbnailWithScaledDimensions()
    {
        var source = Path.Combine(_tempDirectory, "source.png");
        await WritePngAsync(source, width: 400, height: 200);
        var service = new WindowsThumbnailService();

        var result = await service.GetThumbnailAsync(new ImageItem(source));

        Assert.True(result.IsSuccess);
        Assert.Equal(120, result.Value.PixelWidth);
        Assert.Equal(60, result.Value.PixelHeight);
        Assert.True(result.Value.EncodedBytes.Length > 8);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], result.Value.EncodedBytes.Take(4).ToArray());
    }

    [Theory]
    [InlineData(ImageExportFormat.Png, ".png", 0x89, 0x50)]
    [InlineData(ImageExportFormat.Jpeg, ".jpg", 0xFF, 0xD8)]
    [InlineData(ImageExportFormat.Bmp, ".bmp", 0x42, 0x4D)]
    [InlineData(ImageExportFormat.Webp, ".webp", 0x52, 0x49)]
    public async Task ExportImageAsync_ConvertsRealImageToTargetFormat(
        ImageExportFormat targetFormat,
        string extension,
        byte firstByte,
        byte secondByte)
    {
        var source = Path.Combine(_tempDirectory, "source.png");
        var destination = Path.Combine(_tempDirectory, $"export{extension}");
        await WritePngAsync(source, width: 20, height: 10);
        var service = new WindowsImageProcessingService();

        var result = await service.ExportImageAsync(
            source,
            destination,
            ExportOptions.Default with { TargetFormat = targetFormat });

        Assert.True(result.IsSuccess);
        var bytes = await File.ReadAllBytesAsync(destination);
        Assert.Equal(firstByte, bytes[0]);
        Assert.Equal(secondByte, bytes[1]);
    }

    [Fact]
    public async Task ExportImageAsync_Property_WritesRequestedTargetFormat()
    {
        var random = new Random(24680);
        var formats = new[]
        {
            (Format: ImageExportFormat.Png, Extension: ".png", Signature: new byte[] { 0x89, 0x50 }),
            (Format: ImageExportFormat.Jpeg, Extension: ".jpg", Signature: new byte[] { 0xFF, 0xD8 }),
            (Format: ImageExportFormat.Bmp, Extension: ".bmp", Signature: new byte[] { 0x42, 0x4D }),
            (Format: ImageExportFormat.Webp, Extension: ".webp", Signature: new byte[] { 0x52, 0x49 })
        };
        var service = new WindowsImageProcessingService();

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var source = Path.Combine(_tempDirectory, $"source-{iteration}.png");
            await WritePngAsync(source, width: random.Next(2, 32), height: random.Next(2, 32));
            var selected = formats[random.Next(formats.Length)];
            var destination = Path.Combine(_tempDirectory, $"export-{iteration}{selected.Extension}");

            var result = await service.ExportImageAsync(
                source,
                destination,
                ExportOptions.Default with { TargetFormat = selected.Format });

            Assert.True(result.IsSuccess);
            var bytes = await File.ReadAllBytesAsync(destination);
            Assert.Equal(selected.Signature[0], bytes[0]);
            Assert.Equal(selected.Signature[1], bytes[1]);
        }
    }

    [Fact]
    public async Task SaveTransformsAsync_RotatesRealImageOnDisk()
    {
        var source = Path.Combine(_tempDirectory, "rotate.png");
        await WritePngAsync(source, width: 40, height: 20);
        var service = new WindowsImageProcessingService();

        var result = await service.SaveTransformsAsync(source, [TransformOperation.RotateRight]);
        var decoded = await new ImageDecodeService().DecodeImageAsync(source);

        Assert.True(result.IsSuccess);
        Assert.True(decoded.IsSuccess);
        Assert.Equal(20, decoded.Value.Metadata.PixelWidth);
        Assert.Equal(40, decoded.Value.Metadata.PixelHeight);
    }

    [Fact]
    public async Task MetadataService_ValidImageWithoutExif_ReturnsEmptyMetadata()
    {
        var source = Path.Combine(_tempDirectory, "metadata.png");
        await WritePngAsync(source, width: 10, height: 10);
        var service = new WindowsMetadataService();

        var result = await service.ExtractExifAsync(source);

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

    private static async Task WritePngAsync(string path, int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = 0x30;
            pixels[index + 1] = 0x60;
            pixels[index + 2] = 0x90;
            pixels[index + 3] = 0xFF;
        }

        await using var output = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var randomAccessStream = output.AsRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, randomAccessStream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            checked((uint)width),
            checked((uint)height),
            96,
            96,
            pixels);
        await encoder.FlushAsync();
    }
}
