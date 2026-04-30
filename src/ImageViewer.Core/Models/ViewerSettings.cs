namespace ImageViewer.Core.Models;

public enum ViewerTheme
{
    System,
    Light,
    Dark
}

public enum DisplayMode
{
    FitToWindow,
    ActualSize,
    Custom
}

public enum ThumbnailPosition
{
    Top,
    Bottom
}

public sealed record ViewerSettings(
    ViewerTheme Theme,
    DisplayMode DefaultDisplayMode,
    int WindowWidth,
    int WindowHeight,
    int WindowX,
    int WindowY,
    int FadeDelayMs,
    int FadeDurationMs,
    bool ThumbnailStripVisible = false,
    ThumbnailPosition ThumbnailStripPosition = ThumbnailPosition.Bottom,
    int DefaultJpegQuality = 90,
    bool PreserveExifOnExport = true,
    int SlideshowIntervalSeconds = 3,
    TransitionEffect SlideshowTransition = TransitionEffect.Fade)
{
    public const int DefaultWindowWidth = 1200;
    public const int DefaultWindowHeight = 800;
    public const int MinimumWindowWidth = 800;
    public const int MinimumWindowHeight = 600;
    public const int DefaultFadeDelayMs = 2000;
    public const int DefaultFadeDurationMs = 300;
    public const int MinimumFadeDelayMs = 500;
    public const int MinimumFadeDurationMs = 100;

    public static ViewerSettings Default { get; } = new(
        ViewerTheme.System,
        DisplayMode.FitToWindow,
        DefaultWindowWidth,
        DefaultWindowHeight,
        WindowX: -1,
        WindowY: -1,
        DefaultFadeDelayMs,
        DefaultFadeDurationMs);

    public ViewerSettings Normalize()
    {
        return this with
        {
            WindowWidth = WindowWidth < MinimumWindowWidth ? MinimumWindowWidth : WindowWidth,
            WindowHeight = WindowHeight < MinimumWindowHeight ? MinimumWindowHeight : WindowHeight,
            FadeDelayMs = FadeDelayMs < MinimumFadeDelayMs ? MinimumFadeDelayMs : FadeDelayMs,
            FadeDurationMs = FadeDurationMs < MinimumFadeDurationMs ? MinimumFadeDurationMs : FadeDurationMs,
            DefaultJpegQuality = Math.Clamp(DefaultJpegQuality, 1, 100),
            SlideshowIntervalSeconds = SlideshowConfig.NormalizeIntervalSeconds(SlideshowIntervalSeconds)
        };
    }
}
