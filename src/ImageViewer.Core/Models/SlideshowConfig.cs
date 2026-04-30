namespace ImageViewer.Core.Models;

public sealed record SlideshowConfig(
    TimeSpan Interval,
    TransitionEffect Transition,
    bool IsLooping)
{
    public static IReadOnlyList<int> SupportedIntervalSeconds { get; } = [1, 2, 3, 5, 10, 15, 30, 60];

    public static SlideshowConfig Default { get; } = new(
        TimeSpan.FromSeconds(3),
        TransitionEffect.Fade,
        IsLooping: true);

    public static int NormalizeIntervalSeconds(int intervalSeconds)
    {
        return Math.Clamp(intervalSeconds, SupportedIntervalSeconds[0], SupportedIntervalSeconds[^1]);
    }

    public static int StepIntervalSeconds(int currentIntervalSeconds, int direction)
    {
        var normalized = NormalizeIntervalSeconds(currentIntervalSeconds);
        if (direction > 0)
        {
            return SupportedIntervalSeconds.FirstOrDefault(seconds => seconds > normalized, SupportedIntervalSeconds[^1]);
        }

        if (direction < 0)
        {
            return SupportedIntervalSeconds.LastOrDefault(seconds => seconds < normalized, SupportedIntervalSeconds[0]);
        }

        return normalized;
    }

    public static SlideshowConfig FromSettings(ViewerSettings settings)
    {
        var normalized = settings.Normalize();
        return Default with
        {
            Interval = TimeSpan.FromSeconds(normalized.SlideshowIntervalSeconds),
            Transition = normalized.SlideshowTransition
        };
    }
}
