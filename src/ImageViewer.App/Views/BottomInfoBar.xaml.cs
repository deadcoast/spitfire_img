using System.ComponentModel;
using System.Collections.Specialized;
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
        viewModel.SlideshowState.PropertyChanged += OnStateChanged;
        viewModel.ComparisonState.PropertyChanged += OnStateChanged;
        viewModel.AnimationState.PropertyChanged += OnStateChanged;
        viewModel.ComparisonState.VisibleImages.CollectionChanged += OnVisibleImagesChanged;
        viewModel.ComparisonState.Viewports.CollectionChanged += OnVisibleImagesChanged;
        Update();
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Update);
    }

    private void OnVisibleImagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
        PositionText.Text = _viewModel.PlaylistState.TotalCount > 0
            ? $"{_viewModel.PlaylistState.CurrentIndex + 1} / {_viewModel.PlaylistState.TotalCount}"
            : string.Empty;
        DimensionsText.Text = _viewModel.ViewerState.Metadata?.FormattedDimensions ?? string.Empty;
        SlideshowText.Text = $"Slideshow {_viewModel.SlideshowState.Interval.TotalSeconds:0}s";
        SlideshowText.Visibility = _viewModel.SlideshowState.IsActive
            ? Visibility.Visible
            : Visibility.Collapsed;
        ComparisonText.Text = GetComparisonText();
        ComparisonText.Visibility = _viewModel.ComparisonState.IsActive
            ? Visibility.Visible
            : Visibility.Collapsed;
        AnimationText.Text = _viewModel.AnimationState.FramePositionText;
        AnimationText.Visibility = _viewModel.AnimationState.IsAnimationActive
            ? Visibility.Visible
            : Visibility.Collapsed;
        FolderText.Text = string.IsNullOrWhiteSpace(_viewModel.PlaylistState.FolderPath)
            ? string.Empty
            : Path.GetFileName(_viewModel.PlaylistState.FolderPath);
    }

    private string GetComparisonText()
    {
        if (_viewModel is null || !_viewModel.ComparisonState.IsActive)
        {
            return string.Empty;
        }

        var syncText = _viewModel.ComparisonState.IsSyncZoomEnabled
            ? "  Sync"
            : string.Empty;
        return $"Compare {_viewModel.ComparisonState.VisibleIndicesText} / {_viewModel.PlaylistState.TotalCount}{syncText}";
    }
}
