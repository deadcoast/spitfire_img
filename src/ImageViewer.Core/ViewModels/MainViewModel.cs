using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Core.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IFileOpenService _fileOpenService;
    private readonly IImageDecodeService _imageDecodeService;
    private readonly IFolderPlaylistService _folderPlaylistService;
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly IErrorLogService _errorLogService;
    private ViewerSettings _settings = ViewerSettings.Default;
    private bool _isChromeVisible = true;
    private bool _isLoading;
    private string? _errorMessage;

    public MainViewModel(
        IFileOpenService fileOpenService,
        IImageDecodeService imageDecodeService,
        IFolderPlaylistService folderPlaylistService,
        INavigationService navigationService,
        ISettingsService settingsService,
        IErrorLogService errorLogService)
    {
        _fileOpenService = fileOpenService;
        _imageDecodeService = imageDecodeService;
        _folderPlaylistService = folderPlaylistService;
        _navigationService = navigationService;
        _settingsService = settingsService;
        _errorLogService = errorLogService;
    }

    public ViewerState ViewerState { get; } = new();

    public PlaylistState PlaylistState { get; } = new();

    public ViewerSettings Settings
    {
        get => _settings;
        private set => SetProperty(ref _settings, value);
    }

    public bool IsChromeVisible
    {
        get => _isChromeVisible;
        private set => SetProperty(ref _isChromeVisible, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settingsResult = await _settingsService.LoadSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (settingsResult.IsSuccess)
        {
            Settings = settingsResult.Value;
            return;
        }

        Settings = ViewerSettings.Default;
        await HandleErrorAsync(settingsResult.Error, cancellationToken).ConfigureAwait(false);
    }

    public async Task OpenFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var validationResult = _fileOpenService.ValidateFilePath(filePath);
            if (validationResult.IsFailure)
            {
                await HandleErrorAsync(validationResult.Error, cancellationToken).ConfigureAwait(false);
                return;
            }

            var decodeResult = await _imageDecodeService.DecodeImageAsync(validationResult.Value, cancellationToken)
                .ConfigureAwait(false);
            if (decodeResult.IsFailure)
            {
                await HandleErrorAsync(decodeResult.Error, cancellationToken).ConfigureAwait(false);
                return;
            }

            var playlistResult = await _folderPlaylistService.BuildPlaylistAsync(validationResult.Value, cancellationToken)
                .ConfigureAwait(false);
            if (playlistResult.IsFailure)
            {
                await HandleErrorAsync(playlistResult.Error, cancellationToken).ConfigureAwait(false);
                var fallback = new Playlist([new ImageItem(validationResult.Value)], 0, Path.GetDirectoryName(validationResult.Value) ?? string.Empty);
                PlaylistState.SetPlaylist(fallback);
            }
            else
            {
                PlaylistState.SetPlaylist(playlistResult.Value);
            }

            ViewerState.SetImage(decodeResult.Value);
            ShowChrome();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ShowChrome()
    {
        IsChromeVisible = true;
    }

    public Task NavigateNextAsync(CancellationToken cancellationToken = default)
    {
        return NavigateToIndexAsync(PlaylistState.CurrentIndex + 1, cancellationToken);
    }

    public Task NavigatePreviousAsync(CancellationToken cancellationToken = default)
    {
        return NavigateToIndexAsync(PlaylistState.CurrentIndex - 1, cancellationToken);
    }

    public Task NavigateFirstAsync(CancellationToken cancellationToken = default)
    {
        return NavigateToIndexAsync(0, cancellationToken);
    }

    public Task NavigateLastAsync(CancellationToken cancellationToken = default)
    {
        return NavigateToIndexAsync(PlaylistState.TotalCount - 1, cancellationToken);
    }

    public void ZoomIn()
    {
        ViewerState.SetZoom(ViewerState.ZoomLevel + 25);
        ShowChrome();
    }

    public void ZoomOut()
    {
        ViewerState.SetZoom(Math.Max(100, ViewerState.ZoomLevel - 25));
        ShowChrome();
    }

    public void FitToWindow()
    {
        ViewerState.SetDisplayMode(DisplayMode.FitToWindow);
        ViewerState.ResetPan();
        ShowChrome();
    }

    public void ActualSize()
    {
        ViewerState.SetDisplayMode(DisplayMode.ActualSize);
        ViewerState.ResetPan();
        ShowChrome();
    }

    public void ToggleFullscreen()
    {
        ViewerState.IsFullscreen = !ViewerState.IsFullscreen;
        ShowChrome();
    }

    public async Task ToggleThemeAsync(CancellationToken cancellationToken = default)
    {
        var nextTheme = Settings.Theme == ViewerTheme.Dark
            ? ViewerTheme.Light
            : ViewerTheme.Dark;

        var nextSettings = Settings with { Theme = nextTheme };
        Settings = nextSettings;
        var saveResult = await _settingsService.SaveSettingsAsync(nextSettings, cancellationToken)
            .ConfigureAwait(false);

        if (saveResult.IsFailure)
        {
            await HandleErrorAsync(saveResult.Error, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SaveWindowStateAsync(
        int width,
        int height,
        int x,
        int y,
        CancellationToken cancellationToken = default)
    {
        var nextSettings = Settings with
        {
            WindowWidth = width,
            WindowHeight = height,
            WindowX = x,
            WindowY = y
        };

        Settings = nextSettings.Normalize();
        var saveResult = await _settingsService.SaveSettingsAsync(Settings, cancellationToken)
            .ConfigureAwait(false);

        if (saveResult.IsFailure)
        {
            await HandleErrorAsync(saveResult.Error, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task NavigateToIndexAsync(int targetIndex, CancellationToken cancellationToken)
    {
        if (PlaylistState.TotalCount == 0)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var playlist = new Playlist(PlaylistState.Items.ToList(), PlaylistState.CurrentIndex, PlaylistState.FolderPath);
            var navigationResult = await _navigationService.NavigateToIndexAsync(playlist, targetIndex, cancellationToken)
                .ConfigureAwait(false);

            if (navigationResult.IsFailure)
            {
                await HandleErrorAsync(navigationResult.Error, cancellationToken).ConfigureAwait(false);
                return;
            }

            PlaylistState.SetCurrentIndex(navigationResult.Value.Index);
            ViewerState.SetImage(navigationResult.Value.Image);
            ShowChrome();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task HandleErrorAsync(ViewerError error, CancellationToken cancellationToken)
    {
        ErrorMessage = error.UserMessage;
        await _errorLogService.LogErrorAsync(error, cancellationToken).ConfigureAwait(false);
    }
}
