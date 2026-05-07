using System.Collections.ObjectModel;
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
    private readonly IThumbnailService _thumbnailService;
    private readonly IRecentFilesService _recentFilesService;
    private readonly IExportService _exportService;
    private readonly ITransformService _transformService;
    private readonly IMetadataService _metadataService;
    private readonly IComparisonService _comparisonService;
    private ViewerSettings _settings = ViewerSettings.Default;
    private readonly Dictionary<string, ExifMetadata> _metadataCache = new(StringComparer.OrdinalIgnoreCase);
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
        IMetadataService metadataService,
        IComparisonService? comparisonService = null)
    {
        _fileOpenService = fileOpenService;
        _imageDecodeService = imageDecodeService;
        _folderPlaylistService = folderPlaylistService;
        _navigationService = navigationService;
        _settingsService = settingsService;
        _errorLogService = errorLogService;
        _thumbnailService = thumbnailService;
        _recentFilesService = recentFilesService;
        _exportService = exportService;
        _transformService = transformService;
        _metadataService = metadataService;
        _comparisonService = comparisonService ?? new ComparisonService();
        SearchState = new SearchState(searchService);
        ThumbnailState = new ThumbnailState();
        TransformState = new TransformState();
        MetadataState = new MetadataState();
        CommandPaletteState = new CommandPaletteState(BuildCommandPaletteService());
    }

    public ViewerState ViewerState { get; } = new();

    public PlaylistState PlaylistState { get; } = new();

    public ObservableCollection<RecentFile> RecentFiles { get; } = [];

    public SearchState SearchState { get; }

    public ThumbnailState ThumbnailState { get; }

    public TransformState TransformState { get; }

    public MetadataState MetadataState { get; }

    public ComparisonState ComparisonState { get; } = new();

    public CommandPaletteState CommandPaletteState { get; }

    public event EventHandler? OpenFileRequested;

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
            ThumbnailState.SetVisibility(Settings.ThumbnailStripVisible);
            ThumbnailState.SetStripPosition(Settings.ThumbnailStripPosition);
            var recentFilesResult = await _recentFilesService.LoadRecentFilesAsync(cancellationToken).ConfigureAwait(false);
            if (recentFilesResult.IsSuccess)
            {
                SetRecentFiles(recentFilesResult.Value);
            }

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
            var recentResult = await _recentFilesService.AddRecentFileAsync(validationResult.Value, operationToken).ConfigureAwait(false);
            if (recentResult.IsSuccess)
            {
                SetRecentFiles(recentResult.Value);
            }

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

    public void ShowCommandPalette()
    {
        CommandPaletteState.Show();
        ShowChrome();
    }

    public void HideCommandPalette()
    {
        CommandPaletteState.Hide();
        ShowChrome();
    }

    public async Task<Result> ExecuteSelectedCommandPaletteCommandAsync(CancellationToken cancellationToken = default)
    {
        var result = await CommandPaletteState.ExecuteSelectedCommandAsync().ConfigureAwait(false);
        if (result.IsFailure)
        {
            await HandleErrorAsync(result.Error, cancellationToken).ConfigureAwait(false);
        }

        ShowChrome();
        return result;
    }

    public Task NavigateNextAsync(CancellationToken cancellationToken = default)
    {
        if (ComparisonState.IsActive)
        {
            return ShiftComparisonAsync(offset: 1, cancellationToken);
        }

        return NavigateToIndexAsync(PlaylistState.CurrentIndex + 1, cancellationToken);
    }

    public Task NavigatePreviousAsync(CancellationToken cancellationToken = default)
    {
        if (ComparisonState.IsActive)
        {
            return ShiftComparisonAsync(offset: -1, cancellationToken);
        }

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
        if (ComparisonState.IsActive)
        {
            ApplyComparisonZoom(GetActiveComparisonViewport().ZoomLevel + 25);
            ShowChrome();
            return;
        }

        ViewerState.SetZoom(ViewerState.ZoomLevel + 25);
        ShowChrome();
    }

    public void ZoomOut()
    {
        if (ComparisonState.IsActive)
        {
            ApplyComparisonZoom(Math.Max(100, GetActiveComparisonViewport().ZoomLevel - 25));
            ShowChrome();
            return;
        }

        ViewerState.SetZoom(Math.Max(100, ViewerState.ZoomLevel - 25));
        ShowChrome();
    }

    public void FitToWindow()
    {
        if (ComparisonState.IsActive)
        {
            ResetComparisonZoom();
            ShowChrome();
            return;
        }

        ViewerState.SetDisplayMode(DisplayMode.FitToWindow);
        ViewerState.ResetPan();
        ShowChrome();
    }

    public void ActualSize()
    {
        if (ComparisonState.IsActive)
        {
            ApplyComparisonZoom(100);
            ShowChrome();
            return;
        }

        ViewerState.SetDisplayMode(DisplayMode.ActualSize);
        ViewerState.ResetPan();
        ShowChrome();
    }

    public void ToggleFullscreen()
    {
        ViewerState.IsFullscreen = !ViewerState.IsFullscreen;
        ShowChrome();
    }

    public async Task EnterComparisonModeAsync(
        ComparisonLayout layout,
        CancellationToken cancellationToken = default)
    {
        var indicesResult = _comparisonService.CalculateComparisonIndices(
            PlaylistState.CurrentIndex,
            ComparisonService.DefaultImageCount,
            PlaylistState.TotalCount);

        if (indicesResult.IsFailure)
        {
            await HandleErrorAsync(indicesResult.Error, cancellationToken).ConfigureAwait(false);
            return;
        }

        ApplyComparisonWindow(layout, indicesResult.Value);
        ShowChrome();
    }

    public void ExitComparisonMode()
    {
        ComparisonState.ExitComparison();
        ShowChrome();
    }

    public Task SetComparisonImageCountAsync(
        int imageCount,
        CancellationToken cancellationToken = default)
    {
        if (!ComparisonState.IsActive)
        {
            return Task.CompletedTask;
        }

        return ApplyComparisonImageCountAsync(imageCount, cancellationToken);
    }

    public void SetActiveComparisonImage(int visibleIndex)
    {
        if (!ComparisonState.IsActive)
        {
            return;
        }

        ComparisonState.SetActiveImage(visibleIndex);
        ShowChrome();
    }

    public void CycleComparisonActiveImage()
    {
        if (!ComparisonState.IsActive)
        {
            return;
        }

        ComparisonState.CycleActiveImage();
        ShowChrome();
    }

    public async Task OpenActiveComparisonImageAsync(CancellationToken cancellationToken = default)
    {
        if (!ComparisonState.IsActive || ComparisonState.ActivePlaylistIndex < 0)
        {
            return;
        }

        var targetIndex = ComparisonState.ActivePlaylistIndex;
        ComparisonState.ExitComparison();
        await NavigateToIndexAsync(targetIndex, cancellationToken).ConfigureAwait(false);
    }

    public void SetOverlayOpacity(double opacity)
    {
        if (!IsOverlayComparisonActive())
        {
            return;
        }

        ComparisonState.SetOverlayOpacity(opacity);
        ShowChrome();
    }

    public void AdjustOverlayOpacity(double delta)
    {
        if (!IsOverlayComparisonActive())
        {
            return;
        }

        ComparisonState.AdjustOverlayOpacity(delta);
        ShowChrome();
    }

    public void SwapOverlayLayers()
    {
        if (!IsOverlayComparisonActive())
        {
            return;
        }

        ComparisonState.SwapLayers();
        ShowChrome();
    }

    public void ToggleComparisonSyncZoom()
    {
        if (!ComparisonState.IsActive)
        {
            return;
        }

        ComparisonState.ToggleSyncZoom();
        ShowChrome();
    }

    public void SetComparisonImageDimensions(int visibleIndex, ImageDimensions dimensions)
    {
        if (!ComparisonState.IsActive)
        {
            return;
        }

        ComparisonState.SetImageDimensions(visibleIndex, dimensions);
    }

    public void SetComparisonPan(int visibleIndex, ImagePoint panOffset)
    {
        if (!ComparisonState.IsActive ||
            visibleIndex < 0 ||
            visibleIndex >= ComparisonState.ImageViewports.Count)
        {
            return;
        }

        if (!ComparisonState.SyncZoomEnabled)
        {
            var viewport = ComparisonState.ImageViewports[visibleIndex].WithPan(panOffset);
            ComparisonState.ApplyViewportUpdate(visibleIndex, viewport);
            ShowChrome();
            return;
        }

        var synchronized = _comparisonService.SynchronizeZoom(
            ComparisonState.ImageViewports.ToArray(),
            visibleIndex,
            ComparisonState.ImageViewports[visibleIndex].ZoomLevel,
            panOffset);

        if (synchronized.IsSuccess)
        {
            ComparisonState.ApplyViewportUpdates(synchronized.Value);
        }

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

        if (!_metadataCache.TryGetValue(ViewerState.CurrentImage.FilePath, out var metadata))
        {
            var metadataResult = await _metadataService.ExtractExifAsync(ViewerState.CurrentImage.FilePath, cancellationToken)
                .ConfigureAwait(false);
            if (metadataResult.IsFailure)
            {
                await HandleErrorAsync(metadataResult.Error, cancellationToken).ConfigureAwait(false);
                return;
            }

            metadata = metadataResult.Value;
            _metadataCache[ViewerState.CurrentImage.FilePath] = metadata;
        }

        MetadataState.ShowPanel(metadata);
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
            return;
        }

        ViewerState.SetMetadata(transformResult.Value.Metadata);
        ShowChrome();
    }

    public void UndoTransform()
    {
        TransformState.Undo();
        UpdateTransformPreviewMetadata();
        ShowChrome();
    }

    public void RedoTransform()
    {
        TransformState.Redo();
        UpdateTransformPreviewMetadata();
        ShowChrome();
    }

    public async Task SaveTransformsAsync(CancellationToken cancellationToken = default)
    {
        if (ViewerState.CurrentImage is null || !TransformState.HasUnsavedChanges)
        {
            return;
        }

        var operations = TransformState.PendingTransforms.ToArray();
        var result = await _transformService.SaveTransformsAsync(
                ViewerState.CurrentImage.FilePath,
                operations,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            await HandleErrorAsync(result.Error, cancellationToken).ConfigureAwait(false);
            return;
        }

        TransformState.DiscardTransforms();
        ShowChrome();
    }

    public async Task<Result<Thumbnail>> GetThumbnailAsync(
        ImageItem item,
        CancellationToken cancellationToken = default)
    {
        return await _thumbnailService.GetThumbnailAsync(item, cancellationToken).ConfigureAwait(false);
    }

    public async Task ToggleThumbnailStrip(CancellationToken cancellationToken = default)
    {
        ThumbnailState.ToggleVisibility();
        Settings = Settings with { ThumbnailStripVisible = ThumbnailState.IsStripVisible };
        await SaveSettingsOrReportErrorAsync(cancellationToken).ConfigureAwait(false);
        ShowChrome();
    }

    public async Task SetThumbnailPosition(ThumbnailPosition position, CancellationToken cancellationToken = default)
    {
        ThumbnailState.SetStripPosition(position);
        Settings = Settings with { ThumbnailStripPosition = position };
        await SaveSettingsOrReportErrorAsync(cancellationToken).ConfigureAwait(false);
        ShowChrome();
    }

    public async Task<bool> ExportCurrentImageAsync(
        string destinationFilePath,
        ImageExportFormat targetFormat,
        CancellationToken cancellationToken = default)
    {
        if (ViewerState.CurrentImage is null)
        {
            return false;
        }

        var result = await _exportService.ExportImageAsync(
                ViewerState.CurrentImage.FilePath,
                destinationFilePath,
                new ExportOptions(
                    targetFormat,
                    Settings.DefaultJpegQuality,
                    Settings.PreserveExifOnExport,
                    OverwriteExisting: true),
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            await HandleErrorAsync(result.Error, cancellationToken).ConfigureAwait(false);
            return false;
        }

        ShowChrome();
        return true;
    }

    public async Task OpenRecentFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            var recentResult = await _recentFilesService.RemoveRecentFileAsync(filePath, cancellationToken)
                .ConfigureAwait(false);
            if (recentResult.IsSuccess)
            {
                SetRecentFiles(recentResult.Value);
            }

            await HandleErrorAsync(ViewerError.FileAccessError("OpenRecentFile", filePath), cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        await OpenFileAsync(filePath, cancellationToken).ConfigureAwait(false);
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

    public async Task SetSlideshowTransitionAsync(
        TransitionEffect transition,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(transition))
        {
            await HandleErrorAsync(
                    ViewerError.UnexpectedViewerError("SetSlideshowTransition", filePath: null),
                    cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        Settings = Settings with { SlideshowTransition = transition };
        await SaveSettingsOrReportErrorAsync(cancellationToken).ConfigureAwait(false);
        ShowChrome();
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
            TransformState.DiscardTransforms();
            ShowChrome();
        }
        finally
        {
            EndOperation(operation);
        }
    }

    private async Task ShiftComparisonAsync(int offset, CancellationToken cancellationToken)
    {
        var startIndex = ComparisonState.DisplayedIndices.Count > 0
            ? ComparisonState.DisplayedIndices[0]
            : PlaylistState.CurrentIndex;
        var indicesResult = _comparisonService.CalculateComparisonIndices(
            startIndex + offset,
            ComparisonState.ImageCount == 0 ? ComparisonService.DefaultImageCount : ComparisonState.ImageCount,
            PlaylistState.TotalCount);

        if (indicesResult.IsFailure)
        {
            await HandleErrorAsync(indicesResult.Error, cancellationToken).ConfigureAwait(false);
            return;
        }

        ApplyComparisonWindow(ComparisonState.Layout, indicesResult.Value);
        ShowChrome();
    }

    private async Task ApplyComparisonImageCountAsync(int imageCount, CancellationToken cancellationToken)
    {
        var indicesResult = _comparisonService.CalculateComparisonIndices(
            PlaylistState.CurrentIndex,
            imageCount,
            PlaylistState.TotalCount);

        if (indicesResult.IsFailure)
        {
            await HandleErrorAsync(indicesResult.Error, cancellationToken).ConfigureAwait(false);
            return;
        }

        ApplyComparisonWindow(ComparisonState.Layout, indicesResult.Value);
        ShowChrome();
    }

    private void ApplyComparisonWindow(ComparisonLayout layout, IReadOnlyList<int> displayedIndices)
    {
        var displayedItems = displayedIndices
            .Where(index => index >= 0 && index < PlaylistState.TotalCount)
            .Select(index => PlaylistState.Items[index])
            .ToArray();

        if (ComparisonState.IsActive)
        {
            ComparisonState.ShiftImages(displayedItems, displayedIndices);
        }
        else
        {
            ComparisonState.EnterComparison(layout, displayedItems, displayedIndices);
        }

        if (displayedIndices.Count > 0)
        {
            PlaylistState.SetCurrentIndex(displayedIndices[0]);
        }
    }

    private ComparisonImageViewport GetActiveComparisonViewport()
    {
        var activeIndex = Math.Clamp(
            ComparisonState.ActiveImageIndex,
            0,
            Math.Max(0, ComparisonState.ImageViewports.Count - 1));

        return ComparisonState.ImageViewports.Count == 0
            ? ComparisonImageViewport.Default
            : ComparisonState.ImageViewports[activeIndex];
    }

    private void ApplyComparisonZoom(double zoomLevel)
    {
        if (ComparisonState.ImageViewports.Count == 0)
        {
            return;
        }

        if (!ComparisonState.SyncZoomEnabled)
        {
            var activeIndex = Math.Clamp(
                ComparisonState.ActiveImageIndex,
                0,
                ComparisonState.ImageViewports.Count - 1);
            ComparisonState.ApplyViewportUpdate(
                activeIndex,
                ComparisonState.ImageViewports[activeIndex].WithZoom(zoomLevel));
            return;
        }

        var sourceIndex = Math.Clamp(
            ComparisonState.ActiveImageIndex,
            0,
            ComparisonState.ImageViewports.Count - 1);
        var sourceViewport = ComparisonState.ImageViewports[sourceIndex];
        var synchronized = _comparisonService.SynchronizeZoom(
            ComparisonState.ImageViewports.ToArray(),
            sourceIndex,
            zoomLevel,
            sourceViewport.PanOffset);

        if (synchronized.IsSuccess)
        {
            ComparisonState.ApplyViewportUpdates(synchronized.Value);
        }
    }

    private void ResetComparisonZoom()
    {
        if (ComparisonState.ImageViewports.Count == 0)
        {
            return;
        }

        if (!ComparisonState.SyncZoomEnabled)
        {
            var activeIndex = Math.Clamp(
                ComparisonState.ActiveImageIndex,
                0,
                ComparisonState.ImageViewports.Count - 1);
            ComparisonState.ApplyViewportUpdate(
                activeIndex,
                ComparisonState.ImageViewports[activeIndex].FitToWindow());
            return;
        }

        ComparisonState.ApplyViewportUpdates(
            ComparisonState.ImageViewports.Select(viewport => viewport.FitToWindow()).ToArray());
    }

    private bool IsOverlayComparisonActive()
    {
        return ComparisonState.IsActive && ComparisonState.Layout == ComparisonLayout.Overlay;
    }

    private async Task HandleErrorAsync(ViewerError error, CancellationToken cancellationToken)
    {
        CurrentErrorSeverity = error.Severity;
        ErrorMessage = error.UserMessage;
        await _errorLogService.LogErrorAsync(error, cancellationToken).ConfigureAwait(false);
    }

    private async Task SaveSettingsOrReportErrorAsync(CancellationToken cancellationToken)
    {
        var saveResult = await _settingsService.SaveSettingsAsync(Settings, cancellationToken).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            await HandleErrorAsync(saveResult.Error, cancellationToken).ConfigureAwait(false);
        }
    }

    private ICommandPaletteService BuildCommandPaletteService()
    {
        return new CommandPaletteService(
        [
            Register("file.open", "Open File", "File", "Ctrl+O", () => OpenFileRequested?.Invoke(this, EventArgs.Empty)),
            Register("view.fullscreen", "Toggle Fullscreen", "View", "F11", ToggleFullscreen),
            Register("view.fit", "Fit to Window", "View", "Ctrl+0", FitToWindow),
            Register("view.actual-size", "Actual Size", "View", "Ctrl+1", ActualSize),
            Register("view.zoom-in", "Zoom In", "View", "Ctrl++", ZoomIn),
            Register("view.zoom-out", "Zoom Out", "View", "Ctrl+-", ZoomOut),
            RegisterAsync("view.theme", "Toggle Theme", "View", "Theme", () => ToggleThemeAsync()),
            RegisterAsync("view.thumbnails", "Toggle Thumbnails", "View", "Ctrl+T", () => ToggleThumbnailStrip()),
            RegisterAsync("image.metadata", "Toggle Metadata", "Image", "Ctrl+I", () => ToggleMetadataPanelAsync()),
            RegisterAsync("image.rotate-left", "Rotate Left", "Image", "Ctrl+L", () => ApplyTransformAsync(TransformOperation.RotateLeft)),
            RegisterAsync("image.rotate-right", "Rotate Right", "Image", "Ctrl+R", () => ApplyTransformAsync(TransformOperation.RotateRight)),
            RegisterAsync("image.flip-horizontal", "Flip Horizontal", "Image", "Ctrl+H", () => ApplyTransformAsync(TransformOperation.FlipHorizontal)),
            RegisterAsync("image.flip-vertical", "Flip Vertical", "Image", "Ctrl+V", () => ApplyTransformAsync(TransformOperation.FlipVertical)),
            Register("image.undo", "Undo", "Image", "Ctrl+Z", UndoTransform),
            Register("image.redo", "Redo", "Image", "Ctrl+Shift+Z", RedoTransform),
            RegisterAsync("navigation.next", "Next Image", "Navigation", "Right", () => NavigateNextAsync()),
            RegisterAsync("navigation.previous", "Previous Image", "Navigation", "Left", () => NavigatePreviousAsync()),
            RegisterAsync("navigation.first", "First Image", "Navigation", "Home", () => NavigateFirstAsync()),
            RegisterAsync("navigation.last", "Last Image", "Navigation", "End", () => NavigateLastAsync()),
            RegisterAsync("comparison.side-by-side", "Side-by-Side Comparison", "Comparison", "Ctrl+D", () => EnterComparisonModeAsync(ComparisonLayout.SideBySide)),
            RegisterAsync("comparison.overlay", "Overlay Comparison", "Comparison", "Ctrl+Shift+D", () => EnterComparisonModeAsync(ComparisonLayout.Overlay)),
            Register("comparison.sync-zoom", "Toggle Sync Zoom", "Comparison", "Ctrl+L", ToggleComparisonSyncZoom)
        ]);
    }

    private static CommandRegistration Register(
        string id,
        string title,
        string category,
        string keyboardShortcut,
        Action action)
    {
        return new CommandRegistration(
            new CommandItem(id, title, category, keyboardShortcut),
            () =>
            {
                action();
                return Task.FromResult(Result.Success());
            });
    }

    private static CommandRegistration RegisterAsync(
        string id,
        string title,
        string category,
        string keyboardShortcut,
        Func<Task> action)
    {
        return new CommandRegistration(
            new CommandItem(id, title, category, keyboardShortcut),
            async () =>
            {
                await action().ConfigureAwait(false);
                return Result.Success();
            });
    }

    private void SetRecentFiles(IEnumerable<RecentFile> recentFiles)
    {
        RecentFiles.Clear();
        foreach (var recentFile in recentFiles)
        {
            RecentFiles.Add(recentFile);
        }
    }

    private void UpdateTransformPreviewMetadata()
    {
        if (ViewerState.CurrentImage is null)
        {
            return;
        }

        var width = ViewerState.CurrentImage.Metadata.PixelWidth;
        var height = ViewerState.CurrentImage.Metadata.PixelHeight;

        foreach (var operation in TransformState.PendingTransforms)
        {
            if (operation is TransformOperation.RotateLeft or TransformOperation.RotateRight)
            {
                (width, height) = (height, width);
            }
        }

        ViewerState.SetMetadata(ViewerState.CurrentImage.Metadata with
        {
            PixelWidth = width,
            PixelHeight = height
        });
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
