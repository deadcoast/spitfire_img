using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class SlideshowService : ISlideshowService
{
    public bool IsTransitionPlaying { get; private set; }

    public bool CanNavigate => !IsTransitionPlaying;

    public Result<SlideshowTransitionPlan> CreateTransitionPlan(TransitionEffect effect)
    {
        if (!Enum.IsDefined(effect))
        {
            return Result<SlideshowTransitionPlan>.Failure(
                ViewerError.UnexpectedViewerError("CreateTransitionPlan", filePath: null));
        }

        return Result<SlideshowTransitionPlan>.Success(effect switch
        {
            TransitionEffect.None => new SlideshowTransitionPlan(
                effect,
                TimeSpan.Zero,
                TargetFrameRate: 0,
                BlocksNavigation: false,
                UsesHardwareAcceleration: false),
            TransitionEffect.Fade => CreateAnimatedPlan(effect, TimeSpan.FromMilliseconds(300)),
            TransitionEffect.Slide => CreateAnimatedPlan(effect, TimeSpan.FromMilliseconds(400)),
            TransitionEffect.Zoom => CreateAnimatedPlan(effect, TimeSpan.FromMilliseconds(350)),
            _ => throw new InvalidOperationException("Unhandled transition effect.")
        });
    }

    public Result BeginTransition(TransitionEffect effect)
    {
        var planResult = CreateTransitionPlan(effect);
        if (planResult.IsFailure)
        {
            IsTransitionPlaying = false;
            return Result.Success();
        }

        IsTransitionPlaying = planResult.Value.BlocksNavigation;
        return Result.Success();
    }

    public Result CompleteTransition()
    {
        IsTransitionPlaying = false;
        return Result.Success();
    }

    private static SlideshowTransitionPlan CreateAnimatedPlan(TransitionEffect effect, TimeSpan duration)
    {
        return new SlideshowTransitionPlan(
            effect,
            duration,
            TargetFrameRate: 60,
            BlocksNavigation: true,
            UsesHardwareAcceleration: true);
    }
}
