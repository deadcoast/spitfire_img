namespace ImageViewer.Core.Models;

public sealed record ComparisonImageViewport(
    ImageDimensions Dimensions,
    double ZoomLevel,
    ImagePoint PanOffset,
    DisplayMode DisplayMode = DisplayMode.FitToWindow)
{
    public static ComparisonImageViewport Default { get; } = new(
        ImageDimensions.Unknown,
        ZoomLevel: 100,
        new ImagePoint(0, 0));

    public ComparisonImageViewport WithDimensions(ImageDimensions dimensions)
    {
        return this with { Dimensions = dimensions.Normalize() };
    }

    public ComparisonImageViewport WithZoom(double zoomLevel)
    {
        return this with
        {
            ZoomLevel = Math.Max(10, zoomLevel),
            DisplayMode = DisplayMode.Custom
        };
    }

    public ComparisonImageViewport WithPan(ImagePoint panOffset)
    {
        return this with
        {
            PanOffset = panOffset,
            DisplayMode = DisplayMode.Custom
        };
    }

    public ComparisonImageViewport FitToWindow()
    {
        return this with
        {
            ZoomLevel = 100,
            PanOffset = new ImagePoint(0, 0),
            DisplayMode = DisplayMode.FitToWindow
        };
    }
}
