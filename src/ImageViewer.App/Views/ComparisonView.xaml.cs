using System.Collections.Specialized;
using System.ComponentModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace ImageViewer.App.Views;

public sealed partial class ComparisonView : UserControl
{
    private MainViewModel? _viewModel;
    private int _renderVersion;
    private bool _isPanning;
    private Point _lastPointerPosition;

    public ComparisonView()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.ComparisonState.PropertyChanged += OnComparisonStateChanged;
        viewModel.ComparisonState.VisibleImages.CollectionChanged += OnVisibleImagesChanged;
        viewModel.ComparisonState.Viewports.CollectionChanged += OnVisibleImagesChanged;
        Update();
    }

    private void OnComparisonStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Update);
    }

    private void OnVisibleImagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Update);
    }

    private void Update()
    {
        var state = _viewModel?.ComparisonState;
        if (state is null || !state.IsActive)
        {
            Visibility = Visibility.Collapsed;
            ComparisonRoot.Children.Clear();
            ComparisonRoot.ColumnDefinitions.Clear();
            ComparisonRoot.RowDefinitions.Clear();
            _renderVersion++;
            return;
        }

        Visibility = Visibility.Visible;
        if (state.Layout == ComparisonLayout.Overlay)
        {
            RenderOverlay(state.VisibleImages.ToArray());
            return;
        }

        RenderSideBySide(state.VisibleImages.ToArray());
    }

    private void RenderSideBySide(IReadOnlyList<ComparisonImage> images)
    {
        var renderVersion = ++_renderVersion;
        ComparisonRoot.Children.Clear();
        ComparisonRoot.ColumnDefinitions.Clear();
        ComparisonRoot.RowDefinitions.Clear();
        ComparisonRoot.ColumnSpacing = 12;
        ComparisonRoot.RowSpacing = 12;

        var rows = images.Count <= 3 ? 1 : 2;
        var columns = images.Count <= 3 ? images.Count : 2;
        for (var row = 0; row < rows; row++)
        {
            ComparisonRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        for (var column = 0; column < columns; column++)
        {
            ComparisonRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (var index = 0; index < images.Count; index++)
        {
            var cell = CreateImageCell(
                images[index],
                index,
                opacity: 1,
                isSelected: _viewModel?.ComparisonState.ActiveImageIndex == index);
            Grid.SetRow(cell.Root, index / columns);
            Grid.SetColumn(cell.Root, index % columns);
            ComparisonRoot.Children.Add(cell.Root);
            _ = LoadImageAsync(cell.Image, images[index].Image.FilePath, renderVersion);
        }
    }

    private void RenderOverlay(IReadOnlyList<ComparisonImage> images)
    {
        var renderVersion = ++_renderVersion;
        ComparisonRoot.Children.Clear();
        ComparisonRoot.ColumnDefinitions.Clear();
        ComparisonRoot.RowDefinitions.Clear();
        ComparisonRoot.ColumnSpacing = 0;
        ComparisonRoot.RowSpacing = 0;

        foreach (var comparisonImage in images)
        {
            var isTopLayer = ComparisonRoot.Children.Count > 0;
            var cell = CreateImageCell(
                comparisonImage,
                ComparisonRoot.Children.Count,
                opacity: isTopLayer
                    ? _viewModel?.ComparisonState.OverlayOpacity ?? 0.5
                    : 1,
                isSelected: _viewModel?.ComparisonState.ActiveImageIndex == ComparisonRoot.Children.Count);
            ComparisonRoot.Children.Add(cell.Root);
            _ = LoadImageAsync(cell.Image, comparisonImage.Image.FilePath, renderVersion);
        }

        AddOverlayOpacitySlider();
    }

    private ComparisonCell CreateImageCell(
        ComparisonImage comparisonImage,
        int visibleIndex,
        double opacity,
        bool isSelected)
    {
        var root = new Grid
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
        };
        var viewport = _viewModel?.ComparisonState.Viewports.ElementAtOrDefault(visibleIndex);

        var image = new Image
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Stretch = Stretch.Uniform,
            Opacity = opacity,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = CreateViewportTransform(viewport)
        };
        root.Children.Add(image);

        var label = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10),
            Padding = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(180, 0, 0, 0)),
            Child = new TextBlock
            {
                Text = (comparisonImage.PlaylistIndex + 1).ToString(),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 13
            }
        };
        root.Children.Add(label);

        var container = new Border
        {
            BorderBrush = new SolidColorBrush(isSelected
                ? Microsoft.UI.ColorHelper.FromArgb(255, 255, 255, 255)
                : Microsoft.UI.ColorHelper.FromArgb(90, 255, 255, 255)),
            BorderThickness = isSelected ? new Thickness(2) : new Thickness(1),
            Child = root
        };
        container.Tapped += (_, _) => _viewModel?.SelectComparisonImage(visibleIndex);

        return new ComparisonCell(container, image);
    }

    private CompositeTransform CreateViewportTransform(ComparisonViewport? viewport)
    {
        var zoomLevel = viewport?.ZoomLevel ?? 100;
        var relativePan = viewport?.RelativePan ?? new RelativePanOffset(0.5, 0.5);
        var scale = zoomLevel / 100d;
        return new CompositeTransform
        {
            ScaleX = scale,
            ScaleY = scale,
            TranslateX = (relativePan.X - 0.5) * Math.Max(1, ActualWidth),
            TranslateY = (relativePan.Y - 0.5) * Math.Max(1, ActualHeight)
        };
    }

    private void AddOverlayOpacitySlider()
    {
        if (_viewModel is null)
        {
            return;
        }

        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = _viewModel.ComparisonState.OverlayOpacity * 100,
            Width = 220
        };
        AutomationProperties.SetName(slider, "Overlay opacity");
        slider.ValueChanged += (_, args) =>
        {
            if (_viewModel.ComparisonState.IsActive &&
                _viewModel.ComparisonState.Layout == ComparisonLayout.Overlay)
            {
                _viewModel.SetOverlayOpacity(args.NewValue / 100);
            }
        };

        var container = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 20),
            Padding = new Thickness(12, 4, 12, 4),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(180, 0, 0, 0)),
            Child = slider
        };
        ComparisonRoot.Children.Add(container);
    }

    private async Task LoadImageAsync(Image target, string filePath, int renderVersion)
    {
        try
        {
            await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var randomAccessStream = fileStream.AsRandomAccessStream();
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

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_viewModel?.ComparisonState.IsActive != true)
        {
            return;
        }

        _isPanning = true;
        _lastPointerPosition = e.GetCurrentPoint(this).Position;
        CapturePointer(e.Pointer);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPanning || _viewModel?.ComparisonState.ActiveViewport is null)
        {
            return;
        }

        var position = e.GetCurrentPoint(this).Position;
        var delta = new RelativePanOffset(
            (position.X - _lastPointerPosition.X) / Math.Max(1, ActualWidth),
            (position.Y - _lastPointerPosition.Y) / Math.Max(1, ActualHeight));
        var current = _viewModel.ComparisonState.ActiveViewport.RelativePan;
        _viewModel.SetComparisonRelativePan(new RelativePanOffset(
            current.X + delta.X,
            current.Y + delta.Y));
        _lastPointerPosition = position;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isPanning = false;
        ReleasePointerCapture(e.Pointer);
    }

    private sealed record ComparisonCell(Border Root, Image Image);
}
