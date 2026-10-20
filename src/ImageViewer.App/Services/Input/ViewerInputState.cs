namespace ImageViewer.App.Services.Input;

public sealed record ViewerInputState(
    bool IsCommandPaletteVisible,
    bool IsSearchOverlayVisible,
    bool IsMetadataPanelVisible,
    bool IsSlideshowActive,
    bool IsComparisonActive,
    bool IsLayoutModeActive,
    bool IsFullscreen);

