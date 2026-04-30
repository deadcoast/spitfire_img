namespace ImageViewer.Core.Services;

public interface ISlideshowTimer
{
    Task WaitForNextAdvanceAsync(TimeSpan interval, CancellationToken cancellationToken);
}
