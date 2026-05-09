using System.ComponentModel;
using ImageViewer.App.Services;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace ImageViewer.App;

public sealed partial class MainWindow : Window
{
    private const string EvidenceDropIndicatorEnvironmentVariable = "SPITFIRE_EVIDENCE_SHOW_DROP_INDICATOR";
    private const int OemCommaVirtualKey = 188;
    private const int OemPeriodVirtualKey = 190;
    private const int OemPlusVirtualKey = 187;
    private const int OemMinusVirtualKey = 189;

    private readonly MainViewModel _viewModel;
    private readonly AppWindow _appWindow;
    private readonly DispatcherTimer _loadingDelayTimer;
    private readonly DispatcherTimer _warningDismissTimer;
    private readonly Task _initializationTask;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        _appWindow = GetAppWindow();
        _loadingDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _loadingDelayTimer.Tick += OnLoadingDelayElapsed;
        _warningDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _warningDismissTimer.Tick += OnWarningDismissElapsed;

        RootGrid.DataContext = viewModel;
        ImageCanvas.Attach(viewModel);
        ComparisonView.Attach(viewModel);
        GridLayout.Attach(viewModel);
        FilmstripView.Attach(viewModel);
        ThumbnailStrip.Attach(viewModel);
        SearchOverlay.Attach(viewModel);
        CommandPalette.Attach(viewModel);
        MetadataPanel.Attach(viewModel);
        Chrome.Attach(viewModel);
        Chrome.OpenFileRequested += async (_, _) => await OpenFileFromPickerAsync();
        viewModel.OpenFileRequested += OnViewModelOpenFileRequested;
        viewModel.SearchRequested += OnViewModelSearchRequested;
        viewModel.SearchHidden += OnViewModelSearchHidden;
        Chrome.RecentFileRequested += async (_, path) => await _viewModel.OpenRecentFileAsync(path);
        Chrome.ExitRequested += (_, _) => Close();
        Chrome.AboutRequested += (_, _) => ShowInfo("spitfire.img Phase 1 MVP Viewer");
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.ViewerState.PropertyChanged += OnViewerStatePropertyChanged;
        viewModel.CommandPaletteState.PropertyChanged += OnCommandPaletteStatePropertyChanged;
        viewModel.CodecInstallPromptRequested += OnCodecInstallPromptRequested;
        RegisterKeyboardAccelerators();
        Activated += OnActivated;
        RootGrid.Loaded += OnRootGridLoaded;
        Closed += OnClosed;
        _initializationTask = InitializeWindowAsync();
    }

    private void RegisterKeyboardAccelerators()
    {
        AddKeyboardAccelerator(VirtualKey.F, VirtualKeyModifiers.Control, (_, args) =>
        {
            _viewModel.ShowSearch();
            args.Handled = true;
        });

        AddKeyboardAccelerator(VirtualKey.I, VirtualKeyModifiers.Control, async (_, args) =>
        {
            await _viewModel.ToggleMetadataPanelAsync();
            args.Handled = true;
        });

        AddKeyboardAccelerator(VirtualKey.T, VirtualKeyModifiers.Control, async (_, args) =>
        {
            await _viewModel.ToggleThumbnailStrip();
            args.Handled = true;
        });
    }

    private void AddKeyboardAccelerator(
        VirtualKey key,
        VirtualKeyModifiers modifiers,
        TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
    {
        var accelerator = new KeyboardAccelerator
        {
            Key = key,
            Modifiers = modifiers
        };
        accelerator.Invoked += handler;
        RootGrid.KeyboardAccelerators.Add(accelerator);
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        RootGrid.Focus(FocusState.Programmatic);
    }

    private void OnRootGridLoaded(object sender, RoutedEventArgs e)
    {
        ApplySavedWindowPlacement();

        var evidenceDropState = Environment.GetEnvironmentVariable(EvidenceDropIndicatorEnvironmentVariable);
        if (string.Equals(evidenceDropState, "accepted", StringComparison.OrdinalIgnoreCase))
        {
            DropIndicator.Show(isValid: true);
        }
        else if (string.Equals(evidenceDropState, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            DropIndicator.Show(isValid: false);
        }
    }

    private void ApplySavedWindowPlacement()
    {
        if (_viewModel.Settings.WindowWidth > 0 && _viewModel.Settings.WindowHeight > 0)
        {
            _appWindow.Resize(new Windows.Graphics.SizeInt32(
                _viewModel.Settings.WindowWidth,
                _viewModel.Settings.WindowHeight));
        }

        if (_viewModel.Settings.WindowX >= 0 &&
            _viewModel.Settings.WindowY >= 0 &&
            WindowPlacementService.IsSavedWindowPositionVisible(_viewModel.Settings.WindowX, _viewModel.Settings.WindowY))
        {
            _appWindow.Move(new Windows.Graphics.PointInt32(
                _viewModel.Settings.WindowX,
                _viewModel.Settings.WindowY));
        }
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        RootGrid.Loaded -= OnRootGridLoaded;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.ViewerState.PropertyChanged -= OnViewerStatePropertyChanged;
        _viewModel.CommandPaletteState.PropertyChanged -= OnCommandPaletteStatePropertyChanged;
        _viewModel.CodecInstallPromptRequested -= OnCodecInstallPromptRequested;
        _viewModel.OpenFileRequested -= OnViewModelOpenFileRequested;
        _viewModel.SearchRequested -= OnViewModelSearchRequested;
        _viewModel.SearchHidden -= OnViewModelSearchHidden;
        await _viewModel.SaveWindowStateAsync(
            _appWindow.Size.Width,
            _appWindow.Size.Height,
            _appWindow.Position.X,
            _appWindow.Position.Y);
    }

    public async Task OpenInitialFileAsync(string filePath)
    {
        await _initializationTask;
        await _viewModel.OpenFileAsync(filePath);
    }

    private async Task InitializeWindowAsync()
    {
        await _viewModel.InitializeAsync();
        await RunOnDispatcherAsync(() =>
        {
            ApplyTheme();
            ApplySavedWindowPlacement();
        });
    }

    private Task RunOnDispatcherAsync(Action action)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            action();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource();
        if (!DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }))
        {
            completion.SetException(new InvalidOperationException("Unable to enqueue window initialization work."));
        }

        return completion.Task;
    }

    private async Task OpenFileFromPickerAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };

        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".bmp", ".webp" })
        {
            picker.FileTypeFilter.Add(extension);
        }

        if (_viewModel.CanAttemptOpenFormat(".heic"))
        {
            picker.FileTypeFilter.Add(".heic");
        }

        if (_viewModel.CanAttemptOpenFormat(".avif"))
        {
            picker.FileTypeFilter.Add(".avif");
        }

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await _viewModel.OpenFileAsync(file.Path);
        }
    }

    private async void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_viewModel.CommandPaletteState.IsVisible)
        {
            Chrome.PauseFade();
        }
        else
        {
            Chrome.ShowChrome();
        }

        var isControlPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) &
                                Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        var isShiftPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) &
                              Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

        switch (e.Key)
        {
            case VirtualKey.Right:
                await _viewModel.NavigateNextAsync();
                e.Handled = true;
                break;
            case VirtualKey.Left:
                await _viewModel.NavigatePreviousAsync();
                e.Handled = true;
                break;
            case VirtualKey.Up when IsOverlayComparisonActive():
                _viewModel.AdjustOverlayOpacity(10);
                e.Handled = true;
                break;
            case VirtualKey.Down when IsOverlayComparisonActive():
                _viewModel.AdjustOverlayOpacity(-10);
                e.Handled = true;
                break;
            case VirtualKey.Home:
                await _viewModel.NavigateFirstAsync();
                e.Handled = true;
                break;
            case VirtualKey.End:
                await _viewModel.NavigateLastAsync();
                e.Handled = true;
                break;
            case VirtualKey.Tab when _viewModel.ComparisonState.IsActive:
                _viewModel.CycleComparisonActiveImage();
                e.Handled = true;
                break;
            case VirtualKey.Enter when _viewModel.ComparisonState.IsActive:
                await _viewModel.OpenActiveComparisonImageAsync();
                e.Handled = true;
                break;
            case VirtualKey.Enter when _viewModel.LayoutState.IsGridActive:
                await _viewModel.OpenActiveGridImageAsync();
                e.Handled = true;
                break;
            case VirtualKey.F5:
                await _viewModel.ToggleSlideshowAsync();
                e.Handled = true;
                break;
            case VirtualKey.Space when _viewModel.SlideshowState.IsActive:
                _viewModel.ToggleSlideshowPause();
                e.Handled = true;
                break;
            case VirtualKey.F11:
                _viewModel.ToggleFullscreen();
                e.Handled = true;
                break;
            case VirtualKey.Escape:
                if (_viewModel.CommandPaletteState.IsVisible)
                {
                    _viewModel.HideCommandPalette();
                    e.Handled = true;
                }
                else if (_viewModel.SlideshowState.IsActive)
                {
                    await _viewModel.ToggleSlideshowAsync();
                    e.Handled = true;
                }
                else if (_viewModel.ComparisonState.IsActive)
                {
                    _viewModel.ExitComparisonMode();
                    e.Handled = true;
                }
                else if (_viewModel.LayoutState.CurrentLayout != LayoutMode.Single)
                {
                    await _viewModel.ExitLayoutModeAsync();
                    e.Handled = true;
                }
                else if (_viewModel.MetadataState.IsVisible)
                {
                    _viewModel.MetadataState.HidePanel();
                    e.Handled = true;
                }
                else if (_viewModel.ViewerState.IsFullscreen)
                {
                    _viewModel.ToggleFullscreen();
                    e.Handled = true;
                }
                break;
            case VirtualKey.Number0 when isControlPressed:
                _viewModel.FitToWindow();
                e.Handled = true;
                break;
            case VirtualKey.Number1 when isControlPressed && isShiftPressed:
                _viewModel.ActualSize();
                e.Handled = true;
                break;
            case VirtualKey.Number1 when isControlPressed:
                await _viewModel.ExitLayoutModeAsync();
                e.Handled = true;
                break;
            case VirtualKey.Number2 when isControlPressed && _viewModel.ComparisonState.IsActive:
                await _viewModel.SetComparisonImageCountAsync(2);
                e.Handled = true;
                break;
            case VirtualKey.Number3 when isControlPressed && _viewModel.ComparisonState.IsActive:
                await _viewModel.SetComparisonImageCountAsync(3);
                e.Handled = true;
                break;
            case VirtualKey.Number4 when isControlPressed && _viewModel.ComparisonState.IsActive:
                await _viewModel.SetComparisonImageCountAsync(4);
                e.Handled = true;
                break;
            case var key when IsPlusKey(key) && _viewModel.SlideshowState.IsActive:
                await _viewModel.IncreaseSlideshowIntervalAsync();
                e.Handled = true;
                break;
            case var key when IsMinusKey(key) && _viewModel.SlideshowState.IsActive:
                await _viewModel.DecreaseSlideshowIntervalAsync();
                e.Handled = true;
                break;
            case var key when IsPlusKey(key) && isControlPressed && _viewModel.LayoutState.IsGridActive:
                await _viewModel.IncreaseGridDensityAsync(GetLayoutCanvasSize());
                e.Handled = true;
                break;
            case var key when IsMinusKey(key) && isControlPressed && _viewModel.LayoutState.IsGridActive:
                await _viewModel.DecreaseGridDensityAsync(GetLayoutCanvasSize());
                e.Handled = true;
                break;
            case var key when IsPlusKey(key) && isControlPressed:
                _viewModel.ZoomIn();
                e.Handled = true;
                break;
            case var key when IsMinusKey(key) && isControlPressed:
                _viewModel.ZoomOut();
                e.Handled = true;
                break;
            case VirtualKey.O when isControlPressed:
                await OpenFileFromPickerAsync();
                e.Handled = true;
                break;
            case VirtualKey.F when isControlPressed && isShiftPressed:
                await _viewModel.EnterFilmstripLayoutAsync();
                e.Handled = true;
                break;
            case VirtualKey.F when isControlPressed:
                _viewModel.ShowSearch();
                e.Handled = true;
                break;
            case VirtualKey.G when isControlPressed:
                await _viewModel.EnterGridLayoutAsync(GetLayoutCanvasSize());
                e.Handled = true;
                break;
            case VirtualKey.P when isControlPressed && isShiftPressed:
                _viewModel.ShowCommandPalette();
                e.Handled = true;
                break;
            case VirtualKey.P when !isControlPressed && !isShiftPressed:
                _viewModel.ToggleAnimationPlayback();
                e.Handled = true;
                break;
            case var key when IsPeriodKey(key) && !isControlPressed && !isShiftPressed:
                _viewModel.AdvanceAnimationFrame();
                e.Handled = true;
                break;
            case var key when IsCommaKey(key) && !isControlPressed && !isShiftPressed:
                _viewModel.RewindAnimationFrame();
                e.Handled = true;
                break;
            case VirtualKey.D when isControlPressed && isShiftPressed:
                await _viewModel.EnterComparisonModeAsync(ComparisonLayout.Overlay);
                e.Handled = true;
                break;
            case VirtualKey.D when isControlPressed:
                await _viewModel.EnterComparisonModeAsync(ComparisonLayout.SideBySide);
                e.Handled = true;
                break;
            case VirtualKey.T when isControlPressed:
                await _viewModel.ToggleThumbnailStrip();
                e.Handled = true;
                break;
            case VirtualKey.S when IsOverlayComparisonActive() && !isControlPressed:
                _viewModel.SwapOverlayLayers();
                e.Handled = true;
                break;
            case VirtualKey.S when isControlPressed && isShiftPressed:
                await ExportCurrentImageAsync();
                e.Handled = true;
                break;
            case VirtualKey.S when isControlPressed:
                await SaveTransformsWithConfirmationAsync();
                e.Handled = true;
                break;
            case VirtualKey.I when isControlPressed:
                await _viewModel.ToggleMetadataPanelAsync();
                e.Handled = true;
                break;
            case VirtualKey.L when isControlPressed && _viewModel.ComparisonState.IsActive:
                await _viewModel.ToggleComparisonSyncZoomAsync();
                e.Handled = true;
                break;
            case VirtualKey.L when isControlPressed:
                await _viewModel.ApplyTransformAsync(TransformOperation.RotateLeft);
                e.Handled = true;
                break;
            case VirtualKey.R when isControlPressed:
                await _viewModel.ApplyTransformAsync(TransformOperation.RotateRight);
                e.Handled = true;
                break;
            case VirtualKey.H when isControlPressed:
                await _viewModel.ApplyTransformAsync(TransformOperation.FlipHorizontal);
                e.Handled = true;
                break;
            case VirtualKey.V when isControlPressed:
                await _viewModel.ApplyTransformAsync(TransformOperation.FlipVertical);
                e.Handled = true;
                break;
            case VirtualKey.Z when isControlPressed && isShiftPressed:
                _viewModel.RedoTransform();
                e.Handled = true;
                break;
            case VirtualKey.Z when isControlPressed:
                _viewModel.UndoTransform();
                e.Handled = true;
                break;
        }
    }

    private bool IsOverlayComparisonActive()
    {
        return _viewModel.ComparisonState.IsActive &&
               _viewModel.ComparisonState.Layout == ComparisonLayout.Overlay;
    }

    private ImageDimensions GetLayoutCanvasSize()
    {
        var width = RootGrid.ActualWidth > 0 ? RootGrid.ActualWidth : _viewModel.Settings.WindowWidth;
        var height = RootGrid.ActualHeight > 0 ? RootGrid.ActualHeight : _viewModel.Settings.WindowHeight;
        return new ImageDimensions(width, height);
    }

    private void OnRootPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_viewModel.CommandPaletteState.IsVisible)
        {
            Chrome.PauseFade();
            return;
        }

        Chrome.ShowChrome();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsLoading))
        {
            DispatcherQueue.TryEnqueue(UpdateLoadingState);
        }

        if (e.PropertyName == nameof(MainViewModel.IsCancelAvailable))
        {
            DispatcherQueue.TryEnqueue(UpdateCancelState);
        }

        if (e.PropertyName is nameof(MainViewModel.ErrorMessage) or nameof(MainViewModel.CurrentErrorSeverity))
        {
            DispatcherQueue.TryEnqueue(UpdateErrorState);
        }

        if (e.PropertyName == nameof(MainViewModel.Settings))
        {
            DispatcherQueue.TryEnqueue(ApplyTheme);
        }
    }

    private void OnViewerStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewerState.IsFullscreen))
        {
            DispatcherQueue.TryEnqueue(ApplyFullscreenState);
        }
    }

    private void OnCommandPaletteStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CommandPaletteState.IsVisible))
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            if (_viewModel.CommandPaletteState.IsVisible)
            {
                CommandPalette.Show();
                Chrome.PauseFade();
            }
            else
            {
                CommandPalette.Hide();
                Chrome.ResumeFade();
            }
        });
    }

    private async void OnViewModelOpenFileRequested(object? sender, EventArgs e)
    {
        await OpenFileFromPickerAsync();
    }

    private void OnViewModelSearchRequested(object? sender, EventArgs e)
    {
        SearchOverlay.Show();
    }

    private void OnViewModelSearchHidden(object? sender, EventArgs e)
    {
        SearchOverlay.Hide();
    }

    private void OnCodecInstallPromptRequested(object? sender, CodecInstallPromptRequest request)
    {
        DispatcherQueue.TryEnqueue(async () => await ShowCodecInstallPromptAsync(request));
    }

    private async Task ShowCodecInstallPromptAsync(CodecInstallPromptRequest request)
    {
        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            Title = $"{request.FormatName} codec required",
            Content = request.Message,
            PrimaryButtonText = "Open Store",
            CloseButtonText = "Not now",
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary &&
            Uri.TryCreate(request.StoreUrl, UriKind.Absolute, out var storeUri))
        {
            await Launcher.LaunchUriAsync(storeUri);
        }
    }

    private void ApplyFullscreenState()
    {
        if (_viewModel.ViewerState.IsFullscreen)
        {
            _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
        else
        {
            _appWindow.SetPresenter(AppWindowPresenterKind.Default);
        }
    }

    private void ApplyTheme()
    {
        RootGrid.RequestedTheme = _viewModel.Settings.Theme switch
        {
            ViewerTheme.Dark => ElementTheme.Dark,
            ViewerTheme.Light => ElementTheme.Light,
            _ => ElementTheme.Default
        };
    }

    private AppWindow GetAppWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private void ShowInfo(string message)
    {
        ErrorInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational;
        ErrorInfoBar.Title = "Status";
        ErrorInfoBar.Message = message;
        ErrorRecoveryButton.Visibility = Visibility.Collapsed;
        ErrorInfoBar.IsOpen = true;
    }

    private void OnCancelLoadingClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelLoading();
    }

    private async void OnErrorRecoveryClicked(object sender, RoutedEventArgs e)
    {
        await OpenFileFromPickerAsync();
    }

    private void UpdateLoadingState()
    {
        if (_viewModel.IsLoading)
        {
            _loadingDelayTimer.Stop();
            _loadingDelayTimer.Start();
            return;
        }

        _loadingDelayTimer.Stop();
        LoadingRing.IsActive = false;
        LoadingStatusBody.Text = "Decoding the file while the viewer remains available.";
        LoadingOverlay.Visibility = Visibility.Collapsed;
        CancelLoadingButton.Visibility = Visibility.Collapsed;
    }

    private void UpdateCancelState()
    {
        CancelLoadingButton.Visibility = _viewModel.IsCancelAvailable
            ? Visibility.Visible
            : Visibility.Collapsed;
        LoadingStatusBody.Text = _viewModel.IsCancelAvailable
            ? "Decoding the file. You can cancel this load without closing the viewer."
            : "Decoding the file while the viewer remains available.";
    }

    private void UpdateErrorState()
    {
        _warningDismissTimer.Stop();
        var isWarning = _viewModel.CurrentErrorSeverity == ErrorSeverity.Warning;
        ErrorInfoBar.Severity = isWarning
            ? Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning
            : Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
        ErrorInfoBar.Title = isWarning ? "Review needed" : "Unable to open image";
        ErrorInfoBar.Message = _viewModel.ErrorMessage ?? string.Empty;
        ErrorInfoBar.IsOpen = !string.IsNullOrWhiteSpace(_viewModel.ErrorMessage);
        ErrorRecoveryButton.Visibility = ErrorInfoBar.IsOpen && !isWarning
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (ErrorInfoBar.IsOpen && isWarning)
        {
            _warningDismissTimer.Start();
        }
    }

    private void OnLoadingDelayElapsed(object? sender, object e)
    {
        _loadingDelayTimer.Stop();
        if (!_viewModel.IsLoading)
        {
            return;
        }

        LoadingOverlay.Visibility = Visibility.Visible;
        LoadingRing.IsActive = true;
        UpdateCancelState();
    }

    private void OnWarningDismissElapsed(object? sender, object e)
    {
        _warningDismissTimer.Stop();
        ErrorInfoBar.IsOpen = false;
    }

    private async void OnRootDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        var path = await TryGetFirstDroppedFilePathAsync(e);
        var isValid = path is not null && _viewModel.ViewerState.CurrentImage is not null
            ? Path.GetExtension(path).Length > 0
            : path is not null;

        if (path is not null)
        {
            isValid = _viewModel.CanAttemptOpenFormat(Path.GetExtension(path));
        }

        DropIndicator.Show(isValid);
    }

    private void OnRootDragLeave(object sender, DragEventArgs e)
    {
        DropIndicator.Hide();
    }

    private async void OnRootDrop(object sender, DragEventArgs e)
    {
        DropIndicator.Hide();
        var path = await TryGetFirstDroppedFilePathAsync(e);
        if (path is not null)
        {
            await _viewModel.OpenFileAsync(path);
        }
    }

    private static async Task<string?> TryGetFirstDroppedFilePathAsync(DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return null;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        return items.OfType<Windows.Storage.StorageFile>().FirstOrDefault()?.Path;
    }

    private async Task ExportCurrentImageAsync()
    {
        if (_viewModel.ViewerState.CurrentImage is null)
        {
            return;
        }

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            SuggestedFileName = Path.GetFileNameWithoutExtension(_viewModel.ViewerState.CurrentImage.FilePath)
        };
        picker.FileTypeChoices.Add("PNG image", [".png"]);
        picker.FileTypeChoices.Add("JPEG image", [".jpg"]);
        picker.FileTypeChoices.Add("BMP image", [".bmp"]);
        picker.FileTypeChoices.Add("WEBP image", [".webp"]);
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        if (await _viewModel.ExportCurrentImageAsync(file.Path, GetExportFormat(file.Path)))
        {
            ShowInfo("Image exported.");
        }
    }

    private async Task SaveTransformsWithConfirmationAsync()
    {
        if (_viewModel.ViewerState.CurrentImage is null || !_viewModel.TransformState.HasUnsavedChanges)
        {
            return;
        }

        var fileName = Path.GetFileName(_viewModel.ViewerState.CurrentImage.FilePath);
        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            Title = "Save changes",
            Content = $"Save changes to {fileName}? This will overwrite the original file.",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            await _viewModel.SaveTransformsAsync();
        }
    }

    private static ImageExportFormat GetExportFormat(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ImageExportFormat.Jpeg,
            ".bmp" => ImageExportFormat.Bmp,
            ".webp" => ImageExportFormat.Webp,
            _ => ImageExportFormat.Png
        };
    }

    private static bool IsPeriodKey(VirtualKey key)
    {
        return key is VirtualKey.Decimal || (int)key == OemPeriodVirtualKey;
    }

    private static bool IsCommaKey(VirtualKey key)
    {
        return (int)key == OemCommaVirtualKey;
    }

    private static bool IsPlusKey(VirtualKey key)
    {
        return key is VirtualKey.Add || (int)key == OemPlusVirtualKey;
    }

    private static bool IsMinusKey(VirtualKey key)
    {
        return key is VirtualKey.Subtract || (int)key == OemMinusVirtualKey;
    }
}
