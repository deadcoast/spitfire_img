using System.ComponentModel;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ImageViewer.App.Views;

public sealed partial class ThumbnailStrip : UserControl
{
    private MainViewModel? _viewModel;

    public ThumbnailStrip()
    {
        InitializeComponent();
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.ThumbnailState.PropertyChanged += OnStateChanged;
        viewModel.PlaylistState.PropertyChanged += OnStateChanged;
        Refresh();
    }

    public void Toggle()
    {
        _viewModel?.ThumbnailState.ToggleVisibility();
        Refresh();
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        if (_viewModel is null)
        {
            return;
        }

        Visibility = _viewModel.ThumbnailState.IsStripVisible ? Visibility.Visible : Visibility.Collapsed;
        StripBorder.VerticalAlignment = _viewModel.ThumbnailState.StripPosition == Core.Models.ThumbnailPosition.Top
            ? VerticalAlignment.Top
            : VerticalAlignment.Bottom;
        Items.ItemsSource = _viewModel.PlaylistState.Items.Select(item => item.Filename).ToList();
    }
}
