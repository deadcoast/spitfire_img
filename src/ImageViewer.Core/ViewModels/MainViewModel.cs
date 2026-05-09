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
    private readonly ILayoutService _layoutService;
    private readonly IAnimationService _animationService;
    private readonly IAnimationPlaybackTimer _animationPlaybackTimer;
    private readonly ISlideshowTimer _slideshowTimer;
    private readonly ICodecDetectionService _codecDetectionService;
    private readonly HashSet<string> _codecInstallPromptsShown = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DecodedImage> _slideshowPreloadCache = new(StringComparer.OrdinalIgnoreCase);
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
        IComparisonService? comparisonService = null,
        ILayoutService? layoutService = null,
        IAnimationService? animationService = null,
        IAnimationPlaybackTimer? animationPlaybackTimer = null,
        ISlideshowTimer? slideshowTimer = null,
        ICodecDetectionService? codecDetectionService = null)
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
        _layoutService = layoutService ?? new LayoutService();
        _animationService = animationService ?? new NoopAnimationService();
        _animationPlaybackTimer = animationPlaybackTimer ?? new NoopAnimationPlaybackTimer();
        _slideshowTimer = slideshowTimer ?? new NoopSlideshowTimer();
        _codecDetectionService = codecDetectionService ?? new NoopCodecDetectionService();
        _animationPlaybackTimer.Tick += OnAnimationPlaybackTimerTick;
        _slideshowTimer.Tick += OnSlideshowTimerTick;
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

    public LayoutState LayoutState { get; } = new();

    public AnimationState AnimationState { get; } = new();

    public SlideshowState SlideshowState { get; } = new();

    public CommandPaletteState CommandPaletteState { get; }

    public event EventHandler? OpenFileRequested;

    public event EventHandler? SearchRequested;

    public event EventHandler? SearchHidden;

    public event EventHandler<CodecInstallPromptRequest>? CodecInstallPromptRequested;

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
        var settingsResult = await _settingsService.LoadSettingsAsync(cancellationToken);
        if (settingsResult.IsSuccess)
        {
            Settings = settingsResult.Value;
            Settings = Settings.Normalize();
            ThumbnailState.SetVisibility(Settings.ThumbnailStripVisible);
            ThumbnailState.SetStripPosition(Settings.ThumbnailStripPosition);
            var recentFilesResult = await _recentFilesService.LoadRecentFilesAsync(cancellationToken);
            if (recentFilesResult.IsSuccess)
            {
                SetRecentFiles(recentFilesResult.Value);
            }

            return;
        }

        Settings = ViewerSettings.Default;
        await HandleErrorAsync(settingsResult.Error, cancellationToken);
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
                await HandleErrorAsync(validationResult.Error, operationToken);
                return;
            }

            var decodeResult = await _imageDecodeService.DecodeImageAsync(validationResult.Value, operationToken);
            if (decodeResult.IsFailure)
            {
                await HandleErrorAsync(decodeResult.Error, CancellationToken.None);
                return;
            }

            var playlistResult = await _folderPlaylistService.BuildPlaylistAsync(validationResult.Value, operationToken);
            if (playlistResult.IsFailure)
            {
                await HandleErrorAsync(playlistResult.Error, operationToken);
                var fallback = new Playlist([new ImageItem(validationResult.Value)], 0, Path.GetDirectoryName(validationResult.Value) ?? string.Empty);
                PlaylistState.SetPlaylist(fallback);
            }
            else
            {
                PlaylistState.SetPlaylist(playlistResult.Value);
            }

            ViewerState.SetImage(decodeResult.Value);
            await LoadAnimationForImageAsync(decodeResult.Value.FilePath, operationToken);
            SearchState.SetPlaylist(PlaylistState.Items);
            await RestorePreferredLayoutAsync(operationToken);
            var recentResult = await _recentFilesService.AddRecentFileAsync(validationResult.Value, operationToken);
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

    public bool CanAttemptOpenFormat(string extension)
    {
        return _imageDecodeService.IsSupportedFormat(extension) ||
               ImageFormatCatalog.IsHeicFormat(extension) ||
               ImageFormatCatalog.IsAvifFormat(extension);
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

    public void ToggleAnimationPlayback()
    {
        if (!AnimationState.IsAnimated)
        {
            return;
        }

        AnimationState.TogglePlayback();
        if (AnimationState.IsPlaying)
        {
            ScheduleCurrentAnimationFrame();
        }
        else
        {
            _animationPlaybackTimer.Stop();
        }

        ShowChrome();
    }

    public void AdvanceAnimationFrame()
    {
        if (!AnimationState.IsAnimated || AnimationState.IsPlaying)
        {
            return;
        }

        AnimationState.NextFrame();
        ShowChrome();
    }

    public void RewindAnimationFrame()
    {
        if (!AnimationState.IsAnimated || AnimationState.IsPlaying)
        {
            return;
        }

        AnimationState.PreviousFrame();
        ShowChrome();
    }

    public void AdvanceAnimationFrameFromControl()
    {
        if (!AnimationState.IsAnimated)
        {
            return;
        }

        PauseAnimationForManualControl();
        AnimationState.NextFrame();
        ShowChrome();
    }

    public void RewindAnimationFrameFromControl()
    {
        if (!AnimationState.IsAnimated)
        {
            return;
        }

        PauseAnimationForManualControl();
        AnimationState.PreviousFrame();
        ShowChrome();
    }

    public Result SeekAnimationFrameFromControl(int frameIndex)
    {
        if (!AnimationState.IsAnimated)
        {
            return Result.Failure(ViewerError.AnimationError("SeekAnimationFrame", null));
        }

        PauseAnimationForManualControl();
        var result = AnimationState.SeekToFrame(frameIndex);
        ShowChrome();
        return result;
    }

    public void AdvanceAnimationPlaybackFrame()
    {
        if (!AnimationState.IsAnimated || !AnimationState.IsPlaying)
        {
            return;
        }

        AnimationState.NextFrame();
        ScheduleCurrentAnimationFrame();
    }

    private void PauseAnimationForManualControl()
    {
        AnimationState.Pause();
        _animationPlaybackTimer.Stop();
    }

    public Task NavigateNextAsync(CancellationToken cancellationToken = default)
    {
        if (ComparisonState.IsActive)
        {
            return ShiftComparisonAsync(offset: 1, cancellationToken);
        }

        if (LayoutState.IsFilmstripActive)
        {
            return SetFilmstripFocusAsync(PlaylistState.CurrentIndex + 1, cancellationToken);
        }

        return NavigateToIndexAsync(PlaylistState.CurrentIndex + 1, cancellationToken);
    }

    public Task NavigatePreviousAsync(CancellationToken cancellationToken = default)
    {
        if (ComparisonState.IsActive)
        {
            return ShiftComparisonAsync(offset: -1, cancellationToken);
        }

        if (LayoutState.IsFilmstripActive)
        {
            return SetFilmstripFocusAsync(PlaylistState.CurrentIndex - 1, cancellationToken);
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

        LayoutState.ExitLayout();
        ApplyComparisonWindow(layout, indicesResult.Value);
        ShowChrome();
    }

    public void ExitComparisonMode()
    {
        ComparisonState.ExitComparison();
        ShowChrome();
    }

    public async Task EnterGridLayoutAsync(
        ImageDimensions? canvasSize = null,
        CancellationToken cancellationToken = default)
    {
        if (PlaylistState.TotalCount == 0)
        {
            await HandleErrorAsync(
                ViewerError.LayoutError("EnterGridLayout", "Grid layout requires an active playlist."),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var applied = await ApplyGridLayoutAsync(
            Settings.PreferredGridRows,
            Settings.PreferredGridColumns,
            Math.Max(0, PlaylistState.CurrentIndex),
            canvasSize,
            isNewGrid: true,
            cancellationToken).ConfigureAwait(false);

        if (applied)
        {
            ComparisonState.ExitComparison();
            await SaveLayoutPreferenceAsync(LayoutMode.Grid, cancellationToken).ConfigureAwait(false);
            ShowChrome();
        }
    }

    public void ExitLayoutMode()
    {
        LayoutState.ExitLayout();
        ShowChrome();
    }

    public async Task ExitLayoutModeAsync(CancellationToken cancellationToken = default)
    {
        LayoutState.ExitLayout();
        await SaveLayoutPreferenceAsync(LayoutMode.Single, cancellationToken).ConfigureAwait(false);
        ShowChrome();
    }

    public async Task EnterFilmstripLayoutAsync(CancellationToken cancellationToken = default)
    {
        if (PlaylistState.TotalCount == 0)
        {
            await HandleErrorAsync(
                ViewerError.LayoutError("EnterFilmstripLayout", "Filmstrip layout requires an active playlist."),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var dataResult = _layoutService.LoadFilmstripData(
            PlaylistState,
            Math.Max(0, PlaylistState.CurrentIndex),
            LayoutConfig.DefaultFilmstripThumbnailCount);
        if (dataResult.IsFailure)
        {
            await HandleErrorAsync(dataResult.Error, cancellationToken).ConfigureAwait(false);
            LayoutState.ExitLayout();
            return;
        }

        LayoutState.EnterFilmstrip(dataResult.Value);
        ComparisonState.ExitComparison();
        await SaveLayoutPreferenceAsync(LayoutMode.Filmstrip, cancellationToken).ConfigureAwait(false);
        ShowChrome();
    }

    public async Task SetFilmstripFocusAsync(
        int playlistIndex,
        CancellationToken cancellationToken = default)
    {
        if (!LayoutState.IsFilmstripActive || PlaylistState.TotalCount == 0)
        {
            return;
        }

        var targetIndex = Math.Clamp(playlistIndex, 0, PlaylistState.TotalCount - 1);
        await NavigateToIndexAsync(targetIndex, cancellationToken).ConfigureAwait(false);

        var dataResult = _layoutService.LoadFilmstripData(
            PlaylistState,
            PlaylistState.CurrentIndex,
            LayoutState.Config.FilmstripThumbnailCount);
        if (dataResult.IsFailure)
        {
            await HandleErrorAsync(dataResult.Error, cancellationToken).ConfigureAwait(false);
            LayoutState.ExitLayout();
            return;
        }

        LayoutState.EnterFilmstrip(dataResult.Value);
        ShowChrome();
    }

    public async Task ToggleSlideshowAsync(CancellationToken cancellationToken = default)
    {
        if (SlideshowState.IsActive)
        {
            StopSlideshow();
            return;
        }

        if (PlaylistState.TotalCount == 0)
        {
            await HandleErrorAsync(
                ViewerError.UnexpectedViewerError("StartSlideshow", filePath: null),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var config = new SlideshowConfig(
            Settings.Normalize().SlideshowInterval,
            Settings.SlideshowTransition);
        SlideshowState.Start(config, PlaylistState.CurrentIndex);
        _slideshowTimer.Start(config.Interval);
        ScheduleNextSlideshowPreload();
        ShowChrome();
    }

    public void ToggleSlideshowPause()
    {
        if (!SlideshowState.IsActive)
        {
            return;
        }

        if (SlideshowState.IsPaused)
        {
            SlideshowState.Resume();
            _slideshowTimer.Start(SlideshowState.Interval);
        }
        else
        {
            SlideshowState.Pause();
            _slideshowTimer.Stop();
        }

        ShowChrome();
    }

    public async Task IncreaseSlideshowIntervalAsync(CancellationToken cancellationToken = default)
    {
        await StepSlideshowIntervalAsync(delta: 1, cancellationToken).ConfigureAwait(false);
    }

    public async Task DecreaseSlideshowIntervalAsync(CancellationToken cancellationToken = default)
    {
        await StepSlideshowIntervalAsync(delta: -1, cancellationToken).ConfigureAwait(false);
    }

    public Task IncreaseGridDensityAsync(
        ImageDimensions? canvasSize = null,
        CancellationToken cancellationToken = default)
    {
        return ChangeGridDensityAsync(1, canvasSize, cancellationToken);
    }

    public Task DecreaseGridDensityAsync(
        ImageDimensions? canvasSize = null,
        CancellationToken cancellationToken = default)
    {
        return ChangeGridDensityAsync(-1, canvasSize, cancellationToken);
    }

    public void SetActiveGridImage(int visibleIndex)
    {
        if (!LayoutState.IsGridActive)
        {
            return;
        }

        LayoutState.SelectGridImage(visibleIndex);
        ShowChrome();
    }

    public async Task OpenActiveGridImageAsync(CancellationToken cancellationToken = default)
    {
        if (!LayoutState.IsGridActive || LayoutState.ActiveGridPlaylistIndex < 0)
        {
            return;
        }

        var targetIndex = LayoutState.ActiveGridPlaylistIndex;
        LayoutState.ExitLayout();
        await NavigateToIndexAsync(targetIndex, cancellationToken).ConfigureAwait(false);
    }

    public async Task ScrollGridAsync(
        int offset,
        ImageDimensions? canvasSize = null,
        CancellationToken cancellationToken = default)
    {
        if (!LayoutState.IsGridActive)
        {
            return;
        }

        var startIndexResult = _layoutService.CalculateGridStartIndex(
            LayoutState.GridStartIndex,
            offset,
            PlaylistState.TotalCount,
            LayoutState.GridLayout.TotalCells);
        if (startIndexResult.IsFailure)
        {
            await HandleErrorAsync(startIndexResult.Error, cancellationToken).ConfigureAwait(false);
            return;
        }

        await ApplyGridLayoutAsync(
            LayoutState.GridRows,
            LayoutState.GridColumns,
            startIndexResult.Value,
            canvasSize,
            isNewGrid: false,
            cancellationToken).ConfigureAwait(false);
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
        Settings = Settings with { ComparisonSyncZoom = ComparisonState.SyncZoomEnabled };
        ShowChrome();
    }

    public async Task ToggleComparisonSyncZoomAsync(CancellationToken cancellationToken = default)
    {
        ToggleComparisonSyncZoom();
        if (ComparisonState.IsActive)
        {
            await SaveSettingsOrReportErrorAsync(cancellationToken).ConfigureAwait(false);
        }
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
        SearchRequested?.Invoke(this, EventArgs.Empty);
        ShowChrome();
    }

    public void HideSearch()
    {
        SearchState.ClearSearch();
        SearchHidden?.Invoke(this, EventArgs.Empty);
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
            var resolvedIndex = Math.Clamp(targetIndex, 0, PlaylistState.TotalCount - 1);
            var targetItem = PlaylistState.Items[resolvedIndex];
            NavigationResult navigationResult;
            if (_slideshowPreloadCache.Remove(targetItem.FilePath, out var preloadedImage))
            {
                navigationResult = new NavigationResult(resolvedIndex, preloadedImage);
            }
            else
            {
                var playlist = new Playlist(PlaylistState.Items.ToList(), PlaylistState.CurrentIndex, PlaylistState.FolderPath);
                var navigationServiceResult = await _navigationService.NavigateToIndexAsync(playlist, targetIndex, operationToken)
                    .ConfigureAwait(false);

                if (navigationServiceResult.IsFailure)
                {
                    await HandleErrorAsync(navigationServiceResult.Error, CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                navigationResult = navigationServiceResult.Value;
            }

            PlaylistState.SetCurrentIndex(navigationResult.Index);
            ViewerState.SetImage(navigationResult.Image);
            await LoadAnimationForImageAsync(navigationResult.Image.FilePath, operationToken).ConfigureAwait(false);
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
        var indicesResult = CalculateComparisonIndicesWithDegradation(
            startIndex + offset,
            ComparisonState.ImageCount == 0 ? ComparisonService.DefaultImageCount : ComparisonState.ImageCount,
            PlaylistState.TotalCount);

        if (indicesResult.IsFailure)
        {
            await HandleErrorAsync(indicesResult.Error, cancellationToken).ConfigureAwait(false);
            ComparisonState.ExitComparison();
            return;
        }

        ApplyComparisonWindow(ComparisonState.Layout, indicesResult.Value);
        ShowChrome();
    }

    private async Task ApplyComparisonImageCountAsync(int imageCount, CancellationToken cancellationToken)
    {
        var indicesResult = CalculateComparisonIndicesWithDegradation(
            PlaylistState.CurrentIndex,
            imageCount,
            PlaylistState.TotalCount);

        if (indicesResult.IsFailure)
        {
            await HandleErrorAsync(indicesResult.Error, cancellationToken).ConfigureAwait(false);
            ComparisonState.ExitComparison();
            return;
        }

        ApplyComparisonWindow(ComparisonState.Layout, indicesResult.Value);
        ShowChrome();
    }

    private Result<IReadOnlyList<int>> CalculateComparisonIndicesWithDegradation(
        int currentIndex,
        int imageCount,
        int playlistSize)
    {
        if (playlistSize < ComparisonService.MinimumImageCount)
        {
            return _comparisonService.CalculateComparisonIndices(currentIndex, imageCount, playlistSize);
        }

        var requestedImageCount = Math.Clamp(
            imageCount,
            ComparisonService.MinimumImageCount,
            Math.Min(ComparisonService.MaximumImageCount, playlistSize));

        ViewerError? lastMemoryError = null;
        for (var candidateImageCount = requestedImageCount;
             candidateImageCount >= ComparisonService.MinimumImageCount;
             candidateImageCount--)
        {
            var indicesResult = _comparisonService.CalculateComparisonIndices(
                currentIndex,
                candidateImageCount,
                playlistSize);

            if (indicesResult.IsSuccess)
            {
                return indicesResult;
            }

            if (!IsMemoryError(indicesResult.Error))
            {
                return indicesResult;
            }

            lastMemoryError = indicesResult.Error;
        }

        return Result<IReadOnlyList<int>>.Failure(
            lastMemoryError ?? ViewerError.ComparisonError(
                "CalculateComparisonIndices",
                "Unable to load images for comparison."));
    }

    private async Task ChangeGridDensityAsync(
        int delta,
        ImageDimensions? canvasSize,
        CancellationToken cancellationToken)
    {
        if (!LayoutState.IsGridActive)
        {
            return;
        }

        var nextDensity = Math.Clamp(
            LayoutState.GridRows + delta,
            LayoutConfig.MinimumGridDensity,
            LayoutConfig.MaximumGridDensity);

        var applied = await ApplyGridLayoutAsync(
            nextDensity,
            nextDensity,
            LayoutState.GridStartIndex,
            canvasSize,
            isNewGrid: false,
            cancellationToken).ConfigureAwait(false);
        if (applied)
        {
            await SaveLayoutPreferenceAsync(LayoutMode.Grid, cancellationToken).ConfigureAwait(false);
            ShowChrome();
        }
    }

    private async Task<bool> ApplyGridLayoutAsync(
        int rows,
        int columns,
        int startIndex,
        ImageDimensions? canvasSize,
        bool isNewGrid,
        CancellationToken cancellationToken)
    {
        var layoutSize = canvasSize ?? new ImageDimensions(Settings.WindowWidth, Settings.WindowHeight);
        ViewerError? lastMemoryError = null;

        foreach (var (candidateRows, candidateColumns) in GetGridDensityFallbacks(rows, columns))
        {
            var layoutResult = _layoutService.CalculateGridLayout(candidateRows, candidateColumns, layoutSize);
            if (layoutResult.IsFailure)
            {
                if (IsMemoryError(layoutResult.Error))
                {
                    lastMemoryError = layoutResult.Error;
                    continue;
                }

                await HandleLayoutFailureAsync(layoutResult.Error, cancellationToken).ConfigureAwait(false);
                return false;
            }

            var imagesResult = _layoutService.LoadGridImages(PlaylistState, startIndex, layoutResult.Value);
            if (imagesResult.IsFailure)
            {
                if (IsMemoryError(imagesResult.Error))
                {
                    lastMemoryError = imagesResult.Error;
                    continue;
                }

                await HandleLayoutFailureAsync(imagesResult.Error, cancellationToken).ConfigureAwait(false);
                return false;
            }

            if (isNewGrid)
            {
                LayoutState.EnterGrid(layoutResult.Value, imagesResult.Value, startIndex);
            }
            else
            {
                LayoutState.UpdateGrid(layoutResult.Value, imagesResult.Value, startIndex);
            }

            return true;
        }

        if (lastMemoryError is not null)
        {
            await HandleLayoutFailureAsync(lastMemoryError, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private static IEnumerable<(int Rows, int Columns)> GetGridDensityFallbacks(int rows, int columns)
    {
        var seen = new HashSet<(int Rows, int Columns)>();
        var maxDensity = Math.Max(rows, columns);
        for (var density = maxDensity; density >= LayoutConfig.MinimumGridDensity; density--)
        {
            var candidateRows = Math.Clamp(rows, LayoutConfig.MinimumGridDensity, density);
            var candidateColumns = Math.Clamp(columns, LayoutConfig.MinimumGridDensity, density);
            var candidate = (candidateRows, candidateColumns);
            if (seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private async Task HandleLayoutFailureAsync(ViewerError error, CancellationToken cancellationToken)
    {
        await HandleErrorAsync(error, cancellationToken).ConfigureAwait(false);
        LayoutState.ExitLayout();
    }

    private static bool IsMemoryError(ViewerError error)
    {
        return string.Equals(error.ExceptionType, nameof(OutOfMemoryException), StringComparison.Ordinal)
            || error.UserMessage.Contains("memory", StringComparison.OrdinalIgnoreCase)
            || error.UserMessage.Contains("too large", StringComparison.OrdinalIgnoreCase);
    }

    private async Task RestorePreferredLayoutAsync(CancellationToken cancellationToken)
    {
        switch (Settings.PreferredLayout)
        {
            case LayoutMode.Grid:
                await ApplyGridLayoutAsync(
                    Settings.PreferredGridRows,
                    Settings.PreferredGridColumns,
                    Math.Max(0, PlaylistState.CurrentIndex),
                    canvasSize: null,
                    isNewGrid: true,
                    cancellationToken).ConfigureAwait(false);
                break;
            case LayoutMode.Filmstrip:
                var dataResult = _layoutService.LoadFilmstripData(
                    PlaylistState,
                    Math.Max(0, PlaylistState.CurrentIndex),
                    Settings.PreferredFilmstripThumbnailCount);
                if (dataResult.IsFailure)
                {
                    await HandleErrorAsync(dataResult.Error, cancellationToken).ConfigureAwait(false);
                    LayoutState.ExitLayout();
                    return;
                }

                LayoutState.EnterFilmstrip(dataResult.Value);
                ComparisonState.ExitComparison();
                break;
            case LayoutMode.Single:
            default:
                LayoutState.ExitLayout();
                break;
        }
    }

    private async Task SaveLayoutPreferenceAsync(
        LayoutMode preferredLayout,
        CancellationToken cancellationToken)
    {
        Settings = Settings with
        {
            PreferredLayout = preferredLayout,
            PreferredGridRows = LayoutState.GridRows,
            PreferredGridColumns = LayoutState.GridColumns,
            PreferredFilmstripThumbnailCount = LayoutState.Config.FilmstripThumbnailCount
        };
        await SaveSettingsOrReportErrorAsync(cancellationToken).ConfigureAwait(false);
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

        ComparisonState.SetSyncZoom(Settings.ComparisonSyncZoom);

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
        RequestCodecInstallPrompt(error);
    }

    private void RequestCodecInstallPrompt(ViewerError error)
    {
        if (error.Category != ErrorCategory.CodecUnavailableError)
        {
            return;
        }

        var extension = Path.GetExtension(error.FilePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return;
        }

        var prompt = GetCodecPromptDetails(extension);
        if (prompt is null || !_codecInstallPromptsShown.Add(extension))
        {
            return;
        }

        var storeUrl = prompt.Value.GetStoreUrl();
        if (storeUrl.IsFailure)
        {
            return;
        }

        CodecInstallPromptRequested?.Invoke(
            this,
            new CodecInstallPromptRequest(prompt.Value.FormatName, error.UserMessage, storeUrl.Value));
    }

    private (string FormatName, Func<Result<string>> GetStoreUrl)? GetCodecPromptDetails(string? extension)
    {
        if (ImageFormatCatalog.IsHeicFormat(extension))
        {
            return ("HEIC", _codecDetectionService.GetHeicStoreUrl);
        }

        if (ImageFormatCatalog.IsAvifFormat(extension))
        {
            return ("AVIF", _codecDetectionService.GetAvifStoreUrl);
        }

        return null;
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
            Register("view.actual-size", "Actual Size", "View", "Ctrl+Shift+1", ActualSize),
            Register("view.zoom-in", "Zoom In", "View", "Ctrl++", ZoomIn),
            Register("view.zoom-out", "Zoom Out", "View", "Ctrl+-", ZoomOut),
            Register("view.command-palette", "Open Command Palette", "View", "Ctrl+Shift+P", ShowCommandPalette),
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
            Register("navigation.search", "Search Images", "Navigation", "Ctrl+F", ShowSearch),
            RegisterAsync("slideshow.toggle", "Toggle Slideshow", "Slideshow", "F5", () => ToggleSlideshowAsync()),
            Register("slideshow.pause", "Pause or Resume Slideshow", "Slideshow", "Space", ToggleSlideshowPause),
            RegisterAsync("slideshow.interval-up", "Increase Slideshow Interval", "Slideshow", "+", () => IncreaseSlideshowIntervalAsync()),
            RegisterAsync("slideshow.interval-down", "Decrease Slideshow Interval", "Slideshow", "-", () => DecreaseSlideshowIntervalAsync()),
            RegisterAsync("comparison.side-by-side", "Side-by-Side Comparison", "Comparison", "Ctrl+D", () => EnterComparisonModeAsync(ComparisonLayout.SideBySide)),
            RegisterAsync("comparison.overlay", "Overlay Comparison", "Comparison", "Ctrl+Shift+D", () => EnterComparisonModeAsync(ComparisonLayout.Overlay)),
            RegisterAsync("comparison.sync-zoom", "Toggle Sync Zoom", "Comparison", "Ctrl+L", () => ToggleComparisonSyncZoomAsync()),
            Register("comparison.swap-overlay", "Swap Overlay Layers", "Comparison", "S", SwapOverlayLayers),
            RegisterAsync("comparison.count-two", "Show Two Comparison Images", "Comparison", "Ctrl+2", () => SetComparisonImageCountAsync(2)),
            RegisterAsync("comparison.count-three", "Show Three Comparison Images", "Comparison", "Ctrl+3", () => SetComparisonImageCountAsync(3)),
            RegisterAsync("comparison.count-four", "Show Four Comparison Images", "Comparison", "Ctrl+4", () => SetComparisonImageCountAsync(4)),
            RegisterAsync("layout.single", "Single Image View", "Layout", "Ctrl+1", () => ExitLayoutModeAsync()),
            RegisterAsync("layout.grid", "Grid Layout", "Layout", "Ctrl+G", () => EnterGridLayoutAsync()),
            RegisterAsync("layout.filmstrip", "Filmstrip Layout", "Layout", "Ctrl+Shift+F", () => EnterFilmstripLayoutAsync()),
            RegisterAsync("layout.grid-density-up", "Increase Grid Density", "Layout", "Ctrl++", () => IncreaseGridDensityAsync()),
            RegisterAsync("layout.grid-density-down", "Decrease Grid Density", "Layout", "Ctrl+-", () => DecreaseGridDensityAsync()),
            Register("animation.toggle-playback", "Play or Pause Animation", "Animation", "P", ToggleAnimationPlayback),
            Register("animation.next-frame", "Next Animation Frame", "Animation", ".", AdvanceAnimationFrame),
            Register("animation.previous-frame", "Previous Animation Frame", "Animation", ",", RewindAnimationFrame)
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
            new CommandItem(id, title, category, keyboardShortcut, Description: DescribeCommand(id)),
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
            new CommandItem(id, title, category, keyboardShortcut, Description: DescribeCommand(id)),
            async () =>
            {
                await action().ConfigureAwait(false);
                return Result.Success();
            });
    }

    private static string DescribeCommand(string id)
    {
        return id switch
        {
            "file.open" => "Choose an image file and load its folder as the playlist.",
            "view.fullscreen" => "Enter or leave fullscreen without changing the current image.",
            "view.fit" => "Scale the current image to fit inside the viewer.",
            "view.actual-size" => "Inspect the image at its native pixel size.",
            "view.zoom-in" => "Increase zoom for closer image inspection.",
            "view.zoom-out" => "Reduce zoom while keeping the current image open.",
            "view.command-palette" => "Open this command surface for keyboard-driven actions.",
            "view.theme" => "Switch between the available viewer themes.",
            "view.thumbnails" => "Show or hide the thumbnail strip for the current folder.",
            "image.metadata" => "Open or close the metadata panel for the current image.",
            "image.rotate-left" => "Preview a left rotation on the current image.",
            "image.rotate-right" => "Preview a right rotation on the current image.",
            "image.flip-horizontal" => "Preview a horizontal flip on the current image.",
            "image.flip-vertical" => "Preview a vertical flip on the current image.",
            "image.undo" => "Undo the most recent pending image transform.",
            "image.redo" => "Redo the most recently undone image transform.",
            "navigation.next" => "Move to the next image in the folder playlist.",
            "navigation.previous" => "Move to the previous image in the folder playlist.",
            "navigation.first" => "Jump to the first image in the folder playlist.",
            "navigation.last" => "Jump to the last image in the folder playlist.",
            "navigation.search" => "Search images in the active folder playlist.",
            "slideshow.toggle" => "Start or stop slideshow playback.",
            "slideshow.pause" => "Pause or resume the active slideshow.",
            "slideshow.interval-up" => "Increase the slideshow interval.",
            "slideshow.interval-down" => "Decrease the slideshow interval.",
            "comparison.side-by-side" => "Compare images side by side.",
            "comparison.overlay" => "Compare images with overlay opacity controls.",
            "comparison.sync-zoom" => "Toggle shared zoom and pan during comparison.",
            "comparison.swap-overlay" => "Swap the active overlay comparison layers.",
            "comparison.count-two" => "Show two images in comparison mode.",
            "comparison.count-three" => "Show three images in comparison mode.",
            "comparison.count-four" => "Show four images in comparison mode.",
            "layout.single" => "Return to the single image inspection layout.",
            "layout.grid" => "Open the folder playlist in grid layout.",
            "layout.filmstrip" => "Open the folder playlist in filmstrip layout.",
            "layout.grid-density-up" => "Show more images per grid page.",
            "layout.grid-density-down" => "Show fewer images per grid page.",
            "animation.toggle-playback" => "Play or pause the current animated image.",
            "animation.next-frame" => "Advance the animated image by one frame.",
            "animation.previous-frame" => "Move the animated image back one frame.",
            _ => "Run this viewer command."
        };
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

    private async Task LoadAnimationForImageAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!Path.GetExtension(filePath).Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            AnimationState.Clear();
            _animationPlaybackTimer.Stop();
            return;
        }

        var animationResult = await _animationService.LoadAnimatedGifAsync(filePath, cancellationToken).ConfigureAwait(false);
        if (animationResult.IsSuccess)
        {
            AnimationState.LoadAnimation(animationResult.Value);
            if (!Settings.GifAutoPlay)
            {
                AnimationState.Pause();
            }

            if (AnimationState.IsPlaying)
            {
                ScheduleCurrentAnimationFrame();
            }
            else
            {
                _animationPlaybackTimer.Stop();
            }

            return;
        }

        AnimationState.Clear();
        _animationPlaybackTimer.Stop();
        await HandleErrorAsync(animationResult.Error, CancellationToken.None).ConfigureAwait(false);
    }

    private void ScheduleCurrentAnimationFrame()
    {
        var delayResult = AnimationState.GetFrameDelay(AnimationState.CurrentFrame);
        if (delayResult.IsSuccess)
        {
            _animationPlaybackTimer.Start(TimeSpan.FromMilliseconds(delayResult.Value));
        }
    }

    private void OnAnimationPlaybackTimerTick(object? sender, EventArgs e)
    {
        AdvanceAnimationPlaybackFrame();
    }

    private async void OnSlideshowTimerTick(object? sender, EventArgs e)
    {
        await AdvanceSlideshowAsync().ConfigureAwait(false);
    }

    private async Task AdvanceSlideshowAsync(CancellationToken cancellationToken = default)
    {
        if (!SlideshowState.IsActive || SlideshowState.IsPaused || PlaylistState.TotalCount == 0)
        {
            return;
        }

        if (ComparisonState.IsActive)
        {
            await AdvanceComparisonSlideshowAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (LayoutState.IsGridActive)
        {
            await AdvanceGridSlideshowAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (LayoutState.IsFilmstripActive)
        {
            await SetFilmstripFocusAsync(GetNextPlaylistIndex(PlaylistState.CurrentIndex), cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await NavigateToIndexAsync(GetNextPlaylistIndex(PlaylistState.CurrentIndex), cancellationToken)
                .ConfigureAwait(false);
        }

        SlideshowState.SetCurrentImageIndex(PlaylistState.CurrentIndex);

        if (SlideshowState.IsActive && !SlideshowState.IsPaused)
        {
            _slideshowTimer.Start(SlideshowState.Interval);
            ScheduleNextSlideshowPreload();
        }
    }

    private async Task AdvanceComparisonSlideshowAsync(CancellationToken cancellationToken)
    {
        var startIndex = ComparisonState.DisplayedIndices.Count > 0
            ? ComparisonState.DisplayedIndices[0]
            : PlaylistState.CurrentIndex;
        var nextStartIndex = GetNextPlaylistIndex(startIndex);
        var indicesResult = _comparisonService.CalculateComparisonIndices(
            nextStartIndex,
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

    private async Task AdvanceGridSlideshowAsync(CancellationToken cancellationToken)
    {
        var nextStartIndex = GetNextPlaylistIndex(LayoutState.GridStartIndex);
        await ApplyGridLayoutAsync(
            LayoutState.GridRows,
            LayoutState.GridColumns,
            nextStartIndex,
            canvasSize: null,
            isNewGrid: false,
            cancellationToken).ConfigureAwait(false);
        ShowChrome();
    }

    private int GetNextPlaylistIndex(int currentIndex)
    {
        if (PlaylistState.TotalCount == 0)
        {
            return 0;
        }

        var nextIndex = currentIndex + 1;
        return nextIndex >= PlaylistState.TotalCount ? 0 : nextIndex;
    }

    private void StopSlideshow()
    {
        SlideshowState.Stop();
        _slideshowTimer.Stop();
        _slideshowPreloadCache.Clear();
        ShowChrome();
    }

    private void ScheduleNextSlideshowPreload()
    {
        _ = PreloadNextSlideshowImageAsync();
    }

    private async Task PreloadNextSlideshowImageAsync()
    {
        if (!SlideshowState.IsActive || SlideshowState.IsPaused || PlaylistState.TotalCount == 0)
        {
            return;
        }

        var nextIndex = GetNextPlaylistIndex(PlaylistState.CurrentIndex);
        var item = PlaylistState.Items[nextIndex];
        if (_slideshowPreloadCache.ContainsKey(item.FilePath))
        {
            return;
        }

        var decodeResult = await _imageDecodeService.DecodeImageAsync(item.FilePath, CancellationToken.None)
            .ConfigureAwait(false);
        if (decodeResult.IsFailure)
        {
            return;
        }

        _slideshowPreloadCache[item.FilePath] = decodeResult.Value;
        TrimSlideshowPreloadCache();
    }

    private void TrimSlideshowPreloadCache()
    {
        const int maximumPreloadedImages = 2;
        if (_slideshowPreloadCache.Count <= maximumPreloadedImages)
        {
            return;
        }

        foreach (var key in _slideshowPreloadCache.Keys.Take(_slideshowPreloadCache.Count - maximumPreloadedImages).ToArray())
        {
            _slideshowPreloadCache.Remove(key);
        }
    }

    private async Task StepSlideshowIntervalAsync(
        int delta,
        CancellationToken cancellationToken)
    {
        var intervals = SlideshowConfig.AllowedIntervals;
        var currentInterval = SlideshowState.IsActive
            ? SlideshowState.Interval
            : Settings.Normalize().SlideshowInterval;
        var currentIndex = FindIntervalIndex(intervals, currentInterval);
        if (currentIndex < 0)
        {
            currentIndex = FindIntervalIndex(intervals, SlideshowConfig.DefaultInterval);
        }

        var nextIndex = Math.Clamp(currentIndex + delta, 0, intervals.Count - 1);
        var nextInterval = intervals[nextIndex];
        SlideshowState.SetInterval(nextInterval);
        Settings = Settings with { SlideshowInterval = nextInterval };
        await SaveSettingsOrReportErrorAsync(cancellationToken).ConfigureAwait(false);

        if (SlideshowState.IsActive && !SlideshowState.IsPaused)
        {
            _slideshowTimer.Start(nextInterval);
        }

        ShowChrome();
    }

    private static int FindIntervalIndex(IReadOnlyList<TimeSpan> intervals, TimeSpan interval)
    {
        for (var index = 0; index < intervals.Count; index++)
        {
            if (intervals[index] == interval)
            {
                return index;
            }
        }

        return -1;
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

    private sealed class NoopAnimationService : IAnimationService
    {
        public Task<Result<AnimatedGif>> LoadAnimatedGifAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<AnimatedGif>.Failure(
                ViewerError.AnimationError("LoadAnimatedGif", filePath)));
        }
    }

    private sealed class NoopAnimationPlaybackTimer : IAnimationPlaybackTimer
    {
        public event EventHandler? Tick
        {
            add { }
            remove { }
        }

        public void Start(TimeSpan interval)
        {
        }

        public void Stop()
        {
        }
    }

    private sealed class NoopSlideshowTimer : ISlideshowTimer
    {
        public event EventHandler? Tick
        {
            add { }
            remove { }
        }

        public void Start(TimeSpan interval)
        {
        }

        public void Stop()
        {
        }
    }

    private sealed class NoopCodecDetectionService : ICodecDetectionService
    {
        public Task<Result<CodecAvailability>> DetectCodecsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<CodecAvailability>.Success(
                new CodecAvailability(HeicSupported: false, AvifSupported: false, DateTimeOffset.UtcNow)));
        }

        public Result<bool> IsHeicSupported()
        {
            return Result<bool>.Success(false);
        }

        public Result<bool> IsAvifSupported()
        {
            return Result<bool>.Success(false);
        }

        public Result<string> GetHeicStoreUrl()
        {
            return Result<string>.Failure(ViewerError.CodecDetectionError("GetHeicStoreUrl"));
        }

        public Result<string> GetAvifStoreUrl()
        {
            return Result<string>.Failure(ViewerError.CodecDetectionError("GetAvifStoreUrl"));
        }
    }
}
