using ImageViewer.Core.Models;

namespace ImageViewer.Tests.Models;

public sealed class ViewerSettingsTests
{
    [Fact]
    public void Default_CreatesPhaseOneDefaults()
    {
        var settings = ViewerSettings.Default;

        Assert.Equal(ViewerTheme.System, settings.Theme);
        Assert.Equal(DisplayMode.FitToWindow, settings.DefaultDisplayMode);
        Assert.Equal(1200, settings.WindowWidth);
        Assert.Equal(800, settings.WindowHeight);
        Assert.Equal(2000, settings.FadeDelayMs);
        Assert.Equal(300, settings.FadeDurationMs);
        Assert.Equal(TransitionEffect.Fade, settings.SlideshowTransition);
        Assert.Equal(TimeSpan.FromSeconds(3), settings.SlideshowInterval);
        Assert.Equal(LayoutMode.Single, settings.PreferredLayout);
        Assert.Equal(3, settings.PreferredGridRows);
        Assert.Equal(3, settings.PreferredGridColumns);
        Assert.Equal(7, settings.PreferredFilmstripThumbnailCount);
        Assert.True(settings.ComparisonSyncZoom);
        Assert.True(settings.GifAutoPlay);
    }

    [Fact]
    public void Normalize_ClampsInvalidWindowAndFadeValues()
    {
        var settings = new ViewerSettings(
            ViewerTheme.Dark,
            DisplayMode.ActualSize,
            WindowWidth: -10,
            WindowHeight: 0,
            WindowX: 50,
            WindowY: 60,
            FadeDelayMs: 100,
            FadeDurationMs: 20,
            SlideshowInterval: TimeSpan.FromMilliseconds(100),
            PreferredLayout: (LayoutMode)999,
            PreferredGridRows: 0,
            PreferredGridColumns: 99,
            PreferredFilmstripThumbnailCount: 99,
            ComparisonSyncZoom: false,
            GifAutoPlay: false);

        var normalized = settings.Normalize();

        Assert.Equal(800, normalized.WindowWidth);
        Assert.Equal(600, normalized.WindowHeight);
        Assert.Equal(500, normalized.FadeDelayMs);
        Assert.Equal(100, normalized.FadeDurationMs);
        Assert.Equal(TimeSpan.FromSeconds(1), normalized.SlideshowInterval);
        Assert.Equal(LayoutMode.Single, normalized.PreferredLayout);
        Assert.Equal(2, normalized.PreferredGridRows);
        Assert.Equal(6, normalized.PreferredGridColumns);
        Assert.Equal(11, normalized.PreferredFilmstripThumbnailCount);
        Assert.False(normalized.ComparisonSyncZoom);
        Assert.False(normalized.GifAutoPlay);
    }
}
