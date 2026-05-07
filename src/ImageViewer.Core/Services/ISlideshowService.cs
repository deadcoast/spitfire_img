using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface ISlideshowService
{
    bool IsTransitionPlaying { get; }

    bool CanNavigate { get; }

    Result<SlideshowTransitionPlan> CreateTransitionPlan(TransitionEffect effect);

    Result BeginTransition(TransitionEffect effect);

    Result CompleteTransition();
}
