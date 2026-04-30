using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class CommandPaletteService : ICommandPaletteService
{
    private readonly CommandItem[] _commands;

    public CommandPaletteService()
        : this(DefaultCommands)
    {
    }

    public CommandPaletteService(IEnumerable<CommandItem> commands)
    {
        _commands = commands.ToArray();
    }

    private static readonly CommandItem[] DefaultCommands =
    [
        new(CommandIds.OpenFile, "Open File", CommandCategory.File, "Ctrl+O"),
        new(CommandIds.ToggleFullscreen, "Toggle Fullscreen", CommandCategory.View, "F11"),
        new(CommandIds.FitToWindow, "Fit to Window", CommandCategory.View, "Ctrl+0"),
        new(CommandIds.ActualSize, "Actual Size", CommandCategory.View, "Ctrl+1"),
        new(CommandIds.RotateLeft, "Rotate Left", CommandCategory.Image, "Ctrl+L"),
        new(CommandIds.RotateRight, "Rotate Right", CommandCategory.Image, "Ctrl+R"),
        new(CommandIds.NavigatePrevious, "Previous Image", CommandCategory.Navigation, "Left"),
        new(CommandIds.NavigateNext, "Next Image", CommandCategory.Navigation, "Right"),
        new(CommandIds.GridLayout, "Grid Layout", CommandCategory.Layout, "Ctrl+G"),
        new(CommandIds.FilmstripLayout, "Filmstrip Layout", CommandCategory.Layout, "Ctrl+F"),
        new(CommandIds.ToggleSlideshow, "Toggle Slideshow", CommandCategory.Slideshow, "F5"),
        new(CommandIds.EnterComparison, "Side-by-Side Comparison", CommandCategory.Comparison, "Ctrl+D"),
        new(CommandIds.EnterOverlayComparison, "Overlay Comparison", CommandCategory.Comparison, "Ctrl+Shift+D"),
        new(CommandIds.ToggleComparisonSyncZoom, "Toggle Comparison Sync Zoom", CommandCategory.Comparison, "Ctrl+L"),
        new(CommandIds.ToggleGifPlayback, "Toggle GIF Playback", CommandCategory.Animation, "P")
    ];

    public IReadOnlyList<CommandItem> GetRegisteredCommands()
    {
        return _commands;
    }

    public IReadOnlyList<CommandSearchResult> SearchCommands(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _commands
                .Select((command, index) => new CommandSearchResult(command, [], 1000 - index))
                .ToArray();
        }

        var normalizedQuery = query.Trim();
        if (Enum.TryParse<CommandCategory>(normalizedQuery, ignoreCase: true, out var category))
        {
            return _commands
                .Where(command => command.Category == category)
                .Select((command, index) => new CommandSearchResult(command, [], 2000 - index))
                .ToArray();
        }

        return _commands
            .Select(command => TryMatch(command, normalizedQuery))
            .Where(result => result is not null)
            .Select(result => result!)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Command.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static CommandSearchResult? TryMatch(CommandItem command, string query)
    {
        var matchIndexes = new List<int>(query.Length);
        var title = command.Title;
        var searchStart = 0;

        foreach (var queryCharacter in query)
        {
            var matchIndex = IndexOf(title, queryCharacter, searchStart);
            if (matchIndex < 0)
            {
                return null;
            }

            matchIndexes.Add(matchIndex);
            searchStart = matchIndex + 1;
        }

        return new CommandSearchResult(command, matchIndexes, CalculateScore(command, query, matchIndexes));
    }

    private static int IndexOf(string text, char queryCharacter, int startIndex)
    {
        for (var index = startIndex; index < text.Length; index++)
        {
            if (char.ToUpperInvariant(text[index]) == char.ToUpperInvariant(queryCharacter))
            {
                return index;
            }
        }

        return -1;
    }

    private static int CalculateScore(CommandItem command, string query, IReadOnlyList<int> matchIndexes)
    {
        var span = matchIndexes[^1] - matchIndexes[0] + 1;
        var gaps = span - query.Length;
        var startsAtWordBoundary = matchIndexes[0] == 0 ||
                                   char.IsWhiteSpace(command.Title[matchIndexes[0] - 1]) ||
                                   command.Title[matchIndexes[0] - 1] == '-';
        var compactBonus = Math.Max(0, 50 - gaps * 5);
        var boundaryBonus = startsAtWordBoundary ? 25 : 0;
        var prefixBonus = matchIndexes[0] == 0 ? 25 : 0;
        return 1000 + compactBonus + boundaryBonus + prefixBonus - matchIndexes[0];
    }
}
