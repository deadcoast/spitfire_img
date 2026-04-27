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
}
