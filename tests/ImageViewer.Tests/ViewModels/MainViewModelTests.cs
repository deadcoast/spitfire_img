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
        Assert.False(viewModel.IsLoading);
        Assert.Single(services.ErrorLogService.LoggedErrors);
        Assert.Equal(error, services.ErrorLogService.LoggedErrors[0]);
    }

    private sealed class TestServices
    {
        private TestServices(
            IFileOpenService fileOpenService,
            IImageDecodeService imageDecodeService,
            IFolderPlaylistService folderPlaylistService,
            INavigationService navigationService,
            ISettingsService settingsService,
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

        public ISettingsService SettingsService { get; }

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
        public Task<Result<ViewerSettings>> LoadSettingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<ViewerSettings>.Success(ViewerSettings.Default));
        }

        public Task<Result> SaveSettingsAsync(ViewerSettings settings, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
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
}
