using ImageViewer.Core.ViewModels;
using ImageViewer.App.Services.Motion;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System.Collections.Specialized;

namespace ImageViewer.App.Views;

public sealed partial class FadingChrome : UserControl
{
    private readonly DispatcherTimer _fadeTimer;
    private MainViewModel? _viewModel;
    private IViewerMotionService? _motionService;

    public FadingChrome()
    {
        InitializeComponent();
        _fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _fadeTimer.Tick += (_, _) => FadeOutChrome();
        PointerEntered += (_, _) => _fadeTimer.Stop();
        PointerExited += (_, _) => ShowChrome();
    }

    public event EventHandler? OpenFileRequested;

    public event EventHandler<string>? RecentFileRequested;

    public event EventHandler? ExitRequested;

    public event EventHandler? AboutRequested;

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InfoBar.Attach(viewModel);
        FrameControl.Attach(viewModel);
        viewModel.RecentFiles.CollectionChanged += OnRecentFilesChanged;
        viewModel.TransformState.PropertyChanged += OnTransformStateChanged;
        viewModel.PlaylistState.PropertyChanged += OnChromeStateChanged;
        viewModel.ComparisonState.PropertyChanged += OnChromeStateChanged;
        viewModel.AnimationState.PropertyChanged += OnChromeStateChanged;
        viewModel.SlideshowState.PropertyChanged += OnChromeStateChanged;
        RebuildRecentFilesMenu();
        UpdateChromeState();
        ShowChrome();
    }

    public void AttachMotionService(IViewerMotionService motionService)
    {
        _motionService = motionService;
        FrameControl.AttachMotionService(motionService);
    }

    private void OnTransformStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateChromeState);
    }

    private void OnChromeStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateChromeState);
    }

    private void UpdateChromeState()
    {
        UnsavedChangesBadge.Visibility = _viewModel?.TransformState.HasUnsavedChanges == true
            ? Visibility.Visible
            : Visibility.Collapsed;
        ChromeStatusText.Text = GetChromeStatusText();
    }

    private string GetChromeStatusText()
    {
        if (_viewModel is null)
        {
            return string.Empty;
        }

        if (_viewModel.TransformState.HasUnsavedChanges)
        {
            return "Unsaved changes";
        }

        if (_viewModel.ComparisonState.IsActive)
        {
            var layout = _viewModel.ComparisonState.Layout == ImageViewer.Core.Models.ComparisonLayout.Overlay
                ? "Overlay comparison"
                : "Side-by-side comparison";
            return _viewModel.ComparisonState.SyncZoomEnabled ? $"{layout} - sync zoom" : layout;
        }

        if (_viewModel.SlideshowState.IsActive)
        {
            var status = _viewModel.SlideshowState.IsPaused ? "Slideshow paused" : "Slideshow";
            return $"{status} - {_viewModel.SlideshowState.Interval.TotalSeconds:0}s";
        }

        if (_viewModel.AnimationState.IsAnimated)
        {
            return _viewModel.AnimationState.IsPlaying ? "Animation playing" : "Animation paused";
        }

        return _viewModel.PlaylistState.TotalCount > 0 ? "Browsing folder" : "Ready";
    }

    public void ShowChrome()
    {
        _fadeTimer.Stop();
        VisualStateManager.GoToState(this, "Active", true);
        ApplyChromeRevealMotion();
        ViewerMotionStoryboard.AnimateOpacity(ChromeRoot, GetMotionPlan(ViewerMotionCategory.ChromeReveal), GetDoubleResource("ViewerChromeActiveOpacity", 1.0));
        _fadeTimer.Start();
    }

    public void PauseFade()
    {
        _fadeTimer.Stop();
        VisualStateManager.GoToState(this, "Active", true);
        ApplyChromeRevealMotion();
        ViewerMotionStoryboard.AnimateOpacity(ChromeRoot, GetMotionPlan(ViewerMotionCategory.ChromeReveal), GetDoubleResource("ViewerChromeActiveOpacity", 1.0));
    }

    public void ResumeFade()
    {
        ShowChrome();
    }

    private void FadeOutChrome()
    {
        _fadeTimer.Stop();
        VisualStateManager.GoToState(this, "Idle", true);
        ViewerMotionStoryboard.AnimateOpacity(ChromeRoot, GetMotionPlan(ViewerMotionCategory.ChromeIdle), GetDoubleResource("ViewerChromeIdleOpacity", 0.72));
    }

    private void ApplyChromeRevealMotion()
    {
        var motionPlan = GetMotionPlan(ViewerMotionCategory.ChromeReveal);
        ViewerMotionStoryboard.AnimateVerticalTranslation(TopChromeTransform, motionPlan, -motionPlan.OffsetPixels, 0);
        ViewerMotionStoryboard.AnimateVerticalTranslation(BottomInfoBarTransform, motionPlan, motionPlan.OffsetPixels, 0);
    }

    private ViewerMotionPlan GetMotionPlan(ViewerMotionCategory category)
    {
        return _motionService?.CreatePlan(category) ?? ViewerMotionService.CreateDefaultPlan(category, isReducedMotion: false);
    }

    private double GetDoubleResource(string key, double fallback)
    {
        return Application.Current.Resources.TryGetValue(key, out var value) && value is double doubleValue
            ? doubleValue
            : fallback;
    }

    private void OnOpenFileClicked(object sender, RoutedEventArgs e)
    {
        OpenFileRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnRecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(RebuildRecentFilesMenu);
    }

    private void RebuildRecentFilesMenu()
    {
        if (_viewModel is null)
        {
            return;
        }

        while (FileMenu.Items.Count > 3)
        {
            FileMenu.Items.RemoveAt(2);
        }

        foreach (var recentFile in _viewModel.RecentFiles.Reverse())
        {
            var item = new MenuFlyoutItem
            {
                Text = recentFile.Filename
            };
            item.Click += (_, _) => RecentFileRequested?.Invoke(this, recentFile.FilePath);
            FileMenu.Items.Insert(2, item);
        }
    }

    private void OnExitClicked(object sender, RoutedEventArgs e)
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnZoomInClicked(object sender, RoutedEventArgs e) => _viewModel?.ZoomIn();

    private void OnZoomOutClicked(object sender, RoutedEventArgs e) => _viewModel?.ZoomOut();

    private void OnActualSizeClicked(object sender, RoutedEventArgs e) => _viewModel?.ActualSize();

    private void OnFitToWindowClicked(object sender, RoutedEventArgs e) => _viewModel?.FitToWindow();

    private void OnToggleFullscreenClicked(object sender, RoutedEventArgs e) => _viewModel?.ToggleFullscreen();

    private async void OnToggleThemeClicked(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            await _viewModel.ToggleThemeAsync();
        }
    }

    private void OnAboutClicked(object sender, RoutedEventArgs e)
    {
        AboutRequested?.Invoke(this, EventArgs.Empty);
    }
}
