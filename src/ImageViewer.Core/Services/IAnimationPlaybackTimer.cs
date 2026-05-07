namespace ImageViewer.Core.Services;

public interface IAnimationPlaybackTimer
{
    event EventHandler? Tick;

    void Start(TimeSpan interval);

    void Stop();
}
