using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class SlideshowService : ISlideshowService
{
    private readonly ISlideshowTimer _timer;
    private readonly object _gate = new();
    private CancellationTokenSource? _timerCancellation;
    private Playlist? _playlist;
    private SlideshowConfig _config = SlideshowConfig.Default;
    private int _currentIndex;
    private bool _isActive;
    private bool _isPaused;

    public SlideshowService()
        : this(new SystemSlideshowTimer())
    {
    }

    public SlideshowService(ISlideshowTimer timer)
    {
        _timer = timer;
    }

    public event EventHandler<SlideshowAdvanceRequestedEventArgs>? AdvanceRequested;

    public bool IsActive
    {
        get
        {
            lock (_gate)
            {
                return _isActive;
            }
        }
    }

    public bool IsPaused
    {
        get
        {
            lock (_gate)
            {
                return _isPaused;
            }
        }
    }

    public Result StartSlideshow(Playlist playlist, int currentIndex, SlideshowConfig config)
    {
        if (playlist.Items.Count == 0)
        {
            return Result.Failure(ViewerError.NavigationError("StartSlideshow", playlist.FolderPath));
        }

        lock (_gate)
        {
            StopTimerLocked();
            _playlist = playlist;
            _config = config;
            _currentIndex = Math.Clamp(currentIndex, 0, playlist.Items.Count - 1);
            _isActive = true;
            _isPaused = false;
            StartTimerLocked();
        }

        return Result.Success();
    }

    public Result StopSlideshow()
    {
        lock (_gate)
        {
            StopTimerLocked();
            _playlist = null;
            _isActive = false;
            _isPaused = false;
        }

        return Result.Success();
    }

    public Result PauseSlideshow()
    {
        lock (_gate)
        {
            if (_isActive)
            {
                _isPaused = true;
            }
        }

        return Result.Success();
    }

    public Result ResumeSlideshow()
    {
        lock (_gate)
        {
            if (_isActive)
            {
                _isPaused = false;
                RestartTimerLocked();
            }
        }

        return Result.Success();
    }

    public Result UpdateConfig(SlideshowConfig config)
    {
        lock (_gate)
        {
            _config = config;
            if (_isActive)
            {
                RestartTimerLocked();
            }
        }

        return Result.Success();
    }

    public Result ResetTimer()
    {
        lock (_gate)
        {
            if (_isActive)
            {
                RestartTimerLocked();
            }
        }

        return Result.Success();
    }

    private async Task RunTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TimeSpan interval;
                lock (_gate)
                {
                    interval = _config.Interval;
                }

                await _timer.WaitForNextAdvanceAsync(interval, cancellationToken).ConfigureAwait(false);

                SlideshowAdvanceRequestedEventArgs? args = null;
                lock (_gate)
                {
                    if (_isActive && !_isPaused && _playlist is not null && _playlist.Items.Count > 0)
                    {
                        var nextIndex = _currentIndex + 1;
                        if (nextIndex >= _playlist.Items.Count)
                        {
                            nextIndex = _config.IsLooping ? 0 : _playlist.Items.Count - 1;
                        }

                        _currentIndex = nextIndex;
                        args = new SlideshowAdvanceRequestedEventArgs(nextIndex, _playlist.Items[nextIndex]);
                    }
                }

                if (args is not null)
                {
                    AdvanceRequested?.Invoke(this, args);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void RestartTimerLocked()
    {
        StopTimerLocked();
        StartTimerLocked();
    }

    private void StartTimerLocked()
    {
        _timerCancellation = new CancellationTokenSource();
        _ = RunTimerAsync(_timerCancellation.Token);
    }

    private void StopTimerLocked()
    {
        _timerCancellation?.Cancel();
        _timerCancellation?.Dispose();
        _timerCancellation = null;
    }

    private sealed class SystemSlideshowTimer : ISlideshowTimer
    {
        public Task WaitForNextAdvanceAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            return Task.Delay(interval, cancellationToken);
        }
    }
}
