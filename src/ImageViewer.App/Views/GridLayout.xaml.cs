using System.ComponentModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace ImageViewer.App.Views;

public sealed partial class GridLayout : UserControl
{
    private MainViewModel? _viewModel;
    private int _loadVersion;

    public GridLayout()
    {
        InitializeComponent();
        PointerWheelChanged += OnPointerWheelChanged;
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.LayoutState.PropertyChanged += OnLayoutStateChanged;
        Refresh();
    }

    private void OnLayoutStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        if (_viewModel is null)
        {
            return;
        }

        LayoutRoot.Visibility = _viewModel.LayoutState.IsGridActive
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!_viewModel.LayoutState.IsGridActive)
        {
            ImageHost.Children.Clear();
            return;
        }

        _ = LoadImagesAsync(++_loadVersion);
    }

    private async Task LoadImagesAsync(int loadVersion)
    {
        if (_viewModel is null)
        {
            return;
        }

        var gridImages = _viewModel.LayoutState.GridImages.ToArray();
        ImageHost.Children.Clear();

        var frameTasks = gridImages
            .Select((gridImage, visibleIndex) => LoadGridFrameAsync(gridImage, visibleIndex))
            .ToList();

        while (frameTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(frameTasks).ConfigureAwait(true);
            frameTasks.Remove(completedTask);
            var frame = await completedTask.ConfigureAwait(true);
            if (loadVersion != _loadVersion)
            {
                return;
            }

            ImageHost.Children.Add(frame);
        }
    }

    private async Task<FrameworkElement> LoadGridFrameAsync(GridImage gridImage, int visibleIndex)
    {
        var bitmap = await TryLoadBitmapAsync(gridImage.Item.FilePath).ConfigureAwait(true);
        var image = new Image
        {
            Source = bitmap,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var frame = new Border
        {
            Tag = visibleIndex,
            Width = gridImage.Cell.Width,
            Height = gridImage.Cell.Height,
            Padding = new Thickness(4),
            BorderThickness = visibleIndex == _viewModel?.LayoutState.ActiveGridImageIndex
                ? new Thickness(3)
                : new Thickness(1),
            BorderBrush = new SolidColorBrush(visibleIndex == _viewModel?.LayoutState.ActiveGridImageIndex
                ? Colors.White
                : Colors.Transparent),
            Child = image
        };
        frame.Tapped += OnGridImageTapped;

        Canvas.SetLeft(frame, gridImage.Cell.X);
        Canvas.SetTop(frame, gridImage.Cell.Y);
        return frame;
    }

    private static async Task<BitmapImage?> TryLoadBitmapAsync(string filePath)
    {
        try
        {
            await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var randomAccessStream = fileStream.AsRandomAccessStream();
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(randomAccessStream);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private void OnGridImageTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border { Tag: int visibleIndex })
        {
            _viewModel?.SetActiveGridImage(visibleIndex);
            e.Handled = true;
        }
    }

    private async void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (_viewModel is null || !_viewModel.LayoutState.IsGridActive)
        {
            return;
        }

        var wheelDelta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        if (wheelDelta == 0)
        {
            return;
        }

        var pageSize = _viewModel.LayoutState.GridLayout.TotalCells;
        var offset = wheelDelta < 0 ? pageSize : -pageSize;
        await _viewModel.ScrollGridAsync(offset, GetCanvasSize());
        e.Handled = true;
    }

    private ImageDimensions GetCanvasSize()
    {
        var width = LayoutRoot.ActualWidth > 0 ? LayoutRoot.ActualWidth : ActualWidth;
        var height = LayoutRoot.ActualHeight > 0 ? LayoutRoot.ActualHeight : ActualHeight;
        return new ImageDimensions(width, height);
    }
}
