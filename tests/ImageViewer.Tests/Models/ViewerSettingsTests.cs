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
        Assert.Equal(3, settings.SlideshowIntervalSeconds);
        Assert.Equal(TransitionEffect.Fade, settings.SlideshowTransition);
        Assert.Equal(LayoutMode.SingleImage, settings.PreferredLayout);
        Assert.Equal(3, settings.GridRows);
        Assert.Equal(3, settings.GridColumns);
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
            FadeDurationMs: 20);

        var normalized = settings.Normalize();

        Assert.Equal(800, normalized.WindowWidth);
        Assert.Equal(600, normalized.WindowHeight);
        Assert.Equal(500, normalized.FadeDelayMs);
        Assert.Equal(100, normalized.FadeDurationMs);
    }

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(7, 7)]
    [InlineData(90, 60)]
    public void Normalize_ClampsSlideshowIntervalToSupportedRange(int intervalSeconds, int expectedSeconds)
    {
        var settings = ViewerSettings.Default with { SlideshowIntervalSeconds = intervalSeconds };

        var normalized = settings.Normalize();

        Assert.Equal(expectedSeconds, normalized.SlideshowIntervalSeconds);
    }

    [Theory]
    [InlineData(-1, 0, 2, 2)]
    [InlineData(4, 5, 4, 5)]
    [InlineData(9, 12, 6, 6)]
    public void Normalize_ClampsGridDimensionsToSupportedRange(
        int rows,
        int columns,
        int expectedRows,
        int expectedColumns)
    {
        var settings = ViewerSettings.Default with
        {
            GridRows = rows,
            GridColumns = columns
        };

        var normalized = settings.Normalize();

        Assert.Equal(expectedRows, normalized.GridRows);
        Assert.Equal(expectedColumns, normalized.GridColumns);
    }
}
