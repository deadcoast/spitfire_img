namespace ImageViewer.App.Services.Motion;

public sealed record ViewerMotionPlan(
    ViewerMotionCategory Category,
    TimeSpan Duration,
    ViewerMotionEasing Easing,
    double OffsetPixels,
    double ScaleFrom,
    double ScaleTo,
    bool AllowsOpacity,
    bool AllowsTranslation,
    bool AllowsScale,
    bool AllowsLooping,
    bool IsReducedMotion);

