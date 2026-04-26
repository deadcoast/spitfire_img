using System.ComponentModel;
using ImageViewer.Core.ViewModels;
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
        PositionText.Text = _viewModel.PlaylistState.TotalCount > 0
            ? $"{_viewModel.PlaylistState.CurrentIndex + 1} / {_viewModel.PlaylistState.TotalCount}"
            : string.Empty;
        DimensionsText.Text = _viewModel.ViewerState.Metadata?.FormattedDimensions ?? string.Empty;
        FolderText.Text = string.IsNullOrWhiteSpace(_viewModel.PlaylistState.FolderPath)
            ? string.Empty
            : Path.GetFileName(_viewModel.PlaylistState.FolderPath);
    }
}
