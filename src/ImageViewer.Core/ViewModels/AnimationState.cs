using System.Collections.ObjectModel;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class AnimationState : ObservableObject
{
    private bool _isAnimated;
    private bool _isPlaying;
    private int _currentFrame;
    public string? FilePath { get; private set; }

    public bool IsAnimated
    {
        get => _isAnimated;
        private set => SetProperty(ref _isAnimated, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set => SetProperty(ref _isPlaying, value);
    }

    public int CurrentFrame
    {
        get => _currentFrame;
        private set => SetProperty(ref _currentFrame, value);
    }

    public int TotalFrames => Frames.Count;

    public ObservableCollection<AnimationFrame> Frames { get; } = [];

    public IReadOnlyList<int> FrameDelays => Frames
        .Select(frame => checked((int)frame.Delay.TotalMilliseconds))
        .ToArray();

    public string FrameDisplayText => IsAnimated
        ? $"Frame {CurrentFrame + 1} / {TotalFrames}"
        : string.Empty;

    public void LoadAnimation(AnimatedGif gif)
    {
        FilePath = gif.FilePath;
        Frames.Clear();
        foreach (var frame in gif.Frames)
        {
            Frames.Add(frame);
        }

        CurrentFrame = 0;
        IsAnimated = gif.IsAnimated;
        IsPlaying = gif.IsAnimated;
        OnPropertyChanged(nameof(TotalFrames));
        OnPropertyChanged(nameof(FrameDelays));
        OnPropertyChanged(nameof(FrameDisplayText));
        OnPropertyChanged(nameof(CurrentFrame));
    }

    public void Clear()
    {
        FilePath = null;
        Frames.Clear();
        CurrentFrame = 0;
        IsAnimated = false;
        IsPlaying = false;
        OnPropertyChanged(nameof(TotalFrames));
        OnPropertyChanged(nameof(FrameDelays));
        OnPropertyChanged(nameof(FrameDisplayText));
    }

    public void Play()
    {
        if (IsAnimated)
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
        if (IsPlaying)
        {
            Pause();
            return;
        }

        Play();
    }

    public void NextFrame()
    {
        if (TotalFrames == 0)
        {
            CurrentFrame = 0;
            return;
        }

        SetCurrentFrame((CurrentFrame + 1) % TotalFrames);
    }

    public void PreviousFrame()
    {
        if (TotalFrames == 0)
        {
            CurrentFrame = 0;
            return;
        }

        SetCurrentFrame(CurrentFrame == 0 ? TotalFrames - 1 : CurrentFrame - 1);
    }

    public Result SeekToFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= TotalFrames)
        {
            return Result.Failure(ViewerError.AnimationError("SeekToFrame", null));
        }

        SetCurrentFrame(frameIndex);
        return Result.Success();
    }

    public Result<int> GetFrameDelay(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= TotalFrames)
        {
            return Result<int>.Failure(ViewerError.AnimationError("GetFrameDelay", null));
        }

        return Result<int>.Success(checked((int)Frames[frameIndex].Delay.TotalMilliseconds));
    }

    private void SetCurrentFrame(int frameIndex)
    {
        CurrentFrame = frameIndex;
        OnPropertyChanged(nameof(FrameDisplayText));
    }
}
