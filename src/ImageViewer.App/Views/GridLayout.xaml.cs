using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
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
    private int _renderVersion;

    public GridLayout()
    {
        InitializeComponent();
        PointerWheelChanged += OnPointerWheelChanged;
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.LayoutState.PropertyChanged += OnLayoutStateChanged;
        viewModel.LayoutState.VisibleImages.CollectionChanged += OnVisibleImagesChanged;
        Update();
    }

    private void OnLayoutStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Update);
    }

    private void OnVisibleImagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Update);
    }

    private void Update()
    {
        var state = _viewModel?.LayoutState;
        if (state is null || !state.IsActive || state.Mode != LayoutMode.Grid)
        {
            Visibility = Visibility.Collapsed;
            GridLayoutRoot.Children.Clear();
            GridLayoutRoot.RowDefinitions.Clear();
            GridLayoutRoot.ColumnDefinitions.Clear();
            _renderVersion++;
            return;
        }

        Visibility = Visibility.Visible;
        RenderGrid(state);
    }

    private void RenderGrid(LayoutState state)
    {
        var renderVersion = ++_renderVersion;
        GridLayoutRoot.Children.Clear();
        GridLayoutRoot.RowDefinitions.Clear();
        GridLayoutRoot.ColumnDefinitions.Clear();
        GridLayoutRoot.RowSpacing = state.Config.CellSpacing;
        GridLayoutRoot.ColumnSpacing = state.Config.CellSpacing;

        for (var row = 0; row < state.Config.Rows; row++)
        {
            GridLayoutRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        for (var column = 0; column < state.Config.Columns; column++)
        {
            GridLayoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (var index = 0; index < state.VisibleImages.Count; index++)
        {
            var layoutImage = state.VisibleImages[index];
            var cell = CreateGridCell(
                layoutImage,
                index,
                state.ActiveImageIndex == index);
            Microsoft.UI.Xaml.Controls.Grid.SetRow(cell.Root, index / state.Config.Columns);
            Microsoft.UI.Xaml.Controls.Grid.SetColumn(cell.Root, index % state.Config.Columns);
            GridLayoutRoot.Children.Add(cell.Root);
            _ = LoadImageAsync(cell.Image, layoutImage.Image.FilePath, renderVersion);
        }
    }

    private GridCell CreateGridCell(LayoutImage layoutImage, int visibleIndex, bool isSelected)
    {
        var image = new Image
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Stretch = Stretch.Uniform
        };

        var label = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8),
            Padding = new Thickness(7, 3, 7, 3),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(180, 0, 0, 0)),
            Child = new TextBlock
            {
                Text = (layoutImage.PlaylistIndex + 1).ToString(),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 12
            }
        };

        var root = new Grid
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
        };
        root.Children.Add(image);
        root.Children.Add(label);

        var border = new Border
        {
            BorderBrush = new SolidColorBrush(isSelected
                ? Microsoft.UI.ColorHelper.FromArgb(255, 255, 255, 255)
                : Microsoft.UI.ColorHelper.FromArgb(80, 255, 255, 255)),
            BorderThickness = isSelected ? new Thickness(2) : new Thickness(1),
            Child = root
        };
        border.Tapped += (_, _) => _viewModel?.SelectGridImage(visibleIndex);

        return new GridCell(border, image);
    }

    private async void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (_viewModel?.LayoutState.IsActive != true)
        {
            return;
        }

        var wheelDelta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        if (wheelDelta == 0)
        {
            return;
        }

        await _viewModel.ScrollGridLayoutAsync(wheelDelta < 0 ? 1 : -1);
        e.Handled = true;
    }

    private async Task LoadImageAsync(Image target, string filePath, int renderVersion)
    {
        try
        {
            var encodedBytes = await File.ReadAllBytesAsync(filePath);
            if (renderVersion != _renderVersion)
            {
                return;
            }

            using var randomAccessStream = new InMemoryRandomAccessStream();
            await randomAccessStream.WriteAsync(encodedBytes.AsBuffer());
            randomAccessStream.Seek(0);

            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(randomAccessStream);

            if (renderVersion == _renderVersion)
            {
                target.Source = bitmap;
            }
        }
        catch
        {
            if (renderVersion == _renderVersion)
            {
                target.Source = null;
            }
        }
    }

    private sealed record GridCell(Border Root, Image Image);
}
