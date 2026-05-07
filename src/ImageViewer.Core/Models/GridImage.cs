namespace ImageViewer.Core.Models;

public sealed record GridImage(
    ImageItem Item,
    int PlaylistIndex,
    GridCell Cell);
