namespace ImageViewer.Core.Models;

public sealed record SlideshowTransitionPlan(
    TransitionEffect Effect,
    TimeSpan Duration,
    int TargetFrameRate,
    bool BlocksNavigation,
    bool UsesHardwareAcceleration);
