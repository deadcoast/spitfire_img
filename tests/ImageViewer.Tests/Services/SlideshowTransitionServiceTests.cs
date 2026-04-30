using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class SlideshowTransitionServiceTests
{
    [Theory]
    [InlineData(TransitionEffect.None, 0)]
    [InlineData(TransitionEffect.Fade, 300)]
    [InlineData(TransitionEffect.Slide, 400)]
    [InlineData(TransitionEffect.Zoom, 350)]
    public void GetTransition_ReturnsRequiredDurationAndNavigationBlocking(
        TransitionEffect effect,
        int expectedDurationMs)
    {
        var service = new SlideshowTransitionService();

        var transition = service.GetTransition(effect);

        Assert.Equal(effect, transition.Effect);
        Assert.Equal(TimeSpan.FromMilliseconds(expectedDurationMs), transition.Duration);
        Assert.True(transition.BlocksNavigation);
        Assert.Equal(60, transition.TargetFrameRate);
    }
}
