using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace ImageViewer.App.Services.Motion;

public static class ViewerMotionStoryboard
{
    private static readonly ConditionalWeakTable<DependencyObject, RunningAnimation> Running = new();

    public static void SetOpacity(UIElement target, double opacity)
    {
        Cancel(target);
        target.Opacity = opacity;
    }

    public static void AnimateOpacity(UIElement target, ViewerMotionPlan plan, double toOpacity, Action? completed = null)
    {
        Cancel(target);
        Start(target, plan, [Opacity(target, toOpacity)], completed);
    }

    public static void AnimateOverlay(UIElement target, CompositeTransform transform, ViewerMotionPlan plan, bool isEntering, Action? completed = null)
    {
        var interrupted = Cancel(target);
        if (isEntering && !interrupted)
        {
            target.Opacity = 0;
            transform.TranslateY = plan.AllowsTranslation ? -plan.OffsetPixels : 0;
            transform.ScaleX = transform.ScaleY = plan.AllowsScale ? plan.ScaleFrom : plan.ScaleTo;
        }
        var offset = isEntering || !plan.AllowsTranslation ? 0 : -plan.OffsetPixels;
        var scale = isEntering || !plan.AllowsScale ? plan.ScaleTo : plan.ScaleFrom;
        Start(target, plan,
        [
            Opacity(target, isEntering ? 1 : 0),
            Value(transform, "TranslateY", () => transform.TranslateY, value => transform.TranslateY = value, offset),
            Value(transform, "ScaleX", () => transform.ScaleX, value => transform.ScaleX = value, scale),
            Value(transform, "ScaleY", () => transform.ScaleY, value => transform.ScaleY = value, scale)
        ], completed);
    }

    public static void AnimateHorizontalPanel(UIElement target, TranslateTransform transform, ViewerMotionPlan plan, bool isEntering, double hiddenOffset, Action? completed = null)
    {
        var interrupted = Cancel(target);
        if (isEntering && !interrupted)
        {
            target.Opacity = 0;
            transform.X = plan.AllowsTranslation ? hiddenOffset : 0;
        }
        Start(target, plan,
        [
            Opacity(target, isEntering ? 1 : 0),
            Value(transform, "X", () => transform.X, value => transform.X = value, isEntering || !plan.AllowsTranslation ? 0 : hiddenOffset)
        ], completed);
    }

    public static void AnimateVerticalTranslation(TranslateTransform transform, ViewerMotionPlan plan, double fromOffset, double toOffset)
    {
        var interrupted = Cancel(transform);
        if (!interrupted) transform.Y = plan.AllowsTranslation ? fromOffset : toOffset;
        Start(transform, plan, [Value(transform, "Y", () => transform.Y, value => transform.Y = value, toOffset)]);
    }

    private static AnimatedValue Opacity(UIElement target, double to) => Value(target, "Opacity", () => target.Opacity, value => target.Opacity = value, to);
    private static AnimatedValue Value(DependencyObject target, string path, Func<double> read, Action<double> write, double to) => new(target, path, read, write, to);

    private static bool Cancel(DependencyObject owner)
    {
        if (!Running.TryGetValue(owner, out var animation)) return false;
        var current = animation.Values.Select(value => value.Read()).ToArray();
        Running.Remove(owner); // Suppress old completion before stopping its clock.
        animation.Storyboard.Stop();
        for (var i = 0; i < current.Length; i++) animation.Values[i].Write(current[i]);
        return true;
    }

    private static void Start(DependencyObject owner, ViewerMotionPlan plan, AnimatedValue[] values, Action? completed = null)
    {
        if (plan.Duration <= TimeSpan.Zero)
        {
            foreach (var value in values) value.Write(value.To);
            completed?.Invoke();
            return;
        }
        var storyboard = new Storyboard();
        foreach (var value in values)
        {
            var animation = new DoubleAnimation
            {
                From = value.Read(),
                To = value.To,
                Duration = plan.Duration,
                EasingFunction = plan.Easing switch
                {
                    ViewerMotionEasing.EaseIn => new CubicEase { EasingMode = EasingMode.EaseIn },
                    ViewerMotionEasing.EaseOut => new CubicEase { EasingMode = EasingMode.EaseOut },
                    ViewerMotionEasing.EaseInOut => new CubicEase { EasingMode = EasingMode.EaseInOut },
                    _ => null
                }
            };
            Storyboard.SetTarget(animation, value.Target);
            Storyboard.SetTargetProperty(animation, value.Path);
            storyboard.Children.Add(animation);
        }
        var running = new RunningAnimation(storyboard, values);
        Running.Add(owner, running);
        storyboard.Completed += (_, _) =>
        {
            if (!Running.TryGetValue(owner, out var current) || !ReferenceEquals(current, running)) return;
            Running.Remove(owner);
            storyboard.Stop();
            foreach (var value in values) value.Write(value.To);
            completed?.Invoke();
        };
        storyboard.Begin();
    }

    private sealed record AnimatedValue(DependencyObject Target, string Path, Func<double> Read, Action<double> Write, double To);
    private sealed record RunningAnimation(Storyboard Storyboard, AnimatedValue[] Values);
}
