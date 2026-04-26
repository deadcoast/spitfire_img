using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

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
    }

    public event EventHandler? OpenFileRequested;

    public event EventHandler? ExitRequested;

    public event EventHandler? AboutRequested;

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InfoBar.Attach(viewModel);
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
