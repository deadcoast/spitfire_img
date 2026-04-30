using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class SlideshowStateTests
{
    [Fact]
    public void StartPauseResumeStop_UpdatesPlaybackState()
    {
        var state = new SlideshowState();
        var config = new SlideshowConfig(TimeSpan.FromSeconds(3), TransitionEffect.Fade, IsLooping: true);

        state.Start(currentImageIndex: 2, config);
        state.Pause();
        state.Resume();
        state.Stop();

        Assert.False(state.IsActive);
        Assert.False(state.IsPaused);
        Assert.Equal(TimeSpan.FromSeconds(3), state.Interval);
        Assert.Equal(TransitionEffect.Fade, state.Transition);
        Assert.Equal(2, state.CurrentImageIndex);
        Assert.True(state.IsLooping);
    }

    [Fact]
    public void ResetTimer_WhenActive_RecordsReset()
    {
        var state = new SlideshowState();
        state.Start(0, SlideshowConfig.Default);

        state.ResetTimer();

        Assert.Equal(1, state.TimerResetCount);
    }
}
