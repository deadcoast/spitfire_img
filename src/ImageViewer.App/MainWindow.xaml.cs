using System.ComponentModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
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
        Chrome.Attach(viewModel);
        Chrome.OpenFileRequested += async (_, _) => await OpenFileFromPickerAsync();
        Chrome.ExitRequested += (_, _) => Close();
        Chrome.AboutRequested += (_, _) => ShowInfo("spitfire.img Phase 1 MVP Viewer");
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.ViewerState.PropertyChanged += OnViewerStatePropertyChanged;
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

        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".bmp", ".webp" })
        {
            picker.FileTypeFilter.Add(extension);
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
            case VirtualKey.F11:
                _viewModel.ToggleFullscreen();
                e.Handled = true;
                break;
            case VirtualKey.Escape:
                if (_viewModel.ViewerState.IsFullscreen)
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
}
