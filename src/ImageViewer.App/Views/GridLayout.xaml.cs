using System.ComponentModel;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        var frames = new List<FrameworkElement>(gridImages.Length);

        foreach (var gridImage in gridImages)
        {
            var bitmap = await TryLoadBitmapAsync(gridImage.Item.FilePath);
            if (loadVersion != _loadVersion)
            {
                return;
            }

            var image = new Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var frame = new Border
            {
                Width = gridImage.Cell.Width,
                Height = gridImage.Cell.Height,
                Child = image
            };

            Canvas.SetLeft(frame, gridImage.Cell.X);
            Canvas.SetTop(frame, gridImage.Cell.Y);
            frames.Add(frame);
        }

        if (loadVersion != _loadVersion)
        {
            return;
        }

        ImageHost.Children.Clear();
        foreach (var frame in frames)
        {
            ImageHost.Children.Add(frame);
        }
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
}
