using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.App;
using ImageViewer.App.Services;
using ImageViewer.App.Views;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
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
    public void ParseInitialFilePath_UsesCommandLineFallbackWhenActivationArgsAreEmpty()
    {
        var path = Path.Combine(_tempDirectory, "launch image.png");

        var result = App.ParseInitialFilePath(string.Empty, ["ImageViewer.App.exe", path]);

        Assert.Equal(path, result);
    }

    [Fact]
    public void ParseInitialFilePath_PrefersActivationArgs()
    {
        var activationPath = Path.Combine(_tempDirectory, "activation.png");
        var commandLinePath = Path.Combine(_tempDirectory, "command-line.png");

        var result = App.ParseInitialFilePath($"\"{activationPath}\"", ["ImageViewer.App.exe", commandLinePath]);

        Assert.Equal(activationPath, result);
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

    [Fact]
    public async Task MetadataService_ExtractsGpsCoordinatesFromRealJpeg()
    {
        var source = Path.Combine(_tempDirectory, "gps-exif.jpg");
        await WriteJpegWithExifAsync(source, includeGps: true);
        var service = new WindowsMetadataService();

        var result = await service.ExtractExifAsync(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(49.2827, result.Value.Latitude.GetValueOrDefault(), precision: 4);
        Assert.Equal(-123.1207, result.Value.Longitude.GetValueOrDefault(), precision: 4);
        Assert.Equal(42.5, result.Value.Altitude.GetValueOrDefault(), precision: 1);
        Assert.Equal("49.282700 N, 123.120700 W", result.Value.FormattedGpsCoordinates);
    }

    [Fact]
    public async Task WindowsAnimationService_LoadAnimatedGifAsync_ExtractsFramesAndDelaysWithWic()
    {
        var path = Path.Combine(_tempDirectory, "animated.gif");
        await WriteAnimatedGifAsync(path, width: 3, height: 2, frameDelaysMs: [40, 120, 250]);
        var service = new WindowsAnimationService();

        var result = await service.LoadAnimatedGifAsync(path);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsAnimated);
        Assert.Equal(3, result.Value.TotalFrames);
        Assert.Equal(3, result.Value.Metadata.PixelWidth);
        Assert.Equal(2, result.Value.Metadata.PixelHeight);
        Assert.Equal(
            [TimeSpan.FromMilliseconds(40), TimeSpan.FromMilliseconds(120), TimeSpan.FromMilliseconds(250)],
            result.Value.Frames.Select(frame => frame.Delay).ToArray());
        Assert.All(result.Value.Frames, frame => Assert.Equal(3 * 2 * 4, frame.EncodedBytes.Length));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task WindowsAnimationService_LoadAnimatedGifAsync_ExtractsRealAnimatedGifsWithVariousFrameCounts(
        int frameCount)
    {
        var path = Path.Combine(_tempDirectory, $"animated-{frameCount}.gif");
        var delays = Enumerable.Range(0, frameCount)
            .Select(index => 30 + index * 40)
            .ToArray();
        await WriteAnimatedGifAsync(path, width: 4, height: 3, frameDelaysMs: delays);
        var service = new WindowsAnimationService();

        var result = await service.LoadAnimatedGifAsync(path);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsAnimated);
        Assert.Equal(frameCount, result.Value.TotalFrames);
        Assert.Equal(
            delays.Select(delay => TimeSpan.FromMilliseconds(delay)),
            result.Value.Frames.Select(frame => frame.Delay));
        Assert.All(result.Value.Frames, frame => Assert.Equal(4 * 3 * 4, frame.EncodedBytes.Length));
    }

    [Fact]
    public async Task MainViewModel_OpenFileAsync_WithRealAnimatedGif_LoadsPlaybackStateFromWicFrames()
    {
        var path = Path.Combine(_tempDirectory, "playback.gif");
        await WriteAnimatedGifAsync(path, width: 5, height: 4, frameDelaysMs: [50, 110, 230]);
        var viewModel = CreateRealImageViewModel(ViewerSettings.Default, new ManualSlideshowService());
        await viewModel.InitializeAsync();

        await viewModel.OpenFileAsync(path);

        Assert.True(viewModel.AnimationState.IsAnimationActive);
        Assert.True(viewModel.AnimationState.IsPlaying);
        Assert.Equal(3, viewModel.AnimationState.TotalFrames);
        Assert.Equal(TimeSpan.FromMilliseconds(50), viewModel.AnimationState.CurrentFrameDelay);
        Assert.Equal(5 * 4 * 4, viewModel.AnimationState.CurrentFrame?.EncodedBytes.Length);

        viewModel.AdvanceAnimationPlaybackFrame();

        Assert.Equal(1, viewModel.AnimationState.CurrentFrameIndex);
        Assert.Equal(TimeSpan.FromMilliseconds(110), viewModel.AnimationState.CurrentFrameDelay);
    }

    [Fact]
    public async Task MainViewModel_OpenFileAsync_WithRealSingleFrameGif_HidesAnimationState()
    {
        var path = Path.Combine(_tempDirectory, "single-frame.gif");
        await WriteAnimatedGifAsync(path, width: 5, height: 4, frameDelaysMs: [90]);
        var viewModel = CreateRealImageViewModel(ViewerSettings.Default, new ManualSlideshowService());
        await viewModel.InitializeAsync();

        await viewModel.OpenFileAsync(path);

        Assert.False(viewModel.AnimationState.IsAnimationActive);
        Assert.False(viewModel.AnimationState.IsPlaying);
        Assert.Equal(0, viewModel.AnimationState.TotalFrames);
    }

    [Theory]
    [InlineData(TransitionEffect.Fade, 300)]
    [InlineData(TransitionEffect.Slide, 400)]
    [InlineData(TransitionEffect.Zoom, 350)]
    public async Task SlideshowTransition_WithRealImages_BlocksManualNavigationUntilTransitionCompletes(
        TransitionEffect transition,
        int expectedDurationMs)
    {
        var first = Path.Combine(_tempDirectory, "alpha.png");
        var second = Path.Combine(_tempDirectory, "bravo.png");
        var third = Path.Combine(_tempDirectory, "charlie.png");
        await WritePngAsync(first, width: 20, height: 10);
        await WritePngAsync(second, width: 24, height: 12);
        await WritePngAsync(third, width: 28, height: 14);
        var slideshowService = new ManualSlideshowService();
        var viewModel = CreateRealImageViewModel(
            ViewerSettings.Default with { SlideshowTransition = transition },
            slideshowService);
        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(first);
        viewModel.ToggleSlideshow();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        slideshowService.RaiseAdvanceRequested(nextIndex: 1, new ImageItem(second));
        await WaitUntilAsync(() => viewModel.PlaylistState.CurrentIndex == 1 && viewModel.SlideshowState.IsTransitioning);
        await viewModel.NavigateNextAsync();
        Assert.Equal(1, viewModel.PlaylistState.CurrentIndex);

        await WaitUntilAsync(() => !viewModel.SlideshowState.IsTransitioning);
        stopwatch.Stop();
        Assert.InRange(stopwatch.ElapsedMilliseconds, expectedDurationMs - 25, expectedDurationMs + 1500);

        await viewModel.NavigateNextAsync();
        Assert.Equal(2, viewModel.PlaylistState.CurrentIndex);
    }

    [Fact]
    public async Task SlideshowTransition_None_WithRealImages_AdvancesWithoutTransitionDelay()
    {
        var first = Path.Combine(_tempDirectory, "none-alpha.png");
        var second = Path.Combine(_tempDirectory, "none-bravo.png");
        await WritePngAsync(first, width: 20, height: 10);
        await WritePngAsync(second, width: 24, height: 12);
        var slideshowService = new ManualSlideshowService();
        var viewModel = CreateRealImageViewModel(
            ViewerSettings.Default with { SlideshowTransition = TransitionEffect.None },
            slideshowService);
        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(first);
        viewModel.ToggleSlideshow();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        slideshowService.RaiseAdvanceRequested(nextIndex: 1, new ImageItem(second));
        await WaitUntilAsync(() => viewModel.PlaylistState.CurrentIndex == 1);
        await WaitUntilAsync(() => !viewModel.SlideshowState.IsTransitioning);
        stopwatch.Stop();

        Assert.InRange(stopwatch.ElapsedMilliseconds, 0, 500);
    }

    [Fact]
    public async Task SlideshowTransition_WithRealImages_RepeatedTransitionsKeepMemoryBounded()
    {
        var first = Path.Combine(_tempDirectory, "memory-alpha.png");
        var second = Path.Combine(_tempDirectory, "memory-bravo.png");
        await WritePngAsync(first, width: 32, height: 18);
        await WritePngAsync(second, width: 34, height: 20);
        var slideshowService = new ManualSlideshowService();
        var viewModel = CreateRealImageViewModel(
            ViewerSettings.Default with { SlideshowTransition = TransitionEffect.Fade },
            slideshowService);
        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(first);
        viewModel.ToggleSlideshow();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var beforeBytes = GC.GetTotalMemory(forceFullCollection: true);

        for (var index = 0; index < 12; index++)
        {
            var targetIndex = index % 2 == 0 ? 1 : 0;
            var targetPath = targetIndex == 1 ? second : first;
            slideshowService.RaiseAdvanceRequested(targetIndex, new ImageItem(targetPath));
            await WaitUntilAsync(() => viewModel.PlaylistState.CurrentIndex == targetIndex);
            await WaitUntilAsync(() => !viewModel.SlideshowState.IsTransitioning);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterBytes = GC.GetTotalMemory(forceFullCollection: true);
        Assert.InRange(afterBytes - beforeBytes, long.MinValue, 20L * 1024 * 1024);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task ComparisonSideBySide_WithRealImages_LoadsRequestedVisibleImageCount(int imageCount)
    {
        var first = Path.Combine(_tempDirectory, "comparison-0.png");
        var second = Path.Combine(_tempDirectory, "comparison-1.png");
        var third = Path.Combine(_tempDirectory, "comparison-2.png");
        var fourth = Path.Combine(_tempDirectory, "comparison-3.png");
        await WritePngAsync(first, width: 20, height: 10);
        await WritePngAsync(second, width: 24, height: 12);
        await WritePngAsync(third, width: 28, height: 14);
        await WritePngAsync(fourth, width: 32, height: 16);
        var viewModel = CreateRealImageViewModel(ViewerSettings.Default, new ManualSlideshowService());
        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(first);

        await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide, imageCount);

        Assert.True(viewModel.ComparisonState.IsActive);
        Assert.Equal(ComparisonLayout.SideBySide, viewModel.ComparisonState.Layout);
        Assert.Equal(imageCount, viewModel.ComparisonState.VisibleImages.Count);
        Assert.Equal(
            Enumerable.Range(0, imageCount).ToArray(),
            viewModel.ComparisonState.VisibleImages.Select(image => image.PlaylistIndex).ToArray());
        Assert.All(viewModel.ComparisonState.VisibleImages, image => Assert.EndsWith(".png", image.Image.FilePath));
    }

    [Fact]
    public async Task ComparisonOverlay_WithDifferentRealImageDimensions_LoadsCenteredLayers()
    {
        var first = Path.Combine(_tempDirectory, "overlay-0.png");
        var second = Path.Combine(_tempDirectory, "overlay-1.png");
        await WritePngAsync(first, width: 80, height: 30);
        await WritePngAsync(second, width: 25, height: 70);
        var viewModel = CreateRealImageViewModel(ViewerSettings.Default, new ManualSlideshowService());
        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(first);

        await viewModel.EnterComparisonAsync(ComparisonLayout.Overlay);

        Assert.Equal(ComparisonLayout.Overlay, viewModel.ComparisonState.Layout);
        Assert.Equal(0.5, viewModel.ComparisonState.OverlayOpacity);
        Assert.Equal([80, 25], viewModel.ComparisonState.VisibleImages.Select(image => image.Image.Metadata.PixelWidth).ToArray());
        Assert.All(
            viewModel.ComparisonState.Viewports,
            viewport => Assert.Equal(new RelativePanOffset(0.5, 0.5), viewport.RelativePan));
    }

    [Fact]
    public async Task ComparisonSynchronizedZoom_WithDifferentRealImages_UsesRelativePan()
    {
        var first = Path.Combine(_tempDirectory, "sync-0.png");
        var second = Path.Combine(_tempDirectory, "sync-1.png");
        await WritePngAsync(first, width: 120, height: 60);
        await WritePngAsync(second, width: 33, height: 91);
        var viewModel = CreateRealImageViewModel(ViewerSettings.Default, new ManualSlideshowService());
        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(first);
        await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide);
        var pan = new RelativePanOffset(0.2, 0.8);

        viewModel.ZoomIn();
        viewModel.SetComparisonRelativePan(pan);

        Assert.True(viewModel.ComparisonState.IsSyncZoomEnabled);
        Assert.All(viewModel.ComparisonState.Viewports, viewport =>
        {
            Assert.Equal(125, viewport.ZoomLevel);
            Assert.Equal(pan, viewport.RelativePan);
        });
    }

    [Fact]
    public async Task ComparisonLayouts_WithRealImages_RepeatedSwitchesKeepMemoryBounded()
    {
        var first = Path.Combine(_tempDirectory, "layout-memory-0.png");
        var second = Path.Combine(_tempDirectory, "layout-memory-1.png");
        var third = Path.Combine(_tempDirectory, "layout-memory-2.png");
        var fourth = Path.Combine(_tempDirectory, "layout-memory-3.png");
        await WritePngAsync(first, width: 48, height: 24);
        await WritePngAsync(second, width: 52, height: 26);
        await WritePngAsync(third, width: 56, height: 28);
        await WritePngAsync(fourth, width: 60, height: 30);
        var viewModel = CreateRealImageViewModel(ViewerSettings.Default, new ManualSlideshowService());
        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(first);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var beforeBytes = GC.GetTotalMemory(forceFullCollection: true);

        for (var iteration = 0; iteration < 10; iteration++)
        {
            await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide, 2 + (iteration % 3));
            await viewModel.EnterComparisonAsync(ComparisonLayout.Overlay);
            viewModel.ZoomIn();
            viewModel.SetComparisonRelativePan(new RelativePanOffset(0.25, 0.75));
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterBytes = GC.GetTotalMemory(forceFullCollection: true);
        Assert.InRange(afterBytes - beforeBytes, long.MinValue, 20L * 1024 * 1024);
    }

    [Fact]
    public async Task CustomGridLayout_WithRealImages_LoadsVariousGridDensities()
    {
        var paths = await WriteImageSequenceAsync("custom-grid", count: 36);
        var viewModel = CreateRealImageViewModel(ViewerSettings.Default, new ManualSlideshowService());
        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(paths[0]);

        await viewModel.EnterGridLayoutAsync();
        Assert.Equal(LayoutMode.Grid, viewModel.LayoutState.Mode);
        Assert.Equal(3, viewModel.LayoutState.Config.Rows);
        Assert.Equal(9, viewModel.LayoutState.VisibleImages.Count);

        await viewModel.IncreaseGridDensityAsync();
        Assert.Equal(4, viewModel.LayoutState.Config.Rows);
        Assert.Equal(16, viewModel.LayoutState.VisibleImages.Count);

        await viewModel.IncreaseGridDensityAsync();
        Assert.Equal(5, viewModel.LayoutState.Config.Rows);
        Assert.Equal(25, viewModel.LayoutState.VisibleImages.Count);

        await viewModel.IncreaseGridDensityAsync();
        Assert.Equal(6, viewModel.LayoutState.Config.Rows);
        Assert.Equal(36, viewModel.LayoutState.VisibleImages.Count);
        Assert.Equal(
            Enumerable.Range(0, 36),
            viewModel.LayoutState.VisibleImages.Select(image => image.PlaylistIndex));
    }

    [Fact]
    public async Task CustomFilmstripLayout_WithRealImages_UsesRealThumbnailServiceForVisibleStrip()
    {
        var paths = await WriteImageSequenceAsync("custom-filmstrip", count: 12);
        var viewModel = CreateRealImageViewModel(
            ViewerSettings.Default,
            new ManualSlideshowService(),
            new WindowsThumbnailService());
        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(paths[5]);

        await viewModel.EnterFilmstripLayoutAsync();

        Assert.Equal(LayoutMode.Filmstrip, viewModel.LayoutState.Mode);
        Assert.Equal(5, viewModel.LayoutState.FocusImage?.PlaylistIndex);
        Assert.Equal(
            Enumerable.Range(1, 9),
            viewModel.LayoutState.VisibleImages.Select(image => image.PlaylistIndex));

        var focusThumbnail = await viewModel.GetThumbnailAsync(new ImageItem(paths[5]));
        Assert.True(focusThumbnail.IsSuccess);
        Assert.True(focusThumbnail.Value.EncodedBytes.Length > 8);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], focusThumbnail.Value.EncodedBytes.Take(4).ToArray());
    }

    [Fact]
    public async Task CustomLayouts_WithRealImages_SwitchBetweenGridAndFilmstripCleanly()
    {
        var paths = await WriteImageSequenceAsync("custom-switch", count: 12);
        var viewModel = CreateRealImageViewModel(ViewerSettings.Default, new ManualSlideshowService());
        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(paths[4]);

        await viewModel.EnterGridLayoutAsync();
        await viewModel.IncreaseGridDensityAsync();
        Assert.Equal(LayoutMode.Grid, viewModel.LayoutState.Mode);
        Assert.Equal(4, viewModel.LayoutState.Config.Rows);

        await viewModel.EnterFilmstripLayoutAsync();
        Assert.Equal(LayoutMode.Filmstrip, viewModel.LayoutState.Mode);
        Assert.Equal(4, viewModel.LayoutState.FocusImage?.PlaylistIndex);
        Assert.Equal(@"custom-switch-04.png", viewModel.PlaylistState.CurrentItem?.Filename);

        await viewModel.EnterGridLayoutAsync();
        Assert.Equal(LayoutMode.Grid, viewModel.LayoutState.Mode);
        Assert.Equal(4, viewModel.LayoutState.Config.Rows);
        Assert.Equal(4, viewModel.LayoutState.Config.Columns);
        Assert.Equal(4, viewModel.LayoutState.WindowStartIndex);
    }

    [Fact]
    public async Task CustomLayouts_WithManyRealImages_RepeatedSwitchesKeepMemoryBounded()
    {
        var paths = await WriteImageSequenceAsync("custom-memory", count: 48);
        var viewModel = CreateRealImageViewModel(
            ViewerSettings.Default,
            new ManualSlideshowService(),
            new WindowsThumbnailService());
        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(paths[10]);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var beforeBytes = GC.GetTotalMemory(forceFullCollection: true);

        for (var iteration = 0; iteration < 10; iteration++)
        {
            await viewModel.EnterGridLayoutAsync();
            await viewModel.IncreaseGridDensityAsync();
            await viewModel.ScrollGridLayoutAsync(1);
            await viewModel.EnterFilmstripLayoutAsync();
            await viewModel.NavigateNextAsync();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterBytes = GC.GetTotalMemory(forceFullCollection: true);
        Assert.InRange(afterBytes - beforeBytes, long.MinValue, 30L * 1024 * 1024);
    }

    [Fact]
    public async Task CommandPalette_WithRealRegistry_SearchesExecutesAndTracksRecentCommand()
    {
        var viewModel = CreateRealImageViewModel(ViewerSettings.Default, new ManualSlideshowService());
        await viewModel.InitializeAsync();
        viewModel.ShowCommandPalette();
        viewModel.CommandPaletteState.SetSearchQuery("fullscreen");
        viewModel.CommandPaletteState.SelectCommand(CommandIds.ToggleFullscreen);

        await viewModel.ExecuteSelectedCommandAsync();
        viewModel.ShowCommandPalette();

        Assert.True(viewModel.ViewerState.IsFullscreen);
        Assert.Equal(CommandIds.ToggleFullscreen, viewModel.CommandPaletteState.RecentCommands[0].Id);
        Assert.Equal("Recent", viewModel.CommandPaletteState.DisplayItems[0].Header);
        Assert.Contains(viewModel.CommandPaletteState.DisplayItems, item => item.Header == CommandCategory.View.ToString());
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

    private static MainViewModel CreateRealImageViewModel(
        ViewerSettings settings,
        ISlideshowService slideshowService,
        IThumbnailService? thumbnailService = null)
    {
        var decoder = new ImageDecodeService();
        return new MainViewModel(
            new FileOpenService(),
            decoder,
            new FolderPlaylistService(),
            new NavigationService(decoder),
            new StubSettingsService(settings),
            new RecordingErrorLogService(),
            new SearchService(),
            thumbnailService ?? new ThumbnailService(decoder),
            new RecentFilesService(Path.Combine(Path.GetTempPath(), "spitfire-img-app-test-recent", Guid.NewGuid().ToString("N"))),
            new ExportService(),
            new TransformService(),
            new WindowsMetadataService(),
            slideshowService,
            new ComparisonService(decoder),
            new CommandPaletteService(),
            new WindowsAnimationService());
    }

    private async Task<string[]> WriteImageSequenceAsync(string prefix, int count)
    {
        var paths = new string[count];
        for (var index = 0; index < count; index++)
        {
            var path = Path.Combine(_tempDirectory, $"{prefix}-{index:00}.png");
            await WritePngAsync(path, width: 20 + index, height: 12 + index % 7);
            paths[index] = path;
        }

        return paths;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (!condition())
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Delay(10, timeout.Token);
        }
    }

    private static async Task WriteJpegWithExifAsync(
        string path,
        string make = "Canon",
        string model = "R5",
        string software = "spitfire-test",
        ushort iso = 400,
        int width = 16,
        int height = 16,
        bool includeGps = false)
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
        if (includeGps)
        {
            exif.SetValue(ExifTag.GPSLatitudeRef, "N");
            exif.SetValue(ExifTag.GPSLatitude, ToGpsRationals(49.2827));
            exif.SetValue(ExifTag.GPSLongitudeRef, "W");
            exif.SetValue(ExifTag.GPSLongitude, ToGpsRationals(123.1207));
            exif.SetValue(ExifTag.GPSAltitude, new Rational(425, 10));
        }

        image.Metadata.ExifProfile = exif;
        await image.SaveAsJpegAsync(path);
    }

    private static async Task WriteAnimatedGifAsync(
        string path,
        int width,
        int height,
        IReadOnlyList<int> frameDelaysMs)
    {
        using var image = CreateSolidImage(width, height, new Rgba32(255, 0, 0));
        image.Metadata.GetGifMetadata().RepeatCount = 0;
        image.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = frameDelaysMs[0] / 10;

        for (var index = 1; index < frameDelaysMs.Count; index++)
        {
            using var frameImage = CreateSolidImage(width, height, new Rgba32(0, (byte)(80 + index * 40), 255));
            frameImage.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = frameDelaysMs[index] / 10;
            image.Frames.AddFrame(frameImage.Frames.RootFrame);
        }

        await image.SaveAsGifAsync(path);
    }

    private static Image<Rgba32> CreateSolidImage(int width, int height, Rgba32 color)
    {
        var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                row.Fill(color);
            }
        });

        return image;
    }

    private static Rational[] ToGpsRationals(double decimalDegrees)
    {
        var degrees = Math.Floor(decimalDegrees);
        var minutesDecimal = (decimalDegrees - degrees) * 60;
        var minutes = Math.Floor(minutesDecimal);
        var seconds = (minutesDecimal - minutes) * 60;

        return
        [
            new Rational((uint)degrees, 1),
            new Rational((uint)minutes, 1),
            new Rational((uint)Math.Round(seconds * 10000), 10000)
        ];
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

    private sealed class StubSettingsService : ISettingsService
    {
        private readonly ViewerSettings _settings;

        public StubSettingsService(ViewerSettings settings)
        {
            _settings = settings;
        }

        public Task<Result<ViewerSettings>> LoadSettingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<ViewerSettings>.Success(_settings));
        }

        public Task<Result> SaveSettingsAsync(ViewerSettings settings, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class RecordingErrorLogService : IErrorLogService
    {
        public Task<Result> LogErrorAsync(ViewerError error, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> CleanupOldLogsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class ManualSlideshowService : ISlideshowService
    {
        public event EventHandler<SlideshowAdvanceRequestedEventArgs>? AdvanceRequested;

        public bool IsActive { get; private set; }

        public bool IsPaused { get; private set; }

        public Result StartSlideshow(Playlist playlist, int currentIndex, SlideshowConfig config)
        {
            IsActive = true;
            IsPaused = false;
            return Result.Success();
        }

        public Result StopSlideshow()
        {
            IsActive = false;
            IsPaused = false;
            return Result.Success();
        }

        public Result PauseSlideshow()
        {
            IsPaused = true;
            return Result.Success();
        }

        public Result ResumeSlideshow()
        {
            IsPaused = false;
            return Result.Success();
        }

        public Result UpdateConfig(SlideshowConfig config)
        {
            return Result.Success();
        }

        public Result ResetTimer()
        {
            return Result.Success();
        }

        public void RaiseAdvanceRequested(int nextIndex, ImageItem nextItem)
        {
            AdvanceRequested?.Invoke(this, new SlideshowAdvanceRequestedEventArgs(nextIndex, nextItem));
        }
    }
}
