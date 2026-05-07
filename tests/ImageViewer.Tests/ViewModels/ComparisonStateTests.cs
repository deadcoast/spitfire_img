using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class ComparisonStateTests
{
    [Fact]
    public void EnterComparison_ActivatesModeWithVisibleItemsAndIndices()
    {
        var state = new ComparisonState();
        var items = new[]
        {
            new ImageItem(@"C:\Photos\a.png"),
            new ImageItem(@"C:\Photos\b.png")
        };

        state.EnterComparison(ComparisonLayout.SideBySide, items, [0, 1]);

        Assert.True(state.IsActive);
        Assert.Equal(ComparisonLayout.SideBySide, state.Layout);
        Assert.Equal(2, state.ImageCount);
        Assert.Equal(items, state.DisplayedItems);
        Assert.Equal([0, 1], state.DisplayedIndices);
        Assert.Equal("1, 2", state.FormattedDisplayedIndices);
    }

    [Fact]
    public void ExitComparison_ReturnsToSingleImageMode()
    {
        var state = new ComparisonState();
        state.EnterComparison(
            ComparisonLayout.Overlay,
            [new ImageItem(@"C:\Photos\a.png"), new ImageItem(@"C:\Photos\b.png")],
            [0, 1]);

        state.ExitComparison();

        Assert.False(state.IsActive);
        Assert.Empty(state.DisplayedItems);
        Assert.Empty(state.DisplayedIndices);
    }

    [Fact]
    public void EnterComparison_OverlayUsesDefaultTopLayerOpacity()
    {
        var state = new ComparisonState();

        state.EnterComparison(
            ComparisonLayout.Overlay,
            [new ImageItem(@"C:\Photos\a.png"), new ImageItem(@"C:\Photos\b.png")],
            [0, 1]);

        Assert.Equal(50, state.OverlayOpacity);
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(35, 35)]
    [InlineData(120, 100)]
    public void SetOverlayOpacity_ClampsToSupportedRange(double opacity, double expected)
    {
        var state = new ComparisonState();

        state.SetOverlayOpacity(opacity);

        Assert.Equal(expected, state.OverlayOpacity);
    }

    [Fact]
    public void AdjustOverlayOpacity_ChangesOpacityByTenPercentSteps()
    {
        var state = new ComparisonState();

        state.AdjustOverlayOpacity(10);
        state.AdjustOverlayOpacity(-20);

        Assert.Equal(40, state.OverlayOpacity);
    }

    [Fact]
    public void SwapLayers_ReversesOverlayLayerOrder()
    {
        var state = new ComparisonState();
        state.EnterComparison(
            ComparisonLayout.Overlay,
            [new ImageItem(@"C:\Photos\a.png"), new ImageItem(@"C:\Photos\b.png")],
            [0, 1]);

        state.SwapLayers();

        Assert.Equal([1, 0], state.DisplayedIndices);
        Assert.Equal(@"C:\Photos\b.png", state.DisplayedItems[0].FilePath);
        Assert.Equal(@"C:\Photos\a.png", state.DisplayedItems[1].FilePath);
    }

    [Fact]
    public void ToggleSyncZoom_ChangesSyncZoomState()
    {
        var state = new ComparisonState();

        state.ToggleSyncZoom();

        Assert.True(state.SyncZoomEnabled);
    }

    [Fact]
    public void ApplyViewportUpdate_StoresPerImageZoomAndPan()
    {
        var state = new ComparisonState();
        state.EnterComparison(
            ComparisonLayout.SideBySide,
            [new ImageItem(@"C:\Photos\a.png"), new ImageItem(@"C:\Photos\b.png")],
            [0, 1]);

        state.ApplyViewportUpdate(
            1,
            new ComparisonImageViewport(new ImageDimensions(200, 100), 150, new ImagePoint(40, 20)));

        Assert.Equal(150, state.ImageViewports[1].ZoomLevel);
        Assert.Equal(new ImagePoint(40, 20), state.ImageViewports[1].PanOffset);
        Assert.Equal(DisplayMode.Custom, state.ImageViewports[1].DisplayMode);
    }

    [Fact]
    public void SetActiveImage_HighlightsRequestedVisibleImage()
    {
        var state = new ComparisonState();
        state.EnterComparison(
            ComparisonLayout.SideBySide,
            [
                new ImageItem(@"C:\Photos\a.png"),
                new ImageItem(@"C:\Photos\b.png"),
                new ImageItem(@"C:\Photos\c.png")
            ],
            [0, 1, 2]);

        state.SetActiveImage(2);

        Assert.Equal(2, state.ActiveImageIndex);
        Assert.Equal(2, state.ActivePlaylistIndex);
    }

    [Fact]
    public void CycleActiveImage_MovesThroughVisibleImagesAndWraps()
    {
        var state = new ComparisonState();
        state.EnterComparison(
            ComparisonLayout.SideBySide,
            [
                new ImageItem(@"C:\Photos\a.png"),
                new ImageItem(@"C:\Photos\b.png"),
                new ImageItem(@"C:\Photos\c.png")
            ],
            [0, 1, 2]);

        state.CycleActiveImage();
        state.CycleActiveImage();
        state.CycleActiveImage();

        Assert.Equal(0, state.ActiveImageIndex);
        Assert.Equal(0, state.ActivePlaylistIndex);
    }
}
