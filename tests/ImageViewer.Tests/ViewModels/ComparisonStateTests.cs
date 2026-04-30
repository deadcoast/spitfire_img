using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class ComparisonStateTests
{
    [Fact]
    public void EnterAndExitComparison_UpdatesVisibleImagesAndState()
    {
        var state = new ComparisonState();
        var images = new[]
        {
            new ComparisonImage(0, new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 100, 10))),
            new ComparisonImage(1, new DecodedImage(@"C:\Photos\b.png", new ImageMetadata(100, 100, 10)))
        };

        state.EnterComparison(new ComparisonSet(ComparisonLayout.SideBySide, images));
        state.ExitComparison();

        Assert.False(state.IsActive);
        Assert.Empty(state.VisibleImages);
    }

    [Fact]
    public void OverlayOpacity_DefaultsToHalfAndClampsToRange()
    {
        var state = new ComparisonState();

        state.SetOverlayOpacity(1.4);
        var upper = state.OverlayOpacity;
        state.SetOverlayOpacity(-0.25);

        Assert.Equal(0.5, new ComparisonState().OverlayOpacity);
        Assert.Equal(1, upper);
        Assert.Equal(0, state.OverlayOpacity);
    }

    [Fact]
    public void SwapOverlayLayers_WithTwoVisibleImages_ReversesLayerOrder()
    {
        var state = new ComparisonState();
        var images = new[]
        {
            new ComparisonImage(0, new DecodedImage(@"C:\Photos\a.png", new ImageMetadata(100, 100, 10))),
            new ComparisonImage(1, new DecodedImage(@"C:\Photos\b.png", new ImageMetadata(100, 100, 10)))
        };
        state.EnterComparison(new ComparisonSet(ComparisonLayout.Overlay, images));

        state.SwapOverlayLayers();

        Assert.Equal([1, 0], state.VisibleImages.Select(image => image.PlaylistIndex).ToArray());
        Assert.Equal(0, state.ActiveImageIndex);
    }

    [Fact]
    public void SetSynchronizedZoom_WhenSyncEnabled_AppliesZoomToAllVisibleImages()
    {
        var state = CreateEnteredState(imageCount: 3);

        state.SetSynchronizedZoom(175);

        Assert.True(state.IsSyncZoomEnabled);
        Assert.All(state.Viewports, viewport => Assert.Equal(175, viewport.ZoomLevel));
    }

    [Fact]
    public void SetSynchronizedRelativePan_WhenSyncEnabled_AppliesRelativePanToAllVisibleImages()
    {
        var state = CreateEnteredState(imageCount: 3);
        var pan = new RelativePanOffset(0.25, 0.75);

        state.SetSynchronizedRelativePan(pan);

        Assert.All(state.Viewports, viewport => Assert.Equal(pan, viewport.RelativePan));
    }

    [Fact]
    public void ToggleSyncZoom_WhenDisabled_OnlyActiveViewportChanges()
    {
        var state = CreateEnteredState(imageCount: 3);
        state.SelectImage(1);

        state.ToggleSyncZoom();
        state.SetActiveViewportZoom(225);
        state.SetActiveViewportRelativePan(new RelativePanOffset(0.2, 0.8));

        Assert.False(state.IsSyncZoomEnabled);
        Assert.Equal(100, state.Viewports[0].ZoomLevel);
        Assert.Equal(225, state.Viewports[1].ZoomLevel);
        Assert.Equal(100, state.Viewports[2].ZoomLevel);
        Assert.Equal(new RelativePanOffset(0.5, 0.5), state.Viewports[0].RelativePan);
        Assert.Equal(new RelativePanOffset(0.2, 0.8), state.Viewports[1].RelativePan);
        Assert.Equal(new RelativePanOffset(0.5, 0.5), state.Viewports[2].RelativePan);
    }

    [Fact]
    public void ResetSynchronizedZoom_WhenSyncEnabled_ResetsAllViewportsToFit()
    {
        var state = CreateEnteredState(imageCount: 3);
        state.SetSynchronizedZoom(175);
        state.SetSynchronizedRelativePan(new RelativePanOffset(0.1, 0.9));

        state.ResetSynchronizedZoom();

        Assert.All(state.Viewports, viewport =>
        {
            Assert.Equal(100, viewport.ZoomLevel);
            Assert.Equal(new RelativePanOffset(0.5, 0.5), viewport.RelativePan);
        });
    }

    private static ComparisonState CreateEnteredState(int imageCount)
    {
        var images = Enumerable.Range(0, imageCount)
            .Select(index => new ComparisonImage(
                index,
                new DecodedImage($@"C:\Photos\{index}.png", new ImageMetadata(100 + index, 100, 10))))
            .ToArray();
        var state = new ComparisonState();
        state.EnterComparison(new ComparisonSet(ComparisonLayout.SideBySide, images));
        return state;
    }
}
