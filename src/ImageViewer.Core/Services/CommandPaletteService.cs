using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class CommandPaletteService : ICommandPaletteService
{
    private const int MaxCachedSearchQueries = 64;

    private readonly CommandItem[] _commands;
    private readonly CommandSearchIndex[] _indexedCommands;
    private readonly Dictionary<string, IReadOnlyList<CommandSearchResult>> _searchCache = new(StringComparer.Ordinal);
    private readonly Queue<string> _searchCacheOrder = new();
    private readonly object _cacheGate = new();

    public CommandPaletteService()
        : this(DefaultCommands)
    {
    }

    public CommandPaletteService(IEnumerable<CommandItem> commands)
    {
        _commands = commands.ToArray();
        _indexedCommands = _commands
            .Select(command => new CommandSearchIndex(command, command.Title.ToUpperInvariant()))
            .ToArray();
    }

    private static readonly CommandItem[] DefaultCommands =
    [
        new(CommandIds.OpenFile, "Open File", CommandCategory.File, "Ctrl+O"),
        new(CommandIds.ToggleFullscreen, "Toggle Fullscreen", CommandCategory.View, "F11"),
        new(CommandIds.FitToWindow, "Fit to Window", CommandCategory.View, "Ctrl+0"),
        new(CommandIds.ActualSize, "Actual Size", CommandCategory.View, "View menu"),
        new(CommandIds.RotateLeft, "Rotate Left", CommandCategory.Image, "Ctrl+L"),
        new(CommandIds.RotateRight, "Rotate Right", CommandCategory.Image, "Ctrl+R"),
        new(CommandIds.NavigatePrevious, "Previous Image", CommandCategory.Navigation, "Left"),
        new(CommandIds.NavigateNext, "Next Image", CommandCategory.Navigation, "Right"),
        new(CommandIds.GridLayout, "Grid Layout", CommandCategory.Layout, "Ctrl+G"),
        new(CommandIds.FilmstripLayout, "Filmstrip Layout", CommandCategory.Layout, "Ctrl+Shift+F"),
        new(CommandIds.ToggleSlideshow, "Toggle Slideshow", CommandCategory.Slideshow, "F5"),
        new(CommandIds.EnterComparison, "Side-by-Side Comparison", CommandCategory.Comparison, "Ctrl+D"),
        new(CommandIds.EnterOverlayComparison, "Overlay Comparison", CommandCategory.Comparison, "Ctrl+Shift+D"),
        new(CommandIds.ToggleComparisonSyncZoom, "Toggle Comparison Sync Zoom", CommandCategory.Comparison, "Ctrl+L"),
        new(CommandIds.ComparisonTwoImages, "Compare Two Images", CommandCategory.Comparison, "Ctrl+2"),
        new(CommandIds.ComparisonThreeImages, "Compare Three Images", CommandCategory.Comparison, "Ctrl+3"),
        new(CommandIds.ComparisonFourImages, "Compare Four Images", CommandCategory.Comparison, "Ctrl+4"),
        new(CommandIds.ReturnToSingleImage, "Single Image View", CommandCategory.Layout, "Ctrl+1"),
        new(CommandIds.ToggleGifPlayback, "Toggle GIF Playback", CommandCategory.Animation, "P")
    ];

    public IReadOnlyList<CommandItem> GetRegisteredCommands()
    {
        return _commands;
    }

    public IReadOnlyList<CommandSearchResult> SearchCommands(string query)
    {
        var normalizedQuery = NormalizeQuery(query);
        lock (_cacheGate)
        {
            if (_searchCache.TryGetValue(normalizedQuery, out var cachedResults))
            {
                return cachedResults;
            }
        }

        var results = SearchCommandsCore(normalizedQuery);
        lock (_cacheGate)
        {
            if (_searchCache.TryGetValue(normalizedQuery, out var cachedResults))
            {
                return cachedResults;
            }

            AddCachedResults(normalizedQuery, results);
            return results;
        }
    }

    private void AddCachedResults(string normalizedQuery, IReadOnlyList<CommandSearchResult> results)
    {
        while (_searchCacheOrder.Count >= MaxCachedSearchQueries)
        {
            var evictedQuery = _searchCacheOrder.Dequeue();
            _searchCache.Remove(evictedQuery);
        }

        _searchCache[normalizedQuery] = results;
        _searchCacheOrder.Enqueue(normalizedQuery);
    }

    private IReadOnlyList<CommandSearchResult> SearchCommandsCore(string normalizedQuery)
    {
        if (normalizedQuery.Length == 0)
        {
            return _commands
                .Select((command, index) => new CommandSearchResult(command, [], 1000 - index))
                .ToArray();
        }

        if (Enum.TryParse<CommandCategory>(normalizedQuery, ignoreCase: true, out var category))
        {
            return _commands
                .Where(command => command.Category == category)
                .Select((command, index) => new CommandSearchResult(command, [], 2000 - index))
                .ToArray();
        }

        return _indexedCommands
            .Select(command => TryMatch(command, normalizedQuery))
            .Where(result => result is not null)
            .Select(result => result!)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Command.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static CommandSearchResult? TryMatch(CommandSearchIndex command, string normalizedQuery)
    {
        var matchIndexes = new List<int>(normalizedQuery.Length);
        var searchStart = 0;

        foreach (var queryCharacter in normalizedQuery)
        {
            var matchIndex = command.NormalizedTitle.IndexOf(queryCharacter, searchStart);
            if (matchIndex < 0)
            {
                return null;
            }

            matchIndexes.Add(matchIndex);
            searchStart = matchIndex + 1;
        }

        return new CommandSearchResult(
            command.Command,
            matchIndexes,
            CalculateScore(command.Command, normalizedQuery, matchIndexes));
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

    private static string NormalizeQuery(string query)
    {
        return string.IsNullOrWhiteSpace(query)
            ? string.Empty
            : query.Trim().ToUpperInvariant();
    }

    private sealed record CommandSearchIndex(CommandItem Command, string NormalizedTitle);
}
