using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.App.Services;
using ImageViewer.App.Views;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
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
    public void ThumbnailLoadPlanner_PrioritizesCurrentItemThenNearestNeighbors()
    {
        var indexes = ThumbnailLoadPlanner.PrioritizeVisibleFirst(itemCount: 7, currentIndex: 3);

        Assert.Equal([3, 2, 4, 1, 5, 0, 6], indexes);
    }

    [Theory]
    [InlineData(5, -10, new[] { 0, 1, 2, 3, 4 })]
    [InlineData(5, 99, new[] { 4, 3, 2, 1, 0 })]
    [InlineData(0, 0, new int[0])]
    public void ThumbnailLoadPlanner_ClampsInvalidInputs(
        int itemCount,
        int currentIndex,
        int[] expected)
    {
        var indexes = ThumbnailLoadPlanner.PrioritizeVisibleFirst(itemCount, currentIndex);

        Assert.Equal(expected, indexes);
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
    public async Task ExportImageAsync_PreservesExifWhenRequested()
    {
        var source = Path.Combine(_tempDirectory, "source-exif.jpg");
        var destination = Path.Combine(_tempDirectory, "export-exif.jpg");
        await WriteJpegWithExifAsync(source);
        var service = new WindowsImageProcessingService();

        var export = await service.ExportImageAsync(
            source,
            destination,
            ExportOptions.Default with { TargetFormat = ImageExportFormat.Jpeg, PreserveExif = true });
        var metadata = await new WindowsMetadataService().ExtractExifAsync(destination);

        Assert.True(export.IsSuccess);
        Assert.True(metadata.IsSuccess);
        Assert.Equal("Canon", metadata.Value.CameraMake);
        Assert.Equal("R5", metadata.Value.CameraModel);
        Assert.Equal("spitfire-test", metadata.Value.Software);
        Assert.Equal(400, metadata.Value.Iso);
        Assert.Equal(2.8, metadata.Value.Aperture);
        Assert.Equal(0.008, metadata.Value.ShutterSpeed);
        Assert.Equal(50, metadata.Value.FocalLength);
    }

    [Fact]
    public async Task ExportImageAsync_Property_PreservesExifForSupportedFormats()
    {
        var random = new Random(97531);
        var formats = new[]
        {
            (Format: ImageExportFormat.Jpeg, Extension: ".jpg"),
            (Format: ImageExportFormat.Png, Extension: ".png")
        };
        var service = new WindowsImageProcessingService();

        for (var iteration = 0; iteration < 100; iteration++)
        {
            var selected = formats[random.Next(formats.Length)];
            var make = $"Make-{iteration}";
            var model = $"Model-{random.Next(1000, 9999)}";
            var software = $"spitfire-{iteration}";
            var iso = (ushort)random.Next(100, 3200);
            var source = Path.Combine(_tempDirectory, $"source-exif-{iteration}.jpg");
            var destination = Path.Combine(_tempDirectory, $"export-exif-{iteration}{selected.Extension}");
            await WriteJpegWithExifAsync(source, make, model, software, iso);

            var export = await service.ExportImageAsync(
                source,
                destination,
                ExportOptions.Default with { TargetFormat = selected.Format, PreserveExif = true });
            var exportedExif = await ReadExifSnapshotAsync(destination);

            Assert.True(export.IsSuccess);
            Assert.Equal(make, exportedExif.Make);
            Assert.Equal(model, exportedExif.Model);
            Assert.Equal(software, exportedExif.Software);
            Assert.Equal(iso, exportedExif.Iso);
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
    public async Task SaveTransformsAsync_PreservesExifAndResetsOrientationForJpeg()
    {
        var source = Path.Combine(_tempDirectory, "rotate-exif.jpg");
        await WriteJpegWithExifAsync(source, width: 24, height: 12);
        var service = new WindowsImageProcessingService();

        var result = await service.SaveTransformsAsync(source, [TransformOperation.RotateRight]);
        var decoded = await new ImageDecodeService().DecodeImageAsync(source);
        var exif = await ReadExifSnapshotAsync(source);

        Assert.True(result.IsSuccess);
        Assert.True(decoded.IsSuccess);
        Assert.Equal(12, decoded.Value.Metadata.PixelWidth);
        Assert.Equal(24, decoded.Value.Metadata.PixelHeight);
        Assert.Equal("Canon", exif.Make);
        Assert.Equal("R5", exif.Model);
        Assert.Equal("spitfire-test", exif.Software);
        Assert.Equal((ushort)400, exif.Iso);
        Assert.Equal((ushort)1, exif.Orientation);
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

    [Fact]
    public async Task MetadataService_ExtractsExifFromRealJpeg()
    {
        var source = Path.Combine(_tempDirectory, "exif.jpg");
        await WriteJpegWithExifAsync(source);
        var service = new WindowsMetadataService();

        var result = await service.ExtractExifAsync(source);

        Assert.True(result.IsSuccess);
        Assert.Equal("Canon", result.Value.CameraMake);
        Assert.Equal("R5", result.Value.CameraModel);
        Assert.Equal("spitfire-test", result.Value.Software);
        Assert.Equal(400, result.Value.Iso);
        Assert.Equal(2.8, result.Value.Aperture);
        Assert.Equal(0.008, result.Value.ShutterSpeed);
        Assert.Equal(50, result.Value.FocalLength);
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

    private static async Task WriteJpegWithExifAsync(
        string path,
        string make = "Canon",
        string model = "R5",
        string software = "spitfire-test",
        ushort iso = 400,
        int width = 16,
        int height = 16)
    {
        using var image = new Image<Rgba32>(width, height);
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Make, make);
        exif.SetValue(ExifTag.Model, model);
        exif.SetValue(ExifTag.Software, software);
        exif.SetValue(ExifTag.DateTimeOriginal, "2026:04:28 13:45:30");
        exif.SetValue(ExifTag.ISOSpeedRatings, new[] { iso });
        exif.SetValue(ExifTag.FNumber, new Rational(28, 10));
        exif.SetValue(ExifTag.ExposureTime, new Rational(1, 125));
        exif.SetValue(ExifTag.FocalLength, new Rational(50, 1));
        exif.SetValue(ExifTag.Orientation, (ushort)1);
        image.Metadata.ExifProfile = exif;
        await image.SaveAsJpegAsync(path);
    }

    private static async Task<ExifSnapshot> ReadExifSnapshotAsync(string path)
    {
        using var image = await Image.LoadAsync(path);
        var profile = image.Metadata.ExifProfile;
        return new ExifSnapshot(
            GetExifString(profile, ExifTag.Make),
            GetExifString(profile, ExifTag.Model),
            GetExifString(profile, ExifTag.Software),
            GetExifIso(profile),
            GetExifOrientation(profile));
    }

    private static string? GetExifString(ExifProfile? profile, ExifTag<string> tag)
    {
        return profile is not null && profile.TryGetValue(tag, out IExifValue<string>? value)
            ? value.Value
            : null;
    }

    private static ushort? GetExifIso(ExifProfile? profile)
    {
        if (profile is null ||
            !profile.TryGetValue(ExifTag.ISOSpeedRatings, out IExifValue<ushort[]>? value) ||
            value.Value is not { Length: > 0 } isoValues)
        {
            return null;
        }

        return isoValues[0];
    }

    private static ushort? GetExifOrientation(ExifProfile? profile)
    {
        return profile is not null && profile.TryGetValue(ExifTag.Orientation, out IExifValue<ushort>? value)
            ? value.Value
            : null;
    }

    private sealed record ExifSnapshot(string? Make, string? Model, string? Software, ushort? Iso, ushort? Orientation);
}
