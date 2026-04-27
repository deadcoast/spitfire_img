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
    private readonly IRecentFilesService _recentFilesService;
    private readonly ITransformService _transformService;
    private readonly IMetadataService _metadataService;
    private ViewerSettings _settings = ViewerSettings.Default;
    private bool _isChromeVisible = true;
    private bool _isLoading;
    private bool _isCancelAvailable;
    private string? _errorMessage;
    private ErrorSeverity _currentErrorSeverity = ErrorSeverity.Error;
    private CancellationTokenSource? _operationCancellation;

    public MainViewModel(
        IFileOpenService fileOpenService,
        IImageDecodeService imageDecodeService,
        IFolderPlaylistService folderPlaylistService,
        INavigationService navigationService,
        ISettingsService settingsService,
        IErrorLogService errorLogService)
        : this(
            fileOpenService,
            imageDecodeService,
            folderPlaylistService,
            navigationService,
            settingsService,
            errorLogService,
            new SearchService(),
            new ThumbnailService(imageDecodeService),
            new RecentFilesService(Path.Combine(Path.GetTempPath(), "spitfire.img")),
            new ExportService(),
            new TransformService(),
            new MetadataService())
    {
    }

    public MainViewModel(
        IFileOpenService fileOpenService,
        IImageDecodeService imageDecodeService,
        IFolderPlaylistService folderPlaylistService,
        INavigationService navigationService,
        ISettingsService settingsService,
        IErrorLogService errorLogService,
        ISearchService searchService,
        IThumbnailService thumbnailService,
        IRecentFilesService recentFilesService,
        IExportService exportService,
        ITransformService transformService,
        IMetadataService metadataService)
    {
        _fileOpenService = fileOpenService;
        _imageDecodeService = imageDecodeService;
        _folderPlaylistService = folderPlaylistService;
        _navigationService = navigationService;
        _settingsService = settingsService;
        _errorLogService = errorLogService;
        _recentFilesService = recentFilesService;
        _transformService = transformService;
        _metadataService = metadataService;
        SearchState = new SearchState(searchService);
        ThumbnailState = new ThumbnailState();
        TransformState = new TransformState();
        MetadataState = new MetadataState();
    }

    public ViewerState ViewerState { get; } = new();

    public PlaylistState PlaylistState { get; } = new();

    public SearchState SearchState { get; }

    public ThumbnailState ThumbnailState { get; }

    public TransformState TransformState { get; }

    public MetadataState MetadataState { get; }

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

    public bool IsCancelAvailable
    {
        get => _isCancelAvailable;
        private set => SetProperty(ref _isCancelAvailable, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ErrorSeverity CurrentErrorSeverity
    {
        get => _currentErrorSeverity;
        private set => SetProperty(ref _currentErrorSeverity, value);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settingsResult = await _settingsService.LoadSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (settingsResult.IsSuccess)
        {
            Settings = settingsResult.Value;
            await _recentFilesService.LoadRecentFilesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        Settings = ViewerSettings.Default;
        await HandleErrorAsync(settingsResult.Error, cancellationToken).ConfigureAwait(false);
    }

    public async Task OpenFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var operation = BeginOperation(cancellationToken);
        var operationToken = operation.Token;
        ErrorMessage = null;

        try
        {
            var validationResult = _fileOpenService.ValidateFilePath(filePath);
            if (validationResult.IsFailure)
            {
                await HandleErrorAsync(validationResult.Error, operationToken).ConfigureAwait(false);
                return;
            }

            var decodeResult = await _imageDecodeService.DecodeImageAsync(validationResult.Value, operationToken)
                .ConfigureAwait(false);
            if (decodeResult.IsFailure)
            {
                await HandleErrorAsync(decodeResult.Error, CancellationToken.None).ConfigureAwait(false);
                return;
            }

            var playlistResult = await _folderPlaylistService.BuildPlaylistAsync(validationResult.Value, operationToken)
                .ConfigureAwait(false);
            if (playlistResult.IsFailure)
            {
                await HandleErrorAsync(playlistResult.Error, operationToken).ConfigureAwait(false);
                var fallback = new Playlist([new ImageItem(validationResult.Value)], 0, Path.GetDirectoryName(validationResult.Value) ?? string.Empty);
                PlaylistState.SetPlaylist(fallback);
            }
            else
            {
                PlaylistState.SetPlaylist(playlistResult.Value);
            }

            ViewerState.SetImage(decodeResult.Value);
            SearchState.SetPlaylist(PlaylistState.Items);
            await _recentFilesService.AddRecentFileAsync(validationResult.Value, operationToken).ConfigureAwait(false);
            ShowChrome();
        }
        finally
        {
            EndOperation(operation);
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

    public void CancelLoading()
    {
        _operationCancellation?.Cancel();
        IsCancelAvailable = false;
    }

    public void ShowSearch()
    {
        SearchState.SetSearchQuery(SearchState.SearchQuery);
        ShowChrome();
    }

    public void HideSearch()
    {
        SearchState.ClearSearch();
        ShowChrome();
    }

    public void SetSearchQuery(string query)
    {
        SearchState.SetSearchQuery(query);
        ShowChrome();
    }

    public async Task ToggleMetadataPanelAsync(CancellationToken cancellationToken = default)
    {
        if (MetadataState.IsVisible)
        {
            MetadataState.HidePanel();
            ShowChrome();
            return;
        }

        if (ViewerState.CurrentImage is null)
        {
            MetadataState.ShowPanel(ExifMetadata.Empty);
            ShowChrome();
            return;
        }

        var metadataResult = await _metadataService.ExtractExifAsync(ViewerState.CurrentImage.FilePath, cancellationToken)
            .ConfigureAwait(false);
        if (metadataResult.IsFailure)
        {
            await HandleErrorAsync(metadataResult.Error, cancellationToken).ConfigureAwait(false);
            return;
        }

        MetadataState.ShowPanel(metadataResult.Value);
        ShowChrome();
    }

    public async Task ApplyTransformAsync(TransformOperation operation, CancellationToken cancellationToken = default)
    {
        if (ViewerState.CurrentImage is null)
        {
            return;
        }

        TransformState.ApplyTransform(operation);
        var transformResult = await _transformService.ApplyTransformAsync(
                ViewerState.CurrentImage,
                TransformState.PendingTransforms.ToArray(),
                cancellationToken)
            .ConfigureAwait(false);

        if (transformResult.IsFailure)
        {
            await HandleErrorAsync(transformResult.Error, cancellationToken).ConfigureAwait(false);
        }
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

        using var operation = BeginOperation(cancellationToken);
        var operationToken = operation.Token;
        ErrorMessage = null;

        try
        {
            var playlist = new Playlist(PlaylistState.Items.ToList(), PlaylistState.CurrentIndex, PlaylistState.FolderPath);
            var navigationResult = await _navigationService.NavigateToIndexAsync(playlist, targetIndex, operationToken)
                .ConfigureAwait(false);

            if (navigationResult.IsFailure)
            {
                await HandleErrorAsync(navigationResult.Error, CancellationToken.None).ConfigureAwait(false);
                return;
            }

            PlaylistState.SetCurrentIndex(navigationResult.Value.Index);
            ViewerState.SetImage(navigationResult.Value.Image);
            ShowChrome();
        }
        finally
        {
            EndOperation(operation);
        }
    }

    private async Task HandleErrorAsync(ViewerError error, CancellationToken cancellationToken)
    {
        CurrentErrorSeverity = error.Severity;
        ErrorMessage = error.UserMessage;
        await _errorLogService.LogErrorAsync(error, cancellationToken).ConfigureAwait(false);
    }

    private CancellationTokenSource BeginOperation(CancellationToken cancellationToken)
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IsLoading = true;
        IsCancelAvailable = false;
        _ = ShowCancelAvailabilityAfterDelayAsync(_operationCancellation);
        return _operationCancellation;
    }

    private void EndOperation(CancellationTokenSource operation)
    {
        if (ReferenceEquals(_operationCancellation, operation))
        {
            IsLoading = false;
            IsCancelAvailable = false;
            _operationCancellation = null;
        }
    }

    private async Task ShowCancelAvailabilityAfterDelayAsync(CancellationTokenSource operation)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), operation.Token).ConfigureAwait(false);
            if (ReferenceEquals(_operationCancellation, operation) && IsLoading)
            {
                IsCancelAvailable = true;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
