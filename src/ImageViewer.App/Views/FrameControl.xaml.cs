using System.ComponentModel;
using ImageViewer.App.Services.Motion;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace ImageViewer.App.Views;

public sealed partial class FrameControl : UserControl
{
    private MainViewModel? _viewModel;
    private IViewerMotionService? _motionService;
    private bool _isUpdating;
    private bool _isFrameControlVisible;

    public FrameControl()
    {
        InitializeComponent();
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.AnimationState.PropertyChanged += OnAnimationStateChanged;
        Refresh();
    }

    public void AttachMotionService(IViewerMotionService motionService)
    {
        _motionService = motionService;
    }

    private void OnAnimationStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        if (_viewModel is null)
        {
            return;
        }

        var state = _viewModel.AnimationState;
        UpdateFrameControlVisibility(state.IsAnimated);
        PlayPauseIcon.Symbol = state.IsPlaying ? Symbol.Pause : Symbol.Play;
        PlaybackStateText.Text = state.IsPlaying ? "Playing" : "Paused";
        PulseGifState();
        FrameText.Text = state.FrameDisplayText;
        FrameRangeText.Text = state.TotalFrames > 0
            ? $"{state.CurrentFrame + 1} of {state.TotalFrames}"
            : "No frames";
        PreviousButton.IsEnabled = state.IsAnimated && state.TotalFrames > 1;
        PlayPauseButton.IsEnabled = state.IsAnimated;
        NextButton.IsEnabled = state.IsAnimated && state.TotalFrames > 1;
        UpdateAutomationNames(state);

        _isUpdating = true;
        FrameSlider.IsEnabled = state.IsAnimated && state.TotalFrames > 1;
        FrameSlider.Minimum = 1;
        FrameSlider.Maximum = Math.Max(1, state.TotalFrames);
        FrameSlider.Value = state.TotalFrames == 0 ? 1 : state.CurrentFrame + 1;
        _isUpdating = false;
    }

    private void UpdateFrameControlVisibility(bool isAnimated)
    {
        if (isAnimated && !_isFrameControlVisible)
        {
            _isFrameControlVisible = true;
            Visibility = Visibility.Visible;
            ViewerMotionStoryboard.AnimateOpacity(this, GetMotionPlan(), 1);
            return;
        }

        if (!isAnimated && _isFrameControlVisible)
        {
            _isFrameControlVisible = false;
            ViewerMotionStoryboard.AnimateOpacity(
                this,
                GetMotionPlan(),
                0,
                completed: () => Visibility = Visibility.Collapsed);
            return;
        }

        if (!isAnimated)
        {
            Visibility = Visibility.Collapsed;
            Opacity = 0;
        }
    }

    private void PulseGifState()
    {
        if (!_isFrameControlVisible)
        {
            return;
        }

        var motionPlan = GetMotionPlan();
        FrameControlPanel.Opacity = motionPlan.Duration <= TimeSpan.Zero ? 1 : 0.86;
        ViewerMotionStoryboard.AnimateOpacity(FrameControlPanel, motionPlan, 1);
    }

    private void OnPlayPauseClicked(object sender, RoutedEventArgs e)
    {
        _viewModel?.ToggleAnimationPlayback();
    }

    private void OnPreviousClicked(object sender, RoutedEventArgs e)
    {
        _viewModel?.RewindAnimationFrameFromControl();
    }

    private void OnNextClicked(object sender, RoutedEventArgs e)
    {
        _viewModel?.AdvanceAnimationFrameFromControl();
    }

    private void OnFrameSliderValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdating || _viewModel is null)
        {
            return;
        }

        _viewModel.SeekAnimationFrameFromControl((int)Math.Round(e.NewValue) - 1);
    }

    private void UpdateAutomationNames(AnimationState state)
    {
        var playPauseName = state.IsPlaying ? "Pause GIF" : "Play GIF";
        AutomationProperties.SetName(PlayPauseButton, playPauseName);
        AutomationProperties.SetName(FrameSlider, $"GIF frame timeline, frame {state.CurrentFrame + 1} of {Math.Max(1, state.TotalFrames)}");
    }

    private ViewerMotionPlan GetMotionPlan()
    {
        return _motionService?.CreatePlan(ViewerMotionCategory.GifControls)
            ?? ViewerMotionService.CreateDefaultPlan(ViewerMotionCategory.GifControls, isReducedMotion: false);
    }
}
