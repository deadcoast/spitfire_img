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
    private readonly ISlideshowService _slideshowService;
    private readonly IComparisonService _comparisonService;
    private readonly ICommandPaletteService _commandPaletteService;
    private readonly IAnimationService _animationService;
    private readonly ILayoutService _layoutService;
    private readonly ICodecDetectionService? _codecDetectionService;
    private readonly SlideshowTransitionService _slideshowTransitionService = new();
    private readonly HashSet<string> _promptedCodecFormats = new(StringComparer.OrdinalIgnoreCase);
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
            new MetadataService(),
            new SlideshowService(),
            new ComparisonService(imageDecodeService),
            new CommandPaletteService(),
            new AnimationService())
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
        ISlideshowService slideshowService,
        IComparisonService comparisonService,
        ICommandPaletteService commandPaletteService,
        IAnimationService animationService,
        ICodecDetectionService? codecDetectionService = null,
        ILayoutService? layoutService = null)
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
        _slideshowService = slideshowService;
        _comparisonService = comparisonService;
        _commandPaletteService = commandPaletteService;
        _animationService = animationService;
        _layoutService = layoutService ?? new LayoutService(imageDecodeService);
        _codecDetectionService = codecDetectionService;
        SearchState = new SearchState(searchService);
        ThumbnailState = new ThumbnailState();
        TransformState = new TransformState();
        MetadataState = new MetadataState();
        SlideshowState = new SlideshowState();
        ComparisonState = new ComparisonState();
        CommandPaletteState = new CommandPaletteState(_commandPaletteService);
        AnimationState = new AnimationState();
        LayoutState = new LayoutState();
        _slideshowService.AdvanceRequested += OnSlideshowAdvanceRequested;
    }

    public event EventHandler<CodecInstallPrompt>? CodecInstallPromptRequested;

    public event EventHandler? OpenFileRequested;

    public ViewerState ViewerState { get; } = new();

    public PlaylistState PlaylistState { get; } = new();

    public ObservableCollection<RecentFile> RecentFiles { get; } = [];

    public SearchState SearchState { get; }

    public ThumbnailState ThumbnailState { get; }

    public TransformState TransformState { get; }

    public MetadataState MetadataState { get; }

    public SlideshowState SlideshowState { get; }

    public ComparisonState ComparisonState { get; }

    public CommandPaletteState CommandPaletteState { get; }

    public AnimationState AnimationState { get; }

    public LayoutState LayoutState { get; }

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

    public bool IsSupportedOpenExtension(string extension)
    {
        return _imageDecodeService.IsSupportedFormat(extension);
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
            await LoadAnimationForImageAsync(decodeResult.Value.FilePath, operationToken).ConfigureAwait(false);
            SearchState.SetPlaylist(PlaylistState.Items);
            var recentResult = await _recentFilesService.AddRecentFileAsync(validationResult.Value, operationToken).ConfigureAwait(false);
            if (recentResult.IsSuccess)
            {
                SetRecentFiles(recentResult.Value);
            }

            await RestorePreferredLayoutAsync(operationToken).ConfigureAwait(false);
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
        if (ComparisonState.IsActive)
        {
            return ShiftComparisonAsync(1, cancellationToken);
        }

        if (LayoutState is { IsActive: true, Mode: LayoutMode.Filmstrip })
        {
            return NavigateFilmstripAsync(1, cancellationToken);
        }

        return NavigateToIndexAsync(PlaylistState.CurrentIndex + 1, cancellationToken);
    }

    public Task NavigatePreviousAsync(CancellationToken cancellationToken = default)
    {
        if (ComparisonState.IsActive)
        {
            return ShiftComparisonAsync(-1, cancellationToken);
        }

        if (LayoutState is { IsActive: true, Mode: LayoutMode.Filmstrip })
        {
            return NavigateFilmstripAsync(-1, cancellationToken);
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

    public void ToggleSlideshow()
    {
        if (SlideshowState.IsActive)
        {
            _slideshowService.StopSlideshow();
            SlideshowState.Stop();
            ShowChrome();
            return;
        }

        if (PlaylistState.TotalCount == 0)
        {
            return;
        }

        var playlist = new Playlist(PlaylistState.Items.ToList(), PlaylistState.CurrentIndex, PlaylistState.FolderPath);
        var config = SlideshowConfig.FromSettings(Settings);
        var result = _slideshowService.StartSlideshow(playlist, PlaylistState.CurrentIndex, config);
        if (result.IsFailure)
        {
            _ = HandleErrorAsync(result.Error, CancellationToken.None);
            return;
        }

        SlideshowState.Start(PlaylistState.CurrentIndex, config);
        PreloadNextSlideshowImage();
        ShowChrome();
    }

    public Task IncreaseSlideshowIntervalAsync(CancellationToken cancellationToken = default)
    {
        return StepSlideshowIntervalAsync(direction: 1, cancellationToken);
    }

    public Task DecreaseSlideshowIntervalAsync(CancellationToken cancellationToken = default)
    {
        return StepSlideshowIntervalAsync(direction: -1, cancellationToken);
    }

    public Task SetSlideshowIntervalAsync(int intervalSeconds, CancellationToken cancellationToken = default)
    {
        return UpdateSlideshowIntervalAsync(intervalSeconds, cancellationToken);
    }

    public async Task SetSlideshowTransitionAsync(
        TransitionEffect transition,
        CancellationToken cancellationToken = default)
    {
        Settings = Settings with { SlideshowTransition = transition };
        var config = SlideshowConfig.FromSettings(Settings);
        SlideshowState.SetTransition(config.Transition);
        if (SlideshowState.IsActive)
        {
            _slideshowService.UpdateConfig(config);
        }

        await SaveSettingsOrReportErrorAsync(cancellationToken).ConfigureAwait(false);
        ShowChrome();
    }

    public SlideshowTransitionSpec BeginSlideshowTransition(TransitionEffect transition)
    {
        var spec = _slideshowTransitionService.GetTransition(transition);
        SlideshowState.BeginTransition(spec);
        return spec;
    }

    public void EndSlideshowTransition()
    {
        SlideshowState.EndTransition();
    }

    public void StopSlideshow()
    {
        _slideshowService.StopSlideshow();
        SlideshowState.Stop();
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
            _slideshowService.ResumeSlideshow();
            SlideshowState.Resume();
        }
        else
        {
            _slideshowService.PauseSlideshow();
            SlideshowState.Pause();
        }

        ShowChrome();
    }

    public void ToggleAnimationPlayback()
    {
        AnimationState.TogglePlayback();
        if (AnimationState.IsAnimationActive)
        {
            Settings = Settings with { GifAutoPlay = AnimationState.IsPlaying };
            SaveSettingsOrReportError();
        }

        ShowChrome();
    }

    public void AdvanceAnimationFrame()
    {
        AnimationState.AdvanceFrameManually();
        ShowChrome();
    }

    public void RewindAnimationFrame()
    {
        AnimationState.RewindFrameManually();
        ShowChrome();
    }

    public void StepAnimationFrameFromControl(int direction)
    {
        if (direction >= 0)
        {
            AnimationState.StepNextAndPause();
        }
        else
        {
            AnimationState.StepPreviousAndPause();
        }

        ShowChrome();
    }

    public void SeekAnimationFrame(int frameIndex)
    {
        AnimationState.SeekFrameManually(frameIndex);
        ShowChrome();
    }

    public void AdvanceAnimationPlaybackFrame()
    {
        if (AnimationState.IsPlaying)
        {
            AnimationState.AdvanceFrame();
        }
    }

    public async Task EnterComparisonAsync(
        ComparisonLayout layout,
        int imageCount = 2,
        CancellationToken cancellationToken = default)
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
            var result = await _comparisonService.EnterComparisonAsync(playlist, layout, imageCount, operationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                await HandleErrorAsync(result.Error, CancellationToken.None).ConfigureAwait(false);
                return;
            }

            ApplyComparisonSet(result.Value);
            ShowChrome();
        }
        finally
        {
            EndOperation(operation);
        }
    }

    public void ExitComparison()
    {
        ComparisonState.ExitComparison();
        ShowChrome();
    }

    public Task ChangeComparisonImageCountAsync(
        int imageCount,
        CancellationToken cancellationToken = default)
    {
        return ComparisonState.IsActive
            ? EnterComparisonAsync(ComparisonLayout.SideBySide, imageCount, cancellationToken)
            : Task.CompletedTask;
    }

    public void SelectComparisonImage(int visibleIndex)
    {
        ComparisonState.SelectImage(visibleIndex);
        ShowChrome();
    }

    public void SelectNextComparisonImage()
    {
        ComparisonState.SelectNextImage();
        ShowChrome();
    }

    public async Task OpenSelectedComparisonImageAsync(CancellationToken cancellationToken = default)
    {
        var activeImage = ComparisonState.ActiveImage;
        if (activeImage is null)
        {
            return;
        }

        ComparisonState.ExitComparison();
        await NavigateToIndexAsync(activeImage.PlaylistIndex, cancellationToken).ConfigureAwait(false);
    }

    public void SetOverlayOpacity(double opacity)
    {
        ComparisonState.SetOverlayOpacity(opacity);
        ShowChrome();
    }

    public void IncreaseOverlayOpacity()
    {
        ComparisonState.AdjustOverlayOpacity(0.1);
        ShowChrome();
    }

    public void DecreaseOverlayOpacity()
    {
        ComparisonState.AdjustOverlayOpacity(-0.1);
        ShowChrome();
    }

    public void SwapOverlayLayers()
    {
        ComparisonState.SwapOverlayLayers();
        ShowChrome();
    }

    public void ToggleComparisonSyncZoom()
    {
        ComparisonState.ToggleSyncZoom();
        Settings = Settings with { ComparisonSyncZoom = ComparisonState.IsSyncZoomEnabled };
        SaveSettingsOrReportError();
        ShowChrome();
    }

    public void SetComparisonRelativePan(RelativePanOffset relativePan)
    {
        if (ComparisonState.IsSyncZoomEnabled)
        {
            ComparisonState.SetSynchronizedRelativePan(relativePan);
        }
        else
        {
            ComparisonState.SetActiveViewportRelativePan(relativePan);
        }

        ShowChrome();
    }

    public async Task EnterGridLayoutAsync(CancellationToken cancellationToken = default)
    {
        if (PlaylistState.TotalCount == 0)
        {
            return;
        }

        await RefreshGridLayoutAsync(
                CreatePreferredGridConfig(),
                PlaylistState.CurrentIndex,
                persistPreference: true,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task EnterFilmstripLayoutAsync(CancellationToken cancellationToken = default)
    {
        if (PlaylistState.TotalCount == 0)
        {
            return;
        }

        await FocusFilmstripIndexAsync(
                PlaylistState.CurrentIndex,
                persistPreference: true,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SelectFilmstripImageAsync(int visibleIndex, CancellationToken cancellationToken = default)
    {
        if (LayoutState is not { IsActive: true, Mode: LayoutMode.Filmstrip } ||
            visibleIndex < 0 ||
            visibleIndex >= LayoutState.VisibleImages.Count)
        {
            return Task.CompletedTask;
        }

        return FocusFilmstripIndexAsync(
            LayoutState.VisibleImages[visibleIndex].PlaylistIndex,
            persistPreference: false,
            cancellationToken);
    }

    private Task NavigateFilmstripAsync(int offset, CancellationToken cancellationToken)
    {
        if (PlaylistState.TotalCount == 0)
        {
            return Task.CompletedTask;
        }

        var nextIndex = Math.Clamp(PlaylistState.CurrentIndex + offset, 0, PlaylistState.TotalCount - 1);
        return FocusFilmstripIndexAsync(nextIndex, persistPreference: false, cancellationToken: cancellationToken);
    }

    private async Task FocusFilmstripIndexAsync(
        int focusIndex,
        bool persistPreference,
        CancellationToken cancellationToken)
    {
        using var operation = BeginOperation(cancellationToken);
        var operationToken = operation.Token;
        ErrorMessage = null;

        try
        {
            await ApplyFilmstripLayoutAsync(focusIndex, persistPreference, operationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            EndOperation(operation);
        }
    }

    public Task IncreaseGridDensityAsync(CancellationToken cancellationToken = default)
    {
        return StepGridDensityAsync(1, cancellationToken);
    }

    public Task DecreaseGridDensityAsync(CancellationToken cancellationToken = default)
    {
        return StepGridDensityAsync(-1, cancellationToken);
    }

    public Task ScrollGridLayoutAsync(int direction, CancellationToken cancellationToken = default)
    {
        if (!LayoutState.IsActive || PlaylistState.TotalCount == 0)
        {
            return Task.CompletedTask;
        }

        var rowOffset = LayoutState.Config.Columns * Math.Sign(direction);
        if (rowOffset == 0)
        {
            return Task.CompletedTask;
        }

        var nextStartIndex = Mod(LayoutState.WindowStartIndex + rowOffset, PlaylistState.TotalCount);
        return RefreshGridLayoutAsync(
            LayoutState.Config,
            nextStartIndex,
            persistPreference: false,
            cancellationToken: cancellationToken);
    }

    public void SelectGridImage(int visibleIndex)
    {
        LayoutState.SelectImage(visibleIndex);
        ShowChrome();
    }

    public async Task OpenSelectedGridImageAsync(CancellationToken cancellationToken = default)
    {
        var activeImage = LayoutState.ActiveImage;
        if (activeImage is null)
        {
            return;
        }

        await ReturnToSingleImageViewAsync(cancellationToken).ConfigureAwait(false);
        await NavigateToIndexAsync(activeImage.PlaylistIndex, cancellationToken).ConfigureAwait(false);
    }

    private Task StepGridDensityAsync(int direction, CancellationToken cancellationToken)
    {
        if (!LayoutState.IsActive || PlaylistState.TotalCount == 0)
        {
            return Task.CompletedTask;
        }

        var nextConfig = LayoutConfig.StepGridDensity(LayoutState.Config, direction);
        return RefreshGridLayoutAsync(
            nextConfig,
            LayoutState.WindowStartIndex,
            persistPreference: true,
            cancellationToken: cancellationToken);
    }

    private async Task RefreshGridLayoutAsync(
        LayoutConfig config,
        int startIndex,
        bool persistPreference,
        CancellationToken cancellationToken)
    {
        using var operation = BeginOperation(cancellationToken);
        var operationToken = operation.Token;
        ErrorMessage = null;

        try
        {
            await ApplyGridLayoutAsync(config, startIndex, persistPreference, operationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            EndOperation(operation);
        }
    }

    public void ExitLayout()
    {
        LayoutState.ExitLayout();
        ShowChrome();
    }

    public async Task ReturnToSingleImageViewAsync(CancellationToken cancellationToken = default)
    {
        LayoutState.ExitLayout();
        Settings = Settings with { PreferredLayout = LayoutMode.SingleImage };
        await SaveSettingsOrReportErrorAsync(cancellationToken).ConfigureAwait(false);
        ShowChrome();
    }

    private async Task RestorePreferredLayoutAsync(CancellationToken cancellationToken)
    {
        if (PlaylistState.TotalCount == 0)
        {
            return;
        }

        switch (Settings.PreferredLayout)
        {
            case LayoutMode.Grid:
                await ApplyGridLayoutAsync(
                        CreatePreferredGridConfig(),
                        PlaylistState.CurrentIndex,
                        persistPreference: false,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;
            case LayoutMode.Filmstrip:
                await ApplyFilmstripLayoutAsync(
                        PlaylistState.CurrentIndex,
                        persistPreference: false,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;
            default:
                LayoutState.ExitLayout();
                break;
        }
    }

    private async Task<bool> ApplyGridLayoutAsync(
        LayoutConfig config,
        int startIndex,
        bool persistPreference,
        CancellationToken cancellationToken)
    {
        var playlist = new Playlist(PlaylistState.Items.ToList(), PlaylistState.CurrentIndex, PlaylistState.FolderPath);
        var result = await _layoutService.EnterGridAsync(playlist, config, startIndex, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            await HandleErrorAsync(result.Error, CancellationToken.None).ConfigureAwait(false);
            ComparisonState.ExitComparison();
            LayoutState.ExitLayout();
            return false;
        }

        ComparisonState.ExitComparison();
        LayoutState.EnterLayout(result.Value);
        if (persistPreference)
        {
            Settings = Settings with
            {
                PreferredLayout = LayoutMode.Grid,
                GridRows = result.Value.Config.Rows,
                GridColumns = result.Value.Config.Columns
            };
            await SaveSettingsOrReportErrorAsync(cancellationToken).ConfigureAwait(false);
        }

        ShowChrome();
        return true;
    }

    private async Task ApplyFilmstripLayoutAsync(
        int focusIndex,
        bool persistPreference,
        CancellationToken cancellationToken)
    {
        var playlist = new Playlist(PlaylistState.Items.ToList(), PlaylistState.CurrentIndex, PlaylistState.FolderPath);
        var result = await _layoutService.EnterFilmstripAsync(playlist, focusIndex, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            await HandleErrorAsync(result.Error, CancellationToken.None).ConfigureAwait(false);
            ComparisonState.ExitComparison();
            LayoutState.ExitLayout();
            return;
        }

        ComparisonState.ExitComparison();
        LayoutState.EnterLayout(result.Value);
        if (result.Value.FocusImage is { } focusImage)
        {
            PlaylistState.SetCurrentIndex(focusImage.PlaylistIndex);
            ViewerState.SetImage(focusImage.Image);
            SlideshowState.SetCurrentImageIndex(focusImage.PlaylistIndex);
            TransformState.DiscardTransforms();
        }

        if (persistPreference)
        {
            Settings = Settings with { PreferredLayout = LayoutMode.Filmstrip };
            await SaveSettingsOrReportErrorAsync(cancellationToken).ConfigureAwait(false);
        }

        ShowChrome();
    }

    private LayoutConfig CreatePreferredGridConfig()
    {
        var normalizedSettings = Settings.Normalize();
        return LayoutConfig.Default with
        {
            Rows = normalizedSettings.GridRows,
            Columns = normalizedSettings.GridColumns
        };
    }

    public void ShowCommandPalette()
    {
        CommandPaletteState.Open();
        ShowChrome();
    }

    public void HideCommandPalette()
    {
        CommandPaletteState.Close();
        ShowChrome();
    }

    public async Task ExecuteSelectedCommandAsync(CancellationToken cancellationToken = default)
    {
        var selectedCommand = CommandPaletteState.SelectedCommand;
        if (selectedCommand is null)
        {
            return;
        }

        CommandPaletteState.RecordCommandExecuted(selectedCommand);
        CommandPaletteState.Close();
        switch (selectedCommand.Id)
        {
            case CommandIds.OpenFile:
                OpenFileRequested?.Invoke(this, EventArgs.Empty);
                break;
            case CommandIds.ToggleFullscreen:
                ToggleFullscreen();
                break;
            case CommandIds.FitToWindow:
                FitToWindow();
                break;
            case CommandIds.ActualSize:
                ActualSize();
                break;
            case CommandIds.NavigateNext:
                await NavigateNextAsync(cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.NavigatePrevious:
                await NavigatePreviousAsync(cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.RotateLeft:
                await ApplyTransformAsync(TransformOperation.RotateLeft, cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.RotateRight:
                await ApplyTransformAsync(TransformOperation.RotateRight, cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.ToggleSlideshow:
                ToggleSlideshow();
                break;
            case CommandIds.EnterComparison:
                await EnterComparisonAsync(ComparisonLayout.SideBySide, cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.EnterOverlayComparison:
                await EnterComparisonAsync(ComparisonLayout.Overlay, cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.ToggleComparisonSyncZoom:
                ToggleComparisonSyncZoom();
                break;
            case CommandIds.ComparisonTwoImages:
                await ChangeComparisonImageCountAsync(2, cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.ComparisonThreeImages:
                await ChangeComparisonImageCountAsync(3, cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.ComparisonFourImages:
                await ChangeComparisonImageCountAsync(4, cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.ReturnToSingleImage:
                await ReturnToSingleImageViewAsync(cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.GridLayout:
                await EnterGridLayoutAsync(cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.FilmstripLayout:
                await EnterFilmstripLayoutAsync(cancellationToken).ConfigureAwait(false);
                break;
            case CommandIds.ToggleGifPlayback:
                ToggleAnimationPlayback();
                break;
        }

        ShowChrome();
    }

    public void ZoomIn()
    {
        if (ComparisonState.IsActive)
        {
            var zoomLevel = ComparisonState.ActiveViewport?.ZoomLevel ?? 100;
            SetComparisonZoom(zoomLevel + 25);
            return;
        }

        ViewerState.SetZoom(ViewerState.ZoomLevel + 25);
        ShowChrome();
    }

    public void ZoomOut()
    {
        if (ComparisonState.IsActive)
        {
            var zoomLevel = ComparisonState.ActiveViewport?.ZoomLevel ?? 100;
            SetComparisonZoom(Math.Max(100, zoomLevel - 25));
            return;
        }

        ViewerState.SetZoom(Math.Max(100, ViewerState.ZoomLevel - 25));
        ShowChrome();
    }

    public void FitToWindow()
    {
        if (ComparisonState.IsActive)
        {
            ComparisonState.ResetSynchronizedZoom();
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
            SetComparisonZoom(100);
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

    private async Task NavigateToIndexAsync(
        int targetIndex,
        CancellationToken cancellationToken,
        bool resetSlideshowTimer = true)
    {
        if (SlideshowState.IsTransitioning && resetSlideshowTimer)
        {
            ShowChrome();
            return;
        }

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
            await LoadAnimationForImageAsync(navigationResult.Value.Image.FilePath, operationToken).ConfigureAwait(false);
            SlideshowState.SetCurrentImageIndex(navigationResult.Value.Index);
            if (resetSlideshowTimer && SlideshowState.IsActive)
            {
                _slideshowService.ResetTimer();
                SlideshowState.ResetTimer();
            }

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
        if (PlaylistState.TotalCount == 0 || !ComparisonState.IsActive)
        {
            return;
        }

        using var operation = BeginOperation(cancellationToken);
        var operationToken = operation.Token;
        ErrorMessage = null;

        try
        {
            var playlist = new Playlist(PlaylistState.Items.ToList(), PlaylistState.CurrentIndex, PlaylistState.FolderPath);
            var currentIndices = ComparisonState.VisibleImages.Select(image => image.PlaylistIndex).ToArray();
            var result = await _comparisonService.ShiftComparisonAsync(
                    playlist,
                    currentIndices,
                    offset,
                    ComparisonState.Layout,
                    operationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                await HandleErrorAsync(result.Error, CancellationToken.None).ConfigureAwait(false);
                return;
            }

            ApplyComparisonSet(result.Value);
            if (SlideshowState.IsActive)
            {
                _slideshowService.ResetTimer();
                SlideshowState.ResetTimer();
            }

            ShowChrome();
        }
        finally
        {
            EndOperation(operation);
        }
    }

    private void SetComparisonZoom(double zoomLevel)
    {
        if (ComparisonState.IsSyncZoomEnabled)
        {
            ComparisonState.SetSynchronizedZoom(zoomLevel);
        }
        else
        {
            ComparisonState.SetActiveViewportZoom(zoomLevel);
        }

        ShowChrome();
    }

    private void ApplyComparisonSet(ComparisonSet comparison)
    {
        LayoutState.ExitLayout();
        ComparisonState.EnterComparison(comparison);
        ComparisonState.SetSyncZoom(Settings.ComparisonSyncZoom);
        var primaryImage = comparison.Images[0];
        PlaylistState.SetCurrentIndex(primaryImage.PlaylistIndex);
        ViewerState.SetImage(primaryImage.Image);
        AnimationState.ClearAnimation();
        SlideshowState.SetCurrentImageIndex(primaryImage.PlaylistIndex);
        TransformState.DiscardTransforms();
    }

    private async Task LoadAnimationForImageAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(filePath), ".gif", StringComparison.OrdinalIgnoreCase))
        {
            AnimationState.ClearAnimation();
            return;
        }

        var animationResult = await _animationService.LoadAnimatedGifAsync(filePath, cancellationToken)
            .ConfigureAwait(false);
        if (animationResult.IsFailure)
        {
            AnimationState.ClearAnimation();
            if (animationResult.Error.Category == ErrorCategory.UnsupportedFormatError)
            {
                return;
            }

            await HandleErrorAsync(animationResult.Error, CancellationToken.None).ConfigureAwait(false);
            return;
        }

        AnimationState.LoadAnimation(animationResult.Value, Settings.GifAutoPlay);
    }

    private async Task HandleErrorAsync(ViewerError error, CancellationToken cancellationToken)
    {
        CurrentErrorSeverity = error.Severity;
        ErrorMessage = error.UserMessage;
        await _errorLogService.LogErrorAsync(error, cancellationToken).ConfigureAwait(false);
        RequestCodecInstallPromptIfNeeded(error);
    }

    private void RequestCodecInstallPromptIfNeeded(ViewerError error)
    {
        if (error.Category != ErrorCategory.CodecUnavailableError)
        {
            return;
        }

        var extension = Path.GetExtension(error.FilePath);
        var prompt = CreateCodecInstallPrompt(error, extension);
        if (prompt is null || !_promptedCodecFormats.Add(prompt.FormatName))
        {
            return;
        }

        CodecInstallPromptRequested?.Invoke(this, prompt);
    }

    private CodecInstallPrompt? CreateCodecInstallPrompt(ViewerError error, string? extension)
    {
        if (IsHeicExtension(extension))
        {
            var storeUrl = _codecDetectionService?.GetHeicStoreUrl();
            return storeUrl is { IsSuccess: true }
                ? new CodecInstallPrompt("HEIC", "HEIF Image Extensions", storeUrl.Value, error.UserMessage)
                : null;
        }

        if (IsAvifExtension(extension))
        {
            var storeUrl = _codecDetectionService?.GetAvifStoreUrl();
            return storeUrl is { IsSuccess: true }
                ? new CodecInstallPrompt("AVIF", "AV1 Video Extension", storeUrl.Value, error.UserMessage)
                : null;
        }

        return null;
    }

    private static int Mod(int value, int divisor)
    {
        return ((value % divisor) + divisor) % divisor;
    }

    private static bool IsHeicExtension(string? extension)
    {
        return extension is not null
            && (extension.Equals(".heic", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".heif", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAvifExtension(string? extension)
    {
        return extension is not null
            && extension.Equals(".avif", StringComparison.OrdinalIgnoreCase);
    }

    private void OnSlideshowAdvanceRequested(object? sender, SlideshowAdvanceRequestedEventArgs e)
    {
        _ = AdvanceSlideshowAsync(e.NextIndex);
    }

    private async Task SaveSettingsOrReportErrorAsync(CancellationToken cancellationToken)
    {
        var saveResult = await _settingsService.SaveSettingsAsync(Settings, cancellationToken).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            await HandleErrorAsync(saveResult.Error, cancellationToken).ConfigureAwait(false);
        }
    }

    private void SaveSettingsOrReportError()
    {
        var saveResult = _settingsService.SaveSettingsAsync(Settings, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        if (saveResult.IsFailure)
        {
            HandleErrorAsync(saveResult.Error, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }
    }

    private async Task StepSlideshowIntervalAsync(int direction, CancellationToken cancellationToken)
    {
        var nextInterval = SlideshowConfig.StepIntervalSeconds(Settings.SlideshowIntervalSeconds, direction);
        await UpdateSlideshowIntervalAsync(nextInterval, cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateSlideshowIntervalAsync(int intervalSeconds, CancellationToken cancellationToken)
    {
        var nextInterval = SlideshowConfig.NormalizeIntervalSeconds(intervalSeconds);
        Settings = Settings with { SlideshowIntervalSeconds = nextInterval };
        var config = SlideshowConfig.FromSettings(Settings);
        SlideshowState.SetInterval(config.Interval);
        if (SlideshowState.IsActive)
        {
            _slideshowService.UpdateConfig(config);
        }

        await SaveSettingsOrReportErrorAsync(cancellationToken).ConfigureAwait(false);
        ShowChrome();
    }

    private async Task AdvanceSlideshowAsync(int nextIndex)
    {
        var transition = BeginSlideshowTransition(Settings.SlideshowTransition);
        try
        {
            if (ComparisonState.IsActive)
            {
                await AdvanceComparisonSlideshowAsync(nextIndex).ConfigureAwait(false);
            }
            else if (LayoutState is { IsActive: true, Mode: LayoutMode.Grid })
            {
                await AdvanceGridSlideshowAsync(nextIndex).ConfigureAwait(false);
            }
            else if (LayoutState is { IsActive: true, Mode: LayoutMode.Filmstrip })
            {
                await ApplyFilmstripLayoutAsync(
                        nextIndex,
                        persistPreference: false,
                        cancellationToken: CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else
            {
                await NavigateToIndexAsync(nextIndex, CancellationToken.None, resetSlideshowTimer: false)
                    .ConfigureAwait(false);
            }

            PreloadNextSlideshowImage();
            if (transition.Duration > TimeSpan.Zero)
            {
                await Task.Delay(transition.Duration).ConfigureAwait(false);
            }
        }
        finally
        {
            EndSlideshowTransition();
        }
    }

    private void PreloadNextSlideshowImage()
    {
        if (!SlideshowState.IsActive || PlaylistState.TotalCount < 2)
        {
            return;
        }

        var preloadIndex = Mod(SlideshowState.CurrentImageIndex + 1, PlaylistState.TotalCount);
        var preloadPath = PlaylistState.Items[preloadIndex].FilePath;
        _ = Task.Run(async () =>
        {
            try
            {
                await _imageDecodeService.DecodeImageAsync(preloadPath, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
        });
    }

    private async Task AdvanceComparisonSlideshowAsync(int nextIndex)
    {
        var currentPrimaryIndex = ComparisonState.VisibleImages.FirstOrDefault()?.PlaylistIndex ?? PlaylistState.CurrentIndex;
        var offset = nextIndex - currentPrimaryIndex;
        var playlist = new Playlist(PlaylistState.Items.ToList(), PlaylistState.CurrentIndex, PlaylistState.FolderPath);
        var currentIndices = ComparisonState.VisibleImages.Select(image => image.PlaylistIndex).ToArray();
        var result = await _comparisonService.ShiftComparisonAsync(
                playlist,
                currentIndices,
                offset,
                ComparisonState.Layout,
                CancellationToken.None)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            await HandleErrorAsync(result.Error, CancellationToken.None).ConfigureAwait(false);
            return;
        }

        ApplyComparisonSet(result.Value);
        ShowChrome();
    }

    private async Task AdvanceGridSlideshowAsync(int nextIndex)
    {
        var applied = await ApplyGridLayoutAsync(
                LayoutState.Config,
                nextIndex,
                persistPreference: false,
                cancellationToken: CancellationToken.None)
            .ConfigureAwait(false);
        if (!applied)
        {
            return;
        }

        if (LayoutState.ActiveImage is { } activeImage)
        {
            PlaylistState.SetCurrentIndex(activeImage.PlaylistIndex);
            ViewerState.SetImage(activeImage.Image);
            SlideshowState.SetCurrentImageIndex(activeImage.PlaylistIndex);
            TransformState.DiscardTransforms();
        }
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
