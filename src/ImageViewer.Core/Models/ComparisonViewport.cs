namespace ImageViewer.Core.Models;

public sealed record ComparisonViewport(
    int VisibleIndex,
    double ZoomLevel,
    RelativePanOffset RelativePan);
