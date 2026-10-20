using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.App.Services.Motion;
using ImageViewer.App.Services;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace ImageViewer.App.Views;

public sealed partial class ImageCanvas : UserControl
{
    private ViewerState? _viewerState;
    private MainViewModel? _viewModel;
    private IViewerMotionService? _motionService;
    private bool _isPanning;
    private Windows.Foundation.Point _lastPointerPosition;
    private int _imageLoadVersion;
    private ImageViewportGeometry? _geometry;
    private readonly Dictionary<AnimationFrame, BitmapImage> _frameCache = [];
    private string? _loadedPath;
    private XamlRoot? _observedRoot;
    private readonly SlideshowTransitionRenderer _slideshowTransitions = new();
    private bool _isActive = true;

    public ImageCanvas()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCanceled += OnPointerReleased;
        PointerCaptureLost += (_, _) => _isPanning = false;
        PointerWheelChanged += OnPointerWheelChanged;
        DoubleTapped += OnDoubleTapped;
        SizeChanged += (_, _) => UpdateDisplayMode();
        Loaded += (_, _) =>
        {
            if (_observedRoot is not null) _observedRoot.Changed -= OnXamlRootChanged;
            _observedRoot = XamlRoot;
            _observedRoot.Changed += OnXamlRootChanged;
            UpdateDisplayMode();
        };
        Unloaded += (_, _) =>
        {
            _slideshowTransitions.Cancel();
            if (_observedRoot is not null) _observedRoot.Changed -= OnXamlRootChanged;
            _observedRoot = null;
        };
    }

    public event EventHandler? OpenFileRequested;

    public void SetActive(bool isActive)
    {
        if (_isActive == isActive) return;
        _isActive = isActive;
        _imageLoadVersion++;
        _slideshowTransitions.Cancel();
        _frameCache.Clear();
        if (isActive) UpdateImage();
        else
        {
            DisplayedImage.Source = null;
            OutgoingImage.Source = null;
            EmptyStateSurface.Visibility = Visibility.Collapsed;
        }
    }

    private void OnOpenImageClicked(object sender, RoutedEventArgs e) => OpenFileRequested?.Invoke(this, EventArgs.Empty);

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateDisplayMode();

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewerState = viewModel.ViewerState;
        _viewerState.PropertyChanged += OnViewerStateChanged;
        viewModel.AnimationState.PropertyChanged += OnAnimationStateChanged;
        viewModel.TransformState.PropertyChanged += OnTransformStateChanged;
        UpdateImage();
        UpdateDisplayMode();
    }

    public void AttachMotionService(IViewerMotionService motionService)
    {
        _motionService = motionService;
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

    private void OnAnimationStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AnimationState.CurrentFrame)
            or nameof(AnimationState.IsAnimated)
            or nameof(AnimationState.TotalFrames))
        {
            DispatcherQueue.TryEnqueue(UpdateImage);
        }
    }

    private void UpdateImage()
    {
        if (!_isActive) return;
        var image = _viewerState?.CurrentImage;
        var loadVersion = ++_imageLoadVersion;
        EmptyStateSurface.Visibility = image is null ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        if (image is null)
        {
            DisplayedImage.Source = null;
            DisplayedImage.Opacity = 0;
            return;
        }
        if (!string.Equals(_loadedPath, image.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            _frameCache.Clear();
            _loadedPath = image.FilePath;
        }
        UpdateDisplayMode();

        if (_viewModel?.AnimationState.IsAnimated == true &&
            string.Equals(_viewModel.AnimationState.FilePath, image.FilePath, StringComparison.OrdinalIgnoreCase) &&
            _viewModel.AnimationState.CurrentFrame < _viewModel.AnimationState.Frames.Count)
        {
            _ = LoadImageSourceAsync(_viewModel.AnimationState.Frames[_viewModel.AnimationState.CurrentFrame], loadVersion);
            return;
        }

        _ = LoadImageSourceAsync(image.FilePath, loadVersion);
    }

    private async Task LoadImageSourceAsync(string filePath, int loadVersion)
    {
        try
        {
            await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var randomAccessStream = fileStream.AsRandomAccessStream();
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(randomAccessStream);

            if (loadVersion == _imageLoadVersion)
            {
                await PresentBitmapAsync(bitmap, loadVersion);
            }
        }
        catch (Exception ex)
        {
            if (loadVersion == _imageLoadVersion)
            {
                DisplayedImage.Source = null;
                DisplayedImage.Opacity = 0;
                await _viewModel!.ReportPresentationErrorAsync(filePath, ex);
            }
        }
    }

    private async Task PresentBitmapAsync(BitmapImage bitmap, int loadVersion)
    {
        _slideshowTransitions.Cancel();
        var previous = DisplayedImage.Source as BitmapImage;
        if (previous is not null && _viewModel?.SlideshowState.IsActive == true &&
            _viewModel.LayoutState.CurrentLayout == LayoutMode.Single && !_viewModel.ComparisonState.IsActive)
        {
            var oldGeometry = ImageViewportGeometry.Calculate(
                new ImageMetadata(previous.PixelWidth, previous.PixelHeight, 0),
                ActualWidth, ActualHeight, XamlRoot?.RasterizationScale ?? 1,
                DisplayMode.FitToWindow, 100, 0, new PanOffset(0, 0));
            OutgoingImage.Source = previous;
            OutgoingImage.Width = oldGeometry.NaturalWidth;
            OutgoingImage.Height = oldGeometry.NaturalHeight;
            Canvas.SetLeft(OutgoingImage, (ActualWidth - oldGeometry.NaturalWidth) / 2);
            Canvas.SetTop(OutgoingImage, (ActualHeight - oldGeometry.NaturalHeight) / 2);
            OutgoingImageTransform.ScaleX = OutgoingImageTransform.ScaleY = oldGeometry.Scale;
            OutgoingHost.Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, ActualWidth, ActualHeight) };
            DisplayedImage.Source = bitmap;
            UpdateDisplayMode();
            ViewerMotionStoryboard.SetOpacity(DisplayedImage, 1);
            var effect = _motionService?.IsReducedMotionEnabled == true ? TransitionEffect.None : _viewModel.SlideshowState.Transition;
            var plan = new SlideshowService().CreateTransitionPlan(effect).Value;
            await _slideshowTransitions.ApplyTransitionAsync(OutgoingHost, ImageHost,
                OutgoingTransitionTransform, IncomingTransitionTransform, plan);
            if (loadVersion == _imageLoadVersion) OutgoingImage.Source = null;
        }
        else
        {
            OutgoingImage.Source = null;
            DisplayedImage.Source = bitmap;
            UpdateDisplayMode();
            AnimateImageNavigation();
        }
    }

    private static async Task<BitmapImage> CreateBitmapImageAsync(byte[] encodedBytes)
    {
        using var randomAccessStream = new InMemoryRandomAccessStream();
        await randomAccessStream.WriteAsync(encodedBytes.AsBuffer());
        randomAccessStream.Seek(0);

        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(randomAccessStream);
        return bitmap;
    }

    private async Task LoadImageSourceAsync(AnimationFrame frame, int loadVersion)
    {
        try
        {
            if (!_frameCache.TryGetValue(frame, out var bitmap))
            {
                bitmap = await CreateBitmapImageAsync(frame.EncodedBytes);
                // Bound decoded-frame memory; the service already owns encoded frames.
                if (_frameCache.Count >= 8) _frameCache.Clear();
                if (loadVersion == _imageLoadVersion) _frameCache[frame] = bitmap;
            }
            if (loadVersion == _imageLoadVersion)
            {
                DisplayedImage.Source = bitmap;
                _slideshowTransitions.Cancel();
                OutgoingImage.Source = null;
                ViewerMotionStoryboard.SetOpacity(DisplayedImage, 1);
            }
        }
        catch (Exception ex)
        {
            if (loadVersion == _imageLoadVersion)
            {
                DisplayedImage.Source = null;
                DisplayedImage.Opacity = 0;
                if (_loadedPath is not null) await _viewModel!.ReportPresentationErrorAsync(_loadedPath, ex);
            }
        }
    }

    private void UpdateDisplayMode()
    {
        if (!_isActive || _viewerState?.CurrentImage is not { } image || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var transformPreview = CalculateTransformPreview();
        _geometry = ImageViewportGeometry.Calculate(image.Metadata, ActualWidth, ActualHeight,
            XamlRoot?.RasterizationScale ?? 1, _viewerState.DisplayMode, _viewerState.ZoomLevel,
            transformPreview.RotationDegrees, _viewerState.PanOffset);
        _viewerState.FitZoomPercent = _geometry.FitZoomPercent;
        DisplayedImage.Width = _geometry.NaturalWidth;
        DisplayedImage.Height = _geometry.NaturalHeight;
        Canvas.SetLeft(DisplayedImage, (ActualWidth - _geometry.NaturalWidth) / 2);
        Canvas.SetTop(DisplayedImage, (ActualHeight - _geometry.NaturalHeight) / 2);
        ImageHost.Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, ActualWidth, ActualHeight) };
        CanvasRoot.Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, ActualWidth, ActualHeight) };
        ImageTransform.ScaleX = _geometry.Scale * transformPreview.ScaleX;
        ImageTransform.ScaleY = _geometry.Scale * transformPreview.ScaleY;
        ImageTransform.Rotation = transformPreview.RotationDegrees;
        ImageTransform.TranslateX = _geometry.Pan.X;
        ImageTransform.TranslateY = _geometry.Pan.Y;
        if (_viewerState.PanOffset != _geometry.Pan) _viewerState.PanOffset = _geometry.Pan;
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
                    rotation = -rotation;
                    scaleX *= -1;
                    break;
                case TransformOperation.FlipVertical:
                    rotation = -rotation;
                    scaleY *= -1;
                    break;
            }
        }

        return (rotation % 360, scaleX, scaleY);
    }

    private void AnimateImageNavigation()
    {
        var motionPlan = GetMotionPlan(ViewerMotionCategory.ImageNavigation);
        // A fast browse never blanks the previous image while decoding its successor.
        ViewerMotionStoryboard.AnimateOpacity(DisplayedImage, motionPlan, 1);
    }

    private ViewerMotionPlan GetMotionPlan(ViewerMotionCategory category)
    {
        return _motionService?.CreatePlan(category) ?? ViewerMotionService.CreateDefaultPlan(category, isReducedMotion: false);
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (IsControlSource(e.OriginalSource) || _geometry?.CanPan != true ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isPanning = true;
        _lastPointerPosition = e.GetCurrentPoint(this).Position;
        CapturePointer(e.Pointer);
        Focus(FocusState.Programmatic);
        e.Handled = true;
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
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isPanning = false;
        ReleasePointerCapture(e.Pointer);
    }

    private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (IsControlSource(e.OriginalSource)) return;
        _viewModel?.ToggleFullscreen();
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (_viewerState?.CurrentImage is null || _geometry is null || IsControlSource(e.OriginalSource)) return;
        var point = e.GetCurrentPoint(this);
        if (point.Properties.MouseWheelDelta == 0) return;
        var oldScale = _geometry.Scale;
        var nextZoom = Math.Clamp(oldScale * 100 * (point.Properties.MouseWheelDelta > 0 ? 1.25 : .8), 10, 3200);
        var ratio = nextZoom / (oldScale * 100);
        var anchorX = point.Position.X - ActualWidth / 2;
        var anchorY = point.Position.Y - ActualHeight / 2;
        var pan = new PanOffset(anchorX - (anchorX - _geometry.Pan.X) * ratio,
            anchorY - (anchorY - _geometry.Pan.Y) * ratio);
        _viewerState.SetZoom(nextZoom);
        _viewerState.PanOffset = pan;
        UpdateDisplayMode();
        e.Handled = true;
    }

    private static bool IsControlSource(object source)
    {
        var current = source as DependencyObject;
        while (current is not null && current is not ImageCanvas)
        {
            if (current is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase) return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }
}
