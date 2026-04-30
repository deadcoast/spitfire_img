using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class SlideshowServiceTests
{
    [Fact]
    public async Task StartSlideshow_SchedulesAdvanceAtConfiguredInterval()
    {
        var timer = new ManualSlideshowTimer();
        var service = new SlideshowService(timer);
        var playlist = CreatePlaylist(3);
        var config = new SlideshowConfig(TimeSpan.FromSeconds(5), TransitionEffect.None, IsLooping: true);
        SlideshowAdvanceRequestedEventArgs? advance = null;
        service.AdvanceRequested += (_, args) => advance = args;

        var result = service.StartSlideshow(playlist, currentIndex: 0, config);
        await timer.WaitUntilWaitingAsync();
        timer.CompleteWait();
        await timer.WaitUntilWaitingAsync(expectedWaitCount: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(TimeSpan.FromSeconds(5), timer.WaitedIntervals[0]);
        Assert.NotNull(advance);
        Assert.Equal(1, advance.NextIndex);
        Assert.Equal(playlist.Items[1], advance.NextItem);
    }

    [Fact]
    public async Task StartSlideshow_LoopsFromLastImageToFirstImage()
    {
        var timer = new ManualSlideshowTimer();
        var service = new SlideshowService(timer);
        var playlist = CreatePlaylist(4);
        SlideshowAdvanceRequestedEventArgs? advance = null;
        service.AdvanceRequested += (_, args) => advance = args;

        service.StartSlideshow(playlist, currentIndex: 3, SlideshowConfig.Default);
        await timer.WaitUntilWaitingAsync();
        timer.CompleteWait();
        await timer.WaitUntilWaitingAsync(expectedWaitCount: 2);

        Assert.NotNull(advance);
        Assert.Equal(0, advance.NextIndex);
        Assert.Equal(playlist.Items[0], advance.NextItem);
    }

    [Fact]
    public void PauseResumeStop_UpdateServiceState()
    {
        var service = new SlideshowService(new ManualSlideshowTimer());
        var playlist = CreatePlaylist(2);

        service.StartSlideshow(playlist, currentIndex: 0, SlideshowConfig.Default);
        service.PauseSlideshow();
        Assert.True(service.IsPaused);

        service.ResumeSlideshow();
        Assert.False(service.IsPaused);

        service.StopSlideshow();
        Assert.False(service.IsActive);
    }

    private static Playlist CreatePlaylist(int count)
    {
        var items = Enumerable.Range(0, count)
            .Select(index => new ImageItem($@"C:\Photos\{index}.png"))
            .ToArray();

        return new Playlist(items, 0, @"C:\Photos");
    }

    private sealed class ManualSlideshowTimer : ISlideshowTimer
    {
        private TaskCompletionSource _waiting = NewCompletionSource();
        private TaskCompletionSource _resume = NewCompletionSource();

        public List<TimeSpan> WaitedIntervals { get; } = [];

        public Task WaitForNextAdvanceAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            WaitedIntervals.Add(interval);
            _waiting.TrySetResult();
            return _resume.Task.WaitAsync(cancellationToken);
        }

        public async Task WaitUntilWaitingAsync(int expectedWaitCount = 1)
        {
            while (WaitedIntervals.Count < expectedWaitCount)
            {
                await _waiting.Task.WaitAsync(TimeSpan.FromSeconds(1));
            }
        }

        public void CompleteWait()
        {
            _waiting = NewCompletionSource();
            var resume = _resume;
            _resume = NewCompletionSource();
            resume.TrySetResult();
        }

        private static TaskCompletionSource NewCompletionSource()
        {
            return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
