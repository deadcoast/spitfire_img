using System.ComponentModel;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ImageViewer.App.Views;

public sealed partial class FrameControl : UserControl
{
    private MainViewModel? _viewModel;
    private bool _isUpdating;

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
        Visibility = state.IsAnimated ? Visibility.Visible : Visibility.Collapsed;
        PlayPauseIcon.Symbol = state.IsPlaying ? Symbol.Pause : Symbol.Play;
        FrameText.Text = state.FrameDisplayText;

        _isUpdating = true;
        FrameSlider.IsEnabled = state.IsAnimated && state.TotalFrames > 1;
        FrameSlider.Minimum = 1;
        FrameSlider.Maximum = Math.Max(1, state.TotalFrames);
        FrameSlider.Value = state.TotalFrames == 0 ? 1 : state.CurrentFrame + 1;
        _isUpdating = false;
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
}
