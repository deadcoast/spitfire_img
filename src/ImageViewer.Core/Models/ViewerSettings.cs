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

public sealed record ViewerSettings(
    ViewerTheme Theme,
    DisplayMode DefaultDisplayMode,
    int WindowWidth,
    int WindowHeight,
    int WindowX,
    int WindowY,
    int FadeDelayMs,
    int FadeDurationMs)
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
            FadeDurationMs = FadeDurationMs < MinimumFadeDurationMs ? MinimumFadeDurationMs : FadeDurationMs
        };
    }
}
