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

public sealed partial class ComparisonView : UserControl
{
    private MainViewModel? _viewModel;
    private int _loadVersion;
    private bool _isUpdatingSlider;
    private bool _isPanning;
    private int _panningImageIndex = -1;
    private Windows.Foundation.Point _lastPointerPosition;

    public ComparisonView()
    {
        InitializeComponent();
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.ComparisonState.PropertyChanged += OnComparisonStateChanged;
        Refresh();
    }

    private void OnComparisonStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ComparisonState.OverlayOpacity))
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateOverlayControls();
                ApplyOverlayOpacity();
            });
            return;
        }

        if (e.PropertyName == nameof(ComparisonState.ImageViewports))
        {
            DispatcherQueue.TryEnqueue(UpdateRenderedTransforms);
            return;
        }

        DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        if (_viewModel is null)
        {
            return;
        }

        ComparisonRoot.Visibility = _viewModel.ComparisonState.IsActive
            ? Visibility.Visible
            : Visibility.Collapsed;
        UpdateOverlayControls();

        if (!_viewModel.ComparisonState.IsActive)
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

        var state = _viewModel.ComparisonState;
        var items = state.DisplayedItems.ToArray();
        var loadedImages = await Task.WhenAll(items.Select((item, index) => LoadComparisonBitmapAsync(index, item.FilePath)))
            .ConfigureAwait(true);
        if (loadVersion != _loadVersion)
        {
            return;
        }

        var images = new List<FrameworkElement>(items.Length);

        foreach (var loadedImage in loadedImages.OrderBy(image => image.VisibleIndex))
        {
            var bitmap = loadedImage.Bitmap;
            if (bitmap is not null && bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
            {
                _viewModel.SetComparisonImageDimensions(
                    loadedImage.VisibleIndex,
                    new ImageDimensions(bitmap.PixelWidth, bitmap.PixelHeight));
            }

            var image = new Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };

            image.RenderTransform = CreateImageTransform(loadedImage.VisibleIndex);
            images.Add(CreateSelectableImageFrame(image, loadedImage.VisibleIndex, state.ActiveImageIndex));
        }

        if (loadVersion != _loadVersion)
        {
            return;
        }

        RenderImages(state.Layout, images);
    }

    private static async Task<LoadedComparisonImage> LoadComparisonBitmapAsync(int visibleIndex, string filePath)
    {
        return new LoadedComparisonImage(visibleIndex, await TryLoadBitmapAsync(filePath).ConfigureAwait(true));
    }

    private void RenderImages(ComparisonLayout layout, IReadOnlyList<FrameworkElement> images)
    {
        ImageHost.Children.Clear();
        ImageHost.ColumnDefinitions.Clear();
        ImageHost.RowDefinitions.Clear();

        if (layout == ComparisonLayout.Overlay)
        {
            RenderOverlay(images);
            return;
        }

        RenderSideBySide(images);
    }

    private void RenderSideBySide(IReadOnlyList<FrameworkElement> images)
    {
        var columns = images.Count == 4 ? 2 : images.Count;
        var rows = images.Count == 4 ? 2 : 1;

        for (var row = 0; row < rows; row++)
        {
            ImageHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        for (var column = 0; column < columns; column++)
        {
            ImageHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (var index = 0; index < images.Count; index++)
        {
            Grid.SetRow(images[index], index / columns);
            Grid.SetColumn(images[index], index % columns);
            ImageHost.Children.Add(images[index]);
        }
    }

    private void RenderOverlay(IReadOnlyList<FrameworkElement> images)
    {
        for (var index = 0; index < images.Count; index++)
        {
            images[index].HorizontalAlignment = HorizontalAlignment.Stretch;
            images[index].VerticalAlignment = VerticalAlignment.Stretch;
            images[index].Opacity = index == 0 ? 1 : GetOverlayTopOpacity();
            ImageHost.Children.Add(images[index]);
        }
    }

    private Border CreateSelectableImageFrame(Image image, int visibleIndex, int activeImageIndex)
    {
        var border = new Border
        {
            Tag = visibleIndex,
            Padding = new Thickness(6),
            BorderThickness = visibleIndex == activeImageIndex ? new Thickness(3) : new Thickness(1),
            BorderBrush = new SolidColorBrush(visibleIndex == activeImageIndex ? Colors.White : Colors.Transparent),
            Child = image
        };

        border.Tapped += (_, _) => _viewModel?.SetActiveComparisonImage(visibleIndex);
        border.PointerPressed += OnImagePointerPressed;
        border.PointerMoved += OnImagePointerMoved;
        border.PointerReleased += OnImagePointerReleased;
        return border;
    }

    private CompositeTransform CreateImageTransform(int visibleIndex)
    {
        var viewport = GetViewport(visibleIndex);
        return new CompositeTransform
        {
            ScaleX = viewport.ZoomLevel / 100d,
            ScaleY = viewport.ZoomLevel / 100d,
            TranslateX = viewport.PanOffset.X,
            TranslateY = viewport.PanOffset.Y
        };
    }

    private ComparisonImageViewport GetViewport(int visibleIndex)
    {
        if (_viewModel is null ||
            visibleIndex < 0 ||
            visibleIndex >= _viewModel.ComparisonState.ImageViewports.Count)
        {
            return ComparisonImageViewport.Default;
        }

        return _viewModel.ComparisonState.ImageViewports[visibleIndex];
    }

    private void UpdateRenderedTransforms()
    {
        foreach (var child in ImageHost.Children.OfType<Border>())
        {
            if (child.Tag is not int visibleIndex || child.Child is not Image image)
            {
                continue;
            }

            image.RenderTransform = CreateImageTransform(visibleIndex);
        }
    }

    private void UpdateOverlayControls()
    {
        if (_viewModel is null)
        {
            return;
        }

        var isOverlayActive = _viewModel.ComparisonState.IsActive &&
                              _viewModel.ComparisonState.Layout == ComparisonLayout.Overlay;
        OverlayControls.Visibility = isOverlayActive ? Visibility.Visible : Visibility.Collapsed;

        _isUpdatingSlider = true;
        OverlayOpacitySlider.Value = _viewModel.ComparisonState.OverlayOpacity;
        _isUpdatingSlider = false;
    }

    private void ApplyOverlayOpacity()
    {
        if (_viewModel is null || _viewModel.ComparisonState.Layout != ComparisonLayout.Overlay)
        {
            return;
        }

        for (var index = 0; index < ImageHost.Children.Count; index++)
        {
            if (ImageHost.Children[index] is FrameworkElement element)
            {
                element.Opacity = index == 0 ? 1 : GetOverlayTopOpacity();
            }
        }
    }

    private double GetOverlayTopOpacity()
    {
        return (_viewModel?.ComparisonState.OverlayOpacity ?? 50) / 100d;
    }

    private void OnOverlayOpacitySliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingSlider)
        {
            return;
        }

        _viewModel?.SetOverlayOpacity(e.NewValue);
    }

    private void OnImagePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border { Tag: int visibleIndex })
        {
            return;
        }

        var viewport = GetViewport(visibleIndex);
        if (viewport.ZoomLevel <= 100)
        {
            return;
        }

        _isPanning = true;
        _panningImageIndex = visibleIndex;
        _lastPointerPosition = e.GetCurrentPoint(this).Position;
        ((Border)sender).CapturePointer(e.Pointer);
    }

    private void OnImagePointerMoved(object sender, PointerRoutedEventArgs e)
    {
        _viewModel?.ShowChrome();

        if (!_isPanning || _panningImageIndex < 0)
        {
            return;
        }

        var position = e.GetCurrentPoint(this).Position;
        var deltaX = position.X - _lastPointerPosition.X;
        var deltaY = position.Y - _lastPointerPosition.Y;
        var viewport = GetViewport(_panningImageIndex);
        _viewModel?.SetComparisonPan(
            _panningImageIndex,
            new ImagePoint(viewport.PanOffset.X + deltaX, viewport.PanOffset.Y + deltaY));
        _lastPointerPosition = position;
    }

    private void OnImagePointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isPanning = false;
        _panningImageIndex = -1;

        if (sender is Border border)
        {
            border.ReleasePointerCapture(e.Pointer);
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

    private sealed record LoadedComparisonImage(int VisibleIndex, BitmapImage? Bitmap);
}
