using System.ComponentModel;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ImageViewer.App.Views;

public sealed partial class BottomInfoBar : UserControl
{
    private MainViewModel? _viewModel;

    public BottomInfoBar()
    {
        InitializeComponent();
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.ViewerState.PropertyChanged += OnStateChanged;
        viewModel.PlaylistState.PropertyChanged += OnStateChanged;
        viewModel.ComparisonState.PropertyChanged += OnStateChanged;
        viewModel.AnimationState.PropertyChanged += OnStateChanged;
        viewModel.SlideshowState.PropertyChanged += OnStateChanged;
        Update();
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Update);
    }

    private void Update()
    {
        if (_viewModel is null)
        {
            return;
        }

        var current = _viewModel.PlaylistState.CurrentItem;
        FilenameText.Text = current?.Filename ?? string.Empty;
        var positionText = _viewModel.ComparisonState.IsActive
            ? $"{_viewModel.ComparisonState.FormattedDisplayedIndices} / {_viewModel.PlaylistState.TotalCount}{GetSyncIndicator()}"
            : _viewModel.PlaylistState.TotalCount > 0
            ? $"{_viewModel.PlaylistState.CurrentIndex + 1} / {_viewModel.PlaylistState.TotalCount}"
            : string.Empty;
        PositionText.Text = string.IsNullOrWhiteSpace(_viewModel.AnimationState.FrameDisplayText)
            ? positionText
            : $"{positionText}  {_viewModel.AnimationState.FrameDisplayText}";
        DimensionsText.Text = _viewModel.ViewerState.Metadata?.FormattedDimensions ?? string.Empty;
        FolderText.Text = string.IsNullOrWhiteSpace(_viewModel.PlaylistState.FolderPath)
            ? string.Empty
            : Path.GetFileName(_viewModel.PlaylistState.FolderPath);
        var modeText = GetModeIndicator();
        ModeText.Text = modeText;
        ModeChip.Visibility = string.IsNullOrWhiteSpace(modeText) ? Visibility.Collapsed : Visibility.Visible;
    }

    private string GetSyncIndicator()
    {
        return _viewModel?.ComparisonState.SyncZoomEnabled == true ? "  Sync" : string.Empty;
    }

    private string GetModeIndicator()
    {
        if (_viewModel is null)
        {
            return string.Empty;
        }

        if (_viewModel.SlideshowState.IsActive)
        {
            var status = _viewModel.SlideshowState.IsPaused ? "Paused" : "Slideshow";
            return $"{status} {_viewModel.SlideshowState.Interval.TotalSeconds:0}s";
        }

        if (_viewModel.ComparisonState.IsActive && _viewModel.ComparisonState.SyncZoomEnabled)
        {
            return "Sync zoom";
        }

        if (_viewModel.AnimationState.IsAnimated)
        {
            return _viewModel.AnimationState.IsPlaying ? "Playing" : "Paused";
        }

        return string.Empty;
    }
}
