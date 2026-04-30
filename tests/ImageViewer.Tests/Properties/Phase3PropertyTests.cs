using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Properties;

public sealed class Phase3PropertyTests
{
    private readonly Random _random = new(24680);

    [Fact]
    public async Task SlideshowTiming_UsesConfiguredInterval_ForRandomIntervals()
    {
        for (var iteration = 0; iteration < 120; iteration++)
        {
            var intervalSeconds = _random.Next(1, 61);
            var timer = new RecordingSlideshowTimer();
            var service = new SlideshowService(timer);
            var config = new SlideshowConfig(TimeSpan.FromSeconds(intervalSeconds), TransitionEffect.None, IsLooping: true);

            service.StartSlideshow(CreatePlaylist(count: 3, currentIndex: 0), currentIndex: 0, config);
            await timer.WaitUntilWaitingAsync();

            Assert.Equal(TimeSpan.FromSeconds(intervalSeconds), timer.WaitedIntervals.Single());
            service.StopSlideshow();
        }
    }

    [Fact]
    public async Task SlideshowLooping_WrapsToFirstImage_ForRandomPlaylistSizes()
    {
        for (var iteration = 0; iteration < 120; iteration++)
        {
            var count = _random.Next(2, 50);
            var timer = new RecordingSlideshowTimer();
            var service = new SlideshowService(timer);
            SlideshowAdvanceRequestedEventArgs? advance = null;
            service.AdvanceRequested += (_, args) => advance = args;

            service.StartSlideshow(CreatePlaylist(count, currentIndex: count - 1), currentIndex: count - 1, SlideshowConfig.Default);
            await timer.WaitUntilWaitingAsync();
            timer.CompleteWait();
            await timer.WaitUntilWaitingAsync(expectedWaitCount: 2);

            Assert.NotNull(advance);
            Assert.Equal(0, advance.NextIndex);
            service.StopSlideshow();
        }
    }

    [Fact]
    public async Task SlideshowTimerReset_RestartsAdvanceWaitWithSameInterval_ForRandomIntervals()
    {
        for (var iteration = 0; iteration < 120; iteration++)
        {
            var intervalSeconds = _random.Next(1, 61);
            var timer = new RecordingSlideshowTimer();
            var service = new SlideshowService(timer);
            var config = new SlideshowConfig(TimeSpan.FromSeconds(intervalSeconds), TransitionEffect.None, IsLooping: true);

            service.StartSlideshow(CreatePlaylist(count: 5, currentIndex: 2), currentIndex: 2, config);
            await timer.WaitUntilWaitingAsync();
            service.ResetTimer();
            await timer.WaitUntilWaitingAsync(expectedWaitCount: 2);

            Assert.Equal([TimeSpan.FromSeconds(intervalSeconds), TimeSpan.FromSeconds(intervalSeconds)], timer.WaitedIntervals);
            service.StopSlideshow();
        }
    }

