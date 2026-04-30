using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed record SlideshowTransitionSpec(
    TransitionEffect Effect,
    TimeSpan Duration,
    bool BlocksNavigation,
    int TargetFrameRate);

public sealed class SlideshowTransitionService
{
    public SlideshowTransitionSpec GetTransition(TransitionEffect effect)
    {
        var duration = effect switch
        {
            TransitionEffect.None => TimeSpan.Zero,
            TransitionEffect.Fade => TimeSpan.FromMilliseconds(300),
            TransitionEffect.Slide => TimeSpan.FromMilliseconds(400),
            TransitionEffect.Zoom => TimeSpan.FromMilliseconds(350),
            _ => TimeSpan.FromMilliseconds(300)
        };

        return new SlideshowTransitionSpec(effect, duration, BlocksNavigation: true, TargetFrameRate: 60);
    }
}
