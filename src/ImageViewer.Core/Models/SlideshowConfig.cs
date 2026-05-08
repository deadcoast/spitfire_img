namespace ImageViewer.Core.Models;

public sealed record SlideshowConfig(
    TimeSpan Interval,
    TransitionEffect Transition,
    bool IsLooping = true)
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(3);

    public static IReadOnlyList<TimeSpan> AllowedIntervals { get; } =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60)
    ];

    public static SlideshowConfig Default { get; } = new(DefaultInterval, TransitionEffect.Fade);
}
