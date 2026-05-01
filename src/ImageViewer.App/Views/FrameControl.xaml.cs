using System.ComponentModel;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ImageViewer.App.Views;

public sealed partial class FrameControl : UserControl
{
    private MainViewModel? _viewModel;
    private bool _isUpdatingFromState;

    public FrameControl()
    {
        InitializeComponent();
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.AnimationState.PropertyChanged += OnAnimationStateChanged;
        Update();
    }

    private void OnAnimationStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Update);
    }

    private void Update()
    {
        if (_viewModel is null)
        {
            return;
        }

        var state = _viewModel.AnimationState;
        ControlRoot.Visibility = state.IsAnimationActive
            ? Visibility.Visible
            : Visibility.Collapsed;

        PreviousButton.IsEnabled = state.IsAnimationActive;
        PlayPauseButton.IsEnabled = state.IsAnimationActive;
        NextButton.IsEnabled = state.IsAnimationActive;
        FrameSlider.IsEnabled = state.IsAnimationActive;
        FrameText.Text = state.FramePositionText;
        PlayPauseIcon.Glyph = state.IsPlaying ? "\uE769" : "\uE768";

        _isUpdatingFromState = true;
        FrameSlider.Maximum = Math.Max(0, state.TotalFrames - 1);
        FrameSlider.Value = state.IsAnimationActive ? state.CurrentFrameIndex : 0;
        _isUpdatingFromState = false;
    }

    private void OnPreviousClicked(object sender, RoutedEventArgs e)
    {
        _viewModel?.StepAnimationFrameFromControl(-1);
    }

    private async void OnPlayPauseClicked(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            await _viewModel.ToggleAnimationPlayback();
        }
    }

    private void OnNextClicked(object sender, RoutedEventArgs e)
    {
        _viewModel?.StepAnimationFrameFromControl(1);
    }

    private void OnSliderValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingFromState || _viewModel?.AnimationState.IsAnimationActive != true)
        {
            return;
        }

        _viewModel.SeekAnimationFrame((int)Math.Round(e.NewValue));
    }
}
