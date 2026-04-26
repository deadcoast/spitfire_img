namespace ImageViewer.Core.Models;

public sealed record Playlist(
    IReadOnlyList<ImageItem> Items,
    int CurrentIndex,
    string FolderPath)
{
    public int TotalCount => Items.Count;

    public ImageItem? CurrentItem => CurrentIndex >= 0 && CurrentIndex < Items.Count
        ? Items[CurrentIndex]
        : null;
}
