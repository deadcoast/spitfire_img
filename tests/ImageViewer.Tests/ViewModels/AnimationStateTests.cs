using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class AnimationStateTests
{
    [Fact]
    public void LoadAnimation_MultipleFrameGifSetsAnimatedPlaybackState()
    {
        var frames = new[]
        {
            CreateFrame(100),
            CreateFrame(250)
        };
        var gif = new AnimatedGif("animated.gif", frames);
        var state = new AnimationState();

        state.LoadAnimation(gif);

        Assert.True(state.IsAnimated);
        Assert.True(state.IsPlaying);
        Assert.Equal(0, state.CurrentFrame);
        Assert.Equal(2, state.TotalFrames);
        Assert.Equal([100, 250], state.FrameDelays);
        Assert.Equal(frames, state.Frames);
    }

    [Fact]
    public void LoadAnimation_SingleFrameGifKeepsPlaybackStopped()
    {
        var state = new AnimationState();

        state.LoadAnimation(new AnimatedGif("static.gif", [CreateFrame(0)]));

        Assert.False(state.IsAnimated);
        Assert.False(state.IsPlaying);
        Assert.Equal(1, state.TotalFrames);
    }

    [Fact]
    public void NextFrame_LoopsBackToFirstFrame()
    {
        var state = new AnimationState();
        state.LoadAnimation(new AnimatedGif("animated.gif", [CreateFrame(100), CreateFrame(200)]));
        state.SeekToFrame(1);

        state.NextFrame();

        Assert.Equal(0, state.CurrentFrame);
    }

    [Fact]
    public void PreviousFrame_LoopsBackToLastFrame()
    {
        var state = new AnimationState();
        state.LoadAnimation(new AnimatedGif("animated.gif", [CreateFrame(100), CreateFrame(200)]));

        state.PreviousFrame();

        Assert.Equal(1, state.CurrentFrame);
    }

    [Fact]
    public void TogglePlayback_PausesAndResumesAnimatedGif()
    {
        var state = new AnimationState();
        state.LoadAnimation(new AnimatedGif("animated.gif", [CreateFrame(100), CreateFrame(200)]));

        state.TogglePlayback();

        Assert.False(state.IsPlaying);

        state.TogglePlayback();

        Assert.True(state.IsPlaying);
    }

    [Fact]
    public void FrameDisplayText_ShowsCurrentFrameAndTotalForAnimatedGif()
    {
        var state = new AnimationState();
        state.LoadAnimation(new AnimatedGif("animated.gif", [CreateFrame(100), CreateFrame(200)]));

        Assert.Equal("Frame 1 / 2", state.FrameDisplayText);

        state.NextFrame();

        Assert.Equal("Frame 2 / 2", state.FrameDisplayText);
    }

    [Fact]
    public void GetFrameDelay_ReturnsDelayForRequestedFrame()
    {
        var state = new AnimationState();
        state.LoadAnimation(new AnimatedGif("animated.gif", [CreateFrame(100), CreateFrame(250)]));

        var result = state.GetFrameDelay(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(250, result.Value);
    }

    private static AnimationFrame CreateFrame(int delay)
    {
        return new AnimationFrame([0x89, 0x50, 0x4E, 0x47], 2, 2, TimeSpan.FromMilliseconds(delay));
    }
}
