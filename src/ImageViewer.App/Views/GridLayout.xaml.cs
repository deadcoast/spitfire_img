using System.ComponentModel;
using ImageViewer.App.Services.Motion;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Input;
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
    private IViewerMotionService? _motionService;
    private int _loadVersion;
    private bool _isGridVisible;

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

    public void AttachMotionService(IViewerMotionService motionService)
    {
        _motionService = motionService;
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

        if (!_viewModel.LayoutState.IsGridActive)
        {
            HideGrid();
            return;
        }

        ShowGrid();
        _ = LoadImagesAsync(++_loadVersion);
    }

    private void ShowGrid()
    {
        if (_isGridVisible)
        {
            return;
        }

        _isGridVisible = true;
        LayoutRoot.Visibility = Visibility.Visible;
        ViewerMotionStoryboard.AnimateOverlay(
            LayoutRoot,
            GridLayoutTransform,
            GetMotionPlan(ViewerMotionCategory.LayoutModeChange),
            isEntering: true);
    }

    private void HideGrid()
    {
        if (!_isGridVisible && LayoutRoot.Visibility == Visibility.Collapsed)
        {
            ImageHost.Children.Clear();
            return;
        }

        _isGridVisible = false;
        ViewerMotionStoryboard.AnimateOverlay(
            LayoutRoot,
            GridLayoutTransform,
            GetMotionPlan(ViewerMotionCategory.LayoutModeChange),
            isEntering: false,
            completed: () =>
            {
                LayoutRoot.Visibility = Visibility.Collapsed;
                ImageHost.Children.Clear();
            });
    }

    private async Task LoadImagesAsync(int loadVersion)
    {
        if (_viewModel is null)
        {
            return;
        }

        var gridImages = _viewModel.LayoutState.GridImages.ToArray();
        ImageHost.Children.Clear();
        UpdateGridHud(gridImages.Length);

        var frames = gridImages
            .Select((gridImage, visibleIndex) => CreateGridFrame(gridImage, visibleIndex))
            .ToList();

        foreach (var frame in frames)
        {
            ImageHost.Children.Add(frame.Root);
        }

        var frameTasks = frames
            .Select(frame => LoadGridFrameAsync(frame, loadVersion))
            .ToList();

        while (frameTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(frameTasks).ConfigureAwait(true);
            frameTasks.Remove(completedTask);
            await completedTask.ConfigureAwait(true);
            if (loadVersion != _loadVersion)
            {
                return;
            }
        }
    }

    private GridFrameElements CreateGridFrame(GridImage gridImage, int visibleIndex)
    {
        var isActive = visibleIndex == _viewModel?.LayoutState.ActiveGridImageIndex;
        var image = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        var placeholder = new TextBlock
        {
            Text = "Loading",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Style = FindResource<Style>("ViewerTextCaptionStyle")
        };
        var indexText = new TextBlock
        {
            Text = $"{gridImage.PlaylistIndex + 1}",
            Style = FindResource<Style>("ViewerTextNumericStyle")
        };
        var activeText = new TextBlock
        {
            Text = isActive ? "Active" : string.Empty,
            HorizontalAlignment = HorizontalAlignment.Right,
            Style = FindResource<Style>("ViewerTextStatusStyle")
        };
        var imageLayer = new Grid();
        imageLayer.Children.Add(image);
        imageLayer.Children.Add(placeholder);

        var footer = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Style = FindResource<Style>("ViewerGridImageFooterBorderStyle"),
            Child = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children =
                {
                    indexText,
                    activeText
                }
            }
        };
        Grid.SetColumn(activeText, 1);

        var content = new Grid();
        content.Children.Add(imageLayer);
        content.Children.Add(footer);

        var frame = new Border
        {
            Tag = visibleIndex,
            Width = gridImage.Cell.Width,
            Height = gridImage.Cell.Height,
            Padding = (Thickness)Application.Current.Resources["ViewerGridImageFramePadding"],
            Style = FindResource<Style>(isActive
                ? "ViewerGridImageActiveFrameBorderStyle"
                : "ViewerGridImageFrameBorderStyle"),
            Child = content
        };
        frame.PointerEntered += (_, _) => ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        frame.PointerExited += (_, _) => ProtectedCursor = null;
        frame.Tapped += OnGridImageTapped;

        Canvas.SetLeft(frame, gridImage.Cell.X);
        Canvas.SetTop(frame, gridImage.Cell.Y);
        return new GridFrameElements(frame, image, placeholder, gridImage.Item.FilePath);
    }

    private async Task LoadGridFrameAsync(GridFrameElements frame, int loadVersion)
    {
        var bitmap = await TryLoadBitmapAsync(frame.FilePath).ConfigureAwait(true);
        if (loadVersion != _loadVersion)
        {
            return;
        }

        frame.Image.Source = bitmap;
        frame.Placeholder.Text = bitmap is null ? "No preview" : string.Empty;
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

    private void UpdateGridHud(int visibleCount)
    {
        if (_viewModel is null)
        {
            return;
        }

        var layout = _viewModel.LayoutState.GridLayout;
        var start = _viewModel.LayoutState.GridStartIndex;
        var total = _viewModel.PlaylistState.TotalCount;
        var end = visibleCount == 0 ? start : Math.Min(start + visibleCount, total);
        GridDensityText.Text = $"{_viewModel.LayoutState.GridRows} x {_viewModel.LayoutState.GridColumns}";
        GridPageText.Text = total == 0
            ? "No images in this grid"
            : $"Showing {start + 1}-{end} of {total}";
        GridStatusText.Text = _viewModel.LayoutState.ActiveGridPlaylistIndex >= 0
            ? $"Active image {_viewModel.LayoutState.ActiveGridPlaylistIndex + 1}; {layout.TotalCells} cells"
            : $"{layout.TotalCells} cells";
    }

    private T? FindResource<T>(string key)
        where T : class
    {
        if (Resources.TryGetValue(key, out var localValue) && localValue is T localResource)
        {
            return localResource;
        }

        if (Application.Current.Resources.TryGetValue(key, out var appValue) && appValue is T appResource)
        {
            return appResource;
        }

        return null;
    }

    private ViewerMotionPlan GetMotionPlan(ViewerMotionCategory category)
    {
        return _motionService?.CreatePlan(category) ?? ViewerMotionService.CreateDefaultPlan(category, isReducedMotion: false);
    }

    private sealed record GridFrameElements(Border Root, Image Image, TextBlock Placeholder, string FilePath);
}
