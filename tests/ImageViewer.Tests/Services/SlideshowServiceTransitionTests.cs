using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class SlideshowServiceTransitionTests
{
    [Theory]
    [InlineData(TransitionEffect.None, 0)]
    [InlineData(TransitionEffect.Fade, 300)]
    [InlineData(TransitionEffect.Slide, 400)]
    [InlineData(TransitionEffect.Zoom, 350)]
    public void CreateTransitionPlan_ReturnsDocumentedDuration(TransitionEffect effect, int expectedMilliseconds)
    {
        var service = new SlideshowService();

        var result = service.CreateTransitionPlan(effect);

        Assert.True(result.IsSuccess);
        Assert.Equal(effect, result.Value.Effect);
        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), result.Value.Duration);
    }

    [Theory]
    [InlineData(TransitionEffect.Fade)]
    [InlineData(TransitionEffect.Slide)]
    [InlineData(TransitionEffect.Zoom)]
    public void CreateTransitionPlan_AnimatedTransitionsUseSixtyFramesPerSecond(TransitionEffect effect)
    {
        var service = new SlideshowService();

        var result = service.CreateTransitionPlan(effect);

        Assert.True(result.IsSuccess);
        Assert.Equal(60, result.Value.TargetFrameRate);
        Assert.True(result.Value.UsesHardwareAcceleration);
    }

    [Theory]
    [InlineData(TransitionEffect.Fade)]
    [InlineData(TransitionEffect.Slide)]
    [InlineData(TransitionEffect.Zoom)]
    public void BeginTransition_BlocksNavigationUntilTransitionCompletes(TransitionEffect effect)
    {
        var service = new SlideshowService();

        var beginResult = service.BeginTransition(effect);

        Assert.True(beginResult.IsSuccess);
        Assert.True(service.IsTransitionPlaying);
        Assert.False(service.CanNavigate);

        var completeResult = service.CompleteTransition();

        Assert.True(completeResult.IsSuccess);
        Assert.False(service.IsTransitionPlaying);
        Assert.True(service.CanNavigate);
    }

    [Fact]
    public void BeginTransition_NoneDoesNotBlockNavigation()
    {
        var service = new SlideshowService();

        var result = service.BeginTransition(TransitionEffect.None);

        Assert.True(result.IsSuccess);
        Assert.False(service.IsTransitionPlaying);
        Assert.True(service.CanNavigate);
    }

    [Fact]
    public void BeginTransition_WhenTransitionCannotBeCreated_FallsBackToInstantTransition()
    {
        var service = new SlideshowService();

        var result = service.BeginTransition((TransitionEffect)999);

        Assert.True(result.IsSuccess);
        Assert.False(service.IsTransitionPlaying);
        Assert.True(service.CanNavigate);
    }
}
