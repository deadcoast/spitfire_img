using ImageViewer.Core.Models;

namespace ImageViewer.Tests.Models;

public sealed class Phase2ModelTests
{
    [Fact]
    public void RecentFile_ComputesFilenameAndFolderPath()
    {
        var recent = new RecentFile(@"C:\Photos\Trip\image.png", DateTimeOffset.UtcNow);

        Assert.Equal("image.png", recent.Filename);
        Assert.Equal(@"C:\Photos\Trip", recent.FolderPath);
    }

    [Fact]
    public void ExifMetadata_ComputesAvailabilityAndFormattedValues()
    {
        var metadata = new ExifMetadata(
            CameraMake: "Nikon",
            CameraModel: "Z8",
            Software: null,
            CaptureDate: new DateTimeOffset(2026, 4, 27, 12, 0, 0, TimeSpan.Zero),
            Iso: 400,
            Aperture: 2.8,
            ShutterSpeed: 1d / 125d,
            FocalLength: 50,
            FlashFired: false,
            Latitude: 49.2827,
            Longitude: -123.1207,
            Altitude: null,
            ColorSpace: "sRGB");

        Assert.True(metadata.HasCameraInfo);
        Assert.True(metadata.HasCaptureSettings);
        Assert.True(metadata.HasGpsInfo);
        Assert.Equal("f/2.8", metadata.FormattedAperture);
        Assert.Equal("1/125 s", metadata.FormattedShutterSpeed);
        Assert.Equal("50 mm", metadata.FormattedFocalLength);
        Assert.Equal("49.282700 N, 123.120700 W", metadata.FormattedGpsCoordinates);
        Assert.Equal("Apr 27, 2026, 12:00 PM", metadata.FormattedCaptureDate);
        Assert.Equal("No flash", metadata.FormattedFlashStatus);
    }

    [Fact]
    public void ExifMetadata_FormatsAltitudeAndFlash()
    {
        var metadata = new ExifMetadata(
            FlashFired: true,
            Latitude: -33.8688,
            Longitude: 151.2093,
            Altitude: 42.25);

        Assert.Equal("33.868800 S, 151.209300 E", metadata.FormattedGpsCoordinates);
        Assert.Equal("42.3 m", metadata.FormattedAltitude);
        Assert.Equal("Flash fired", metadata.FormattedFlashStatus);
    }

    [Fact]
    public void ExportOptions_DefaultsMatchPhase2Requirements()
    {
        var options = ExportOptions.Default;

        Assert.Equal(ImageExportFormat.Png, options.TargetFormat);
        Assert.Equal(90, options.JpegQuality);
        Assert.True(options.PreserveExif);
        Assert.False(options.OverwriteExisting);
    }

    [Fact]
    public void ViewerSettings_DefaultsIncludePhase2Preferences()
    {
        var settings = ViewerSettings.Default;

        Assert.False(settings.ThumbnailStripVisible);
        Assert.Equal(ThumbnailPosition.Bottom, settings.ThumbnailStripPosition);
        Assert.Equal(90, settings.DefaultJpegQuality);
        Assert.True(settings.PreserveExifOnExport);
    }

    [Fact]
    public void ViewerError_Phase2FactoriesReturnUserSafeMessages()
    {
        var export = ViewerError.ExportError("Export", @"C:\out.jpg", new IOException("raw failure"));
        var transformSave = ViewerError.TransformSaveError("SaveTransform", @"C:\image.jpg");

        Assert.Equal(ErrorCategory.ExportError, export.Category);
        Assert.Equal("Unable to save a copy of this image to the selected location.", export.UserMessage);
        Assert.DoesNotContain("raw failure", export.UserMessage);
        Assert.Equal(ErrorCategory.TransformSaveError, transformSave.Category);
        Assert.Equal("Unable to save changes to this image.", transformSave.UserMessage);
    }
}
