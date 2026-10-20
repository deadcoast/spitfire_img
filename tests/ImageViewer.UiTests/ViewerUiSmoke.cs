using System.Text.Json;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.App.Services.Motion;
using ImageViewer.Core.Models;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using Image = Microsoft.UI.Xaml.Controls.Image;

namespace ImageViewer.App.Verification;

// This file is excluded from normal app builds. It exercises the real XAML tree
// and captures its rendered content; it is not a desktop pointer/keyboard test.
internal static class ViewerUiSmoke
{
    internal static string OutputDirectory =>
        Environment.GetEnvironmentVariable("SPITFIRE_UI_TEST_OUTPUT")
        ?? throw new InvalidOperationException("Set SPITFIRE_UI_TEST_OUTPUT to a verification directory.");

    internal static async Task RunAsync(MainWindow window, MainViewModel viewModel)
    {
        var checks = new List<object>();
        Directory.CreateDirectory(OutputDirectory);
        try
        {
            var fixtures = await CreateFixturesAsync();
            await Task.Delay(800);
            var root = (FrameworkElement)window.Content;
            var chrome = Find<Views.FadingChrome>(root, "Chrome");
            await CaptureAsync(root, "01-empty");
            chrome.ShowChrome();
            await Task.Delay(2400);
            var chromeRoot = Find<Grid>(chrome, "ChromeRoot");
            checks.Add(new { name = "empty-window-remains-usable", passed = chromeRoot.Opacity > .99 && Find<Button>(root, "OpenImageButton").IsEnabled, opacity = chromeRoot.Opacity });
            var fixture = Environment.GetEnvironmentVariable("SPITFIRE_UI_TEST_IMAGE") ?? fixtures.Image;
            if (!string.IsNullOrWhiteSpace(fixture))
            {
                await window.OpenInitialFileAsync(fixture);
                await Task.Delay(600);
                chrome.ShowChrome();
                await Task.Delay(150);
                await CaptureAsync(root, "02-image-active");
                var displayedImage = Find<Image>(root, "DisplayedImage");
                checks.Add(new { name = "image-rendered", passed = displayedImage.Source is not null && displayedImage.Opacity > .99 });
                await Task.Delay(2300);
                await CaptureAsync(root, "03-image-idle");
                checks.Add(new { name = "idle-chrome-hidden", passed = chromeRoot.Opacity < .01, opacity = chromeRoot.Opacity });
                chrome.ShowChrome();
                await Task.Delay(120);
                var top = Find<Border>(chrome, "TopChromeSurface");
                var before = top.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point());
                chrome.ShowChrome();
                var after = top.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point());
                checks.Add(new { name = "repeated-activity-keeps-chrome-stationary", passed = Math.Abs(after.Y - before.Y) < .01, beforeY = before.Y, afterY = after.Y });
                checks.Add(new { name = "compact-toolbar", passed = top.ActualHeight <= 44, height = top.ActualHeight });

                var viewMenu = Find<DropDownButton>(chrome, "ViewMenuButton");
                viewMenu.Flyout.ShowAt(viewMenu);
                await Task.Delay(2450);
                checks.Add(new { name = "menu-remains-visible-after-idle-delay", passed = chromeRoot.Opacity > .99 });
                foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot))
                    if (popup.Child is FrameworkElement presenter)
                        await CaptureAsync(presenter, "04-view-menu");
                viewMenu.Flyout.Hide();
                await Task.Delay(200);

                var initialIndex = viewModel.PlaylistState.CurrentIndex;
                Invoke(Find<Button>(chrome, "NextButton"));
                await Task.Delay(500);
                checks.Add(new { name = "next-button-opens-sibling-image", passed = viewModel.PlaylistState.CurrentIndex == initialIndex + 1 && displayedImage.Source is not null });
                Invoke(Find<Button>(chrome, "PreviousButton"));
                await Task.Delay(500);
                checks.Add(new { name = "previous-button-returns-to-image", passed = viewModel.PlaylistState.CurrentIndex == initialIndex });

                Invoke(Find<Button>(chrome, "ActualSizeButton"));
                await Task.Delay(200);
                checks.Add(new { name = "actual-size-physical-pixels", passed =
                    Math.Abs(displayedImage.ActualWidth * root.XamlRoot.RasterizationScale - viewModel.ViewerState.CurrentImage!.Metadata.PixelWidth) < 1 });
                Invoke(Find<Button>(chrome, "FitButton"));
                await Task.Delay(100);
                await viewModel.ExecuteCommandAsync("view.thumbnails");
                await Task.Delay(700);
                var strip = Find<Views.ThumbnailStrip>(root, "ThumbnailStrip");
                var list = Find<ListView>(strip, "ThumbnailStripList");
                checks.Add(new { name = "thumbnails-render-folder", passed = strip.Visibility == Visibility.Visible && list.Items.Count == viewModel.PlaylistState.TotalCount && CountImages(strip) > 0 });
                await CaptureAsync(root, "05-thumbnails");
                var firstThumbnail = list.Items[0];
                await viewModel.NavigateNextAsync();
                await Task.Delay(400);
                checks.Add(new { name = "navigation-preserves-thumbnail-items", passed = ReferenceEquals(firstThumbnail, list.Items[0]) });
                await viewModel.ExecuteCommandAsync("view.thumbnails");

                Invoke(Find<Button>(chrome, "SearchButton"));
                await Task.Delay(250);
                var search = Find<Views.SearchOverlay>(root, "SearchOverlay");
                Find<TextBox>(search, "SearchInput").Text = "bravo";
                await Task.Delay(300);
                checks.Add(new { name = "search-filters-folder", passed = viewModel.SearchState.ResultCount == 1 });
                await CaptureAsync(root, "06-search");
                viewModel.HideSearch();
                await Task.Delay(30);
                viewModel.ShowSearch();
                await Task.Delay(300);
                checks.Add(new { name = "rapid-search-reopen-survives-old-hide", passed = search.Visibility == Visibility.Visible && search.Opacity > .99 });
                viewModel.HideSearch();
                await Task.Delay(200);

                await viewModel.ExecuteCommandAsync("image.metadata");
                await Task.Delay(400);
                await CaptureAsync(root, "07-metadata");
                checks.Add(new { name = "metadata-opens", passed = viewModel.MetadataState.IsVisible });
                viewModel.MetadataState.HidePanel();
                await viewModel.ExecuteCommandAsync("layout.grid");
                await Task.Delay(700);
                var grid = Find<Views.GridLayout>(root, "GridLayout");
                checks.Add(new { name = "grid-renders-images", passed = CountImages(grid) >= 2 });
                var gridHost = Find<Canvas>(grid, "ImageHost");
                checks.Add(new { name = "grid-cells-fit-current-viewport", passed = gridHost.Children.OfType<FrameworkElement>().All(cell =>
                    Canvas.GetLeft(cell) >= 0 && Canvas.GetTop(cell) >= 0 &&
                    Canvas.GetLeft(cell) + cell.ActualWidth <= gridHost.ActualWidth + 1 &&
                    Canvas.GetTop(cell) + cell.ActualHeight <= gridHost.ActualHeight + 1) });
                await CaptureAsync(root, "08-grid");
                await viewModel.ExecuteCommandAsync("layout.filmstrip");
                await Task.Delay(700);
                var filmstrip = Find<Views.FilmstripView>(root, "FilmstripView");
                checks.Add(new { name = "filmstrip-renders-focus-and-thumbnails", passed = filmstrip.Visibility == Visibility.Visible && CountImages(filmstrip) >= 2 });
                var focusSurface = Find<Border>(filmstrip, "FilmstripFocusSurface");
                var thumbnailSurface = Find<Border>(filmstrip, "FilmstripThumbnailsSurface");
                var focusBounds = Bounds(focusSurface, root);
                var thumbnailBounds = Bounds(thumbnailSurface, root);
                checks.Add(new { name = "filmstrip-thumbnails-below-image", passed = thumbnailBounds.Top >= focusBounds.Bottom && thumbnailBounds.Bottom <= root.ActualHeight });
                await CaptureAsync(root, "09-filmstrip");
                await viewModel.ExecuteCommandAsync("comparison.side-by-side");
                await Task.Delay(600);
                var comparison = Find<Views.ComparisonView>(root, "ComparisonView");
                checks.Add(new { name = "comparison-renders-two-images", passed = CountImages(comparison) >= 2 });
                await CaptureAsync(root, "10-comparison");
                await viewModel.ExecuteCommandAsync("comparison.overlay");
                await Task.Delay(500);
                viewModel.SetOverlayOpacity(40);
                await CaptureAsync(root, "11-overlay");
                await viewModel.ExecuteCommandAsync("layout.single");
                await Task.Delay(400);
                checks.Add(new { name = "single-view-exits-comparison-and-layout", passed = !viewModel.ComparisonState.IsActive && viewModel.LayoutState.CurrentLayout == LayoutMode.Single });

                // Save only a generated scratch file. Compare rendered pixels,
                // not just transform flags or the dimensions stored in the model.
                var transformFolder = Path.Combine(OutputDirectory, "fixtures", "transforms");
                Directory.CreateDirectory(transformFolder);
                var transformPath = Path.Combine(transformFolder, "transform.png");
                using (var source = CreateCalibrationImage(640, 480))
                    await source.SaveAsPngAsync(transformPath);
                await viewModel.OpenFileAsync(transformPath);
                await viewModel.ApplyTransformAsync(TransformOperation.RotateRight);
                await viewModel.ApplyTransformAsync(TransformOperation.FlipHorizontal);
                await Task.Delay(350);
                var imageCanvas = Find<Views.ImageCanvas>(root, "ImageCanvas");
                var previewPixels = await RenderPixelsAsync(imageCanvas);
                await CaptureAsync(imageCanvas, "17-transform-preview");
                await viewModel.SaveTransformsAsync();
                await Task.Delay(400);
                var savedPixels = await RenderPixelsAsync(imageCanvas);
                var displayAfterSave = viewModel.ViewerState.CurrentImage?.Metadata;
                await viewModel.OpenFileAsync(transformPath);
                await Task.Delay(400);
                var reopenedPixels = await RenderPixelsAsync(imageCanvas);
                var previewDifference = MeanPixelDifference(previewPixels, reopenedPixels);
                var savedDifference = MeanPixelDifference(savedPixels, reopenedPixels);
                checks.Add(new { name = "rotate-flip-preview-matches-saved-pixels", passed = previewDifference < 2, previewDifference });
                checks.Add(new { name = "save-refreshes-image-without-changing-appearance", passed = savedDifference < 2 &&
                    displayAfterSave?.PixelWidth == 480 && displayAfterSave.PixelHeight == 640, savedDifference });
                await CaptureAsync(imageCanvas, "18-transform-saved");
                await viewModel.OpenFileAsync(fixture);
                await Task.Delay(300);

                viewModel.ShowCommandPalette();
                await Task.Delay(300);
                await CaptureAsync(root, "12-command-palette");
                viewModel.HideCommandPalette();
                await Task.Delay(200);

                var gif = Environment.GetEnvironmentVariable("SPITFIRE_UI_TEST_GIF") ?? fixtures.Gif;
                if (!string.IsNullOrWhiteSpace(gif))
                {
                    await viewModel.OpenFileAsync(gif);
                    await Task.Delay(500);
                    var firstFrame = viewModel.AnimationState.CurrentFrame;
                    var minOpacity = 1d;
                    for (var frame = 0; frame < 15; frame++)
                    {
                        await Task.Delay(30);
                        minOpacity = Math.Min(minOpacity, displayedImage.Opacity);
                    }
                    checks.Add(new { name = "gif-plays-without-fading-frames", passed = viewModel.AnimationState.IsAnimated && viewModel.AnimationState.CurrentFrame != firstFrame && minOpacity > .99, minOpacity });
                    var frameControl = Find<Views.FrameControl>(chrome, "FrameControl");
                    Invoke(Find<Button>(frameControl, "PlayPauseButton"));
                    await Task.Delay(150);
                    var pausedFrame = viewModel.AnimationState.CurrentFrame;
                    Invoke(Find<Button>(frameControl, "NextButton"));
                    await Task.Delay(200);
                    checks.Add(new { name = "gif-pause-and-step-controls", passed = !viewModel.AnimationState.IsPlaying && viewModel.AnimationState.CurrentFrame == (pausedFrame + 1) % viewModel.AnimationState.TotalFrames });
                    chrome.ShowChrome();
                    await Task.Delay(100);
                    await CaptureAsync(root, "13-gif");
                }

                await viewModel.OpenFileAsync(fixture);
                await Task.Delay(300);
                await viewModel.ToggleSlideshowAsync();
                var slideshowIndex = viewModel.PlaylistState.CurrentIndex;
                await Task.Delay(viewModel.SlideshowState.Interval + TimeSpan.FromMilliseconds(700));
                checks.Add(new { name = "slideshow-advances-on-real-dispatcher-timer", passed = viewModel.PlaylistState.CurrentIndex != slideshowIndex });
                viewModel.ToggleSlideshowPause();
                foreach (var effect in Enum.GetValues<TransitionEffect>())
                {
                    await viewModel.SetSlideshowTransitionAsync(effect);
                    await viewModel.NavigateToPlaylistIndexAsync(initialIndex);
                    await Task.Delay(550);
                    await viewModel.NavigateNextAsync();
                    await Task.Delay(120);
                    var outgoing = Find<Canvas>(root, "OutgoingHost");
                    var incoming = Find<Canvas>(root, "ImageHost");
                    var incomingTransform = (CompositeTransform)incoming.RenderTransform;
                    var outgoingTransform = (CompositeTransform)outgoing.RenderTransform;
                    var inProgress = effect switch
                    {
                        TransitionEffect.None => outgoing.Opacity < .01 && incoming.Opacity > .99,
                        TransitionEffect.Fade => outgoing.Opacity > 0 && outgoing.Opacity < 1 && incoming.Opacity > 0 && incoming.Opacity < 1,
                        TransitionEffect.Slide => incomingTransform.TranslateX > 0,
                        TransitionEffect.Zoom => outgoingTransform.ScaleX < 1,
                        _ => false
                    };
                    checks.Add(new { name = $"slideshow-{effect}-rendered", passed = inProgress });
                    await Task.Delay(450);
                    checks.Add(new { name = $"slideshow-{effect}-settles", passed = outgoing.Opacity < .01 && incoming.Opacity > .99 && incomingTransform.TranslateX == 0 });
                }
                await viewModel.ToggleSlideshowAsync();

                var large = Environment.GetEnvironmentVariable("SPITFIRE_UI_TEST_LARGE") ?? fixtures.Large;
                if (!string.IsNullOrWhiteSpace(large))
                {
                    await viewModel.OpenFileAsync(large);
                    await Task.Delay(400);
                    viewModel.ActualSize();
                    viewModel.ViewerState.PanOffset = new PanOffset(100000, -100000);
                    await Task.Delay(200);
                    var transform = (CompositeTransform)displayedImage.RenderTransform;
                    var imageBounds = Bounds(displayedImage, root);
                    checks.Add(new { name = "large-image-actual-size-pan-is-bounded", passed =
                        Math.Abs(imageBounds.Width * root.XamlRoot.RasterizationScale - viewModel.ViewerState.CurrentImage!.Metadata.PixelWidth) < 1 &&
                        Math.Abs(imageBounds.Left) < 1 && Math.Abs(imageBounds.Bottom - root.ActualHeight) < 1 &&
                        transform.TranslateX > 0 && transform.TranslateY < 0 });
                    await CaptureAsync(root, "14-large-pan");
                    viewModel.FitToWindow();
                    await Task.Delay(100);
                }
                window.AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
                await Task.Delay(350);
                chrome.ShowChrome();
                await Task.Delay(100);
                await CaptureAsync(root, "14-narrow");
                var lastButton = Find<Button>(chrome, "SearchButton");
                var lastBounds = lastButton.TransformToVisual(root).TransformBounds(new Windows.Foundation.Rect(0, 0, lastButton.ActualWidth, lastButton.ActualHeight));
                checks.Add(new { name = "toolbar-controls-fit-narrow-window", passed = lastBounds.Right <= root.ActualWidth });
                await viewModel.ToggleThemeAsync();
                if (root.ActualTheme != ElementTheme.Light)
                {
                    await viewModel.ToggleThemeAsync();
                    await Task.Delay(100);
                }
                await Task.Delay(250);
                checks.Add(new { name = "light-theme-applied", passed = root.ActualTheme == ElementTheme.Light });
                await CaptureAsync(root, "15-light");
                viewModel.ToggleFullscreen();
                await Task.Delay(300);
                checks.Add(new { name = "fullscreen-presenter-applied", passed = window.AppWindow.Presenter.Kind == Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen });
                viewModel.ToggleFullscreen();

                // Reduced motion must cancel a running clock and its obsolete completion.
                var oldCallback = false;
                ViewerMotionStoryboard.AnimateOpacity(chromeRoot, ViewerMotionService.CreateDefaultPlan(ViewerMotionCategory.ChromeIdle, false), 0, () => oldCallback = true);
                ViewerMotionStoryboard.AnimateOpacity(chromeRoot, ViewerMotionService.CreateDefaultPlan(ViewerMotionCategory.ChromeReveal, true), 1);
                await Task.Delay(350);
                checks.Add(new { name = "reduced-motion-cancels-old-animation", passed = !oldCallback && chromeRoot.Opacity > .99 });
                var previousPath = viewModel.ViewerState.CurrentImage?.FilePath;
                var badPath = Path.Combine(OutputDirectory, "corrupt.png");
                await File.WriteAllTextAsync(badPath, "Not an image");
                await viewModel.OpenFileAsync(badPath);
                await Task.Delay(250);
                checks.Add(new { name = "corrupt-image-keeps-current-image-and-shows-error", passed =
                    viewModel.ViewerState.CurrentImage?.FilePath == previousPath && Find<InfoBar>(root, "ErrorInfoBar").IsOpen });
                await CaptureAsync(root, "16-error-recovery");
            }
        }
        catch (Exception ex)
        {
            checks.Add(new { name = "runner-error", passed = false, error = ex.ToString() });
        }
        finally
        {
            var results = JsonSerializer.Serialize(checks, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "results.json"), results);
            using var parsed = JsonDocument.Parse(results);
            Environment.ExitCode = parsed.RootElement.EnumerateArray().All(check => check.TryGetProperty("passed", out var passed) && passed.GetBoolean()) ? 0 : 1;
            window.Close();
        }
    }

    private static async Task<(string Image, string Gif, string Large)> CreateFixturesAsync()
    {
        var folder = Path.Combine(OutputDirectory, "fixtures", "browse");
        var media = Path.Combine(OutputDirectory, "fixtures", "media");
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(media);
        using var picture = CreateCalibrationImage(640, 480);
        await picture.SaveAsPngAsync(Path.Combine(folder, "alpha.png"));
        var image = Path.Combine(folder, "bravo.jpg");
        await picture.SaveAsJpegAsync(image);
        using (var portrait = CreateCalibrationImage(480, 720, 60))
            await portrait.SaveAsBmpAsync(Path.Combine(folder, "charlie.bmp"));
        using (var panorama = CreateCalibrationImage(960, 320, 120))
            await panorama.SaveAsGifAsync(Path.Combine(folder, "delta.gif"));
        using (var square = CreateCalibrationImage(480, 480, 180))
            await square.SaveAsTiffAsync(Path.Combine(folder, "echo.tif"));
        var large = Path.Combine(media, "large.png");
        using (var largeImage = CreateCalibrationImage(3840, 2160))
            await largeImage.SaveAsPngAsync(large);
        var gif = Path.Combine(media, "animated.gif");
        using var animation = CreateCalibrationImage(160, 120);
        animation.Metadata.GetGifMetadata().RepeatCount = 0;
        animation.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = 10;
        for (var index = 1; index < 20; index++)
        {
            using var frame = CreateCalibrationImage(160, 120, index * 10);
            frame.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = 10;
            animation.Frames.AddFrame(frame.Frames.RootFrame);
        }
        await animation.SaveAsGifAsync(gif, new GifEncoder());
        return (image, gif, large);
    }

    private static SixLabors.ImageSharp.Image<Rgba32> CreateCalibrationImage(int width, int height, int offset = 0)
    {
        var image = new SixLabors.ImageSharp.Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < width; x++)
                {
                    var grid = x % 80 < 2 || y % 80 < 2;
                    row[x] = grid ? new Rgba32(240, 240, 240) :
                        new Rgba32((byte)((x * 255 / width + offset) % 256), (byte)(y * 255 / height), 100);
                }
            }
        });
        return image;
    }

    private static Windows.Foundation.Rect Bounds(FrameworkElement element, FrameworkElement root) =>
        element.TransformToVisual(root).TransformBounds(new Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight));

    private static void Invoke(Button button)
    {
        if (!button.IsEnabled) throw new InvalidOperationException($"Disabled button: {button.Name}");
        var peer = new ButtonAutomationPeer(button);
        ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
    }

    private static int CountImages(DependencyObject parent)
    {
        var count = parent is Image { Source: not null } ? 1 : 0;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            count += CountImages(VisualTreeHelper.GetChild(parent, index));
        return count;
    }

    internal static T Find<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent is T element && element.Name == name)
            return element;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            try { return Find<T>(VisualTreeHelper.GetChild(parent, index), name); }
            catch (KeyNotFoundException) { }
        }
        throw new KeyNotFoundException(name);
    }

    private static async Task CaptureAsync(FrameworkElement root, string name)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(root);
        var pixels = await bitmap.GetPixelsAsync();
        var file = await StorageFile.GetFileFromPathAsync(CreateFile(name));
        using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels.ToArray());
        await encoder.FlushAsync();
    }

    private static async Task<byte[]> RenderPixelsAsync(FrameworkElement element)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(element);
        return (await bitmap.GetPixelsAsync()).ToArray();
    }

    private static double MeanPixelDifference(byte[] left, byte[] right)
    {
        if (left.Length != right.Length) return 255;
        long difference = 0;
        for (var index = 0; index < left.Length; index++)
            difference += Math.Abs(left[index] - right[index]);
        return difference / (double)left.Length;
    }

    private static string CreateFile(string name)
    {
        var path = Path.Combine(OutputDirectory, name + ".png");
        using (File.Create(path)) { }
        return path;
    }
}
