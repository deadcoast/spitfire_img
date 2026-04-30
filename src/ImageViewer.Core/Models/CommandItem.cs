namespace ImageViewer.Core.Models;

public sealed record CommandItem(
    string Id,
    string Title,
    CommandCategory Category,
    string Shortcut);
