namespace ImageViewer.Core.Services;

public interface ISlideshowTimer
{
    event EventHandler? Tick;

    void Start(TimeSpan interval);

    void Stop();
}
