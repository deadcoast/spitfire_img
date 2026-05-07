namespace ImageViewer.Core.Models;

public sealed record CommandPaletteEntry(string HeaderText, CommandPaletteMatch? Match)
{
    public bool IsHeader => Match is null;

    public CommandItem? Command => Match?.Command;

    public string Title => Match?.Title ?? HeaderText;

    public string Category => Match?.Category ?? HeaderText;

    public string KeyboardShortcut => Match?.KeyboardShortcut ?? string.Empty;

    public static CommandPaletteEntry Header(string headerText)
    {
        return new CommandPaletteEntry(headerText, null);
    }

    public static CommandPaletteEntry CommandRow(CommandPaletteMatch match)
    {
        return new CommandPaletteEntry(string.Empty, match);
    }
}
