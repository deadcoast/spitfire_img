using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class SlideshowState : ObservableObject
{
    private bool _isActive;
    private bool _isPaused;
    private TimeSpan _interval = SlideshowConfig.DefaultInterval;
    private TransitionEffect _transition = TransitionEffect.Fade;
    private int _currentImageIndex;

    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    public bool IsPaused
    {
        get => _isPaused;
        private set => SetProperty(ref _isPaused, value);
    }

    public TimeSpan Interval
    {
        get => _interval;
        private set => SetProperty(ref _interval, value);
    }

    public TransitionEffect Transition
    {
        get => _transition;
        private set => SetProperty(ref _transition, value);
    }

    public int CurrentImageIndex
    {
        get => _currentImageIndex;
        private set => SetProperty(ref _currentImageIndex, value);
    }

    public bool IsLooping => true;

    public void Start(SlideshowConfig config, int currentImageIndex)
    {
        Interval = config.Interval;
        Transition = config.Transition;
        CurrentImageIndex = Math.Max(0, currentImageIndex);
        IsPaused = false;
        IsActive = true;
    }

    public void Stop()
    {
        IsPaused = false;
        IsActive = false;
    }

    public void Pause()
    {
        if (IsActive)
        {
            IsPaused = true;
        }
    }

    public void Resume()
    {
        if (IsActive)
        {
            IsPaused = false;
        }
    }

    public void SetInterval(TimeSpan interval)
    {
        Interval = interval;
    }

    public void SetCurrentImageIndex(int currentImageIndex)
    {
        CurrentImageIndex = Math.Max(0, currentImageIndex);
    }
}
