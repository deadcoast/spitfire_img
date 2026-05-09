namespace ImageViewer.Core.Models;

public sealed record CommandItem(
    string Id,
    string Title,
    string Category,
    string KeyboardShortcut,
    bool IsAvailable = true,
    string Description = "");
