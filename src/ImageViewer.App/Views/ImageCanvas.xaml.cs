using System.ComponentModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ImageViewer.App.Views;

public sealed partial class ImageCanvas : UserControl
{
    private ViewerState? _viewerState;
    private MainViewModel? _viewModel;
    private bool _isPanning;
    private Windows.Foundation.Point _lastPointerPosition;

    public ImageCanvas()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        DoubleTapped += OnDoubleTapped;
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewerState = viewModel.ViewerState;
        _viewerState.PropertyChanged += OnViewerStateChanged;
        viewModel.TransformState.PropertyChanged += OnTransformStateChanged;
        UpdateImage();
        UpdateDisplayMode();
    }

    private void OnViewerStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewerState.CurrentImage))
        {
            DispatcherQueue.TryEnqueue(UpdateImage);
        }

        if (e.PropertyName is nameof(ViewerState.DisplayMode) or nameof(ViewerState.ZoomLevel) or nameof(ViewerState.PanOffset))
        {
            DispatcherQueue.TryEnqueue(UpdateDisplayMode);
        }
    }

    private void OnTransformStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateDisplayMode);
    }

    private void UpdateImage()
    {
        var image = _viewerState?.CurrentImage;
        EmptyStateText.Visibility = image is null ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        if (image is null)
        {
            DisplayedImage.Source = null;
            return;
        }

        DisplayedImage.Source = new BitmapImage(new Uri(image.FilePath));
    }

    private void UpdateDisplayMode()
    {
        if (_viewerState is null)
        {
            return;
        }

        DisplayedImage.Stretch = _viewerState.DisplayMode == DisplayMode.FitToWindow
            ? Microsoft.UI.Xaml.Media.Stretch.Uniform
            : Microsoft.UI.Xaml.Media.Stretch.None;

        var scale = _viewerState.DisplayMode == DisplayMode.Custom
            ? _viewerState.ZoomLevel / 100d
            : 1d;

        var transformPreview = CalculateTransformPreview();
        ImageTransform.ScaleX = scale * transformPreview.ScaleX;
        ImageTransform.ScaleY = scale * transformPreview.ScaleY;
        ImageTransform.Rotation = transformPreview.RotationDegrees;
        ImageTransform.TranslateX = _viewerState.PanOffset.X;
        ImageTransform.TranslateY = _viewerState.PanOffset.Y;
    }

    private (double RotationDegrees, double ScaleX, double ScaleY) CalculateTransformPreview()
    {
        var rotation = 0d;
        var scaleX = 1d;
        var scaleY = 1d;

        if (_viewModel is null)
        {
            return (rotation, scaleX, scaleY);
        }

        foreach (var operation in _viewModel.TransformState.PendingTransforms)
        {
            switch (operation)
            {
                case TransformOperation.RotateLeft:
                    rotation -= 90;
                    break;
                case TransformOperation.RotateRight:
                    rotation += 90;
                    break;
                case TransformOperation.FlipHorizontal:
                    scaleX *= -1;
                    break;
                case TransformOperation.FlipVertical:
                    scaleY *= -1;
                    break;
            }
        }

        return (rotation % 360, scaleX, scaleY);
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_viewerState?.DisplayMode != DisplayMode.Custom || _viewerState.ZoomLevel <= 100)
        {
            return;
        }

        _isPanning = true;
        _lastPointerPosition = e.GetCurrentPoint(this).Position;
        CapturePointer(e.Pointer);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        _viewModel?.ShowChrome();

        if (!_isPanning || _viewerState is null)
        {
            return;
        }

        var position = e.GetCurrentPoint(this).Position;
        var deltaX = position.X - _lastPointerPosition.X;
        var deltaY = position.Y - _lastPointerPosition.Y;
        _viewerState.PanOffset = new PanOffset(
            _viewerState.PanOffset.X + deltaX,
            _viewerState.PanOffset.Y + deltaY);
        _lastPointerPosition = position;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isPanning = false;
        ReleasePointerCapture(e.Pointer);
    }

    private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        _viewModel?.ToggleFullscreen();
    }
}
