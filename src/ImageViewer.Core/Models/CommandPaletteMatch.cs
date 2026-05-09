namespace ImageViewer.Core.Models;

public sealed record CommandPaletteMatch(
    CommandItem Command,
    IReadOnlyList<int> MatchedTitleIndices,
    int Score)
{
    public string Id => Command.Id;

    public string Title => Command.Title;

    public string Category => Command.Category;

    public string KeyboardShortcut => Command.KeyboardShortcut;

    public string Description => Command.Description;
}
