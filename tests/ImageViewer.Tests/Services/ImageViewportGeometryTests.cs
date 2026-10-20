using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.Services;

public sealed class ImageViewportGeometryTests
{
    [Fact]
    public void ActualSize_OnHighDpi_UsesOneScreenPixelPerImagePixel()
    {
        var geometry = Calculate(4000, 3000, 1000, 700, 1.5, DisplayMode.ActualSize);
        Assert.Equal(4000, geometry.NaturalWidth * geometry.Scale * 1.5, 6);
        Assert.True(geometry.CanPan);
    }

    [Fact]
    public void Fit_RotatedLandscape_UsesRotatedBoundsAndCentersImage()
    {
        var geometry = Calculate(4000, 2000, 1000, 800, 1, DisplayMode.FitToWindow, rotation: 90);
        Assert.Equal(.2, geometry.Scale, 6);
        Assert.Equal(new PanOffset(0, 0), geometry.Pan);
        Assert.False(geometry.CanPan);
    }

    [Fact]
    public void Pan_AtActualSize_ClampsEdgesAndCentersNonOverflowingAxis()
    {
        var geometry = Calculate(2000, 400, 1000, 800, 1, DisplayMode.ActualSize);
        Assert.Equal(new PanOffset(500, 0), geometry.Pan);
    }

    [Fact]
    public void ZoomingFromFit_UsesDisplayedScaleInsteadOfAssumingOneHundredPercent()
    {
        var state = new ViewerState { FitZoomPercent = 25 };
        state.SetZoom(state.EffectiveZoomPercent + 25);
        Assert.Equal(50, state.ZoomLevel);
    }

    private static ImageViewportGeometry Calculate(int width, int height, double viewportWidth, double viewportHeight,
        double dpi, DisplayMode mode, double rotation = 0) =>
        ImageViewportGeometry.Calculate(new ImageMetadata(width, height, 1024), viewportWidth, viewportHeight, dpi,
            mode, 100, rotation, new PanOffset(100000, -100000));
}
