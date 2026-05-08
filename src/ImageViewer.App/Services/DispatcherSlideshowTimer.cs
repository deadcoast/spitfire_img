using ImageViewer.Core.Services;
using Microsoft.UI.Xaml;

namespace ImageViewer.App.Services;

public sealed class DispatcherSlideshowTimer : ISlideshowTimer
{
    private readonly DispatcherTimer _timer = new();

    public DispatcherSlideshowTimer()
    {
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Tick;

    public void Start(TimeSpan interval)
    {
        _timer.Stop();
        _timer.Interval = interval;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }
}
