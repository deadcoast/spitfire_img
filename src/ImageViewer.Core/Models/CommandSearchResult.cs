namespace ImageViewer.Core.Models;

public sealed record CommandSearchResult(
    CommandItem Command,
    IReadOnlyList<int> MatchIndexes,
    int Score)
{
    public string Title => Command.Title;

    public CommandCategory Category => Command.Category;

    public string Shortcut => Command.Shortcut;
}
