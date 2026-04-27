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
        RightTapped += OnRightTapped;
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.ThumbnailState.PropertyChanged += OnStateChanged;
        viewModel.PlaylistState.PropertyChanged += OnStateChanged;
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

    private void OnRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        var flyout = new MenuFlyout();
        var top = new MenuFlyoutItem { Text = "Top" };
        top.Click += (_, _) => _viewModel.SetThumbnailPosition(Core.Models.ThumbnailPosition.Top);
        var bottom = new MenuFlyoutItem { Text = "Bottom" };
        bottom.Click += (_, _) => _viewModel.SetThumbnailPosition(Core.Models.ThumbnailPosition.Bottom);
        flyout.Items.Add(top);
        flyout.Items.Add(bottom);
        flyout.ShowAt(this, e.GetPosition(this));
    }
}
