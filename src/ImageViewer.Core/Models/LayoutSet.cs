namespace ImageViewer.Core.Models;

public sealed record LayoutSet(
    LayoutMode Mode,
    LayoutConfig Config,
    IReadOnlyList<LayoutImage> VisibleImages,
    int WindowStartIndex = 0,
    int ActiveImageIndex = 0,
    int TotalImageCount = 0)
{
    public LayoutImage? FocusImage => ActiveImageIndex >= 0 && ActiveImageIndex < VisibleImages.Count
        ? VisibleImages[ActiveImageIndex]
        : null;
}
