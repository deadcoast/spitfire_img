using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class ViewerStateTests
{
    [Fact]
    public void SetImage_UpdatesCurrentImageMetadataAndResetsPan()
    {
        var state = new ViewerState { PanOffset = new PanOffset(10, 20) };
        var image = new DecodedImage(@"C:\Photos\image.png", new ImageMetadata(320, 240, 1024));

        state.SetImage(image);

        Assert.Equal(image, state.CurrentImage);
        Assert.Equal(image.Metadata, state.Metadata);
        Assert.Equal(new PanOffset(0, 0), state.PanOffset);
        Assert.Equal(DisplayMode.FitToWindow, state.DisplayMode);
    }

    [Fact]
    public void SetZoom_ChangesDisplayModeToCustom()
    {
        var state = new ViewerState();

        state.SetZoom(150);

        Assert.Equal(150, state.ZoomLevel);
        Assert.Equal(DisplayMode.Custom, state.DisplayMode);
    }

    [Fact]
    public void SetDisplayMode_ActualSizeSetsZoomToOneHundred()
    {
        var state = new ViewerState();

        state.SetDisplayMode(DisplayMode.ActualSize);

        Assert.Equal(DisplayMode.ActualSize, state.DisplayMode);
        Assert.Equal(100, state.ZoomLevel);
    }
}
