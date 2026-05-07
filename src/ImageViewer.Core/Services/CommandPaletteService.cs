using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class CommandPaletteService : ICommandPaletteService
{
    private const int RecentCommandLimit = 10;
    private readonly IReadOnlyList<CommandRegistration> _registrations;
    private readonly List<CommandItem> _recentCommands = [];

    public CommandPaletteService(IEnumerable<CommandRegistration> registrations)
    {
        _registrations = registrations.ToArray();
    }

    public Result<IReadOnlyList<CommandItem>> GetAllCommands()
    {
        return Result<IReadOnlyList<CommandItem>>.Success(
            _registrations.Select(registration => registration.Command).ToArray());
    }

    public Result<IReadOnlyList<CommandPaletteMatch>> FilterCommands(string query)
    {
        var normalizedQuery = query.Trim();
        var matches = _registrations
            .Select((registration, index) => new
            {
                Match = MatchCommand(registration.Command, normalizedQuery),
                Index = index
            })
            .Where(item => item.Match is not null)
            .OrderByDescending(item => item.Match!.Score)
            .ThenBy(item => item.Index)
            .Select(item => item.Match!)
            .ToArray();

        return Result<IReadOnlyList<CommandPaletteMatch>>.Success(matches);
    }

    public Result<IReadOnlyList<CommandItem>> GetRecentCommands()
    {
        return Result<IReadOnlyList<CommandItem>>.Success(_recentCommands.ToArray());
    }

    public async Task<Result> ExecuteCommandAsync(string commandId)
    {
        var registration = _registrations.FirstOrDefault(item =>
            string.Equals(item.Command.Id, commandId, StringComparison.Ordinal));

        if (registration is null)
        {
            return Result.Failure(
                ViewerError.CommandPaletteError("ExecuteCommand", $"Command '{commandId}' was not found."));
        }

        if (!registration.Command.IsAvailable)
        {
            return Result.Failure(
                ViewerError.CommandPaletteError("ExecuteCommand", $"Command '{registration.Command.Title}' is not available."));
        }

        var result = await registration.ExecuteAsync().ConfigureAwait(false);
        if (result.IsSuccess)
        {
            AddRecentCommand(registration.Command);
        }

        return result;
    }

    private static CommandPaletteMatch? MatchCommand(CommandItem command, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new CommandPaletteMatch(command, [], 0);
        }

        var titleMatch = MatchCommandTitle(command, query);
        if (!command.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return titleMatch;
        }

        return titleMatch is null
            ? new CommandPaletteMatch(command, [], 20_000 - command.Title.Length)
            : titleMatch with { Score = titleMatch.Score + 20_000 };
    }

    private static CommandPaletteMatch? MatchCommandTitle(CommandItem command, string query)
    {
        var title = command.Title;
        var matchedIndices = new List<int>(query.Length);
        var searchStart = 0;

        foreach (var queryCharacter in query)
        {
            var matchedIndex = IndexOfCharacter(title, queryCharacter, searchStart);
            if (matchedIndex < 0)
            {
                return null;
            }

            matchedIndices.Add(matchedIndex);
            searchStart = matchedIndex + 1;
        }

        return new CommandPaletteMatch(command, matchedIndices, CalculateScore(title, query, matchedIndices));
    }

    private void AddRecentCommand(CommandItem command)
    {
        _recentCommands.RemoveAll(recentCommand => string.Equals(
            recentCommand.Id,
            command.Id,
            StringComparison.Ordinal));
        _recentCommands.Insert(0, command);

        if (_recentCommands.Count > RecentCommandLimit)
        {
            _recentCommands.RemoveRange(RecentCommandLimit, _recentCommands.Count - RecentCommandLimit);
        }
    }

    private static int IndexOfCharacter(string value, char character, int startIndex)
    {
        for (var index = startIndex; index < value.Length; index++)
        {
            if (char.ToUpperInvariant(value[index]) == char.ToUpperInvariant(character))
            {
                return index;
            }
        }

        return -1;
    }

    private static int CalculateScore(string title, string query, IReadOnlyList<int> matchedIndices)
    {
        var score = 10_000;
        var contiguousIndex = title.IndexOf(query, StringComparison.OrdinalIgnoreCase);

        if (title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 5_000;
        }
        else if (contiguousIndex >= 0)
        {
            score += 3_000 - (contiguousIndex * 10);
        }

        score -= matchedIndices[0] * 10;
        score -= title.Length;

        for (var index = 1; index < matchedIndices.Count; index++)
        {
            score -= (matchedIndices[index] - matchedIndices[index - 1] - 1) * 5;
        }

        return score;
    }
}
