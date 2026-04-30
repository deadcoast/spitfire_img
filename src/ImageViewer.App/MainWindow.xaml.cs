using System.ComponentModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace ImageViewer.App;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly AppWindow _appWindow;
    private readonly DispatcherTimer _loadingDelayTimer;
    private readonly DispatcherTimer _warningDismissTimer;
    private readonly DispatcherTimer _animationTimer;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        _appWindow = GetAppWindow();
        _loadingDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _loadingDelayTimer.Tick += OnLoadingDelayElapsed;
        _warningDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _warningDismissTimer.Tick += OnWarningDismissElapsed;
        _animationTimer = new DispatcherTimer();
        _animationTimer.Tick += OnAnimationTimerTick;

        RootGrid.DataContext = viewModel;
        ImageCanvas.Attach(viewModel);
        ThumbnailStrip.Attach(viewModel);
        ComparisonView.Attach(viewModel);
        SearchOverlay.Attach(viewModel);
        CommandPalette.Attach(viewModel);
        MetadataPanel.Attach(viewModel);
        Chrome.Attach(viewModel);
        Chrome.OpenFileRequested += async (_, _) => await OpenFileFromPickerAsync();
        Chrome.RecentFileRequested += async (_, path) => await _viewModel.OpenRecentFileAsync(path);
        Chrome.ExitRequested += (_, _) => Close();
        Chrome.AboutRequested += (_, _) => ShowInfo("spitfire.img Phase 1 MVP Viewer");
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.ViewerState.PropertyChanged += OnViewerStatePropertyChanged;
        viewModel.CommandPaletteState.PropertyChanged += OnCommandPaletteStatePropertyChanged;
        viewModel.AnimationState.PropertyChanged += OnAnimationStatePropertyChanged;
        viewModel.CodecInstallPromptRequested += OnCodecInstallPromptRequested;
        Activated += OnActivated;
        Closed += OnClosed;
        _ = InitializeWindowAsync();
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        RootGrid.Focus(FocusState.Programmatic);
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.ViewerState.PropertyChanged -= OnViewerStatePropertyChanged;
        _viewModel.CommandPaletteState.PropertyChanged -= OnCommandPaletteStatePropertyChanged;
        _viewModel.AnimationState.PropertyChanged -= OnAnimationStatePropertyChanged;
        _viewModel.CodecInstallPromptRequested -= OnCodecInstallPromptRequested;
        _animationTimer.Stop();
        await _viewModel.SaveWindowStateAsync(
            _appWindow.Size.Width,
            _appWindow.Size.Height,
            _appWindow.Position.X,
            _appWindow.Position.Y);
    }

    public Task OpenInitialFileAsync(string filePath)
    {
        return _viewModel.OpenFileAsync(filePath);
    }

    private async Task InitializeWindowAsync()
    {
        await _viewModel.InitializeAsync();
        ApplyTheme();

        if (_viewModel.Settings.WindowWidth > 0 && _viewModel.Settings.WindowHeight > 0)
        {
            _appWindow.Resize(new Windows.Graphics.SizeInt32(
                _viewModel.Settings.WindowWidth,
                _viewModel.Settings.WindowHeight));
        }

        if (_viewModel.Settings.WindowX >= 0 &&
            _viewModel.Settings.WindowY >= 0 &&
            IsSavedWindowPositionVisible(_viewModel.Settings.WindowX, _viewModel.Settings.WindowY))
        {
            _appWindow.Move(new Windows.Graphics.PointInt32(
                _viewModel.Settings.WindowX,
                _viewModel.Settings.WindowY));
        }
    }

    private async Task OpenFileFromPickerAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };

        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".bmp", ".webp", ".gif", ".tiff", ".tif", ".heic", ".avif" })
        {
            if (_viewModel.IsSupportedOpenExtension(extension))
            {
                picker.FileTypeFilter.Add(extension);
            }
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
        Chrome.ShowChrome();

        var isControlPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) &
                                Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        var isShiftPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) &
                              Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

        if (!isControlPressed && _viewModel.AnimationState.IsAnimationActive)
        {
            if (e.Key == VirtualKey.P)
            {
                _viewModel.ToggleAnimationPlayback();
                e.Handled = true;
                return;
            }

            if (IsPeriodKey(e.Key))
            {
                _viewModel.AdvanceAnimationFrame();
                e.Handled = true;
                return;
            }

            if (IsCommaKey(e.Key))
            {
                _viewModel.RewindAnimationFrame();
                e.Handled = true;
                return;
            }
        }

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
            case VirtualKey.Home:
                await _viewModel.NavigateFirstAsync();
                e.Handled = true;
                break;
            case VirtualKey.End:
                await _viewModel.NavigateLastAsync();
                e.Handled = true;
                break;
            case VirtualKey.F5:
                _viewModel.ToggleSlideshow();
                e.Handled = true;
                break;
            case VirtualKey.D when isControlPressed && isShiftPressed:
                await _viewModel.EnterComparisonAsync(ComparisonLayout.Overlay);
                e.Handled = true;
                break;
            case VirtualKey.D when isControlPressed:
                await _viewModel.EnterComparisonAsync(ComparisonLayout.SideBySide);
                e.Handled = true;
                break;
            case VirtualKey.P when isControlPressed && isShiftPressed:
                _viewModel.ShowCommandPalette();
                e.Handled = true;
                break;
            case VirtualKey.Number2 when isControlPressed && _viewModel.ComparisonState.IsActive:
                await _viewModel.ChangeComparisonImageCountAsync(2);
                e.Handled = true;
                break;
            case VirtualKey.Number3 when isControlPressed && _viewModel.ComparisonState.IsActive:
                await _viewModel.ChangeComparisonImageCountAsync(3);
                e.Handled = true;
                break;
            case VirtualKey.Number4 when isControlPressed && _viewModel.ComparisonState.IsActive:
                await _viewModel.ChangeComparisonImageCountAsync(4);
                e.Handled = true;
                break;
            case VirtualKey.Tab when _viewModel.ComparisonState.IsActive:
                _viewModel.SelectNextComparisonImage();
                e.Handled = true;
                break;
            case VirtualKey.Enter when _viewModel.ComparisonState.IsActive:
                await _viewModel.OpenSelectedComparisonImageAsync();
                e.Handled = true;
                break;
            case VirtualKey.Up when _viewModel.ComparisonState is { IsActive: true, Layout: ComparisonLayout.Overlay }:
                _viewModel.IncreaseOverlayOpacity();
                e.Handled = true;
                break;
            case VirtualKey.Down when _viewModel.ComparisonState is { IsActive: true, Layout: ComparisonLayout.Overlay }:
                _viewModel.DecreaseOverlayOpacity();
                e.Handled = true;
                break;
            case VirtualKey.S when !isControlPressed && _viewModel.ComparisonState is { IsActive: true, Layout: ComparisonLayout.Overlay }:
                _viewModel.SwapOverlayLayers();
                e.Handled = true;
                break;
            case VirtualKey.Space when _viewModel.SlideshowState.IsActive:
                _viewModel.ToggleSlideshowPause();
                e.Handled = true;
                break;
            case VirtualKey.Add when _viewModel.SlideshowState.IsActive:
                await _viewModel.IncreaseSlideshowIntervalAsync();
                e.Handled = true;
                break;
            case VirtualKey.Subtract when _viewModel.SlideshowState.IsActive:
                await _viewModel.DecreaseSlideshowIntervalAsync();
                e.Handled = true;
                break;
            case VirtualKey.F11:
                _viewModel.ToggleFullscreen();
                e.Handled = true;
                break;
            case VirtualKey.Escape:
                if (_viewModel.CommandPaletteState.IsOpen)
                {
                    _viewModel.HideCommandPalette();
                    e.Handled = true;
                }
                else if (_viewModel.SlideshowState.IsActive)
                {
                    _viewModel.StopSlideshow();
                    e.Handled = true;
                }
                else if (_viewModel.ComparisonState.IsActive)
                {
                    _viewModel.ExitComparison();
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
            case VirtualKey.Number1 when isControlPressed:
                _viewModel.ActualSize();
                e.Handled = true;
                break;
            case VirtualKey.Add when isControlPressed:
                _viewModel.ZoomIn();
                e.Handled = true;
                break;
            case VirtualKey.Subtract when isControlPressed:
                _viewModel.ZoomOut();
                e.Handled = true;
                break;
            case VirtualKey.O when isControlPressed:
                await OpenFileFromPickerAsync();
                e.Handled = true;
                break;
            case VirtualKey.F when isControlPressed:
                _viewModel.ShowSearch();
                SearchOverlay.Show();
                e.Handled = true;
                break;
            case VirtualKey.T when isControlPressed:
                await _viewModel.ToggleThumbnailStrip();
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
                _viewModel.ToggleComparisonSyncZoom();
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

    private void OnRootPointerMoved(object sender, PointerRoutedEventArgs e)
    {
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

    private void OnCommandPaletteStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CommandPaletteState.IsOpen))
        {
            DispatcherQueue.TryEnqueue(UpdateChromeFadeState);
        }
    }

    private void OnAnimationStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AnimationState.IsPlaying)
            or nameof(AnimationState.IsAnimationActive)
            or nameof(AnimationState.CurrentFrameIndex)
            or nameof(AnimationState.CurrentAnimation))
        {
            DispatcherQueue.TryEnqueue(UpdateAnimationTimer);
        }
    }

    private void OnViewerStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewerState.IsFullscreen))
        {
            DispatcherQueue.TryEnqueue(ApplyFullscreenState);
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

    private void UpdateChromeFadeState()
    {
        if (_viewModel.CommandPaletteState.IsOpen)
        {
            Chrome.PauseAutoFade();
        }
        else
        {
            Chrome.ResumeAutoFade();
        }
    }

    private void UpdateAnimationTimer()
    {
        _animationTimer.Stop();
        if (!_viewModel.AnimationState.IsAnimationActive || !_viewModel.AnimationState.IsPlaying)
        {
            return;
        }

        var delay = _viewModel.AnimationState.CurrentFrameDelay;
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        _animationTimer.Interval = delay;
        _animationTimer.Start();
    }

    private void OnAnimationTimerTick(object? sender, object e)
    {
        _animationTimer.Stop();
        _viewModel.AdvanceAnimationPlaybackFrame();
        UpdateAnimationTimer();
    }

    private static bool IsPeriodKey(VirtualKey key)
    {
        return key == VirtualKey.Decimal || (int)key == 190;
    }

    private static bool IsCommaKey(VirtualKey key)
    {
        return (int)key == 188;
    }

    private AppWindow GetAppWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private bool IsSavedWindowPositionVisible(int x, int y)
    {
        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Nearest);
        var workArea = displayArea.WorkArea;
        return x >= workArea.X &&
               y >= workArea.Y &&
               x < workArea.X + workArea.Width &&
               y < workArea.Y + workArea.Height;
    }

    private void ShowInfo(string message)
    {
        ErrorInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational;
        ErrorInfoBar.Message = message;
        ErrorInfoBar.IsOpen = true;
    }

    private void OnCancelLoadingClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelLoading();
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
        LoadingOverlay.Visibility = Visibility.Collapsed;
        CancelLoadingButton.Visibility = Visibility.Collapsed;
    }

    private void UpdateCancelState()
    {
        CancelLoadingButton.Visibility = _viewModel.IsCancelAvailable
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateErrorState()
    {
        _warningDismissTimer.Stop();
        ErrorInfoBar.Severity = _viewModel.CurrentErrorSeverity == ErrorSeverity.Warning
            ? Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning
            : Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
        ErrorInfoBar.Message = _viewModel.ErrorMessage ?? string.Empty;
        ErrorInfoBar.IsOpen = !string.IsNullOrWhiteSpace(_viewModel.ErrorMessage);

        if (ErrorInfoBar.IsOpen && _viewModel.CurrentErrorSeverity == ErrorSeverity.Warning)
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
            isValid = _viewModel.IsSupportedOpenExtension(Path.GetExtension(path));
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

    private async void OnCodecInstallPromptRequested(object? sender, CodecInstallPrompt prompt)
    {
        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            Title = $"{prompt.FormatName} codec required",
            Content = prompt.Message,
            PrimaryButtonText = "Open Store",
            CloseButtonText = "Not now",
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            await Launcher.LaunchUriAsync(new Uri(prompt.StoreUrl));
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
}
