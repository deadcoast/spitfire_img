using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Core.ViewModels;

public sealed class SlideshowState : ObservableObject
{
    private bool _isActive;
    private bool _isPaused;
    private TimeSpan _interval = SlideshowConfig.Default.Interval;
    private TransitionEffect _transition = SlideshowConfig.Default.Transition;
    private int _currentImageIndex = -1;
    private bool _isLooping = true;
    private int _timerResetCount;
    private bool _isTransitioning;
    private SlideshowTransitionSpec? _currentTransition;

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

    public bool IsLooping
    {
        get => _isLooping;
        private set => SetProperty(ref _isLooping, value);
    }

    public int TimerResetCount
    {
        get => _timerResetCount;
        private set => SetProperty(ref _timerResetCount, value);
    }

    public bool IsTransitioning
    {
        get => _isTransitioning;
        private set => SetProperty(ref _isTransitioning, value);
    }

    public SlideshowTransitionSpec? CurrentTransition
    {
        get => _currentTransition;
        private set => SetProperty(ref _currentTransition, value);
    }

    public void Start(int currentImageIndex, SlideshowConfig config)
    {
        CurrentImageIndex = currentImageIndex;
        Interval = config.Interval;
        Transition = config.Transition;
        IsLooping = config.IsLooping;
        IsActive = true;
        IsPaused = false;
    }

    public void Stop()
    {
        IsActive = false;
        IsPaused = false;
        EndTransition();
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

    public void SetCurrentImageIndex(int currentImageIndex)
    {
        CurrentImageIndex = currentImageIndex;
    }

    public void SetInterval(TimeSpan interval)
    {
        Interval = interval;
    }

    public void SetTransition(TransitionEffect transition)
    {
        Transition = transition;
    }

    public void BeginTransition(SlideshowTransitionSpec transition)
    {
        CurrentTransition = transition;
        Transition = transition.Effect;
        IsTransitioning = transition.BlocksNavigation;
    }

    public void EndTransition()
    {
        IsTransitioning = false;
        CurrentTransition = null;
    }

    public void ResetTimer()
    {
        if (IsActive)
        {
            TimerResetCount++;
        }
    }
}
