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
    public async Task OpenFileAsync_AnimatedGif_LoadsAnimationState()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(320, 240, 100));
        var playlist = new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos");
        var animation = new AnimatedGif(
            image.FilePath,
            image.Metadata,
            [
                new AnimationFrame(0, TimeSpan.FromMilliseconds(40), [0x01], 320, 240),
                new AnimationFrame(1, TimeSpan.FromMilliseconds(80), [0x02], 320, 240)
            ]);
        var animationService = new RecordingAnimationService(Result<AnimatedGif>.Success(animation));
        var services = TestServices.Success(image, playlist, animationService);
        var viewModel = services.CreateViewModel();

        await viewModel.OpenFileAsync(image.FilePath);

        Assert.True(viewModel.AnimationState.IsAnimationActive);
        Assert.Same(animation, viewModel.AnimationState.CurrentAnimation);
        Assert.Equal(2, viewModel.AnimationState.TotalFrames);
        Assert.Equal(image.FilePath, animationService.LoadedFilePaths.Single());
    }

    [Fact]
    public async Task ToggleAnimationPlayback_WhenAnimatedGifIsActive_TogglesAnimationState()
    {
        var viewModel = await CreateAnimatedGifViewModelAsync();

        viewModel.ToggleAnimationPlayback();

        Assert.False(viewModel.AnimationState.IsPlaying);

        viewModel.ToggleAnimationPlayback();

        Assert.True(viewModel.AnimationState.IsPlaying);
    }

    [Fact]
    public async Task AdvanceAnimationFrame_WhenPaused_AdvancesCurrentFrame()
    {
        var viewModel = await CreateAnimatedGifViewModelAsync();
        viewModel.ToggleAnimationPlayback();

        viewModel.AdvanceAnimationFrame();

        Assert.False(viewModel.AnimationState.IsPlaying);
        Assert.Equal(1, viewModel.AnimationState.CurrentFrameIndex);
    }

    [Fact]
    public async Task RewindAnimationFrame_WhenPaused_RewindsCurrentFrame()
    {
        var viewModel = await CreateAnimatedGifViewModelAsync();
        viewModel.ToggleAnimationPlayback();

        viewModel.RewindAnimationFrame();

        Assert.False(viewModel.AnimationState.IsPlaying);
        Assert.Equal(1, viewModel.AnimationState.CurrentFrameIndex);
    }

    [Fact]
    public async Task SeekAnimationFrame_FromFrameControl_JumpsToFrameAndPauses()
    {
        var viewModel = await CreateAnimatedGifViewModelAsync();

        viewModel.SeekAnimationFrame(1);

        Assert.False(viewModel.AnimationState.IsPlaying);
        Assert.Equal(1, viewModel.AnimationState.CurrentFrameIndex);
    }

    [Fact]
    public async Task StepAnimationFrameFromControl_WhenPlaying_AdvancesAndPauses()
    {
        var viewModel = await CreateAnimatedGifViewModelAsync();

        viewModel.StepAnimationFrameFromControl(1);

        Assert.False(viewModel.AnimationState.IsPlaying);
        Assert.Equal(1, viewModel.AnimationState.CurrentFrameIndex);
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
    public async Task OpenFileAsync_HeicCodecUnavailable_RaisesInstallPromptOncePerSession()
    {
        var filePath = @"C:\Photos\phone.heic";
        var error = ViewerError.CodecUnavailableError("ValidateFilePath", filePath);
        var codecService = new StubCodecDetectionService(heicSupported: false);
        var services = TestServices.WithFileValidationFailure(error, codecService);
        var viewModel = services.CreateViewModel();
        var prompts = new List<CodecInstallPrompt>();
        viewModel.CodecInstallPromptRequested += (_, prompt) => prompts.Add(prompt);

        await viewModel.OpenFileAsync(filePath);
        await viewModel.OpenFileAsync(filePath);

        Assert.Equal(error.UserMessage, viewModel.ErrorMessage);
        var prompt = Assert.Single(prompts);
        Assert.Equal("HEIC", prompt.FormatName);
        Assert.Equal("HEIF Image Extensions", prompt.CodecName);
        Assert.Equal("ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG", prompt.StoreUrl);
        Assert.Equal(error.UserMessage, prompt.Message);
        Assert.Equal(2, services.ErrorLogService.LoggedErrors.Count);
    }

    [Fact]
    public async Task OpenFileAsync_AvifCodecUnavailable_RaisesInstallPromptOncePerSession()
    {
        var filePath = @"C:\Photos\modern.avif";
        var error = ViewerError.CodecUnavailableError("ValidateFilePath", filePath);
        var codecService = new StubCodecDetectionService(avifSupported: false);
        var services = TestServices.WithFileValidationFailure(error, codecService);
        var viewModel = services.CreateViewModel();
        var prompts = new List<CodecInstallPrompt>();
        viewModel.CodecInstallPromptRequested += (_, prompt) => prompts.Add(prompt);

        await viewModel.OpenFileAsync(filePath);
        await viewModel.OpenFileAsync(filePath);

        Assert.Equal(error.UserMessage, viewModel.ErrorMessage);
        var prompt = Assert.Single(prompts);
        Assert.Equal("AVIF", prompt.FormatName);
        Assert.Equal("AV1 Video Extension", prompt.CodecName);
        Assert.Equal("ms-windows-store://pdp/?ProductId=9MVZQVXJBQ9V", prompt.StoreUrl);
        Assert.Equal(error.UserMessage, prompt.Message);
        Assert.Equal(2, services.ErrorLogService.LoggedErrors.Count);
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
    public void ToggleSlideshow_StartsAndStopsSlideshowWithCurrentPlaylist()
    {
        var playlist = CreateTwoItemPlaylist();
        var slideshowService = new RecordingSlideshowService();
        var services = TestServices.WithPlaylist(playlist, slideshowService: slideshowService);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        viewModel.ToggleSlideshow();
        viewModel.ToggleSlideshow();

        Assert.Equal(1, slideshowService.StartCount);
        Assert.Equal(1, slideshowService.StopCount);
        Assert.False(viewModel.SlideshowState.IsActive);
        Assert.True(viewModel.IsChromeVisible);
    }

    [Fact]
    public async Task ToggleSlideshow_UsesSavedIntervalSetting()
    {
        var playlist = CreateTwoItemPlaylist();
        var slideshowService = new RecordingSlideshowService();
        var services = TestServices.WithSettings(
            ViewerSettings.Default with { SlideshowIntervalSeconds = 10 },
            slideshowService);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.InitializeAsync();

        viewModel.ToggleSlideshow();

        Assert.Equal(TimeSpan.FromSeconds(10), slideshowService.StartedConfig?.Interval);
        Assert.Equal(TimeSpan.FromSeconds(10), viewModel.SlideshowState.Interval);
    }

    [Fact]
    public async Task IncreaseSlideshowInterval_WhenActive_StepsAndPersistsSetting()
    {
        var playlist = CreateTwoItemPlaylist();
        var slideshowService = new RecordingSlideshowService();
        var services = TestServices.WithSettings(
            ViewerSettings.Default with { SlideshowIntervalSeconds = 3 },
            slideshowService);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.InitializeAsync();
        viewModel.ToggleSlideshow();

        await viewModel.IncreaseSlideshowIntervalAsync();

        Assert.Equal(5, viewModel.Settings.SlideshowIntervalSeconds);
        Assert.Equal(TimeSpan.FromSeconds(5), viewModel.SlideshowState.Interval);
        Assert.Equal(5, services.SettingsService.SavedSettings.Single().SlideshowIntervalSeconds);
        Assert.Equal(TimeSpan.FromSeconds(5), slideshowService.UpdatedConfigs.Single().Interval);
    }

    [Fact]
    public async Task DecreaseSlideshowInterval_WhenActive_StepsAndPersistsSetting()
    {
        var playlist = CreateTwoItemPlaylist();
        var slideshowService = new RecordingSlideshowService();
        var services = TestServices.WithSettings(
            ViewerSettings.Default with { SlideshowIntervalSeconds = 3 },
            slideshowService);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.InitializeAsync();
        viewModel.ToggleSlideshow();

        await viewModel.DecreaseSlideshowIntervalAsync();

        Assert.Equal(2, viewModel.Settings.SlideshowIntervalSeconds);
        Assert.Equal(TimeSpan.FromSeconds(2), viewModel.SlideshowState.Interval);
        Assert.Equal(2, services.SettingsService.SavedSettings.Single().SlideshowIntervalSeconds);
        Assert.Equal(TimeSpan.FromSeconds(2), slideshowService.UpdatedConfigs.Single().Interval);
    }

    [Fact]
    public async Task SetSlideshowInterval_WhenActive_PersistsSupportedInterval()
    {
        var playlist = CreateTwoItemPlaylist();
        var slideshowService = new RecordingSlideshowService();
        var services = TestServices.WithSettings(ViewerSettings.Default, slideshowService);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.InitializeAsync();
        viewModel.ToggleSlideshow();

        await viewModel.SetSlideshowIntervalAsync(15);

        Assert.Equal(15, viewModel.Settings.SlideshowIntervalSeconds);
        Assert.Equal(TimeSpan.FromSeconds(15), viewModel.SlideshowState.Interval);
        Assert.Equal(15, services.SettingsService.SavedSettings.Single().SlideshowIntervalSeconds);
        Assert.Equal(TimeSpan.FromSeconds(15), slideshowService.UpdatedConfigs.Single().Interval);
    }

    [Fact]
    public async Task ToggleSlideshow_UsesSavedTransitionSetting()
    {
        var playlist = CreateTwoItemPlaylist();
        var slideshowService = new RecordingSlideshowService();
        var services = TestServices.WithSettings(
            ViewerSettings.Default with { SlideshowTransition = TransitionEffect.Zoom },
            slideshowService);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.InitializeAsync();

        viewModel.ToggleSlideshow();

        Assert.Equal(TransitionEffect.Zoom, slideshowService.StartedConfig?.Transition);
        Assert.Equal(TransitionEffect.Zoom, viewModel.SlideshowState.Transition);
    }

    [Fact]
    public async Task SetSlideshowTransition_WhenActive_PersistsAndUpdatesServiceConfig()
    {
        var playlist = CreateTwoItemPlaylist();
        var slideshowService = new RecordingSlideshowService();
        var services = TestServices.WithSettings(ViewerSettings.Default, slideshowService);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.InitializeAsync();
        viewModel.ToggleSlideshow();

        await viewModel.SetSlideshowTransitionAsync(TransitionEffect.Slide);

        Assert.Equal(TransitionEffect.Slide, viewModel.Settings.SlideshowTransition);
        Assert.Equal(TransitionEffect.Slide, viewModel.SlideshowState.Transition);
        Assert.Equal(TransitionEffect.Slide, services.SettingsService.SavedSettings.Single().SlideshowTransition);
        Assert.Equal(TransitionEffect.Slide, slideshowService.UpdatedConfigs.Single().Transition);
    }

    [Fact]
    public async Task NavigateNextAsync_DuringSlideshowTransition_BlocksManualNavigation()
    {
        var playlist = CreateTwoItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        viewModel.BeginSlideshowTransition(TransitionEffect.Fade);

        await viewModel.NavigateNextAsync();

        Assert.Equal(0, viewModel.PlaylistState.CurrentIndex);
        Assert.True(viewModel.SlideshowState.IsTransitioning);
    }

    [Fact]
    public void ToggleSlideshowPause_PausesAndResumesActiveSlideshow()
    {
        var playlist = CreateTwoItemPlaylist();
        var slideshowService = new RecordingSlideshowService();
        var services = TestServices.WithPlaylist(playlist, slideshowService: slideshowService);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        viewModel.ToggleSlideshow();
        viewModel.ToggleSlideshowPause();
        viewModel.ToggleSlideshowPause();

        Assert.Equal(1, slideshowService.PauseCount);
        Assert.Equal(1, slideshowService.ResumeCount);
        Assert.True(viewModel.SlideshowState.IsActive);
        Assert.False(viewModel.SlideshowState.IsPaused);
    }

    [Fact]
    public async Task NavigateNextAsync_ActiveSlideshow_ResetsTimer()
    {
        var playlist = CreateTwoItemPlaylist();
        var slideshowService = new RecordingSlideshowService();
        var services = TestServices.WithPlaylist(playlist, slideshowService: slideshowService);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        viewModel.ToggleSlideshow();

        await viewModel.NavigateNextAsync();

        Assert.Equal(1, slideshowService.ResetCount);
        Assert.Equal(1, viewModel.SlideshowState.TimerResetCount);
    }

    [Fact]
    public async Task EnterComparisonAsync_WithTwoImages_DisplaysCurrentAndNextImages()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide);

        Assert.True(viewModel.ComparisonState.IsActive);
        Assert.Equal(ComparisonLayout.SideBySide, viewModel.ComparisonState.Layout);
        Assert.Equal([0, 1], viewModel.ComparisonState.VisibleImages.Select(image => image.PlaylistIndex).ToArray());
    }

    [Fact]
    public async Task EnterComparisonAsync_WithSingleImage_ShowsComparisonError()
    {
        var playlist = new Playlist([new ImageItem(@"C:\Photos\a.png")], 0, @"C:\Photos");
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide);

        Assert.False(viewModel.ComparisonState.IsActive);
        Assert.Equal("Comparison mode requires at least two images", viewModel.ErrorMessage);
        Assert.Equal(ErrorCategory.ComparisonError, services.ErrorLogService.LoggedErrors.Single().Category);
    }

    [Fact]
    public async Task NavigateNextAsync_InComparisonMode_ShiftsVisibleImagesTogether()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide);

        await viewModel.NavigateNextAsync();

        Assert.Equal([1, 2], viewModel.ComparisonState.VisibleImages.Select(image => image.PlaylistIndex).ToArray());
        Assert.Equal(1, viewModel.PlaylistState.CurrentIndex);
    }

    [Fact]
    public async Task ExitComparison_ReturnsToSingleImageView()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide);

        viewModel.ExitComparison();

        Assert.False(viewModel.ComparisonState.IsActive);
        Assert.Empty(viewModel.ComparisonState.VisibleImages);
        Assert.True(viewModel.IsChromeVisible);
    }

    [Fact]
    public async Task EnterComparisonAsync_WithOverlayLayout_UsesOverlayLayout()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);

        await viewModel.EnterComparisonAsync(ComparisonLayout.Overlay);

        Assert.True(viewModel.ComparisonState.IsActive);
        Assert.Equal(ComparisonLayout.Overlay, viewModel.ComparisonState.Layout);
        Assert.Equal([0, 1], viewModel.ComparisonState.VisibleImages.Select(image => image.PlaylistIndex).ToArray());
    }

    [Fact]
    public async Task ChangeComparisonImageCountAsync_InComparisonMode_DisplaysRequestedImageCount()
    {
        var playlist = CreateFourItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide);

        await viewModel.ChangeComparisonImageCountAsync(4);

        Assert.True(viewModel.ComparisonState.IsActive);
        Assert.Equal(ComparisonLayout.SideBySide, viewModel.ComparisonState.Layout);
        Assert.Equal([0, 1, 2, 3], viewModel.ComparisonState.VisibleImages.Select(image => image.PlaylistIndex).ToArray());
    }

    [Fact]
    public async Task SelectNextComparisonImage_CyclesActiveSelection()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide, imageCount: 3);

        viewModel.SelectNextComparisonImage();
        viewModel.SelectNextComparisonImage();
        viewModel.SelectNextComparisonImage();

        Assert.Equal(0, viewModel.ComparisonState.ActiveImageIndex);
    }

    [Fact]
    public async Task OpenSelectedComparisonImageAsync_ExitsComparisonAndDisplaysSelectedImage()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide, imageCount: 3);
        viewModel.SelectComparisonImage(2);

        await viewModel.OpenSelectedComparisonImageAsync();

        Assert.False(viewModel.ComparisonState.IsActive);
        Assert.Equal(2, viewModel.PlaylistState.CurrentIndex);
        Assert.Equal(@"C:\Photos\c.png", viewModel.ViewerState.CurrentImage?.FilePath);
    }

    [Fact]
    public async Task OverlayCommands_AdjustOpacityAndSwapLayers()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonAsync(ComparisonLayout.Overlay);

        viewModel.IncreaseOverlayOpacity();
        viewModel.SwapOverlayLayers();

        Assert.Equal(0.6, viewModel.ComparisonState.OverlayOpacity, precision: 5);
        Assert.Equal([1, 0], viewModel.ComparisonState.VisibleImages.Select(image => image.PlaylistIndex).ToArray());
    }

    [Fact]
    public async Task ZoomIn_InComparisonMode_SynchronizesAllVisibleImages()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide, imageCount: 3);

        viewModel.ZoomIn();

        Assert.All(viewModel.ComparisonState.Viewports, viewport => Assert.Equal(125, viewport.ZoomLevel));
    }

    [Fact]
    public async Task ToggleComparisonSyncZoom_DisablesSynchronizedViewportUpdates()
    {
        var playlist = CreateThreeItemPlaylist();
        var services = TestServices.WithPlaylist(playlist);
        var viewModel = services.CreateViewModel();
        viewModel.PlaylistState.SetPlaylist(playlist);
        await viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide, imageCount: 3);
        viewModel.SelectComparisonImage(1);

        viewModel.ToggleComparisonSyncZoom();
        viewModel.ZoomIn();

        Assert.False(viewModel.ComparisonState.IsSyncZoomEnabled);
        Assert.Equal([100d, 125d, 100d], viewModel.ComparisonState.Viewports.Select(viewport => viewport.ZoomLevel).ToArray());
    }

    [Fact]
    public void ShowAndHideCommandPalette_UpdatesPaletteStateAndChrome()
    {
        var viewModel = TestServices.WithPlaylist(CreateTwoItemPlaylist()).CreateViewModel();

        viewModel.ShowCommandPalette();
        viewModel.HideCommandPalette();

        Assert.False(viewModel.CommandPaletteState.IsOpen);
        Assert.True(viewModel.IsChromeVisible);
        Assert.NotEmpty(viewModel.CommandPaletteState.Commands);
    }

    [Fact]
    public async Task ExecuteSelectedCommandAsync_RunsCommandAndClosesPalette()
    {
        var viewModel = TestServices.WithPlaylist(CreateTwoItemPlaylist()).CreateViewModel();
        viewModel.ShowCommandPalette();
        viewModel.CommandPaletteState.SelectCommand(CommandIds.ToggleFullscreen);

        await viewModel.ExecuteSelectedCommandAsync();

        Assert.True(viewModel.ViewerState.IsFullscreen);
        Assert.False(viewModel.CommandPaletteState.IsOpen);
    }

    [Fact]
    public async Task ExecuteSelectedCommandAsync_TracksRecentCommand()
    {
        var viewModel = TestServices.WithPlaylist(CreateTwoItemPlaylist()).CreateViewModel();
        viewModel.ShowCommandPalette();
        viewModel.CommandPaletteState.SelectCommand(CommandIds.ToggleFullscreen);

        await viewModel.ExecuteSelectedCommandAsync();
        viewModel.ShowCommandPalette();

        Assert.Equal(CommandIds.ToggleFullscreen, viewModel.CommandPaletteState.RecentCommands[0].Id);
        Assert.Equal("Recent", viewModel.CommandPaletteState.DisplayItems[0].Header);
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

        viewModel.ShowSearch();
        viewModel.SetSearchQuery("b");

        Assert.True(viewModel.SearchState.IsSearchActive);
        Assert.Single(viewModel.SearchState.FilteredPlaylist);

        viewModel.HideSearch();
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

    private static async Task<MainViewModel> CreateAnimatedGifViewModelAsync()
    {
        var image = new DecodedImage(@"C:\Photos\animated.gif", new ImageMetadata(320, 240, 100));
        var playlist = new Playlist([new ImageItem(image.FilePath)], 0, @"C:\Photos");
        var animation = new AnimatedGif(
            image.FilePath,
            image.Metadata,
            [
                new AnimationFrame(0, TimeSpan.FromMilliseconds(40), [0x01], 320, 240),
                new AnimationFrame(1, TimeSpan.FromMilliseconds(80), [0x02], 320, 240)
            ]);
        var services = TestServices.Success(
            image,
            playlist,
            new RecordingAnimationService(Result<AnimatedGif>.Success(animation)));
        var viewModel = services.CreateViewModel();
        await viewModel.OpenFileAsync(image.FilePath);
        return viewModel;
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
            ISlideshowService? slideshowService = null,
            IComparisonService? comparisonService = null,
            ICommandPaletteService? commandPaletteService = null,
            IAnimationService? animationService = null,
            ICodecDetectionService? codecDetectionService = null)
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
            SlideshowService = slideshowService ?? new RecordingSlideshowService();
            ComparisonService = comparisonService ?? new ComparisonService(ImageDecodeService);
            CommandPaletteService = commandPaletteService ?? new CommandPaletteService();
            AnimationService = animationService ?? new RecordingAnimationService(Result<AnimatedGif>.Failure(
                ViewerError.UnsupportedFormatError("LoadAnimatedGif", null)));
            CodecDetectionService = codecDetectionService;
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

        public ISlideshowService SlideshowService { get; }

        public IComparisonService ComparisonService { get; }

        public ICommandPaletteService CommandPaletteService { get; }

        public IAnimationService AnimationService { get; }

        public ICodecDetectionService? CodecDetectionService { get; }

        public static TestServices Success(
            DecodedImage image,
            Playlist playlist,
            IAnimationService? animationService = null)
        {
            var decoder = new StubImageDecodeService(Result<DecodedImage>.Success(image));
            return new TestServices(
                new StubFileOpenService(Result<string>.Success(image.FilePath)),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Success(playlist)),
                new NavigationService(decoder),
                new StubSettingsService(),
                new RecordingErrorLogService(),
                animationService: animationService);
        }

        public static TestServices WithPlaylist(
            Playlist playlist,
            string? failDecodePath = null,
            RecordingTransformService? transformService = null,
            RecordingExportService? exportService = null,
            RecordingMetadataService? metadataService = null,
            ISlideshowService? slideshowService = null)
        {
            var decoder = new PathImageDecodeService(failDecodePath);
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
                slideshowService: slideshowService);
        }

        public static TestServices WithSettings(ViewerSettings settings, ISlideshowService? slideshowService = null)
        {
            var decoder = new PathImageDecodeService();
            return new TestServices(
                new StubFileOpenService(Result<string>.Success(@"C:\Photos\a.png")),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Success(CreateTwoItemPlaylist())),
                new NavigationService(decoder),
                new StubSettingsService(settings),
                new RecordingErrorLogService(),
                slideshowService: slideshowService);
        }

        public static TestServices WithFileValidationFailure(
            ViewerError error,
            ICodecDetectionService? codecDetectionService = null)
        {
            var decoder = new StubImageDecodeService(Result<DecodedImage>.Failure(ViewerError.DecodeError("DecodeImage", null)));
            return new TestServices(
                new StubFileOpenService(Result<string>.Failure(error)),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Failure(ViewerError.PlaylistScanError("BuildPlaylist", null))),
                new NavigationService(decoder),
                new StubSettingsService(),
                new RecordingErrorLogService(),
                codecDetectionService: codecDetectionService);
        }

        public static TestServices WithDecoder(string filePath, IImageDecodeService decoder)
        {
            var playlist = new Playlist([new ImageItem(filePath)], 0, Path.GetDirectoryName(filePath) ?? string.Empty);
            return new TestServices(
                new StubFileOpenService(Result<string>.Success(filePath)),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Success(playlist)),
                new NavigationService(decoder),
                new StubSettingsService(),
                new RecordingErrorLogService());
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
                SlideshowService,
                ComparisonService,
                CommandPaletteService,
                AnimationService,
                CodecDetectionService);
        }
    }

    private sealed class StubCodecDetectionService : ICodecDetectionService
    {
        private readonly bool _heicSupported;
        private readonly bool _avifSupported;

        public StubCodecDetectionService(bool heicSupported = false, bool avifSupported = false)
        {
            _heicSupported = heicSupported;
            _avifSupported = avifSupported;
        }

        public Task<Result<CodecAvailability>> DetectCodecsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<CodecAvailability>.Success(
                new CodecAvailability(_heicSupported, _avifSupported, DateTimeOffset.UtcNow)));
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
            return Result<string>.Success("ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG");
        }

        public Result<string> GetAvifStoreUrl()
        {
            return Result<string>.Success("ms-windows-store://pdp/?ProductId=9MVZQVXJBQ9V");
        }
    }

    private sealed class RecordingAnimationService : IAnimationService
    {
        private readonly Result<AnimatedGif> _loadResult;

        public RecordingAnimationService(Result<AnimatedGif> loadResult)
        {
            _loadResult = loadResult;
        }

        public List<string> LoadedFilePaths { get; } = [];

        public Task<Result<AnimatedGif>> LoadAnimatedGifAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            LoadedFilePaths.Add(filePath);
            return Task.FromResult(_loadResult);
        }

        public Task<Result<bool>> IsAnimatedGifAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_loadResult.IsSuccess
                ? Result<bool>.Success(_loadResult.Value.IsAnimated)
                : Result<bool>.Failure(_loadResult.Error));
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
        private readonly ViewerSettings _settings;

        public StubSettingsService()
            : this(ViewerSettings.Default)
        {
        }

        public StubSettingsService(ViewerSettings settings)
        {
            _settings = settings;
        }

        public List<ViewerSettings> SavedSettings { get; } = [];

        public Task<Result<ViewerSettings>> LoadSettingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<ViewerSettings>.Success(_settings));
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

    private sealed class RecordingSlideshowService : ISlideshowService
    {
        public event EventHandler<SlideshowAdvanceRequestedEventArgs>? AdvanceRequested;

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public int PauseCount { get; private set; }

        public int ResumeCount { get; private set; }

        public int ResetCount { get; private set; }

        public bool IsActive { get; private set; }

        public bool IsPaused { get; private set; }

        public SlideshowConfig? StartedConfig { get; private set; }

        public List<SlideshowConfig> UpdatedConfigs { get; } = [];

        public Result StartSlideshow(Playlist playlist, int currentIndex, SlideshowConfig config)
        {
            StartCount++;
            StartedConfig = config;
            IsActive = true;
            IsPaused = false;
            return Result.Success();
        }

        public Result StopSlideshow()
        {
            StopCount++;
            IsActive = false;
            IsPaused = false;
            return Result.Success();
        }

        public Result PauseSlideshow()
        {
            PauseCount++;
            IsPaused = true;
            return Result.Success();
        }

        public Result ResumeSlideshow()
        {
            ResumeCount++;
            IsPaused = false;
            return Result.Success();
        }

        public Result UpdateConfig(SlideshowConfig config)
        {
            UpdatedConfigs.Add(config);
            return Result.Success();
        }

        public Result ResetTimer()
        {
            ResetCount++;
            return Result.Success();
        }

        public void RaiseAdvanceRequested(int nextIndex, ImageItem nextItem)
        {
            AdvanceRequested?.Invoke(this, new SlideshowAdvanceRequestedEventArgs(nextIndex, nextItem));
        }
    }
}
