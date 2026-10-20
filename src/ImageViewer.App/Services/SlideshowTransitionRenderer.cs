using ImageViewer.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace ImageViewer.App.Services;

/// <summary>Owns the two presentation layers; never changes the image viewport transform.</summary>
public sealed class SlideshowTransitionRenderer
{
    private Storyboard? _storyboard;
    private Action? _finish;

    public void Cancel()
    {
        _storyboard?.Stop();
        _storyboard = null;
        var finish = _finish;
        _finish = null;
        finish?.Invoke();
    }

    public Task ApplyTransitionAsync(FrameworkElement outgoing, FrameworkElement incoming,
        CompositeTransform outgoingTransform, CompositeTransform incomingTransform, SlideshowTransitionPlan plan)
    {
        Cancel();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Finish()
        {
            outgoing.Opacity = 0;
            incoming.Opacity = 1;
            outgoingTransform.ScaleX = outgoingTransform.ScaleY = 1;
            incomingTransform.ScaleX = incomingTransform.ScaleY = 1;
            outgoingTransform.TranslateX = incomingTransform.TranslateX = 0;
            completion.TrySetResult();
        }
        if (plan.Duration <= TimeSpan.Zero || plan.Effect == TransitionEffect.None)
        {
            Finish();
            return completion.Task;
        }
        outgoing.Opacity = 1;
        incoming.Opacity = plan.Effect == TransitionEffect.Slide ? 1 : 0;
        var storyboard = new Storyboard();
        Add(storyboard, outgoing, "Opacity", 1, 0, plan.Duration);
        if (plan.Effect != TransitionEffect.Slide)
            Add(storyboard, incoming, "Opacity", 0, 1, plan.Duration);
        if (plan.Effect == TransitionEffect.Slide)
            Add(storyboard, incomingTransform, "TranslateX", incoming.ActualWidth, 0, plan.Duration);
        if (plan.Effect == TransitionEffect.Zoom)
        {
            Add(storyboard, outgoingTransform, "ScaleX", 1, .92, plan.Duration);
            Add(storyboard, outgoingTransform, "ScaleY", 1, .92, plan.Duration);
        }
        _finish = Finish;
        _storyboard = storyboard;
        storyboard.Completed += (_, _) =>
        {
            if (!ReferenceEquals(_storyboard, storyboard)) return;
            Cancel();
        };
        storyboard.Begin();
        return completion.Task;
    }

    private static void Add(Storyboard storyboard, DependencyObject target, string property,
        double from, double to, TimeSpan duration)
    {
        var animation = new DoubleAnimation { From = from, To = to, Duration = duration };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        storyboard.Children.Add(animation);
    }
}
