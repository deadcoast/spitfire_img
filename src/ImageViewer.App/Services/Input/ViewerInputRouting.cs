namespace ImageViewer.App.Services.Input;

public static class ViewerInputRouting
{
    public static ViewerInputLayer ResolveTopLayer(ViewerInputState state)
    {
        if (state.IsCommandPaletteVisible)
        {
            return ViewerInputLayer.CommandPalette;
        }

        if (state.IsSearchOverlayVisible)
        {
            return ViewerInputLayer.SearchOverlay;
        }

        if (state.IsMetadataPanelVisible)
        {
            return ViewerInputLayer.MetadataPanel;
        }

        if (state.IsSlideshowActive)
        {
            return ViewerInputLayer.Slideshow;
        }

        if (state.IsComparisonActive)
        {
            return ViewerInputLayer.Comparison;
        }

        if (state.IsLayoutModeActive)
        {
            return ViewerInputLayer.LayoutMode;
        }

        return state.IsFullscreen ? ViewerInputLayer.Fullscreen : ViewerInputLayer.Viewer;
    }

    public static ViewerEscapeAction ResolveEscapeAction(ViewerInputState state)
    {
        return ResolveTopLayer(state) switch
        {
            ViewerInputLayer.CommandPalette => ViewerEscapeAction.HideCommandPalette,
            ViewerInputLayer.SearchOverlay => ViewerEscapeAction.HideSearchOverlay,
            ViewerInputLayer.MetadataPanel => ViewerEscapeAction.HideMetadataPanel,
            ViewerInputLayer.Slideshow => ViewerEscapeAction.StopSlideshow,
            ViewerInputLayer.Comparison => ViewerEscapeAction.ExitComparison,
            ViewerInputLayer.LayoutMode => ViewerEscapeAction.ExitLayoutMode,
            ViewerInputLayer.Fullscreen => ViewerEscapeAction.ExitFullscreen,
            _ => ViewerEscapeAction.None
        };
    }

    public static bool ShouldSuppressRootNavigation(ViewerInputLayer topLayer)
    {
        return topLayer is ViewerInputLayer.CommandPalette
            or ViewerInputLayer.SearchOverlay
            or ViewerInputLayer.MetadataPanel;
    }
}

