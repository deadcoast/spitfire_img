using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed record PanOffset(double X, double Y);

public sealed class ViewerState : ObservableObject
{
    private DecodedImage? _currentImage;
    private ImageMetadata? _metadata;
    private double _zoomLevel = 100;
    private DisplayMode _displayMode = DisplayMode.FitToWindow;
    private PanOffset _panOffset = new(0, 0);
    private bool _isFullscreen;
    public double FitZoomPercent { get; set; } = 100;
    public double EffectiveZoomPercent => DisplayMode == DisplayMode.FitToWindow ? FitZoomPercent : ZoomLevel;

    public DecodedImage? CurrentImage
    {
        get => _currentImage;
        private set
        {
            // A reload can have equal metadata while the pixels have changed.
            _currentImage = value;
            OnPropertyChanged();
        }
    }

    public ImageMetadata? Metadata
    {
        get => _metadata;
        private set => SetProperty(ref _metadata, value);
    }

    public double ZoomLevel
    {
        get => _zoomLevel;
        private set => SetProperty(ref _zoomLevel, value);
    }

    public DisplayMode DisplayMode
    {
        get => _displayMode;
        private set => SetProperty(ref _displayMode, value);
    }

    public PanOffset PanOffset
    {
        get => _panOffset;
        set => SetProperty(ref _panOffset, value);
    }

    public bool IsFullscreen
    {
        get => _isFullscreen;
        set => SetProperty(ref _isFullscreen, value);
    }

    public void SetImage(DecodedImage image)
    {
        CurrentImage = image;
        Metadata = image.Metadata;
        SetDisplayMode(DisplayMode.FitToWindow);
        ResetPan();
    }

    public void SetMetadata(ImageMetadata metadata)
    {
        Metadata = metadata;
    }

    public void SetZoom(double zoomLevel)
    {
        ZoomLevel = double.IsFinite(zoomLevel) ? Math.Clamp(zoomLevel, 10, 3200) : 100;
        DisplayMode = DisplayMode.Custom;
    }

    public void SetDisplayMode(DisplayMode displayMode)
    {
        DisplayMode = displayMode;
        if (displayMode == DisplayMode.ActualSize)
        {
            ZoomLevel = 100;
        }
        else if (displayMode == DisplayMode.FitToWindow)
        {
            ZoomLevel = 100;
        }
    }

    public void ResetPan()
    {
        PanOffset = new PanOffset(0, 0);
    }
}
