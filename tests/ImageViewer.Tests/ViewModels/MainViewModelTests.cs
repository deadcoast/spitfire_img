using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task OpenFileAsync_ValidFile_UpdatesViewerAndPlaylistState()
    {
        var image = new DecodedImage(@"C:\Photos\b.png", new ImageMetadata(320, 240, 100));
        var playlist = new Playlist(
            [new ImageItem(@"C:\Photos\a.png"), new ImageItem(@"C:\Photos\b.png")],
            1,
            @"C:\Photos");
        var services = TestServices.Success(image, playlist);
        var viewModel = services.CreateViewModel();

        await viewModel.OpenFileAsync(@"C:\Photos\b.png");

        Assert.Equal(image, viewModel.ViewerState.CurrentImage);
        Assert.Equal(playlist.TotalCount, viewModel.PlaylistState.TotalCount);
        Assert.Equal(1, viewModel.PlaylistState.CurrentIndex);
        Assert.False(viewModel.IsLoading);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task OpenFileAsync_ValidStartupPathWithSpacesAndNonAsciiCharacters_UpdatesViewerAndPlaylistState()
    {
        var path = @"C:\Photos\release launch folder\café image.png";
        var image = new DecodedImage(path, new ImageMetadata(320, 240, 100));
        var playlist = new Playlist(
            [
                new ImageItem(@"C:\Photos\release launch folder\a.png"),
                new ImageItem(path)
            ],
            1,
            @"C:\Photos\release launch folder");
        var services = TestServices.Success(image, playlist);
        var viewModel = services.CreateViewModel();

        await viewModel.OpenFileAsync(path);

        Assert.Equal(path, viewModel.ViewerState.CurrentImage?.FilePath);
        Assert.Equal(playlist.TotalCount, viewModel.PlaylistState.TotalCount);
        Assert.Equal(1, viewModel.PlaylistState.CurrentIndex);
        Assert.False(viewModel.IsLoading);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task OpenFileAsync_AnimatedGifLoadsAnimationStateAndStartsPlayback()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(2, 2, 100));
        var playlist = new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos");
        var animation = CreateAnimatedGif(image.FilePath);
        var animationService = new RecordingAnimationService(Result<AnimatedGif>.Success(animation));
        var services = TestServices.Success(image, playlist, animationService);
        var viewModel = services.CreateViewModel();

        await viewModel.OpenFileAsync(image.FilePath);

        Assert.Equal([image.FilePath], animationService.LoadedPaths);
        Assert.True(viewModel.AnimationState.IsAnimated);
        Assert.True(viewModel.AnimationState.IsPlaying);
        Assert.Equal(2, viewModel.AnimationState.TotalFrames);
        Assert.Equal([100, 200], viewModel.AnimationState.FrameDelays);
    }

    [Fact]
    public async Task OpenFileAsync_AnimatedGifStartsPlaybackTimerWithCurrentFrameDelay()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(2, 2, 100));
        var playlist = new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos");
        var timer = new RecordingAnimationPlaybackTimer();
        var services = TestServices.Success(
            image,
            playlist,
            new RecordingAnimationService(Result<AnimatedGif>.Success(CreateAnimatedGif(image.FilePath))),
            timer);
        var viewModel = services.CreateViewModel();

        await viewModel.OpenFileAsync(image.FilePath);

        Assert.Equal([TimeSpan.FromMilliseconds(100)], timer.StartedIntervals);
        Assert.False(timer.IsStopped);
    }

    [Fact]
    public async Task OpenFileAsync_WhenGifAutoPlayDisabled_LoadsFramesWithoutStartingPlayback()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(2, 2, 100));
        var playlist = new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos");
        var timer = new RecordingAnimationPlaybackTimer();
        var services = TestServices.Success(
            image,
            playlist,
            new RecordingAnimationService(Result<AnimatedGif>.Success(CreateAnimatedGif(image.FilePath))),
            timer,
            settings: ViewerSettings.Default with { GifAutoPlay = false });
        var viewModel = services.CreateViewModel();
        await viewModel.InitializeAsync();

        await viewModel.OpenFileAsync(image.FilePath);

        Assert.True(viewModel.AnimationState.IsAnimated);
        Assert.False(viewModel.AnimationState.IsPlaying);
        Assert.True(timer.IsStopped);
    }

    [Fact]
    public async Task AdvanceAnimationPlaybackFrame_LoopsAndSchedulesNextFrameDelay()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(2, 2, 100));
        var timer = new RecordingAnimationPlaybackTimer();
        var services = TestServices.Success(
            image,
            new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos"),
            new RecordingAnimationService(Result<AnimatedGif>.Success(CreateAnimatedGif(image.FilePath))),
            timer);
        var viewModel = services.CreateViewModel();
        await viewModel.OpenFileAsync(image.FilePath);

        viewModel.AdvanceAnimationPlaybackFrame();
        viewModel.AdvanceAnimationPlaybackFrame();

        Assert.Equal(0, viewModel.AnimationState.CurrentFrame);
        Assert.Equal(
            [TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(100)],
            timer.StartedIntervals);
    }

    [Fact]
    public async Task ToggleAnimationPlayback_PausesAndResumesTimer()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(2, 2, 100));
        var timer = new RecordingAnimationPlaybackTimer();
        var services = TestServices.Success(
            image,
            new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos"),
            new RecordingAnimationService(Result<AnimatedGif>.Success(CreateAnimatedGif(image.FilePath))),
            timer);
        var viewModel = services.CreateViewModel();
        await viewModel.OpenFileAsync(image.FilePath);

        viewModel.ToggleAnimationPlayback();

        Assert.False(viewModel.AnimationState.IsPlaying);
        Assert.True(timer.IsStopped);

        viewModel.ToggleAnimationPlayback();

        Assert.True(viewModel.AnimationState.IsPlaying);
        Assert.Equal(TimeSpan.FromMilliseconds(100), timer.StartedIntervals[^1]);
    }

    [Fact]
    public async Task ManualAnimationFrameControlsWorkOnlyWhenPaused()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(2, 2, 100));
        var timer = new RecordingAnimationPlaybackTimer();
        var services = TestServices.Success(
            image,
            new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos"),
            new RecordingAnimationService(Result<AnimatedGif>.Success(CreateAnimatedGif(image.FilePath))),
            timer);
        var viewModel = services.CreateViewModel();
        await viewModel.OpenFileAsync(image.FilePath);

        viewModel.AdvanceAnimationFrame();

        Assert.Equal(0, viewModel.AnimationState.CurrentFrame);

        viewModel.ToggleAnimationPlayback();
        viewModel.AdvanceAnimationFrame();
        viewModel.RewindAnimationFrame();

        Assert.Equal(0, viewModel.AnimationState.CurrentFrame);
        Assert.True(timer.IsStopped);
    }

    [Fact]
    public async Task FrameControlNext_PausesPlaybackAndAdvancesFrame()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(2, 2, 100));
        var timer = new RecordingAnimationPlaybackTimer();
        var services = TestServices.Success(
            image,
            new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos"),
            new RecordingAnimationService(Result<AnimatedGif>.Success(CreateAnimatedGif(image.FilePath))),
            timer);
        var viewModel = services.CreateViewModel();
        await viewModel.OpenFileAsync(image.FilePath);

        viewModel.AdvanceAnimationFrameFromControl();

        Assert.False(viewModel.AnimationState.IsPlaying);
        Assert.Equal(1, viewModel.AnimationState.CurrentFrame);
        Assert.True(timer.IsStopped);
    }

    [Fact]
    public async Task FrameControlPrevious_PausesPlaybackAndRewindsFrame()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(2, 2, 100));
        var timer = new RecordingAnimationPlaybackTimer();
        var services = TestServices.Success(
            image,
            new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos"),
            new RecordingAnimationService(Result<AnimatedGif>.Success(CreateAnimatedGif(image.FilePath))),
            timer);
        var viewModel = services.CreateViewModel();
        await viewModel.OpenFileAsync(image.FilePath);

        viewModel.RewindAnimationFrameFromControl();

        Assert.False(viewModel.AnimationState.IsPlaying);
        Assert.Equal(1, viewModel.AnimationState.CurrentFrame);
        Assert.True(timer.IsStopped);
    }

    [Fact]
    public async Task FrameControlSeek_PausesPlaybackAndMovesToRequestedFrame()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(2, 2, 100));
        var timer = new RecordingAnimationPlaybackTimer();
        var services = TestServices.Success(
            image,
            new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos"),
            new RecordingAnimationService(Result<AnimatedGif>.Success(CreateAnimatedGif(image.FilePath))),
            timer);
        var viewModel = services.CreateViewModel();
        await viewModel.OpenFileAsync(image.FilePath);

        var result = viewModel.SeekAnimationFrameFromControl(1);

        Assert.True(result.IsSuccess);
        Assert.False(viewModel.AnimationState.IsPlaying);
        Assert.Equal(1, viewModel.AnimationState.CurrentFrame);
        Assert.True(timer.IsStopped);
    }

    [Fact]
    public async Task OpenFileAsync_WhenAnimatedGifLoadFails_DisplaysStaticImageAndHidesFrameControls()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(2, 2, 100));
        var playlist = new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos");
        var animationService = new RecordingAnimationService(
            Result<AnimatedGif>.Failure(ViewerError.AnimationError("LoadAnimatedGif", image.FilePath)));
        var services = TestServices.Success(image, playlist, animationService);
        var viewModel = services.CreateViewModel();

        await viewModel.OpenFileAsync(image.FilePath);

        Assert.Equal(image, viewModel.ViewerState.CurrentImage);
        Assert.False(viewModel.AnimationState.IsAnimated);
        Assert.Empty(viewModel.AnimationState.Frames);
        Assert.Equal(string.Empty, viewModel.AnimationState.FrameDisplayText);
        Assert.Equal(ErrorCategory.AnimationError, services.ErrorLogService.LoggedErrors.Single().Category);
    }

    [Fact]
    public async Task OpenFileAsync_ValidationFailure_LogsErrorAndSetsUserMessage()
    {
        var error = ViewerError.FileAccessError("ValidateFilePath", @"C:\missing.png");
        var services = TestServices.WithFileValidationFailure(error);
        var viewModel = services.CreateViewModel();

        await viewModel.OpenFileAsync(@"C:\missing.png");

        Assert.Equal(error.UserMessage, viewModel.ErrorMessage);
        Assert.Equal(error.Severity, viewModel.CurrentErrorSeverity);
        Assert.False(viewModel.IsLoading);
        Assert.Single(services.ErrorLogService.LoggedErrors);
        Assert.Equal(error, services.ErrorLogService.LoggedErrors[0]);
    }

    [Fact]
    public async Task OpenFileAsync_HeicCodecUnavailable_RequestsStorePromptOnlyOncePerSession()
    {
        var path = @"C:\Photos\phone.heic";
        var error = ViewerError.CodecUnavailableError(
            "DecodeImage",
            "HEIC",
            "HEIF Image Extensions",
            path);
        var codecDetection = new RecordingCodecDetectionService(heicSupported: false);
        var services = TestServices.WithDecoder(
            path,
            new StubImageDecodeService(Result<DecodedImage>.Failure(error)),
            codecDetection);
        var viewModel = services.CreateViewModel();
        var prompts = new List<CodecInstallPromptRequest>();
        viewModel.CodecInstallPromptRequested += (_, request) => prompts.Add(request);

        await viewModel.OpenFileAsync(path);
        await viewModel.OpenFileAsync(path);

        Assert.Equal(2, services.ErrorLogService.LoggedErrors.Count);
        Assert.All(services.ErrorLogService.LoggedErrors, logged => Assert.Equal(ErrorCategory.CodecUnavailableError, logged.Category));
        var prompt = Assert.Single(prompts);
        Assert.Equal("HEIC", prompt.FormatName);
        Assert.Equal(error.UserMessage, prompt.Message);
        Assert.Equal("ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG", prompt.StoreUrl);
    }

    [Fact]
    public async Task OpenFileAsync_HeicCodecUnavailable_DoesNotPromptWhenStoreUrlCannotBeResolved()
    {
        var path = @"C:\Photos\phone.heic";
        var error = ViewerError.CodecUnavailableError(
            "DecodeImage",
            "HEIC",
            "HEIF Image Extensions",
            path);
        var codecDetection = new RecordingCodecDetectionService(
            heicSupported: false,
            heicStoreUrl: Result<string>.Failure(ViewerError.CodecDetectionError("GetHeicStoreUrl")));
        var services = TestServices.WithDecoder(
            path,
            new StubImageDecodeService(Result<DecodedImage>.Failure(error)),
            codecDetection);
        var viewModel = services.CreateViewModel();
        var prompts = new List<CodecInstallPromptRequest>();
        viewModel.CodecInstallPromptRequested += (_, request) => prompts.Add(request);

        await viewModel.OpenFileAsync(path);

        Assert.Empty(prompts);
        Assert.Equal(ErrorCategory.CodecUnavailableError, services.ErrorLogService.LoggedErrors.Single().Category);
    }

    [Fact]
    public async Task OpenFileAsync_AvifCodecUnavailable_RequestsStorePromptOnlyOncePerSession()
    {
        var path = @"C:\Photos\modern.avif";
        var error = ViewerError.CodecUnavailableError(
            "DecodeImage",
            "AVIF",
            "AV1 Video Extension",
            path);
        var codecDetection = new RecordingCodecDetectionService(heicSupported: false, avifSupported: false);
        var services = TestServices.WithDecoder(
            path,
            new StubImageDecodeService(Result<DecodedImage>.Failure(error)),
            codecDetection);
        var viewModel = services.CreateViewModel();
        var prompts = new List<CodecInstallPromptRequest>();
        viewModel.CodecInstallPromptRequested += (_, request) => prompts.Add(request);

        await viewModel.OpenFileAsync(path);
        await viewModel.OpenFileAsync(path);

        Assert.Equal(2, services.ErrorLogService.LoggedErrors.Count);
        Assert.All(services.ErrorLogService.LoggedErrors, logged => Assert.Equal(ErrorCategory.CodecUnavailableError, logged.Category));
        var prompt = Assert.Single(prompts);
        Assert.Equal("AVIF", prompt.FormatName);
        Assert.Equal(error.UserMessage, prompt.Message);
        Assert.Equal("ms-windows-store://pdp/?ProductId=9MVZQVXJBQ9V", prompt.StoreUrl);
    }

    [Theory]
    [InlineData(@"C:\Photos\release launch folder\café phone.heic", "HEIC", "HEIF Image Extensions", "ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG")]
    [InlineData(@"C:\Photos\release launch folder\café modern.avif", "AVIF", "AV1 Video Extension", "ms-windows-store://pdp/?ProductId=9MVZQVXJBQ9V")]
    public async Task OpenFileAsync_CodecDependentStartupPathWithSpacesAndNonAsciiCharacters_RequestsStorePrompt(
        string path,
        string formatName,
        string codecName,
        string expectedStoreUrl)
    {
        var error = ViewerError.CodecUnavailableError(
            "DecodeImage",
            formatName,
            codecName,
            path);
        var codecDetection = new RecordingCodecDetectionService(heicSupported: false, avifSupported: false);
        var services = TestServices.WithDecoder(
            path,
            new StubImageDecodeService(Result<DecodedImage>.Failure(error)),
            codecDetection);
        var viewModel = services.CreateViewModel();
        var prompts = new List<CodecInstallPromptRequest>();
        viewModel.CodecInstallPromptRequested += (_, request) => prompts.Add(request);

        await viewModel.OpenFileAsync(path);

        Assert.Equal(ErrorCategory.CodecUnavailableError, services.ErrorLogService.LoggedErrors.Single().Category);
        var prompt = Assert.Single(prompts);
        Assert.Equal(formatName, prompt.FormatName);
        Assert.Equal(error.UserMessage, prompt.Message);
        Assert.Equal(expectedStoreUrl, prompt.StoreUrl);
    }

    [Fact]
    public async Task NavigateNextAsync_UpdatesCurrentIndexAndImage()
    {
        var playlist = CreateTwoItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.NavigateNextAsync();

        Assert.Equal(1, viewModel.PlaylistState.CurrentIndex);
        Assert.Equal(@"C:\Photos\b.png", viewModel.ViewerState.CurrentImage?.FilePath);
        Assert.True(viewModel.IsChromeVisible);
    }

    [Fact]
    public async Task EnterComparisonModeAsync_ActivatesSideBySideWithCurrentAndNextImages()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        Assert.True(viewModel.ComparisonState.IsActive);
        Assert.Equal(ComparisonLayout.SideBySide, viewModel.ComparisonState.Layout);
        Assert.Equal([0, 1], viewModel.ComparisonState.DisplayedIndices);
        Assert.Equal(@"C:\Photos\a.png", viewModel.ComparisonState.DisplayedItems[0].FilePath);
        Assert.Equal(@"C:\Photos\b.png", viewModel.ComparisonState.DisplayedItems[1].FilePath);
    }

    [Fact]
    public async Task EnterComparisonModeAsync_OneImagePlaylistShowsComparisonError()
    {
        var playlist = new Playlist([new ImageItem(@"C:\Photos\a.png")], 0, @"C:\Photos");
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        Assert.False(viewModel.ComparisonState.IsActive);
        Assert.Equal("Comparison mode requires at least two images", viewModel.ErrorMessage);
        Assert.Equal(ErrorCategory.ComparisonError, services.ErrorLogService.LoggedErrors.Single().Category);
    }

    [Fact]
    public async Task ExitComparisonMode_ReturnsToSingleImageView()
    {
        var playlist = CreateThreeItemPlaylist();
        var viewModel = TestServices.WithPlaylist(playlist).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        viewModel.ExitComparisonMode();

        Assert.False(viewModel.ComparisonState.IsActive);
    }

    [Fact]
    public async Task EnterGridLayoutAsync_ActivatesDefaultGridWithCurrentPlaylistWindow()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 2);
        var viewModel = TestServices.WithPlaylist(playlist).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));

        Assert.True(viewModel.LayoutState.IsGridActive);
        Assert.Equal(LayoutMode.Grid, viewModel.LayoutState.CurrentLayout);
        Assert.Equal(3, viewModel.LayoutState.GridRows);
        Assert.Equal(3, viewModel.LayoutState.GridColumns);
        Assert.Equal(2, viewModel.LayoutState.GridStartIndex);
        Assert.Equal(9, viewModel.LayoutState.GridImages.Count);
        Assert.Equal([2, 3, 4, 5, 6, 7, 8, 9, 10], viewModel.LayoutState.GridImages.Select(image => image.PlaylistIndex));
        Assert.False(viewModel.ComparisonState.IsActive);
    }

    [Fact]
    public async Task EnterGridLayoutAsync_SavesGridLayoutPreference()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 2);
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));

        var saved = services.SettingsService.SavedSettings.Single();
        Assert.Equal(LayoutMode.Grid, saved.PreferredLayout);
        Assert.Equal(3, saved.PreferredGridRows);
        Assert.Equal(3, saved.PreferredGridColumns);
    }

    [Fact]
    public async Task EnterGridLayoutAsync_WhenPreferredGridExceedsMemory_ReducesDensity()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 2);
        var settings = ViewerSettings.Default with
        {
            PreferredGridRows = 4,
            PreferredGridColumns = 4
        };
        var services = TestServices.WithPlaylistAndSettings(
            playlist,
            settings,
            layoutService: new MemoryConstrainedLayoutService(maxGridDensity: 2));
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));

        Assert.True(viewModel.LayoutState.IsGridActive);
        Assert.Equal(2, viewModel.LayoutState.GridRows);
        Assert.Equal(2, viewModel.LayoutState.GridColumns);
        Assert.Equal([2, 3, 4, 5], viewModel.LayoutState.GridImages.Select(image => image.PlaylistIndex));
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task EnterGridLayoutAsync_WhenLayoutCriticallyFails_FallsBackToSingleImageView()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 2);
        var services = TestServices.WithPlaylist(
            playlist,
            layoutService: new FailingLayoutService());
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));

        Assert.Equal(LayoutMode.Single, viewModel.LayoutState.CurrentLayout);
        Assert.False(viewModel.LayoutState.IsGridActive);
        Assert.Equal(ErrorCategory.LayoutError, services.ErrorLogService.LoggedErrors.Single().Category);
    }

    [Fact]
    public async Task EnterFilmstripLayoutAsync_ActivatesFilmstripWithCurrentImageFocused()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 6);
        var viewModel = TestServices.WithPlaylist(playlist).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterFilmstripLayoutAsync();

        Assert.True(viewModel.LayoutState.IsFilmstripActive);
        Assert.Equal(LayoutMode.Filmstrip, viewModel.LayoutState.CurrentLayout);
        Assert.Equal(6, viewModel.LayoutState.FilmstripFocusIndex);
        Assert.Equal(7, viewModel.LayoutState.FilmstripThumbnails.Count);
        Assert.Equal([3, 4, 5, 6, 7, 8, 9], viewModel.LayoutState.FilmstripThumbnails.Select(image => image.PlaylistIndex));
        Assert.False(viewModel.ComparisonState.IsActive);
    }

    [Fact]
    public async Task EnterFilmstripLayoutAsync_SavesFilmstripLayoutPreference()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 6);
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterFilmstripLayoutAsync();

        var saved = services.SettingsService.SavedSettings.Single();
        Assert.Equal(LayoutMode.Filmstrip, saved.PreferredLayout);
        Assert.Equal(7, saved.PreferredFilmstripThumbnailCount);
    }

    [Fact]
    public async Task EnterFilmstripLayoutAsync_WhenLayoutFails_FallsBackToSingleImageView()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 6);
        var services = TestServices.WithPlaylist(
            playlist,
            layoutService: new FailingLayoutService());
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterFilmstripLayoutAsync();

        Assert.Equal(LayoutMode.Single, viewModel.LayoutState.CurrentLayout);
        Assert.False(viewModel.LayoutState.IsFilmstripActive);
        Assert.Equal(ErrorCategory.LayoutError, services.ErrorLogService.LoggedErrors.Single().Category);
    }

    [Fact]
    public async Task SetFilmstripFocusAsync_NavigatesToSelectedThumbnailAndRecentersStrip()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 6);
        var viewModel = TestServices.WithPlaylist(playlist).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterFilmstripLayoutAsync();

        await viewModel.SetFilmstripFocusAsync(9);

        Assert.Equal(9, viewModel.PlaylistState.CurrentIndex);
        Assert.Equal(9, viewModel.LayoutState.FilmstripFocusIndex);
        Assert.Equal(@"C:\Photos\image-09.png", viewModel.ViewerState.CurrentImage?.FilePath);
        Assert.Equal([5, 6, 7, 8, 9, 10, 11], viewModel.LayoutState.FilmstripThumbnails.Select(image => image.PlaylistIndex));
    }

    [Fact]
    public async Task NavigateNextAsync_WhenFilmstripActiveAdvancesFocusAndRecentersStrip()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 6);
        var viewModel = TestServices.WithPlaylist(playlist).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterFilmstripLayoutAsync();

        await viewModel.NavigateNextAsync();

        Assert.Equal(7, viewModel.PlaylistState.CurrentIndex);
        Assert.Equal(7, viewModel.LayoutState.FilmstripFocusIndex);
        Assert.Equal([4, 5, 6, 7, 8, 9, 10], viewModel.LayoutState.FilmstripThumbnails.Select(image => image.PlaylistIndex));
    }

    [Fact]
    public async Task ExitLayoutModeAsync_SavesSingleImageLayoutPreference()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 2);
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));

        await viewModel.ExitLayoutModeAsync();

        Assert.Equal(LayoutMode.Single, viewModel.LayoutState.CurrentLayout);
        Assert.Equal(LayoutMode.Single, services.SettingsService.SavedSettings.Last().PreferredLayout);
    }

    [Fact]
    public async Task OpenFileAsync_RestoresSavedGridLayoutPreference()
    {
        var settings = ViewerSettings.Default with
        {
            PreferredLayout = LayoutMode.Grid,
            PreferredGridRows = 4,
            PreferredGridColumns = 4
        };
        var services = TestServices.WithSettings(settings);
        var viewModel = services.CreateViewModel();

        await viewModel.InitializeAsync();
        await viewModel.OpenFileAsync(@"C:\Photos\a.png");

        Assert.Equal(LayoutMode.Grid, viewModel.LayoutState.CurrentLayout);
        Assert.Equal(4, viewModel.LayoutState.GridRows);
        Assert.Equal(4, viewModel.LayoutState.GridColumns);
    }

    [Fact]
    public async Task ToggleSlideshowAsync_StartsAndStopsSlideshowWithSavedConfig()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 2);
        var settings = ViewerSettings.Default with
        {
            SlideshowInterval = TimeSpan.FromSeconds(5),
            SlideshowTransition = TransitionEffect.Zoom
        };
        var services = TestServices.WithPlaylistAndSettings(playlist, settings);
        var viewModel = services.CreateViewModel();
        await viewModel.InitializeAsync();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.ToggleSlideshowAsync();

        Assert.True(viewModel.SlideshowState.IsActive);
        Assert.False(viewModel.SlideshowState.IsPaused);
        Assert.Equal(TimeSpan.FromSeconds(5), viewModel.SlideshowState.Interval);
        Assert.Equal(TransitionEffect.Zoom, viewModel.SlideshowState.Transition);
        Assert.Equal(TimeSpan.FromSeconds(5), services.SlideshowTimer.StartedIntervals.Single());

        await viewModel.ToggleSlideshowAsync();

        Assert.False(viewModel.SlideshowState.IsActive);
        Assert.Equal(1, services.SlideshowTimer.StopCount);
    }

    [Fact]
    public async Task ToggleSlideshowPause_PausesAndResumesTimer()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 2);
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.ToggleSlideshowAsync();

        viewModel.ToggleSlideshowPause();

        Assert.True(viewModel.SlideshowState.IsPaused);
        Assert.Equal(1, services.SlideshowTimer.StopCount);

        viewModel.ToggleSlideshowPause();

        Assert.False(viewModel.SlideshowState.IsPaused);
        Assert.Equal(TimeSpan.FromSeconds(3), services.SlideshowTimer.StartedIntervals.Last());
    }

    [Fact]
    public async Task ToggleSlideshowAsync_PreloadsNextImageDuringCurrentDisplay()
    {
        var playlist = CreateThreeItemPlaylist();
        var decoder = new CountingPathImageDecodeService();
        var services = TestServices.WithPlaylist(playlist, imageDecodeService: decoder);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.ToggleSlideshowAsync();

        Assert.Contains(@"C:\Photos\b.png", decoder.DecodedPaths);
    }

    [Fact]
    public async Task SlideshowTimerTick_UsesPreloadedNextImageAndPreloadsFollowingImage()
    {
        var playlist = CreateThreeItemPlaylist();
        var decoder = new CountingPathImageDecodeService();
        var services = TestServices.WithPlaylist(playlist, imageDecodeService: decoder);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.ToggleSlideshowAsync();
        decoder.DecodedPaths.Clear();

        await services.SlideshowTimer.RaiseTickAsync();

        Assert.Equal(1, viewModel.PlaylistState.CurrentIndex);
        Assert.Equal(@"C:\Photos\b.png", viewModel.ViewerState.CurrentImage?.FilePath);
        Assert.DoesNotContain(@"C:\Photos\b.png", decoder.DecodedPaths);
        Assert.Contains(@"C:\Photos\c.png", decoder.DecodedPaths);
    }

    [Fact]
    public async Task SlideshowTimerTick_AdvancesAndLoopsPlaylist()
    {
        var playlist = CreateThreeItemPlaylist() with { CurrentIndex = 2 };
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.ToggleSlideshowAsync();

        await services.SlideshowTimer.RaiseTickAsync();

        Assert.Equal(0, viewModel.PlaylistState.CurrentIndex);
        Assert.Equal(@"C:\Photos\a.png", viewModel.ViewerState.CurrentImage?.FilePath);
        Assert.Equal(TimeSpan.FromSeconds(3), services.SlideshowTimer.StartedIntervals.Last());
    }

    [Fact]
    public async Task IncreaseAndDecreaseSlideshowIntervalAsync_StepsAndSavesPreference()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.ToggleSlideshowAsync();

        await viewModel.IncreaseSlideshowIntervalAsync();
        await viewModel.DecreaseSlideshowIntervalAsync();

        Assert.Equal(TimeSpan.FromSeconds(3), viewModel.SlideshowState.Interval);
        Assert.Contains(services.SettingsService.SavedSettings, saved => saved.SlideshowInterval == TimeSpan.FromSeconds(5));
        Assert.Equal(TimeSpan.FromSeconds(3), services.SettingsService.SavedSettings.Last().SlideshowInterval);
    }

    [Fact]
    public async Task SlideshowTimerTick_WhenComparisonActiveAdvancesDisplayedImagesTogether()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);
        await viewModel.ToggleSlideshowAsync();

        await services.SlideshowTimer.RaiseTickAsync();

        Assert.Equal([1, 2], viewModel.ComparisonState.DisplayedIndices);
        Assert.Equal(1, viewModel.PlaylistState.CurrentIndex);
        Assert.True(viewModel.SlideshowState.IsActive);
    }

    [Fact]
    public async Task SlideshowTimerTick_WhenGridActiveAdvancesVisibleGridWindow()
    {
        var playlist = CreateNumberedPlaylist(count: 12, currentIndex: 0);
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));
        await viewModel.ToggleSlideshowAsync();

        await services.SlideshowTimer.RaiseTickAsync();

        Assert.Equal(1, viewModel.LayoutState.GridStartIndex);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], viewModel.LayoutState.GridImages.Select(image => image.PlaylistIndex));
        Assert.True(viewModel.SlideshowState.IsActive);
    }

    [Fact]
    public async Task SlideshowTimerTick_WhenFilmstripActiveAdvancesFocusImage()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 6);
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterFilmstripLayoutAsync();
        await viewModel.ToggleSlideshowAsync();

        await services.SlideshowTimer.RaiseTickAsync();

        Assert.Equal(7, viewModel.PlaylistState.CurrentIndex);
        Assert.Equal(7, viewModel.LayoutState.FilmstripFocusIndex);
        Assert.Equal([4, 5, 6, 7, 8, 9, 10], viewModel.LayoutState.FilmstripThumbnails.Select(image => image.PlaylistIndex));
        Assert.True(viewModel.SlideshowState.IsActive);
    }

    [Fact]
    public async Task EnterComparisonModeAsync_ExitsGridLayout()
    {
        var playlist = CreateTwelveItemPlaylist(currentIndex: 0);
        var viewModel = TestServices.WithPlaylist(playlist).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));

        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        Assert.False(viewModel.LayoutState.IsGridActive);
        Assert.Empty(viewModel.LayoutState.GridImages);
        Assert.True(viewModel.ComparisonState.IsActive);
    }

    [Fact]
    public async Task IncreaseGridDensityAsync_WhenGridActiveExpandsToNextDensity()
    {
        var playlist = CreateNumberedPlaylist(count: 20, currentIndex: 0);
        var viewModel = TestServices.WithPlaylist(playlist).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));

        await viewModel.IncreaseGridDensityAsync(new ImageDimensions(900, 600));

        Assert.Equal(4, viewModel.LayoutState.GridRows);
        Assert.Equal(4, viewModel.LayoutState.GridColumns);
        Assert.Equal(16, viewModel.LayoutState.GridImages.Count);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15], viewModel.LayoutState.GridImages.Select(image => image.PlaylistIndex));
    }

    [Fact]
    public async Task GridDensityCommands_ClampBetweenTwoByTwoAndSixBySix()
    {
        var playlist = CreateNumberedPlaylist(count: 50, currentIndex: 0);
        var viewModel = TestServices.WithPlaylist(playlist).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));

        await viewModel.DecreaseGridDensityAsync(new ImageDimensions(900, 600));
        await viewModel.DecreaseGridDensityAsync(new ImageDimensions(900, 600));
        await viewModel.DecreaseGridDensityAsync(new ImageDimensions(900, 600));

        Assert.Equal(2, viewModel.LayoutState.GridRows);
        Assert.Equal(2, viewModel.LayoutState.GridColumns);
        Assert.Equal(4, viewModel.LayoutState.GridImages.Count);

        for (var index = 0; index < 8; index++)
        {
            await viewModel.IncreaseGridDensityAsync(new ImageDimensions(900, 600));
        }

        Assert.Equal(6, viewModel.LayoutState.GridRows);
        Assert.Equal(6, viewModel.LayoutState.GridColumns);
        Assert.Equal(36, viewModel.LayoutState.GridImages.Count);
    }

    [Fact]
    public async Task SetActiveGridImage_SelectsVisibleGridImage()
    {
        var playlist = CreateNumberedPlaylist(count: 12, currentIndex: 2);
        var viewModel = TestServices.WithPlaylist(playlist).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));

        viewModel.SetActiveGridImage(5);

        Assert.Equal(5, viewModel.LayoutState.ActiveGridImageIndex);
        Assert.Equal(7, viewModel.LayoutState.ActiveGridPlaylistIndex);
    }

    [Fact]
    public async Task OpenActiveGridImageAsync_ExitsGridAndDisplaysSelectedImage()
    {
        var playlist = CreateNumberedPlaylist(count: 12, currentIndex: 2);
        var viewModel = TestServices.WithPlaylist(playlist).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));
        viewModel.SetActiveGridImage(5);

        await viewModel.OpenActiveGridImageAsync();

        Assert.False(viewModel.LayoutState.IsGridActive);
        Assert.Equal(7, viewModel.PlaylistState.CurrentIndex);
        Assert.Equal(@"C:\Photos\image-07.png", viewModel.ViewerState.CurrentImage?.FilePath);
    }

    [Fact]
    public async Task ScrollGridAsync_LoadsNextAndPreviousPlaylistPages()
    {
        var playlist = CreateNumberedPlaylist(count: 20, currentIndex: 0);
        var viewModel = TestServices.WithPlaylist(playlist).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterGridLayoutAsync(new ImageDimensions(900, 600));

        await viewModel.ScrollGridAsync(9, new ImageDimensions(900, 600));

        Assert.Equal(9, viewModel.LayoutState.GridStartIndex);
        Assert.Equal([9, 10, 11, 12, 13, 14, 15, 16, 17], viewModel.LayoutState.GridImages.Select(image => image.PlaylistIndex));

        await viewModel.ScrollGridAsync(9, new ImageDimensions(900, 600));

        Assert.Equal(11, viewModel.LayoutState.GridStartIndex);
        Assert.Equal([11, 12, 13, 14, 15, 16, 17, 18, 19], viewModel.LayoutState.GridImages.Select(image => image.PlaylistIndex));

        await viewModel.ScrollGridAsync(-9, new ImageDimensions(900, 600));

        Assert.Equal(2, viewModel.LayoutState.GridStartIndex);
        Assert.Equal([2, 3, 4, 5, 6, 7, 8, 9, 10], viewModel.LayoutState.GridImages.Select(image => image.PlaylistIndex));
    }

    [Fact]
    public async Task NavigateNextAsync_WhenComparisonActiveShiftsVisibleImagesForward()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        await viewModel.NavigateNextAsync();

        Assert.Equal([1, 2], viewModel.ComparisonState.DisplayedIndices);
        Assert.Equal(1, viewModel.PlaylistState.CurrentIndex);
    }

    [Fact]
    public async Task NavigatePreviousAsync_WhenComparisonActiveShiftsVisibleImagesBackward()
    {
        var playlist = CreateThreeItemPlaylist() with { CurrentIndex = 1 };
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        await viewModel.NavigatePreviousAsync();

        Assert.Equal([0, 1], viewModel.ComparisonState.DisplayedIndices);
        Assert.Equal(0, viewModel.PlaylistState.CurrentIndex);
    }

    [Fact]
    public async Task SetComparisonImageCountAsync_UpdatesSideBySideVisibleImages()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        await viewModel.SetComparisonImageCountAsync(4);

        Assert.Equal(4, viewModel.ComparisonState.ImageCount);
        Assert.Equal([0, 1, 2, 3], viewModel.ComparisonState.DisplayedIndices);
    }

    [Fact]
    public async Task SetComparisonImageCountAsync_WhenComparisonExceedsMemory_ReducesImageCount()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(
            playlist,
            comparisonService: new MemoryConstrainedComparisonService(maxImageCount: 2));
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        await viewModel.SetComparisonImageCountAsync(4);

        Assert.True(viewModel.ComparisonState.IsActive);
        Assert.Equal(2, viewModel.ComparisonState.ImageCount);
        Assert.Equal([0, 1], viewModel.ComparisonState.DisplayedIndices);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task CycleComparisonActiveImage_MovesActiveSelection()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);
        await viewModel.SetComparisonImageCountAsync(3);

        viewModel.CycleComparisonActiveImage();

        Assert.Equal(1, viewModel.ComparisonState.ActiveImageIndex);
        Assert.Equal(1, viewModel.ComparisonState.ActivePlaylistIndex);
    }

    [Fact]
    public async Task OpenActiveComparisonImageAsync_ExitsComparisonAndShowsSelectedImage()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);
        viewModel.SetActiveComparisonImage(1);

        await viewModel.OpenActiveComparisonImageAsync();

        Assert.False(viewModel.ComparisonState.IsActive);
        Assert.Equal(1, viewModel.PlaylistState.CurrentIndex);
        Assert.Equal(@"C:\Photos\b.png", viewModel.ViewerState.CurrentImage?.FilePath);
    }

    [Fact]
    public async Task EnterComparisonModeAsync_OverlayStartsWithTwoLayeredImagesAndDefaultOpacity()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterComparisonModeAsync(ComparisonLayout.Overlay);

        Assert.Equal(ComparisonLayout.Overlay, viewModel.ComparisonState.Layout);
        Assert.Equal([0, 1], viewModel.ComparisonState.DisplayedIndices);
        Assert.Equal(50, viewModel.ComparisonState.OverlayOpacity);
    }

    [Fact]
    public async Task AdjustOverlayOpacity_WhenOverlayActiveChangesByTenPercent()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.Overlay);

        viewModel.AdjustOverlayOpacity(10);

        Assert.Equal(60, viewModel.ComparisonState.OverlayOpacity);
    }

    [Fact]
    public async Task SwapOverlayLayers_WhenOverlayActiveReversesVisiblePair()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.Overlay);

        viewModel.SwapOverlayLayers();

        Assert.Equal([1, 0], viewModel.ComparisonState.DisplayedIndices);
        Assert.Equal(@"C:\Photos\b.png", viewModel.ComparisonState.DisplayedItems[0].FilePath);
        Assert.Equal(@"C:\Photos\a.png", viewModel.ComparisonState.DisplayedItems[1].FilePath);
    }

    [Fact]
    public async Task NavigateNextAsync_WhenOverlayActiveReplacesBothImagesWithNextPair()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.Overlay);

        await viewModel.NavigateNextAsync();

        Assert.Equal(ComparisonLayout.Overlay, viewModel.ComparisonState.Layout);
        Assert.Equal([1, 2], viewModel.ComparisonState.DisplayedIndices);
    }

    [Fact]
    public async Task ToggleComparisonSyncZoom_WhenComparisonActiveTogglesSyncIndicatorState()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        viewModel.ToggleComparisonSyncZoom();

        Assert.False(viewModel.ComparisonState.SyncZoomEnabled);
    }

    [Fact]
    public async Task EnterComparisonModeAsync_AppliesSavedSyncZoomPreference()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylistAndSettings(
            playlist,
            ViewerSettings.Default with { ComparisonSyncZoom = false });
        var viewModel = services.CreateViewModel();
        await viewModel.InitializeAsync();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        Assert.True(viewModel.ComparisonState.IsActive);
        Assert.False(viewModel.ComparisonState.SyncZoomEnabled);
    }

    [Fact]
    public async Task ToggleComparisonSyncZoomAsync_SavesPreference()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        await viewModel.ToggleComparisonSyncZoomAsync();

        Assert.False(viewModel.ComparisonState.SyncZoomEnabled);
        Assert.False(services.SettingsService.SavedSettings.Last().ComparisonSyncZoom);
    }

    [Fact]
    public async Task SetComparisonPan_WhenSyncZoomEnabledAppliesRelativePanToAllImages()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);
        viewModel.SetComparisonImageDimensions(0, new ImageDimensions(1000, 500));
        viewModel.SetComparisonImageDimensions(1, new ImageDimensions(2000, 1000));

        viewModel.SetComparisonPan(0, new ImagePoint(500, 250));

        Assert.Equal(new ImagePoint(500, 250), viewModel.ComparisonState.ImageViewports[0].PanOffset);
        Assert.Equal(new ImagePoint(1000, 500), viewModel.ComparisonState.ImageViewports[1].PanOffset);
    }

    [Fact]
    public async Task ZoomIn_WhenComparisonSyncZoomEnabledAppliesZoomToAllVisibleImages()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);

        viewModel.ZoomIn();

        Assert.All(viewModel.ComparisonState.ImageViewports, viewport => Assert.Equal(125, viewport.ZoomLevel));
    }

    [Fact]
    public async Task FitToWindow_WhenComparisonSyncZoomEnabledResetsAllVisibleImages()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);
        viewModel.ZoomIn();
        viewModel.SetComparisonPan(0, new ImagePoint(25, 25));

        viewModel.FitToWindow();

        Assert.All(viewModel.ComparisonState.ImageViewports, viewport =>
        {
            Assert.Equal(100, viewport.ZoomLevel);
            Assert.Equal(DisplayMode.FitToWindow, viewport.DisplayMode);
            Assert.Equal(new ImagePoint(0, 0), viewport.PanOffset);
        });
    }

    [Fact]
    public async Task NavigatePreviousAsync_AtFirstItem_RemainsAtFirstItem()
    {
        var playlist = CreateTwoItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.NavigatePreviousAsync();

        Assert.Equal(0, viewModel.PlaylistState.CurrentIndex);
        Assert.Equal(@"C:\Photos\a.png", viewModel.ViewerState.CurrentImage?.FilePath);
    }

    [Fact]
    public async Task NavigateLastAsync_DecodeFailure_LogsNavigationError()
    {
        var playlist = CreateTwoItemPlaylist();
        var services = TestServices.WithPlaylist(playlist, failDecodePath: @"C:\Photos\b.png");
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.NavigateLastAsync();

        Assert.Equal(ErrorCategory.NavigationError, services.ErrorLogService.LoggedErrors.Single().Category);
        Assert.Equal("Unable to load the requested image.", viewModel.ErrorMessage);
    }

    [Fact]
    public void ZoomCommands_UpdateViewerState()
    {
        var viewModel = TestServices.WithPlaylist(CreateTwoItemPlaylist()).CreateViewModel();

        viewModel.ZoomIn();
        Assert.Equal(125, viewModel.ViewerState.ZoomLevel);
        Assert.Equal(DisplayMode.Custom, viewModel.ViewerState.DisplayMode);

        viewModel.ZoomOut();
        Assert.Equal(100, viewModel.ViewerState.ZoomLevel);

        viewModel.ActualSize();
        Assert.Equal(DisplayMode.ActualSize, viewModel.ViewerState.DisplayMode);

        viewModel.FitToWindow();
        Assert.Equal(DisplayMode.FitToWindow, viewModel.ViewerState.DisplayMode);
    }

    [Fact]
    public async Task ToggleThemeAsync_TogglesBetweenDarkAndLightAndSaves()
    {
        var settings = ViewerSettings.Default with { Theme = ViewerTheme.Dark };
        var services = TestServices.WithSettings(settings);
        var viewModel = services.CreateViewModel();
        await viewModel.InitializeAsync();

        await viewModel.ToggleThemeAsync();

        Assert.Equal(ViewerTheme.Light, viewModel.Settings.Theme);
        Assert.Equal(ViewerTheme.Light, services.SettingsService.SavedSettings.Single().Theme);
    }

    [Fact]
    public void ShowCommandPalette_LoadsCommandsFromImplementedPhases()
    {
        var viewModel = TestServices.WithPlaylist(CreateFourItemPlaylist()).CreateViewModel();

        viewModel.ShowCommandPalette();

        Assert.True(viewModel.CommandPaletteState.IsVisible);
        Assert.Contains(viewModel.CommandPaletteState.AllCommands, command => command.Category == "File");
        Assert.Contains(viewModel.CommandPaletteState.AllCommands, command => command.Category == "View");
        Assert.Contains(viewModel.CommandPaletteState.AllCommands, command => command.Category == "Image");
        Assert.Contains(viewModel.CommandPaletteState.AllCommands, command => command.Category == "Navigation");
        Assert.Contains(viewModel.CommandPaletteState.AllCommands, command => command.Category == "Comparison");
        Assert.Contains(viewModel.CommandPaletteState.AllCommands, command => command.Category == "Layout");
        Assert.Contains(viewModel.CommandPaletteState.AllCommands, command => command.Category == "Slideshow");
        Assert.Contains(viewModel.CommandPaletteState.AllCommands, command => command.Category == "Animation");
        Assert.All(viewModel.CommandPaletteState.AllCommands, command => Assert.False(string.IsNullOrWhiteSpace(command.KeyboardShortcut)));
    }

    [Fact]
    public void ShowCommandPalette_ExposesPhase3IntegrationShortcuts()
    {
        var viewModel = TestServices.WithPlaylist(CreateFourItemPlaylist()).CreateViewModel();

        viewModel.ShowCommandPalette();

        AssertCommandShortcut(viewModel, "view.command-palette", "Ctrl+Shift+P");
        AssertCommandShortcut(viewModel, "navigation.search", "Ctrl+F");
        AssertCommandShortcut(viewModel, "slideshow.toggle", "F5");
        AssertCommandShortcut(viewModel, "slideshow.pause", "Space");
        AssertCommandShortcut(viewModel, "slideshow.interval-up", "+");
        AssertCommandShortcut(viewModel, "slideshow.interval-down", "-");
        AssertCommandShortcut(viewModel, "animation.toggle-playback", "P");
        AssertCommandShortcut(viewModel, "animation.next-frame", ".");
        AssertCommandShortcut(viewModel, "animation.previous-frame", ",");
        AssertCommandShortcut(viewModel, "layout.filmstrip", "Ctrl+Shift+F");
    }

    [Fact]
    public async Task CommandPalette_ExecuteSelectedCommandRunsCommandAndClosesPalette()
    {
        var viewModel = TestServices.WithPlaylist(CreateFourItemPlaylist()).CreateViewModel();
        viewModel.ShowCommandPalette();
        viewModel.CommandPaletteState.SelectCommandById("view.fullscreen");

        var result = await viewModel.ExecuteSelectedCommandPaletteCommandAsync();

        Assert.True(result.IsSuccess);
        Assert.True(viewModel.ViewerState.IsFullscreen);
        Assert.False(viewModel.CommandPaletteState.IsVisible);
    }

    [Fact]
    public async Task SetSlideshowTransitionAsync_UpdatesTransitionPreferenceAndSaves()
    {
        var settings = ViewerSettings.Default with { SlideshowTransition = TransitionEffect.Fade };
        var services = TestServices.WithSettings(settings);
        var viewModel = services.CreateViewModel();
        await viewModel.InitializeAsync();

        await viewModel.SetSlideshowTransitionAsync(TransitionEffect.Zoom);

        Assert.Equal(TransitionEffect.Zoom, viewModel.Settings.SlideshowTransition);
        Assert.Equal(TransitionEffect.Zoom, services.SettingsService.SavedSettings.Single().SlideshowTransition);
    }

    [Fact]
    public void ToggleFullscreen_UpdatesViewerState()
    {
        var viewModel = TestServices.WithPlaylist(CreateTwoItemPlaylist()).CreateViewModel();

        viewModel.ToggleFullscreen();

        Assert.True(viewModel.ViewerState.IsFullscreen);
    }

    [Fact]
    public async Task InitializeAsync_LoadsSettings()
    {
        var settings = ViewerSettings.Default with { Theme = ViewerTheme.Light, WindowWidth = 1400 };
        var services = TestServices.WithSettings(settings);
        var viewModel = services.CreateViewModel();

        await viewModel.InitializeAsync();

        Assert.Equal(settings, viewModel.Settings);
    }

    [Fact]
    public async Task InitializeAsync_SettingsLoadFailure_FallsBackToDefaultsAndLogsStructuredWarning()
    {
        var settingsPath = @"C:\Users\User\AppData\Local\spitfire.img\settings.json";
        var settingsError = ViewerError.SettingsPersistenceError(
            "LoadSettings",
            settingsPath,
            new InvalidDataException("bad settings"));
        var services = TestServices.WithSettingsLoadResult(Result<ViewerSettings>.Failure(settingsError));
        var viewModel = services.CreateViewModel();

        await viewModel.InitializeAsync();

        Assert.Equal(ViewerSettings.Default, viewModel.Settings);
        var loggedError = Assert.Single(services.ErrorLogService.LoggedErrors);
        Assert.Equal(ErrorSeverity.Warning, loggedError.Severity);
        Assert.Equal(ErrorCategory.SettingsPersistenceError, loggedError.Category);
        Assert.Equal("LoadSettings", loggedError.Operation);
        Assert.Equal(settingsPath, loggedError.FilePath);
        Assert.Equal("Unable to load settings. Default preferences were restored.", loggedError.UserMessage);
    }

    [Fact]
    public async Task SaveWindowStateAsync_PersistsWindowBounds()
    {
        var services = TestServices.WithSettings(ViewerSettings.Default);
        var viewModel = services.CreateViewModel();

        await viewModel.SaveWindowStateAsync(1600, 900, 40, 50);

        var saved = services.SettingsService.SavedSettings.Single();
        Assert.Equal(1600, saved.WindowWidth);
        Assert.Equal(900, saved.WindowHeight);
        Assert.Equal(40, saved.WindowX);
        Assert.Equal(50, saved.WindowY);
    }

    [Fact]
    public async Task CancelLoading_CancelsLongRunningOpenOperation()
    {
        var filePath = @"C:\Photos\slow.png";
        var decoder = new BlockingImageDecodeService();
        var services = TestServices.WithDecoder(filePath, decoder);
        var viewModel = services.CreateViewModel();

        var openTask = viewModel.OpenFileAsync(filePath);
        await decoder.Started.Task;

        viewModel.CancelLoading();
        await openTask;

        Assert.True(decoder.WasCanceled);
        Assert.False(viewModel.IsLoading);
        Assert.False(viewModel.IsCancelAvailable);
    }

    [Fact]
    public async Task OpenFileAsync_Success_AddsFileToRecentFilesAndSearchPlaylist()
    {
        var image = new DecodedImage(@"C:\Photos\b.png", new ImageMetadata(320, 240, 100));
        var playlist = new Playlist(
            [new ImageItem(@"C:\Photos\a.png"), new ImageItem(@"C:\Photos\b.png")],
            1,
            @"C:\Photos");
        var services = TestServices.Success(image, playlist);
        var viewModel = services.CreateViewModel();

        await viewModel.OpenFileAsync(@"C:\Photos\b.png");

        Assert.Equal(@"C:\Photos\b.png", services.RecentFilesService.RecentFiles[0].FilePath);
        Assert.Equal(2, viewModel.SearchState.ResultCount);
    }

    [Fact]
    public async Task InitializeAsync_LoadsRecentFilesIntoViewModel()
    {
        var services = TestServices.WithPlaylist(CreateTwoItemPlaylist());
        services.RecentFilesService.RecentFiles.Add(new RecentFile(@"C:\Photos\a.png", DateTimeOffset.UtcNow));
        var viewModel = services.CreateViewModel();

        await viewModel.InitializeAsync();

        Assert.Single(viewModel.RecentFiles);
        Assert.Equal(@"C:\Photos\a.png", viewModel.RecentFiles[0].FilePath);
    }

    [Fact]
    public async Task InitializeAsync_AppliesPhase2ThumbnailSettings()
    {
        var settings = ViewerSettings.Default with
        {
            ThumbnailStripVisible = true,
            ThumbnailStripPosition = ThumbnailPosition.Top
        };
        var services = TestServices.WithSettings(settings);
        var viewModel = services.CreateViewModel();

        await viewModel.InitializeAsync();

        Assert.True(viewModel.ThumbnailState.IsStripVisible);
        Assert.Equal(ThumbnailPosition.Top, viewModel.ThumbnailState.StripPosition);
    }

    [Fact]
    public async Task OpenRecentFileAsync_MissingFile_RemovesRecentAndShowsFileAccessError()
    {
        var services = TestServices.WithPlaylist(CreateTwoItemPlaylist());
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        services.RecentFilesService.RecentFiles.Add(new RecentFile(missing, DateTimeOffset.UtcNow));
        var viewModel = services.CreateViewModel();
        await viewModel.InitializeAsync();

        await viewModel.OpenRecentFileAsync(missing);

        Assert.Empty(viewModel.RecentFiles);
        Assert.Equal(ErrorCategory.FileAccessError, services.ErrorLogService.LoggedErrors.Single().Category);
    }

    [Fact]
    public void SearchCommands_FilterAndClearSearchState()
    {
        var viewModel = TestServices.WithPlaylist(CreateTwoItemPlaylist()).CreateViewModel();
        viewModel.SearchState.SetPlaylist(CreateTwoItemPlaylist().Items);
        var searchRequestedCount = 0;
        var searchHiddenCount = 0;
        viewModel.SearchRequested += (_, _) => searchRequestedCount++;
        viewModel.SearchHidden += (_, _) => searchHiddenCount++;

        viewModel.ShowSearch();
        viewModel.SetSearchQuery("b");

        Assert.Equal(1, searchRequestedCount);
        Assert.True(viewModel.SearchState.IsSearchActive);
        Assert.Single(viewModel.SearchState.FilteredPlaylist);

        viewModel.HideSearch();
        Assert.Equal(1, searchHiddenCount);
        Assert.False(viewModel.SearchState.IsSearchActive);
    }

    [Fact]
    public async Task ToggleMetadataPanelAsync_ExtractsMetadataAndTogglesVisibility()
    {
        var viewModel = TestServices.WithPlaylist(CreateTwoItemPlaylist()).CreateViewModel();
        viewModel.ViewerState.SetImage(new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 100, 10)));

        await viewModel.ToggleMetadataPanelAsync();

        Assert.True(viewModel.MetadataState.IsVisible);

        await viewModel.ToggleMetadataPanelAsync();
        Assert.False(viewModel.MetadataState.IsVisible);
    }

    [Fact]
    public async Task ToggleMetadataPanelAsync_ReusesCachedMetadataForSameImage()
    {
        var metadataService = new RecordingMetadataService();
        var services = TestServices.WithPlaylist(CreateTwoItemPlaylist(), metadataService: metadataService);
        var viewModel = services.CreateViewModel();
        viewModel.ViewerState.SetImage(new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 100, 10)));

        await viewModel.ToggleMetadataPanelAsync();
        await viewModel.ToggleMetadataPanelAsync();
        await viewModel.ToggleMetadataPanelAsync();

        Assert.Equal(1, metadataService.ExtractCount);
    }

    [Fact]
    public async Task TransformCommands_ApplyUndoAndRedoTransforms()
    {
        var viewModel = TestServices.WithPlaylist(CreateTwoItemPlaylist()).CreateViewModel();
        viewModel.ViewerState.SetImage(new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 200, 10)));

        await viewModel.ApplyTransformAsync(TransformOperation.RotateRight);
        viewModel.UndoTransform();
        viewModel.RedoTransform();

        Assert.True(viewModel.TransformState.HasUnsavedChanges);
        Assert.Equal([TransformOperation.RotateRight], viewModel.TransformState.PendingTransforms);
    }

    [Fact]
    public async Task ApplyTransformAsync_UpdatesDisplayedMetadataFromTransformResult()
    {
        var transformService = new RecordingTransformService();
        var services = TestServices.WithPlaylist(CreateTwoItemPlaylist(), transformService: transformService);
        var viewModel = services.CreateViewModel();
        viewModel.ViewerState.SetImage(new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 200, 10)));

        await viewModel.ApplyTransformAsync(TransformOperation.RotateRight);

        Assert.Equal("200 × 100", viewModel.ViewerState.Metadata?.FormattedDimensions);
    }

    [Fact]
    public async Task TransformUndoRedo_UpdatesDisplayedPreviewMetadata()
    {
        var viewModel = TestServices.WithPlaylist(CreateTwoItemPlaylist()).CreateViewModel();
        viewModel.ViewerState.SetImage(new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 200, 10)));

        await viewModel.ApplyTransformAsync(TransformOperation.RotateRight);
        viewModel.UndoTransform();

        Assert.Equal("100 × 200", viewModel.ViewerState.Metadata?.FormattedDimensions);

        viewModel.RedoTransform();
        Assert.Equal("200 × 100", viewModel.ViewerState.Metadata?.FormattedDimensions);
    }

    [Fact]
    public async Task SaveTransformsAsync_WithPendingTransforms_SavesAndClearsPendingState()
    {
        var transformService = new RecordingTransformService();
        var services = TestServices.WithPlaylist(CreateTwoItemPlaylist(), transformService: transformService);
        var viewModel = services.CreateViewModel();
        viewModel.ViewerState.SetImage(new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 200, 10)));
        await viewModel.ApplyTransformAsync(TransformOperation.RotateRight);

        await viewModel.SaveTransformsAsync();

        Assert.Equal(@"C:\Photos\a.png", transformService.SavedFilePath);
        Assert.Equal([TransformOperation.RotateRight], transformService.SavedOperations);
        Assert.False(viewModel.TransformState.HasUnsavedChanges);
    }

    [Fact]
    public async Task SaveTransformsAsync_Failure_LogsErrorAndKeepsPendingState()
    {
        var error = ViewerError.TransformSaveError("SaveTransforms", @"C:\Photos\a.png");
        var transformService = new RecordingTransformService(saveResult: Result.Failure(error));
        var services = TestServices.WithPlaylist(CreateTwoItemPlaylist(), transformService: transformService);
        var viewModel = services.CreateViewModel();
        viewModel.ViewerState.SetImage(new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 200, 10)));
        await viewModel.ApplyTransformAsync(TransformOperation.RotateRight);

        await viewModel.SaveTransformsAsync();

        Assert.True(viewModel.TransformState.HasUnsavedChanges);
        Assert.Equal(error.UserMessage, viewModel.ErrorMessage);
        Assert.Equal(error, services.ErrorLogService.LoggedErrors.Single());
    }

    [Fact]
    public async Task NavigateNextAsync_DiscardsUnsavedTransformsWhenImageChanges()
    {
        var viewModel = TestServices.WithPlaylist(CreateTwoItemPlaylist()).CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(CreateTwoItemPlaylist());
        viewModel.ViewerState.SetImage(new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 200, 10)));
        await viewModel.ApplyTransformAsync(TransformOperation.RotateRight);

        await viewModel.NavigateNextAsync();

        Assert.False(viewModel.TransformState.HasUnsavedChanges);
    }

    [Fact]
    public async Task ThumbnailCommands_ToggleAndPersistPositionInStateAndSettings()
    {
        var services = TestServices.WithPlaylist(CreateTwoItemPlaylist());
        var viewModel = services.CreateViewModel();

        await viewModel.ToggleThumbnailStrip();
        await viewModel.SetThumbnailPosition(ThumbnailPosition.Top);

        Assert.True(viewModel.ThumbnailState.IsStripVisible);
        Assert.Equal(ThumbnailPosition.Top, viewModel.ThumbnailState.StripPosition);
        Assert.Equal(ThumbnailPosition.Top, services.SettingsService.SavedSettings.Last().ThumbnailStripPosition);
        Assert.True(services.SettingsService.SavedSettings.Last().ThumbnailStripVisible);
    }

    [Fact]
    public async Task ExportCurrentImageAsync_UsesCurrentImageAndExportService()
    {
        var services = TestServices.WithPlaylist(CreateTwoItemPlaylist());
        var viewModel = services.CreateViewModel();
        viewModel.ViewerState.SetImage(new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 100, 10)));

        var exported = await viewModel.ExportCurrentImageAsync(@"C:\Export\a.jpg", ImageExportFormat.Jpeg);

        Assert.True(exported);
        Assert.Equal(@"C:\Photos\a.png", services.ExportService.SourcePath);
        Assert.Equal(@"C:\Export\a.jpg", services.ExportService.DestinationPath);
        Assert.Equal(ImageExportFormat.Jpeg, services.ExportService.Options?.TargetFormat);
    }

    [Fact]
    public async Task ExportCurrentImageAsync_Failure_ReturnsFalseAndLogsError()
    {
        var error = ViewerError.ExportError("ExportImage", @"C:\Export\a.jpg");
        var exportService = new RecordingExportService(Result.Failure(error));
        var services = TestServices.WithPlaylist(CreateTwoItemPlaylist(), exportService: exportService);
        var viewModel = services.CreateViewModel();
        viewModel.ViewerState.SetImage(new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 100, 10)));

        var exported = await viewModel.ExportCurrentImageAsync(@"C:\Export\a.jpg", ImageExportFormat.Jpeg);

        Assert.False(exported);
        Assert.Equal(error.UserMessage, viewModel.ErrorMessage);
        Assert.Equal(error, services.ErrorLogService.LoggedErrors.Single());
    }

    private static Playlist CreateTwoItemPlaylist()
    {
        return new Playlist(
            [new ImageItem(@"C:\Photos\a.png"), new ImageItem(@"C:\Photos\b.png")],
            0,
            @"C:\Photos");
    }

    private static Playlist CreateThreeItemPlaylist()
    {
        return new Playlist(
            [
                new ImageItem(@"C:\Photos\a.png"),
                new ImageItem(@"C:\Photos\b.png"),
                new ImageItem(@"C:\Photos\c.png")
            ],
            0,
            @"C:\Photos");
    }

    private static Playlist CreateFourItemPlaylist()
    {
        return new Playlist(
            [
                new ImageItem(@"C:\Photos\a.png"),
                new ImageItem(@"C:\Photos\b.png"),
                new ImageItem(@"C:\Photos\c.png"),
                new ImageItem(@"C:\Photos\d.png")
            ],
            0,
            @"C:\Photos");
    }

    private static Playlist CreateTwelveItemPlaylist(int currentIndex)
    {
        return CreateNumberedPlaylist(12, currentIndex);
    }

    private static Playlist CreateNumberedPlaylist(int count, int currentIndex)
    {
        return new Playlist(
            Enumerable.Range(0, count)
                .Select(index => new ImageItem($@"C:\Photos\image-{index:00}.png"))
                .ToArray(),
            currentIndex,
            @"C:\Photos");
    }

    private static AnimatedGif CreateAnimatedGif(string filePath)
    {
        return new AnimatedGif(
            filePath,
            [
                new AnimationFrame([0x89, 0x50, 0x4E, 0x47], 2, 2, TimeSpan.FromMilliseconds(100)),
                new AnimationFrame([0x89, 0x50, 0x4E, 0x47], 2, 2, TimeSpan.FromMilliseconds(200))
            ]);
    }

    private static void AssertCommandShortcut(
        MainViewModel viewModel,
        string commandId,
        string expectedShortcut)
    {
        var command = Assert.Single(
            viewModel.CommandPaletteState.AllCommands,
            command => command.Id == commandId);
        Assert.Equal(expectedShortcut, command.KeyboardShortcut);
    }

    private sealed class TestServices
    {
        private TestServices(
            IFileOpenService fileOpenService,
            IImageDecodeService imageDecodeService,
            IFolderPlaylistService folderPlaylistService,
            INavigationService navigationService,
            StubSettingsService settingsService,
            RecordingErrorLogService errorLogService,
            RecordingRecentFilesService? recentFilesService = null,
            RecordingMetadataService? metadataService = null,
            RecordingExportService? exportService = null,
            RecordingTransformService? transformService = null,
            RecordingAnimationService? animationService = null,
            RecordingAnimationPlaybackTimer? animationPlaybackTimer = null,
            RecordingSlideshowTimer? slideshowTimer = null,
            RecordingCodecDetectionService? codecDetectionService = null,
            IComparisonService? comparisonService = null,
            ILayoutService? layoutService = null)
        {
            FileOpenService = fileOpenService;
            ImageDecodeService = imageDecodeService;
            FolderPlaylistService = folderPlaylistService;
            NavigationService = navigationService;
            SettingsService = settingsService;
            ErrorLogService = errorLogService;
            RecentFilesService = recentFilesService ?? new RecordingRecentFilesService();
            MetadataService = metadataService ?? new RecordingMetadataService();
            ExportService = exportService ?? new RecordingExportService();
            TransformService = transformService ?? new RecordingTransformService();
            AnimationService = animationService ?? new RecordingAnimationService(
                Result<AnimatedGif>.Success(new AnimatedGif(string.Empty, [])));
            AnimationPlaybackTimer = animationPlaybackTimer ?? new RecordingAnimationPlaybackTimer();
            SlideshowTimer = slideshowTimer ?? new RecordingSlideshowTimer();
            CodecDetectionService = codecDetectionService ?? new RecordingCodecDetectionService(heicSupported: false);
            ComparisonService = comparisonService;
            LayoutService = layoutService;
        }

        public IFileOpenService FileOpenService { get; }

        public IImageDecodeService ImageDecodeService { get; }

        public IFolderPlaylistService FolderPlaylistService { get; }

        public INavigationService NavigationService { get; }

        public StubSettingsService SettingsService { get; }

        public RecordingErrorLogService ErrorLogService { get; }

        public RecordingRecentFilesService RecentFilesService { get; }

        public RecordingMetadataService MetadataService { get; }

        public RecordingExportService ExportService { get; }

        public RecordingTransformService TransformService { get; }

        public RecordingAnimationService AnimationService { get; }

        public RecordingAnimationPlaybackTimer AnimationPlaybackTimer { get; }

        public RecordingSlideshowTimer SlideshowTimer { get; }

        public RecordingCodecDetectionService CodecDetectionService { get; }

        public IComparisonService? ComparisonService { get; }

        public ILayoutService? LayoutService { get; }

        public static TestServices Success(
            DecodedImage image,
            Playlist playlist,
            RecordingAnimationService? animationService = null,
            RecordingAnimationPlaybackTimer? animationPlaybackTimer = null,
            ViewerSettings? settings = null)
        {
            var decoder = new StubImageDecodeService(Result<DecodedImage>.Success(image));
            return new TestServices(
                new StubFileOpenService(Result<string>.Success(image.FilePath)),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Success(playlist)),
                new NavigationService(decoder),
                new StubSettingsService(settings ?? ViewerSettings.Default),
                new RecordingErrorLogService(),
                animationService: animationService,
                animationPlaybackTimer: animationPlaybackTimer);
        }

        public static TestServices WithPlaylist(
            Playlist playlist,
            string? failDecodePath = null,
            RecordingTransformService? transformService = null,
            RecordingExportService? exportService = null,
            RecordingMetadataService? metadataService = null,
            IComparisonService? comparisonService = null,
            ILayoutService? layoutService = null,
            IImageDecodeService? imageDecodeService = null)
        {
            var decoder = imageDecodeService ?? new PathImageDecodeService(failDecodePath);
            return new TestServices(
                new StubFileOpenService(Result<string>.Success(playlist.CurrentItem?.FilePath ?? string.Empty)),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Success(playlist)),
                new NavigationService(decoder),
                new StubSettingsService(),
                new RecordingErrorLogService(),
                metadataService: metadataService,
                exportService: exportService,
                transformService: transformService,
                comparisonService: comparisonService,
                layoutService: layoutService);
        }

        public static TestServices WithPlaylistAndSettings(
            Playlist playlist,
            ViewerSettings settings,
            ILayoutService? layoutService = null)
        {
            var decoder = new PathImageDecodeService();
            return new TestServices(
                new StubFileOpenService(Result<string>.Success(playlist.CurrentItem?.FilePath ?? string.Empty)),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Success(playlist)),
                new NavigationService(decoder),
                new StubSettingsService(settings),
                new RecordingErrorLogService(),
                layoutService: layoutService);
        }

        public static TestServices WithSettings(ViewerSettings settings)
        {
            var decoder = new PathImageDecodeService();
            return new TestServices(
                new StubFileOpenService(Result<string>.Success(@"C:\Photos\a.png")),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Success(CreateTwoItemPlaylist())),
                new NavigationService(decoder),
                new StubSettingsService(settings),
                new RecordingErrorLogService());
        }

        public static TestServices WithSettingsLoadResult(Result<ViewerSettings> settingsResult)
        {
            var decoder = new PathImageDecodeService();
            return new TestServices(
                new StubFileOpenService(Result<string>.Success(@"C:\Photos\a.png")),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Success(CreateTwoItemPlaylist())),
                new NavigationService(decoder),
                new StubSettingsService(settingsResult),
                new RecordingErrorLogService());
        }

        public static TestServices WithFileValidationFailure(ViewerError error)
        {
            var decoder = new StubImageDecodeService(Result<DecodedImage>.Failure(ViewerError.DecodeError("DecodeImage", null)));
            return new TestServices(
                new StubFileOpenService(Result<string>.Failure(error)),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Failure(ViewerError.PlaylistScanError("BuildPlaylist", null))),
                new NavigationService(decoder),
                new StubSettingsService(),
                new RecordingErrorLogService());
        }

        public static TestServices WithDecoder(
            string filePath,
            IImageDecodeService decoder,
            RecordingCodecDetectionService? codecDetectionService = null)
        {
            var playlist = new Playlist([new ImageItem(filePath)], 0, Path.GetDirectoryName(filePath) ?? string.Empty);
            return new TestServices(
                new StubFileOpenService(Result<string>.Success(filePath)),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Success(playlist)),
                new NavigationService(decoder),
                new StubSettingsService(),
                new RecordingErrorLogService(),
                codecDetectionService: codecDetectionService);
        }

        public MainViewModel CreateViewModel()
        {
            return new MainViewModel(
                FileOpenService,
                ImageDecodeService,
                FolderPlaylistService,
                NavigationService,
                SettingsService,
                ErrorLogService,
                new SearchService(),
                new ThumbnailService(ImageDecodeService),
                RecentFilesService,
                ExportService,
                TransformService,
                MetadataService,
                animationService: AnimationService,
                animationPlaybackTimer: AnimationPlaybackTimer,
                slideshowTimer: SlideshowTimer,
                codecDetectionService: CodecDetectionService,
                comparisonService: ComparisonService,
                layoutService: LayoutService);
        }
    }

    private sealed class StubFileOpenService : IFileOpenService
    {
        private readonly Result<string> _result;

        public StubFileOpenService(Result<string> result)
        {
            _result = result;
        }

        public Result<string> ValidateFilePath(string filePath) => _result;
    }

    private sealed class StubImageDecodeService : IImageDecodeService
    {
        private readonly Result<DecodedImage> _result;

        public StubImageDecodeService(Result<DecodedImage> result)
        {
            _result = result;
        }

        public Task<Result<DecodedImage>> DecodeImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }

        public bool IsSupportedFormat(string extension) => true;

        public bool IsDeferredFormat(string extension) => false;

        public Task<Result<int>> GetFrameCountAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }

    private sealed class StubFolderPlaylistService : IFolderPlaylistService
    {
        private readonly Result<Playlist> _result;

        public StubFolderPlaylistService(Result<Playlist> result)
        {
            _result = result;
        }

        public Task<Result<Playlist>> BuildPlaylistAsync(string openedFilePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class StubSettingsService : ISettingsService
    {
        private readonly Result<ViewerSettings> _loadResult;

        public StubSettingsService()
            : this(ViewerSettings.Default)
        {
        }

        public StubSettingsService(ViewerSettings settings)
            : this(Result<ViewerSettings>.Success(settings))
        {
        }

        public StubSettingsService(Result<ViewerSettings> loadResult)
        {
            _loadResult = loadResult;
        }

        public List<ViewerSettings> SavedSettings { get; } = [];

        public Task<Result<ViewerSettings>> LoadSettingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_loadResult);
        }

        public Task<Result> SaveSettingsAsync(ViewerSettings settings, CancellationToken cancellationToken = default)
        {
            SavedSettings.Add(settings);
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class PathImageDecodeService : IImageDecodeService
    {
        private readonly string? _failPath;

        public PathImageDecodeService(string? failPath = null)
        {
            _failPath = failPath;
        }

        public Task<Result<DecodedImage>> DecodeImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.Equals(filePath, _failPath, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Result<DecodedImage>.Failure(ViewerError.DecodeError("DecodeImage", filePath)));
            }

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

    private sealed class CountingPathImageDecodeService : IImageDecodeService
    {
        public List<string> DecodedPaths { get; } = [];

        public Task<Result<DecodedImage>> DecodeImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            DecodedPaths.Add(filePath);
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

    private sealed class BlockingImageDecodeService : IImageDecodeService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool WasCanceled { get; private set; }

        public async Task<Result<DecodedImage>> DecodeImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                WasCanceled = true;
                return Result<DecodedImage>.Failure(ViewerError.DecodeError("DecodeImage", filePath));
            }

            return Result<DecodedImage>.Success(new DecodedImage(filePath, new ImageMetadata(100, 100, 10)));
        }

        public bool IsSupportedFormat(string extension) => true;

        public bool IsDeferredFormat(string extension) => false;

        public Task<Result<int>> GetFrameCountAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }

    private sealed class RecordingErrorLogService : IErrorLogService
    {
        public List<ViewerError> LoggedErrors { get; } = [];

        public Task<Result> LogErrorAsync(ViewerError error, CancellationToken cancellationToken = default)
        {
            LoggedErrors.Add(error);
            return Task.FromResult(Result.Success());
        }

        public Task<Result> CleanupOldLogsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class RecordingRecentFilesService : IRecentFilesService
    {
        public List<RecentFile> RecentFiles { get; } = [];

        public Task<Result<IReadOnlyList<RecentFile>>> LoadRecentFilesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<IReadOnlyList<RecentFile>>.Success(RecentFiles));
        }

        public Task<Result<IReadOnlyList<RecentFile>>> AddRecentFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            RecentFiles.Insert(0, new RecentFile(filePath, DateTimeOffset.UtcNow));
            return Task.FromResult(Result<IReadOnlyList<RecentFile>>.Success(RecentFiles));
        }

        public Task<Result<IReadOnlyList<RecentFile>>> RemoveRecentFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            RecentFiles.RemoveAll(file => string.Equals(file.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(Result<IReadOnlyList<RecentFile>>.Success(RecentFiles));
        }

        public Task<Result> ClearRecentFilesAsync(CancellationToken cancellationToken = default)
        {
            RecentFiles.Clear();
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class RecordingMetadataService : IMetadataService
    {
        public int ExtractCount { get; private set; }

        public Task<Result<ExifMetadata>> ExtractExifAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ExtractCount++;
            return Task.FromResult(Result<ExifMetadata>.Success(new ExifMetadata(CameraMake: "Test")));
        }
    }

    private sealed class RecordingExportService : IExportService
    {
        private readonly Result _result;

        public RecordingExportService(Result? result = null)
        {
            _result = result ?? Result.Success();
        }

        public string? SourcePath { get; private set; }

        public string? DestinationPath { get; private set; }

        public ExportOptions? Options { get; private set; }

        public Task<Result> ExportImageAsync(
            string sourceFilePath,
            string destinationFilePath,
            ExportOptions options,
            CancellationToken cancellationToken = default)
        {
            SourcePath = sourceFilePath;
            DestinationPath = destinationFilePath;
            Options = options;
            return Task.FromResult(_result);
        }
    }

    private sealed class RecordingTransformService : ITransformService
    {
        private readonly Result _saveResult;

        public RecordingTransformService(Result? saveResult = null)
        {
            _saveResult = saveResult ?? Result.Success();
        }

        public string? SavedFilePath { get; private set; }

        public IReadOnlyList<TransformOperation>? SavedOperations { get; private set; }

        public Task<Result<TransformedImage>> ApplyTransformAsync(
            DecodedImage sourceImage,
            IReadOnlyList<TransformOperation> operations,
            CancellationToken cancellationToken = default)
        {
            var width = sourceImage.Metadata.PixelWidth;
            var height = sourceImage.Metadata.PixelHeight;

            foreach (var operation in operations)
            {
                if (operation is TransformOperation.RotateLeft or TransformOperation.RotateRight)
                {
                    (width, height) = (height, width);
                }
            }

            return Task.FromResult(Result<TransformedImage>.Success(new TransformedImage(
                sourceImage,
                sourceImage.Metadata with { PixelWidth = width, PixelHeight = height },
                operations.ToArray())));
        }

        public Task<Result> SaveTransformsAsync(
            string filePath,
            IReadOnlyList<TransformOperation> operations,
            CancellationToken cancellationToken = default)
        {
            SavedFilePath = filePath;
            SavedOperations = operations.ToArray();
            return Task.FromResult(_saveResult);
        }
    }

    private sealed class RecordingAnimationService : IAnimationService
    {
        private readonly Result<AnimatedGif> _result;

        public RecordingAnimationService(Result<AnimatedGif> result)
        {
            _result = result;
        }

        public List<string> LoadedPaths { get; } = [];

        public Task<Result<AnimatedGif>> LoadAnimatedGifAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            LoadedPaths.Add(filePath);
            return Task.FromResult(_result);
        }
    }

    private sealed class RecordingAnimationPlaybackTimer : IAnimationPlaybackTimer
    {
        public event EventHandler? Tick;

        public List<TimeSpan> StartedIntervals { get; } = [];

        public bool IsStopped { get; private set; } = true;

        public void Start(TimeSpan interval)
        {
            StartedIntervals.Add(interval);
            IsStopped = false;
        }

        public void Stop()
        {
            IsStopped = true;
        }

        public void RaiseTick()
        {
            Tick?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class RecordingSlideshowTimer : ISlideshowTimer
    {
        public event EventHandler? Tick;

        public List<TimeSpan> StartedIntervals { get; } = [];

        public int StopCount { get; private set; }

        public void Start(TimeSpan interval)
        {
            StartedIntervals.Add(interval);
        }

        public void Stop()
        {
            StopCount++;
        }

        public async Task RaiseTickAsync()
        {
            Tick?.Invoke(this, EventArgs.Empty);
            await Task.Yield();
        }
    }

    private sealed class MemoryConstrainedComparisonService : IComparisonService
    {
        private readonly ComparisonService _inner = new();
        private readonly int _maxImageCount;

        public MemoryConstrainedComparisonService(int maxImageCount)
        {
            _maxImageCount = maxImageCount;
        }

        public Result<IReadOnlyList<int>> CalculateComparisonIndices(
            int currentIndex,
            int imageCount,
            int playlistSize)
        {
            if (imageCount > _maxImageCount)
            {
                return Result<IReadOnlyList<int>>.Failure(
                    ViewerError.ComparisonError(
                        "CalculateComparisonIndices",
                        "Not enough memory to load that many comparison images.",
                        exception: new OutOfMemoryException()));
            }

            return _inner.CalculateComparisonIndices(currentIndex, imageCount, playlistSize);
        }

        public Result<ComparisonLayoutPlan> CalculateSideBySideLayout(int imageCount)
        {
            return _inner.CalculateSideBySideLayout(imageCount);
        }

        public Result<RelativePosition> ConvertToRelativePosition(
            ImagePoint absolutePosition,
            ImageDimensions imageSize)
        {
            return _inner.ConvertToRelativePosition(absolutePosition, imageSize);
        }

        public Result<IReadOnlyList<ComparisonImageViewport>> SynchronizeZoom(
            IReadOnlyList<ComparisonImageViewport> viewports,
            int sourceIndex,
            double zoomLevel,
            ImagePoint panOffset)
        {
            return _inner.SynchronizeZoom(viewports, sourceIndex, zoomLevel, panOffset);
        }
    }

    private sealed class MemoryConstrainedLayoutService : ILayoutService
    {
        private readonly LayoutService _inner = new();
        private readonly int _maxGridDensity;

        public MemoryConstrainedLayoutService(int maxGridDensity)
        {
            _maxGridDensity = maxGridDensity;
        }

        public Result<GridLayoutPlan> CalculateGridLayout(
            int rows,
            int columns,
            ImageDimensions canvasSize)
        {
            if (rows > _maxGridDensity || columns > _maxGridDensity)
            {
                return Result<GridLayoutPlan>.Failure(
                    ViewerError.LayoutError(
                        "CalculateGridLayout",
                        "Not enough memory to display that many grid images.",
                        new OutOfMemoryException()));
            }

            return _inner.CalculateGridLayout(rows, columns, canvasSize);
        }

        public Result<IReadOnlyList<GridImage>> LoadGridImages(
            PlaylistState playlist,
            int startIndex,
            GridLayoutPlan layout)
        {
            return _inner.LoadGridImages(playlist, startIndex, layout);
        }

        public Result<FilmstripData> LoadFilmstripData(
            PlaylistState playlist,
            int focusIndex,
            int thumbnailCount)
        {
            return _inner.LoadFilmstripData(playlist, focusIndex, thumbnailCount);
        }

        public Result<int> CalculateGridStartIndex(
            int currentStartIndex,
            int offset,
            int playlistSize,
            int visibleCapacity)
        {
            return _inner.CalculateGridStartIndex(currentStartIndex, offset, playlistSize, visibleCapacity);
        }
    }

    private sealed class FailingLayoutService : ILayoutService
    {
        private readonly LayoutService _inner = new();

        public Result<GridLayoutPlan> CalculateGridLayout(
            int rows,
            int columns,
            ImageDimensions canvasSize)
        {
            return Result<GridLayoutPlan>.Failure(
                ViewerError.LayoutError(
                    "CalculateGridLayout",
                    "Unable to load custom layout.",
                    new InvalidOperationException("Layout failed.")));
        }

        public Result<IReadOnlyList<GridImage>> LoadGridImages(
            PlaylistState playlist,
            int startIndex,
            GridLayoutPlan layout)
        {
            return _inner.LoadGridImages(playlist, startIndex, layout);
        }

        public Result<FilmstripData> LoadFilmstripData(
            PlaylistState playlist,
            int focusIndex,
            int thumbnailCount)
        {
            return Result<FilmstripData>.Failure(
                ViewerError.LayoutError(
                    "LoadFilmstripData",
                    "Unable to load custom layout.",
                    new InvalidOperationException("Layout failed.")));
        }

        public Result<int> CalculateGridStartIndex(
            int currentStartIndex,
            int offset,
            int playlistSize,
            int visibleCapacity)
        {
            return _inner.CalculateGridStartIndex(currentStartIndex, offset, playlistSize, visibleCapacity);
        }
    }

    private sealed class RecordingCodecDetectionService : ICodecDetectionService
    {
        private readonly bool _heicSupported;
        private readonly Result<string> _heicStoreUrl;
        private readonly bool _avifSupported;
        private readonly Result<string> _avifStoreUrl;

        public RecordingCodecDetectionService(
            bool heicSupported,
            bool avifSupported = false,
            Result<string>? heicStoreUrl = null,
            Result<string>? avifStoreUrl = null)
        {
            _heicSupported = heicSupported;
            _avifSupported = avifSupported;
            _heicStoreUrl = heicStoreUrl ?? Result<string>.Success("ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG");
            _avifStoreUrl = avifStoreUrl ?? Result<string>.Success("ms-windows-store://pdp/?ProductId=9MVZQVXJBQ9V");
        }

        public Task<Result<CodecAvailability>> DetectCodecsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<CodecAvailability>.Success(
                new CodecAvailability(_heicSupported, _avifSupported, DateTimeOffset.UnixEpoch)));
        }

        public Result<bool> IsHeicSupported()
        {
            return Result<bool>.Success(_heicSupported);
        }

        public Result<bool> IsAvifSupported()
        {
            return Result<bool>.Success(_avifSupported);
        }

        public Result<string> GetHeicStoreUrl()
        {
            return _heicStoreUrl;
        }

        public Result<string> GetAvifStoreUrl()
        {
            return _avifStoreUrl;
        }
    }
}