    [Fact]
    public void SlideshowIntervalStepping_StaysWithinSupportedIntervals_ForRandomStartingValues()
    {
        var supportedIntervals = new[] { 1, 2, 3, 5, 10, 15, 30, 60 };

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var start = _random.Next(-20, 90);

            var increased = SlideshowConfig.StepIntervalSeconds(start, direction: 1);
            var decreased = SlideshowConfig.StepIntervalSeconds(start, direction: -1);

            Assert.Contains(increased, supportedIntervals);
            Assert.Contains(decreased, supportedIntervals);
            Assert.InRange(increased, Math.Clamp(start, 1, 60), 60);
            Assert.InRange(decreased, 1, Math.Clamp(start, 1, 60));
        }
    }

    [Fact]
    public void TransitionNavigationBlocking_IsEnabledForEveryTransitionEffect()
    {
        var service = new SlideshowTransitionService();

        foreach (var effect in Enum.GetValues<TransitionEffect>())
        {
            var transition = service.GetTransition(effect);

            Assert.True(transition.BlocksNavigation);
        }
    }

    [Fact]
    public async Task ComparisonSynchronizedNavigation_ShiftsVisibleImagesTogether_ForRandomPlaylists()
    {
        var service = new ComparisonService(new PathImageDecodeService());

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var count = _random.Next(2, 40);
            var currentIndex = _random.Next(0, count);
            var imageCount = _random.Next(2, Math.Min(4, count) + 1);
            var offset = _random.Next(-count * 2, count * 2 + 1);
            var playlist = CreatePlaylist(count, currentIndex);
            var current = await service.EnterComparisonAsync(
                playlist,
                ComparisonLayout.SideBySide,
                imageCount);

            var shifted = await service.ShiftComparisonAsync(
                playlist,
                current.Value.Images.Select(image => image.PlaylistIndex).ToArray(),
                offset,
                ComparisonLayout.SideBySide);

            Assert.True(shifted.IsSuccess);
            Assert.Equal(
                Enumerable.Range(0, imageCount)
                    .Select(index => Mod(currentIndex + offset + index, count))
                    .ToArray(),
                shifted.Value.Images.Select(image => image.PlaylistIndex).ToArray());
        }
    }

    [Fact]
    public void SideBySideAspectRatioPreservation_FitsImagesInsideLayoutRegions_ForRandomDimensions()
    {
        var service = new ComparisonService(new PathImageDecodeService());

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var imageWidth = _random.Next(1, 12000);
            var imageHeight = _random.Next(1, 12000);
            var canvasWidth = _random.Next(200, 8000);
            var canvasHeight = _random.Next(200, 8000);
            var imageCount = _random.Next(2, 5);

            foreach (var region in service.CalculateSideBySideLayout(imageCount, canvasWidth, canvasHeight))
            {
                var fit = service.CalculateAspectPreservingFit(
                    new ImageMetadata(imageWidth, imageHeight, 10),
                    region);

                Assert.True(fit.Width <= region.Width + 0.0001);
                Assert.True(fit.Height <= region.Height + 0.0001);
                Assert.Equal((double)imageWidth / imageHeight, fit.Width / fit.Height, precision: 8);
            }
        }
    }

    [Fact]
    public void OverlayOpacityRange_ClampsRandomAdjustmentsBetweenZeroAndOne()
    {
        var state = new ImageViewer.Core.ViewModels.ComparisonState();

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var opacity = (_random.NextDouble() * 4) - 1.5;

            state.SetOverlayOpacity(opacity);

            Assert.InRange(state.OverlayOpacity, 0, 1);
        }
    }

    [Fact]
    public void SynchronizedZoomWithRelativePositioning_AppliesSameRelativePan_ForRandomViewports()
    {
        for (var iteration = 0; iteration < 120; iteration++)
        {
            var imageCount = _random.Next(2, 5);
            var images = Enumerable.Range(0, imageCount)
                .Select(index => new ComparisonImage(
                    index,
                    new DecodedImage(
                        $@"C:\Images\{index}.png",
                        new ImageMetadata(_random.Next(1, 12000), _random.Next(1, 12000), 10))))
                .ToArray();
            var state = new ImageViewer.Core.ViewModels.ComparisonState();
            var pan = new RelativePanOffset(_random.NextDouble(), _random.NextDouble());
            var zoom = _random.Next(10, 800);

            state.EnterComparison(new ComparisonSet(ComparisonLayout.SideBySide, images));
            state.SetSynchronizedZoom(zoom);
            state.SetSynchronizedRelativePan(pan);

            Assert.All(state.Viewports, viewport =>
            {
                Assert.Equal(zoom, viewport.ZoomLevel);
                Assert.Equal(pan, viewport.RelativePan);
            });
        }
    }

    [Fact]
    public void FuzzyMatchingCharacterOrder_ReturnsOnlyOrderedMatches_ForRandomSubsequences()
    {
        var service = new CommandPaletteService();
        var commands = service.GetRegisteredCommands();

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var command = commands[_random.Next(commands.Count)];
            var query = BuildRandomOrderedQuery(command.Title);

            var results = service.SearchCommands(query);

            Assert.Contains(results, result => result.Command.Id == command.Id);
            Assert.All(results, result =>
            {
                Assert.True(IsOrderedSubsequence(query, result.Command.Title));
                Assert.Equal(query.Length, result.MatchIndexes.Count);
                Assert.True(result.MatchIndexes.SequenceEqual(result.MatchIndexes.OrderBy(index => index)));
            });
        }
    }

    [Fact]
    public void FuzzyMatchRanking_IsStableAndSortedByScore_ForRandomQueries()
    {
        var service = new CommandPaletteService();
        var queries = new[] { "rot", "fs", "cmp", "nav", "zoom", "gif", "file", "view" };

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var query = queries[_random.Next(queries.Length)];

            var first = service.SearchCommands(query);
            var second = service.SearchCommands(query);

            Assert.Equal(first.Select(result => result.Command.Id), second.Select(result => result.Command.Id));
            Assert.Equal(first.Select(result => result.Score), first.Select(result => result.Score).OrderByDescending(score => score));
        }
    }

    [Fact]
    public void GifFrameTimingAccuracy_ReportsEmbeddedDelayForDisplayedFrame_ForRandomDelays()
    {
        var edgeDelays = new[] { 10, 20, 100, 500, 1000, 5000 };

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var frameCount = _random.Next(2, 80);
            var delays = Enumerable.Range(0, frameCount)
                .Select(index => index < edgeDelays.Length
                    ? edgeDelays[index]
                    : _random.Next(1, 501) * 10)
                .OrderBy(_ => _random.Next())
                .ToArray();
            var animation = CreateAnimation(delays);
            var state = new ImageViewer.Core.ViewModels.AnimationState();

            state.LoadAnimation(animation);

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                Assert.Equal(frameIndex, state.CurrentFrameIndex);
                Assert.InRange(
                    state.CurrentFrameDelay.TotalMilliseconds,
                    delays[frameIndex] - 50,
                    delays[frameIndex] + 50);

                state.AdvanceFrame();
            }
        }
    }

    [Fact]
    public void GifAnimationLooping_WrapsToFirstFrameAfterLastFrame_ForRandomFrameCounts()
    {
        for (var iteration = 0; iteration < 120; iteration++)
        {
            var frameCount = iteration == 0
                ? 2
                : _random.Next(2, 120);
            var animation = CreateAnimation(
                Enumerable.Range(0, frameCount)
                    .Select(index => 10 + index * 10)
                    .ToArray());
            var state = new ImageViewer.Core.ViewModels.AnimationState();
            state.LoadAnimation(animation);

            for (var step = 0; step < frameCount * 3; step++)
            {
                Assert.Equal(step % frameCount, state.CurrentFrameIndex);
                Assert.Equal(animation.Frames[step % frameCount], state.CurrentFrame);

                state.AdvanceFrame();
            }

            Assert.Equal(0, state.CurrentFrameIndex);
        }
    }

    private static Playlist CreatePlaylist(int count, int currentIndex)
    {
        var items = Enumerable.Range(0, count)
            .Select(index => new ImageItem($@"C:\Images\{index}.png"))
            .ToArray();

        return new Playlist(items, currentIndex, @"C:\Images");
    }

    private static int Mod(int value, int divisor)
    {
        return ((value % divisor) + divisor) % divisor;
    }

    private string BuildRandomOrderedQuery(string title)
    {
        var letters = title.Where(char.IsLetterOrDigit).ToArray();
        var length = _random.Next(1, Math.Min(4, letters.Length) + 1);
        var indexes = Enumerable.Range(0, letters.Length)
            .OrderBy(_ => _random.Next())
            .Take(length)
            .Order()
            .ToArray();
        return new string(indexes.Select(index => letters[index]).ToArray()).ToLowerInvariant();
    }

    private static bool IsOrderedSubsequence(string query, string text)
    {
        var queryIndex = 0;
        foreach (var character in text)
        {
            if (queryIndex < query.Length &&
                char.ToUpperInvariant(character) == char.ToUpperInvariant(query[queryIndex]))
            {
                queryIndex++;
            }
        }

        return queryIndex == query.Length;
    }

    private static AnimatedGif CreateAnimation(IReadOnlyList<int> frameDelaysMs)
    {
        var frames = frameDelaysMs
            .Select((delay, index) => new AnimationFrame(
                index,
                TimeSpan.FromMilliseconds(delay),
                [(byte)(index % byte.MaxValue)],
                10,
                10))
            .ToArray();

        return new AnimatedGif(
            @"C:\Images\animated.gif",
            new ImageMetadata(10, 10, frames.Sum(frame => frame.EncodedBytes.Length)),
            frames);
    }

    private sealed class PathImageDecodeService : IImageDecodeService
    {
        public Task<Result<DecodedImage>> DecodeImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<DecodedImage>.Success(
                new DecodedImage(filePath, new ImageMetadata(100, 100, 10))));
        }

        public bool IsSupportedFormat(string extension) => true;

        public bool IsDeferredFormat(string extension) => false;

        public Task<Result<int>> GetFrameCountAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }

    private sealed class RecordingSlideshowTimer : ISlideshowTimer
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
