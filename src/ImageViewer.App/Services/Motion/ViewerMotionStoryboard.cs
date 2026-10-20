using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace ImageViewer.App.Services.Motion;

public static class ViewerMotionStoryboard
{
    public static void AnimateOpacity(UIElement target, ViewerMotionPlan motionPlan, double toOpacity, Action? completed = null)
    {
        if (motionPlan.Duration <= TimeSpan.Zero)
        {
            target.Opacity = toOpacity;
            completed?.Invoke();
            return;
        }

        var storyboard = new Storyboard();
        storyboard.Children.Add(CreateDoubleAnimation(target, "Opacity", toOpacity, motionPlan));
        storyboard.Completed += (_, _) => completed?.Invoke();
        storyboard.Begin();
    }

    public static void AnimateOverlay(
        UIElement target,
        CompositeTransform transform,
        ViewerMotionPlan motionPlan,
        bool isEntering,
        Action? completed = null)
    {
        var targetOpacity = isEntering ? 1 : 0;
        var targetOffset = isEntering || !motionPlan.AllowsTranslation ? 0 : -motionPlan.OffsetPixels;
        var targetScale = isEntering || !motionPlan.AllowsScale ? motionPlan.ScaleTo : motionPlan.ScaleFrom;

        if (isEntering)
        {
            target.Opacity = 0;
            transform.TranslateY = motionPlan.AllowsTranslation ? -motionPlan.OffsetPixels : 0;
            transform.ScaleX = motionPlan.AllowsScale ? motionPlan.ScaleFrom : motionPlan.ScaleTo;
            transform.ScaleY = motionPlan.AllowsScale ? motionPlan.ScaleFrom : motionPlan.ScaleTo;
        }

        if (motionPlan.Duration <= TimeSpan.Zero)
        {
            target.Opacity = targetOpacity;
            transform.TranslateY = 0;
            transform.ScaleX = motionPlan.ScaleTo;
            transform.ScaleY = motionPlan.ScaleTo;
            completed?.Invoke();
            return;
        }

        var storyboard = new Storyboard();
        storyboard.Children.Add(CreateDoubleAnimation(target, "Opacity", targetOpacity, motionPlan));
        storyboard.Children.Add(CreateDoubleAnimation(transform, "TranslateY", targetOffset, motionPlan));
        storyboard.Children.Add(CreateDoubleAnimation(transform, "ScaleX", targetScale, motionPlan));
        storyboard.Children.Add(CreateDoubleAnimation(transform, "ScaleY", targetScale, motionPlan));
        storyboard.Completed += (_, _) => completed?.Invoke();
        storyboard.Begin();
    }

    public static void AnimateHorizontalPanel(
        UIElement target,
        TranslateTransform transform,
        ViewerMotionPlan motionPlan,
        bool isEntering,
        double hiddenOffset,
        Action? completed = null)
    {
        var targetOpacity = isEntering ? 1 : 0;
        var targetOffset = isEntering || !motionPlan.AllowsTranslation ? 0 : hiddenOffset;

        if (isEntering)
        {
            target.Opacity = 0;
            transform.X = motionPlan.AllowsTranslation ? hiddenOffset : 0;
        }

        if (motionPlan.Duration <= TimeSpan.Zero)
        {
            target.Opacity = targetOpacity;
            transform.X = 0;
            completed?.Invoke();
            return;
        }

        var storyboard = new Storyboard();
        storyboard.Children.Add(CreateDoubleAnimation(target, "Opacity", targetOpacity, motionPlan));
        storyboard.Children.Add(CreateDoubleAnimation(transform, "X", targetOffset, motionPlan));
        storyboard.Completed += (_, _) => completed?.Invoke();
        storyboard.Begin();
    }

    public static void AnimateVerticalTranslation(
        TranslateTransform transform,
        ViewerMotionPlan motionPlan,
        double fromOffset,
        double toOffset)
    {
        if (!motionPlan.AllowsTranslation || motionPlan.Duration <= TimeSpan.Zero)
        {
            transform.Y = toOffset;
            return;
        }

        transform.Y = fromOffset;
        var storyboard = new Storyboard();
        storyboard.Children.Add(CreateDoubleAnimation(transform, "Y", toOffset, motionPlan));
        storyboard.Begin();
    }

    private static DoubleAnimation CreateDoubleAnimation(
        DependencyObject target,
        string propertyPath,
        double to,
        ViewerMotionPlan motionPlan)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = motionPlan.Duration,
            EnableDependentAnimation = !motionPlan.IsReducedMotion
        };

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, propertyPath);
        return animation;
    }
}

