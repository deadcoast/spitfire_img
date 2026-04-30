namespace ImageViewer.Core.Models;

public sealed record CommandPaletteDisplayItem(
    string? Header,
    CommandSearchResult? Result)
{
    public bool IsHeader => Header is not null;

    public CommandItem? Command => Result?.Command;

    public static CommandPaletteDisplayItem HeaderItem(string header)
    {
        return new CommandPaletteDisplayItem(header, null);
    }

    public static CommandPaletteDisplayItem CommandItem(CommandSearchResult result)
    {
        return new CommandPaletteDisplayItem(null, result);
    }
}
