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

    private static Playlist CreateTwoItemPlaylist()
    {
        return new Playlist(
            [new ImageItem(@"C:\Photos\a.png"), new ImageItem(@"C:\Photos\b.png")],
            0,
            @"C:\Photos");
    }

    private sealed class TestServices
    {
        private TestServices(
            IFileOpenService fileOpenService,
            IImageDecodeService imageDecodeService,
            IFolderPlaylistService folderPlaylistService,
            INavigationService navigationService,
            StubSettingsService settingsService,
            RecordingErrorLogService errorLogService)
        {
            FileOpenService = fileOpenService;
            ImageDecodeService = imageDecodeService;
            FolderPlaylistService = folderPlaylistService;
            NavigationService = navigationService;
            SettingsService = settingsService;
            ErrorLogService = errorLogService;
        }

        public IFileOpenService FileOpenService { get; }

        public IImageDecodeService ImageDecodeService { get; }

        public IFolderPlaylistService FolderPlaylistService { get; }

        public INavigationService NavigationService { get; }

        public StubSettingsService SettingsService { get; }

        public RecordingErrorLogService ErrorLogService { get; }

        public static TestServices Success(DecodedImage image, Playlist playlist)
        {
            var decoder = new StubImageDecodeService(Result<DecodedImage>.Success(image));
            return new TestServices(
                new StubFileOpenService(Result<string>.Success(image.FilePath)),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Success(playlist)),
                new NavigationService(decoder),
                new StubSettingsService(),
                new RecordingErrorLogService());
        }

        public static TestServices WithPlaylist(Playlist playlist, string? failDecodePath = null)
        {
            var decoder = new PathImageDecodeService(failDecodePath);
            return new TestServices(
                new StubFileOpenService(Result<string>.Success(playlist.CurrentItem?.FilePath ?? string.Empty)),
                decoder,
                new StubFolderPlaylistService(Result<Playlist>.Success(playlist)),
                new NavigationService(decoder),
                new StubSettingsService(),
                new RecordingErrorLogService());
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
                ErrorLogService);
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
}
