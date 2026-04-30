using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class AnimationState : ObservableObject
{
    private AnimatedGif? _currentAnimation;
    private int _currentFrameIndex;
    private bool _isPlaying;

    public AnimatedGif? CurrentAnimation
    {
        get => _currentAnimation;
        private set => SetProperty(ref _currentAnimation, value);
    }

    public int CurrentFrameIndex
    {
        get => _currentFrameIndex;
        private set
        {
            if (SetProperty(ref _currentFrameIndex, value))
            {
                OnPropertyChanged(nameof(CurrentFrame));
                OnPropertyChanged(nameof(CurrentFrameDelay));
                OnPropertyChanged(nameof(FramePositionText));
            }
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set => SetProperty(ref _isPlaying, value);
    }

    public bool IsAnimationActive => CurrentAnimation?.IsAnimated == true;

    public int TotalFrames => CurrentAnimation?.TotalFrames ?? 0;

    public AnimationFrame? CurrentFrame => CurrentAnimation is null
        ? null
        : CurrentAnimation.Frames[CurrentFrameIndex];

    public TimeSpan CurrentFrameDelay => CurrentFrame?.Delay ?? TimeSpan.Zero;

    public string FramePositionText => IsAnimationActive
        ? $"Frame {CurrentFrameIndex + 1} / {TotalFrames}"
        : string.Empty;

    public void LoadAnimation(AnimatedGif animation)
    {
        if (!animation.IsAnimated)
        {
            ClearAnimation();
            return;
        }

        CurrentAnimation = animation;
        CurrentFrameIndex = 0;
        IsPlaying = true;
        NotifyComputedProperties();
    }

    public void ClearAnimation()
    {
        CurrentAnimation = null;
        CurrentFrameIndex = 0;
        IsPlaying = false;
        NotifyComputedProperties();
    }

    public void Play()
    {
        if (IsAnimationActive)
        {
            IsPlaying = true;
        }
    }

    public void Pause()
    {
        IsPlaying = false;
    }

    public void TogglePlayback()
    {
        if (!IsAnimationActive)
        {
            return;
        }

        IsPlaying = !IsPlaying;
    }

    public bool AdvanceFrame()
    {
        if (!IsAnimationActive)
        {
            return false;
        }

        CurrentFrameIndex = (CurrentFrameIndex + 1) % TotalFrames;
        return true;
    }

    public bool AdvanceFrameManually()
    {
        if (!IsAnimationActive || IsPlaying)
        {
            return false;
        }

        return AdvanceFrame();
    }

    public bool RewindFrameManually()
    {
        if (!IsAnimationActive || IsPlaying)
        {
            return false;
        }

        CurrentFrameIndex = CurrentFrameIndex == 0
            ? TotalFrames - 1
            : CurrentFrameIndex - 1;
        return true;
    }

    public bool StepNextAndPause()
    {
        if (!IsAnimationActive)
        {
            return false;
        }

        IsPlaying = false;
        return AdvanceFrame();
    }

    public bool StepPreviousAndPause()
    {
        if (!IsAnimationActive)
        {
            return false;
        }

        IsPlaying = false;
        CurrentFrameIndex = CurrentFrameIndex == 0
            ? TotalFrames - 1
            : CurrentFrameIndex - 1;
        return true;
    }

    public bool SeekFrameManually(int frameIndex)
    {
        if (!IsAnimationActive)
        {
            return false;
        }

        IsPlaying = false;
        CurrentFrameIndex = Math.Clamp(frameIndex, 0, TotalFrames - 1);
        return true;
    }

    private void NotifyComputedProperties()
    {
        OnPropertyChanged(nameof(IsAnimationActive));
        OnPropertyChanged(nameof(TotalFrames));
        OnPropertyChanged(nameof(CurrentFrame));
        OnPropertyChanged(nameof(CurrentFrameDelay));
        OnPropertyChanged(nameof(FramePositionText));
    }
}
