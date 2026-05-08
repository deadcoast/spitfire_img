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
    TimeSpan SlideshowInterval = default,
    TransitionEffect SlideshowTransition = TransitionEffect.Fade,
    LayoutMode PreferredLayout = LayoutMode.Single,
    int PreferredGridRows = LayoutConfig.DefaultGridRows,
    int PreferredGridColumns = LayoutConfig.DefaultGridColumns,
    int PreferredFilmstripThumbnailCount = LayoutConfig.DefaultFilmstripThumbnailCount,
    bool ComparisonSyncZoom = true,
    bool GifAutoPlay = true)
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
        DefaultFadeDurationMs,
        SlideshowInterval: SlideshowConfig.DefaultInterval);

    public ViewerSettings Normalize()
    {
        return this with
        {
            WindowWidth = WindowWidth < MinimumWindowWidth ? MinimumWindowWidth : WindowWidth,
            WindowHeight = WindowHeight < MinimumWindowHeight ? MinimumWindowHeight : WindowHeight,
            FadeDelayMs = FadeDelayMs < MinimumFadeDelayMs ? MinimumFadeDelayMs : FadeDelayMs,
            FadeDurationMs = FadeDurationMs < MinimumFadeDurationMs ? MinimumFadeDurationMs : FadeDurationMs,
            DefaultJpegQuality = Math.Clamp(DefaultJpegQuality, 1, 100),
            SlideshowInterval = NormalizeSlideshowInterval(SlideshowInterval),
            SlideshowTransition = Enum.IsDefined(SlideshowTransition)
                ? SlideshowTransition
                : TransitionEffect.Fade,
            PreferredLayout = Enum.IsDefined(PreferredLayout)
                ? PreferredLayout
                : LayoutMode.Single,
            PreferredGridRows = Math.Clamp(
                PreferredGridRows,
                LayoutConfig.MinimumGridDensity,
                LayoutConfig.MaximumGridDensity),
            PreferredGridColumns = Math.Clamp(
                PreferredGridColumns,
                LayoutConfig.MinimumGridDensity,
                LayoutConfig.MaximumGridDensity),
            PreferredFilmstripThumbnailCount = Math.Clamp(
                PreferredFilmstripThumbnailCount,
                LayoutConfig.MinimumFilmstripThumbnailCount,
                LayoutConfig.MaximumFilmstripThumbnailCount)
        };
    }

    private static TimeSpan NormalizeSlideshowInterval(TimeSpan interval)
    {
        if (interval == default)
        {
            return SlideshowConfig.DefaultInterval;
        }

        return TimeSpan.FromSeconds(Math.Clamp(interval.TotalSeconds, 1, 60));
    }
}
