using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System.Collections.Specialized;

namespace ImageViewer.App.Views;

public sealed partial class FadingChrome : UserControl
{
    private readonly DispatcherTimer _fadeTimer;
    private MainViewModel? _viewModel;

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
        viewModel.RecentFiles.CollectionChanged += OnRecentFilesChanged;
        RebuildRecentFilesMenu();
        ShowChrome();
    }

    public void ShowChrome()
    {
        _fadeTimer.Stop();
        AnimateOpacity(1.0, 100);
        _fadeTimer.Start();
    }

    private void FadeOutChrome()
    {
        _fadeTimer.Stop();
        AnimateOpacity(0.1, 300);
    }

    private void AnimateOpacity(double opacity, int durationMs)
    {
        var animation = new DoubleAnimation
        {
            To = opacity,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EnableDependentAnimation = true
        };

        Storyboard.SetTarget(animation, ChromeRoot);
        Storyboard.SetTargetProperty(animation, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void OnOpenFileClicked(object sender, RoutedEventArgs e)
    {
        OpenFileRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnRecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildRecentFilesMenu();
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
