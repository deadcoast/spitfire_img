namespace ImageViewer.Core.Models;

public sealed record FilmstripImage(
    ImageItem Item,
    int PlaylistIndex,
    bool IsFocused);
