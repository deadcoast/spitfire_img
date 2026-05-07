using ImageViewer.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace ImageViewer.App.Services;

public sealed class SlideshowTransitionRenderer
{
    public Task<Result> ApplyTransitionAsync(
        Image currentImage,
        Image nextImage,
        CompositeTransform currentTransform,
        CompositeTransform nextTransform,
        SlideshowTransitionPlan plan)
    {
        return plan.Effect switch
        {
            TransitionEffect.None => ApplyInstantTransitionAsync(currentImage, nextImage, currentTransform, nextTransform),
            TransitionEffect.Fade => RunStoryboardTransitionAsync(
                currentImage,
                nextImage,
                currentTransform,
                nextTransform,
                CreateFadeStoryboard(currentImage, nextImage, plan.Duration)),
            TransitionEffect.Slide => RunStoryboardTransitionAsync(
                currentImage,
                nextImage,
                currentTransform,
                nextTransform,
                CreateSlideStoryboard(currentImage, nextImage, nextTransform, plan.Duration)),
            TransitionEffect.Zoom => RunStoryboardTransitionAsync(
                currentImage,
                nextImage,
                currentTransform,
                nextTransform,
                CreateZoomStoryboard(currentImage, nextImage, currentTransform, plan.Duration)),
            _ => Task.FromResult(Result.Failure(
                ViewerError.UnexpectedViewerError("ApplyTransition", filePath: null)))
        };
    }

    private static Task<Result> ApplyInstantTransitionAsync(
        Image currentImage,
        Image nextImage,
        CompositeTransform currentTransform,
        CompositeTransform nextTransform)
    {
        currentImage.Source = nextImage.Source;
        ResetVisualState(currentImage, nextImage, currentTransform, nextTransform);
        return Task.FromResult(Result.Success());
    }

    private static async Task<Result> RunStoryboardTransitionAsync(
        Image currentImage,
        Image nextImage,
        CompositeTransform currentTransform,
        CompositeTransform nextTransform,
        Storyboard storyboard)
    {
        try
        {
            await BeginStoryboardAsync(storyboard).ConfigureAwait(true);
            currentImage.Source = nextImage.Source;
            ResetVisualState(currentImage, nextImage, currentTransform, nextTransform);
            return Result.Success();
        }
        catch (Exception ex)
        {
            ResetVisualState(currentImage, nextImage, currentTransform, nextTransform);
            return Result.Failure(ViewerError.UnexpectedViewerError("ApplyTransition", filePath: null, ex));
        }
    }

    private static Storyboard CreateFadeStoryboard(Image currentImage, Image nextImage, TimeSpan duration)
    {
        nextImage.Opacity = 0;

        var storyboard = new Storyboard();
        storyboard.Children.Add(CreateDoubleAnimation(currentImage, "Opacity", 1, 0, duration));
        storyboard.Children.Add(CreateDoubleAnimation(nextImage, "Opacity", 0, 1, duration));
        return storyboard;
    }

    private static Storyboard CreateSlideStoryboard(
        Image currentImage,
        Image nextImage,
        CompositeTransform nextTransform,
        TimeSpan duration)
    {
        var slideDistance = currentImage.ActualWidth > 0 ? currentImage.ActualWidth : 320;
        nextImage.Opacity = 1;
        nextTransform.TranslateX = slideDistance;

        var storyboard = new Storyboard();
        storyboard.Children.Add(CreateDoubleAnimation(currentImage, "Opacity", 1, 0, duration));
        storyboard.Children.Add(CreateDoubleAnimation(nextTransform, "TranslateX", slideDistance, 0, duration));
        return storyboard;
    }

    private static Storyboard CreateZoomStoryboard(
        Image currentImage,
        Image nextImage,
        CompositeTransform currentTransform,
        TimeSpan duration)
    {
        currentTransform.ScaleX = 1;
        currentTransform.ScaleY = 1;
        nextImage.Opacity = 0;

        var storyboard = new Storyboard();
        storyboard.Children.Add(CreateDoubleAnimation(currentImage, "Opacity", 1, 0, duration));
        storyboard.Children.Add(CreateDoubleAnimation(nextImage, "Opacity", 0, 1, duration));
        storyboard.Children.Add(CreateDoubleAnimation(currentTransform, "ScaleX", 1, 0.92, duration));
        storyboard.Children.Add(CreateDoubleAnimation(currentTransform, "ScaleY", 1, 0.92, duration));
        return storyboard;
    }

    private static DoubleAnimation CreateDoubleAnimation(
        DependencyObject target,
        string targetProperty,
        double from,
        double to,
        TimeSpan duration)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = duration,
            EnableDependentAnimation = true
        };

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, targetProperty);
        return animation;
    }

    private static Task BeginStoryboardAsync(Storyboard storyboard)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        storyboard.Completed += OnCompleted;
        storyboard.Begin();
        return completion.Task;

        void OnCompleted(object? sender, object e)
        {
            storyboard.Completed -= OnCompleted;
            completion.TrySetResult();
        }
    }

    private static void ResetVisualState(
        Image currentImage,
        Image nextImage,
        CompositeTransform currentTransform,
        CompositeTransform nextTransform)
    {
        currentImage.Opacity = 1;
        nextImage.Opacity = 0;
        currentTransform.ScaleX = 1;
        currentTransform.ScaleY = 1;
        currentTransform.TranslateX = 0;
        currentTransform.TranslateY = 0;
        nextTransform.ScaleX = 1;
        nextTransform.ScaleY = 1;
        nextTransform.TranslateX = 0;
        nextTransform.TranslateY = 0;
    }
}
