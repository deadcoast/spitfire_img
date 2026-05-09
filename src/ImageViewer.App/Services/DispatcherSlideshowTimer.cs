using ImageViewer.Core.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace ImageViewer.App.Services;

public sealed class DispatcherSlideshowTimer : ISlideshowTimer
{
    private readonly DispatcherTimer _timer = new();
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public DispatcherSlideshowTimer()
    {
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Tick;

    public void Start(TimeSpan interval)
    {
        RunOnDispatcher(() =>
        {
            _timer.Stop();
            _timer.Interval = interval;
            _timer.Start();
        });
    }

    public void Stop()
    {
        RunOnDispatcher(_timer.Stop);
    }

    private void RunOnDispatcher(Action action)
    {
        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
            return;
        }

        _dispatcherQueue.TryEnqueue(() => action());
    }
}
