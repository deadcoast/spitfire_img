namespace ImageViewer.Core.Models;

public sealed record FilmstripData(
    ImageItem FocusItem,
    int FocusIndex,
    IReadOnlyList<FilmstripImage> Thumbnails);
