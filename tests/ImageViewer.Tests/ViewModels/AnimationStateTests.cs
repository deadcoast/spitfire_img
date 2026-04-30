using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class AnimationStateTests
{
    [Fact]
    public void LoadAnimation_WithAnimatedGif_ActivatesStateAtFirstFrame()
    {
        var animation = new AnimatedGif(
            @"C:\Photos\animated.gif",
            new ImageMetadata(10, 20, 300),
            [
                new AnimationFrame(0, TimeSpan.FromMilliseconds(50), [0x01], 10, 20),
                new AnimationFrame(1, TimeSpan.FromMilliseconds(80), [0x02], 10, 20)
            ]);
        var state = new AnimationState();

        state.LoadAnimation(animation);

        Assert.True(state.IsAnimationActive);
        Assert.True(state.IsPlaying);
        Assert.Same(animation, state.CurrentAnimation);
        Assert.Equal(0, state.CurrentFrameIndex);
        Assert.Equal(2, state.TotalFrames);
        Assert.Equal(TimeSpan.FromMilliseconds(50), state.CurrentFrameDelay);
        Assert.Equal("Frame 1 / 2", state.FramePositionText);
        Assert.Equal(animation.Frames[0], state.CurrentFrame);
    }

    [Fact]
    public void LoadAnimation_WithStaticGif_ClearsActiveAnimation()
    {
        var animation = new AnimatedGif(
            @"C:\Photos\static.gif",
            new ImageMetadata(10, 20, 300),
            [new AnimationFrame(0, TimeSpan.FromMilliseconds(50), [0x01], 10, 20)]);
        var state = new AnimationState();

        state.LoadAnimation(animation);

        Assert.False(state.IsAnimationActive);
        Assert.Null(state.CurrentAnimation);
        Assert.Null(state.CurrentFrame);
        Assert.False(state.IsPlaying);
        Assert.Equal(0, state.TotalFrames);
        Assert.Equal(string.Empty, state.FramePositionText);
    }

    [Fact]
    public void ClearAnimation_RemovesAnimationState()
    {
        var animation = new AnimatedGif(
            @"C:\Photos\animated.gif",
            new ImageMetadata(10, 20, 300),
            [
                new AnimationFrame(0, TimeSpan.FromMilliseconds(50), [0x01], 10, 20),
                new AnimationFrame(1, TimeSpan.FromMilliseconds(80), [0x02], 10, 20)
            ]);
        var state = new AnimationState();
        state.LoadAnimation(animation);

        state.ClearAnimation();

        Assert.False(state.IsAnimationActive);
        Assert.Null(state.CurrentAnimation);
        Assert.Null(state.CurrentFrame);
        Assert.Equal(0, state.CurrentFrameIndex);
        Assert.False(state.IsPlaying);
    }

    [Fact]
    public void TogglePlayback_WhenAnimationIsActive_PausesAndResumes()
    {
        var state = new AnimationState();
        state.LoadAnimation(CreateAnimation(frameCount: 2));

        state.TogglePlayback();

        Assert.False(state.IsPlaying);

        state.TogglePlayback();

        Assert.True(state.IsPlaying);
    }

    [Fact]
    public void AdvanceFrame_WhenPlaying_LoopsFromLastFrameToFirst()
    {
        var state = new AnimationState();
        state.LoadAnimation(CreateAnimation(frameCount: 3));
        state.AdvanceFrame();
        state.AdvanceFrame();

        state.AdvanceFrame();

        Assert.Equal(0, state.CurrentFrameIndex);
        Assert.Equal("Frame 1 / 3", state.FramePositionText);
    }

    [Fact]
    public void AdvanceFrameManually_WhenPaused_MovesToNextFrameAndStaysPaused()
    {
        var state = new AnimationState();
        state.LoadAnimation(CreateAnimation(frameCount: 3));
        state.Pause();

        var advanced = state.AdvanceFrameManually();

        Assert.True(advanced);
        Assert.False(state.IsPlaying);
        Assert.Equal(1, state.CurrentFrameIndex);
        Assert.Equal(TimeSpan.FromMilliseconds(80), state.CurrentFrameDelay);
    }

    [Fact]
    public void AdvanceFrameManually_WhenPlaying_DoesNotMoveFrame()
    {
        var state = new AnimationState();
        state.LoadAnimation(CreateAnimation(frameCount: 3));

        var advanced = state.AdvanceFrameManually();

        Assert.False(advanced);
        Assert.Equal(0, state.CurrentFrameIndex);
    }

    [Fact]
    public void RewindFrameManually_WhenPaused_WrapsFromFirstFrameToLast()
    {
        var state = new AnimationState();
        state.LoadAnimation(CreateAnimation(frameCount: 3));
        state.Pause();

        var rewound = state.RewindFrameManually();

        Assert.True(rewound);
        Assert.False(state.IsPlaying);
        Assert.Equal(2, state.CurrentFrameIndex);
        Assert.Equal("Frame 3 / 3", state.FramePositionText);
    }

    [Fact]
    public void SeekFrameManually_WhenAnimationIsActive_JumpsToFrameAndPauses()
    {
        var state = new AnimationState();
        state.LoadAnimation(CreateAnimation(frameCount: 5));

        var sought = state.SeekFrameManually(3);

        Assert.True(sought);
        Assert.False(state.IsPlaying);
        Assert.Equal(3, state.CurrentFrameIndex);
        Assert.Equal("Frame 4 / 5", state.FramePositionText);
    }

    [Fact]
    public void SeekFrameManually_ClampsOutOfRangeIndex()
    {
        var state = new AnimationState();
        state.LoadAnimation(CreateAnimation(frameCount: 5));

        state.SeekFrameManually(99);

        Assert.Equal(4, state.CurrentFrameIndex);
    }

    [Fact]
    public void StepNextAndPause_WhenPlaying_MovesFrameAndPauses()
    {
        var state = new AnimationState();
        state.LoadAnimation(CreateAnimation(frameCount: 3));

        var advanced = state.StepNextAndPause();

        Assert.True(advanced);
        Assert.False(state.IsPlaying);
        Assert.Equal(1, state.CurrentFrameIndex);
    }

    [Fact]
    public void StepPreviousAndPause_WhenPlaying_MovesFrameAndPauses()
    {
        var state = new AnimationState();
        state.LoadAnimation(CreateAnimation(frameCount: 3));

        var rewound = state.StepPreviousAndPause();

        Assert.True(rewound);
        Assert.False(state.IsPlaying);
        Assert.Equal(2, state.CurrentFrameIndex);
    }

    private static AnimatedGif CreateAnimation(int frameCount)
    {
        return new AnimatedGif(
            @"C:\Photos\animated.gif",
            new ImageMetadata(10, 20, 300),
            Enumerable.Range(0, frameCount)
                .Select(index => new AnimationFrame(
                    index,
                    TimeSpan.FromMilliseconds(50 + index * 30),
                    [(byte)(index + 1)],
                    10,
                    20))
                .ToArray());
    }
}
