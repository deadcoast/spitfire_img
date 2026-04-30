using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class LayoutStateTests
{
    [Fact]
    public void EnterLayout_StoresGridConfigAndVisibleImages()
    {
        var state = new LayoutState();
        var layout = new LayoutSet(
            LayoutMode.Grid,
            LayoutConfig.Default,
            [
                new LayoutImage(0, new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 80, 1))),
                new LayoutImage(1, new DecodedImage(@"C:\Photos\b.png", new ImageMetadata(80, 100, 1)))
            ]);

        state.EnterLayout(layout);

        Assert.True(state.IsActive);
        Assert.Equal(LayoutMode.Grid, state.Mode);
        Assert.Equal(3, state.Config.Rows);
        Assert.Equal(0, state.WindowStartIndex);
        Assert.Equal(0, state.ActiveImageIndex);
        Assert.Equal([0, 1], state.VisibleImages.Select(image => image.PlaylistIndex).ToArray());
    }

    [Fact]
    public void SelectImage_UpdatesActiveVisibleImage()
    {
        var state = new LayoutState();
        state.EnterLayout(new LayoutSet(
            LayoutMode.Grid,
            LayoutConfig.Default,
            [
                new LayoutImage(4, new DecodedImage(@"C:\Photos\e.png", new ImageMetadata(100, 80, 1))),
                new LayoutImage(5, new DecodedImage(@"C:\Photos\f.png", new ImageMetadata(80, 100, 1)))
            ],
            WindowStartIndex: 4));

        state.SelectImage(1);

        Assert.Equal(1, state.ActiveImageIndex);
        Assert.Equal(5, state.ActiveImage?.PlaylistIndex);
    }

    [Fact]
    public void PositionText_WithGridLayout_DisplaysVisibleRangeDensityAndTotalCount()
    {
        var state = new LayoutState();
        state.EnterLayout(new LayoutSet(
            LayoutMode.Grid,
            LayoutConfig.Default,
            [
                new LayoutImage(3, new DecodedImage(@"C:\Photos\d.png", new ImageMetadata(100, 80, 1))),
                new LayoutImage(4, new DecodedImage(@"C:\Photos\e.png", new ImageMetadata(80, 100, 1))),
                new LayoutImage(5, new DecodedImage(@"C:\Photos\f.png", new ImageMetadata(90, 90, 1)))
            ],
            WindowStartIndex: 3,
            ActiveImageIndex: 0,
            TotalImageCount: 12));

        Assert.Equal("Grid 3x3 4-6 / 12", state.PositionText);
    }

    [Fact]
    public void PositionText_WithWrappedGridLayout_DisplaysSplitVisibleRanges()
    {
        var state = new LayoutState();
        state.EnterLayout(new LayoutSet(
            LayoutMode.Grid,
            LayoutConfig.Default,
            [
                new LayoutImage(8, new DecodedImage(@"C:\Photos\i.png", new ImageMetadata(100, 80, 1))),
                new LayoutImage(9, new DecodedImage(@"C:\Photos\j.png", new ImageMetadata(80, 100, 1))),
                new LayoutImage(0, new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(90, 90, 1))),
                new LayoutImage(1, new DecodedImage(@"C:\Photos\b.png", new ImageMetadata(90, 90, 1)))
            ],
            WindowStartIndex: 8,
            ActiveImageIndex: 0,
            TotalImageCount: 10));

        Assert.Equal("Grid 3x3 9-10, 1-2 / 10", state.PositionText);
    }

    [Fact]
    public void EnterLayout_WithFilmstripSet_UsesProvidedFocusImageIndex()
    {
        var state = new LayoutState();

        state.EnterLayout(new LayoutSet(
            LayoutMode.Filmstrip,
            LayoutConfig.Filmstrip,
            [
                new LayoutImage(3, new DecodedImage(@"C:\Photos\d.png", new ImageMetadata(100, 80, 1))),
                new LayoutImage(4, new DecodedImage(@"C:\Photos\e.png", new ImageMetadata(80, 100, 1))),
                new LayoutImage(5, new DecodedImage(@"C:\Photos\f.png", new ImageMetadata(90, 90, 1)))
            ],
            WindowStartIndex: 3,
            ActiveImageIndex: 1));

        Assert.True(state.IsActive);
        Assert.Equal(LayoutMode.Filmstrip, state.Mode);
        Assert.Equal(1, state.ActiveImageIndex);
        Assert.Equal(4, state.ActiveImage?.PlaylistIndex);
        Assert.Same(state.ActiveImage, state.FocusImage);
    }

    [Fact]
    public void ExitLayout_ClearsVisibleImagesAndReturnsToSingleImageMode()
    {
        var state = new LayoutState();
        state.EnterLayout(new LayoutSet(
            LayoutMode.Grid,
            LayoutConfig.Default,
            [new LayoutImage(0, new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 80, 1)))],
            WindowStartIndex: 0));

        state.ExitLayout();

        Assert.False(state.IsActive);
        Assert.Equal(LayoutMode.SingleImage, state.Mode);
        Assert.Empty(state.VisibleImages);
    }
}
