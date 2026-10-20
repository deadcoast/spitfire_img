using ImageViewer.App.Services.Input;

namespace ImageViewer.App.Tests;

public sealed class ViewerInputRoutingTests
{
    [Fact]
    public void ResolveTopLayer_GivesTextAndOverlaySurfacesPriorityOverViewerModes()
    {
        var state = new ViewerInputState(
            IsCommandPaletteVisible: true,
            IsSearchOverlayVisible: true,
            IsMetadataPanelVisible: true,
            IsSlideshowActive: true,
            IsComparisonActive: true,
            IsLayoutModeActive: true,
            IsFullscreen: true);

        Assert.Equal(ViewerInputLayer.CommandPalette, ViewerInputRouting.ResolveTopLayer(state));

        state = state with { IsCommandPaletteVisible = false };
        Assert.Equal(ViewerInputLayer.SearchOverlay, ViewerInputRouting.ResolveTopLayer(state));

        state = state with { IsSearchOverlayVisible = false };
        Assert.Equal(ViewerInputLayer.MetadataPanel, ViewerInputRouting.ResolveTopLayer(state));
    }

    [Fact]
    public void ResolveEscapeAction_DismissesOneLayerAtATimeInPredictableOrder()
    {
        var state = new ViewerInputState(
            IsCommandPaletteVisible: false,
            IsSearchOverlayVisible: false,
            IsMetadataPanelVisible: false,
            IsSlideshowActive: true,
            IsComparisonActive: true,
            IsLayoutModeActive: true,
            IsFullscreen: true);

        Assert.Equal(ViewerEscapeAction.StopSlideshow, ViewerInputRouting.ResolveEscapeAction(state));

        state = state with { IsSlideshowActive = false };
        Assert.Equal(ViewerEscapeAction.ExitComparison, ViewerInputRouting.ResolveEscapeAction(state));

        state = state with { IsComparisonActive = false };
        Assert.Equal(ViewerEscapeAction.ExitLayoutMode, ViewerInputRouting.ResolveEscapeAction(state));

        state = state with { IsLayoutModeActive = false };
        Assert.Equal(ViewerEscapeAction.ExitFullscreen, ViewerInputRouting.ResolveEscapeAction(state));
    }

    [Fact]
    public void ShouldSuppressRootNavigation_SuppressesGlobalNavigationBehindOpenTextSurfaces()
    {
        Assert.True(ViewerInputRouting.ShouldSuppressRootNavigation(ViewerInputLayer.CommandPalette));
        Assert.True(ViewerInputRouting.ShouldSuppressRootNavigation(ViewerInputLayer.SearchOverlay));
        Assert.True(ViewerInputRouting.ShouldSuppressRootNavigation(ViewerInputLayer.MetadataPanel));
        Assert.False(ViewerInputRouting.ShouldSuppressRootNavigation(ViewerInputLayer.LayoutMode));
        Assert.False(ViewerInputRouting.ShouldSuppressRootNavigation(ViewerInputLayer.Viewer));
    }
}
