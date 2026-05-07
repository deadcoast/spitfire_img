using System.Collections.ObjectModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Core.ViewModels;

public sealed class CommandPaletteState : ObservableObject
{
    private static readonly string[] CategoryOrder =
    [
        "File",
        "View",
        "Image",
        "Navigation",
        "Layout",
        "Slideshow",
        "Comparison",
        "Animation"
    ];

    private readonly ICommandPaletteService _commandPaletteService;
    private bool _isVisible;
    private string _searchText = string.Empty;
    private int _selectedIndex = -1;

    public CommandPaletteState(ICommandPaletteService commandPaletteService)
    {
        _commandPaletteService = commandPaletteService;
    }

    public ObservableCollection<CommandItem> AllCommands { get; } = [];

    public ObservableCollection<CommandPaletteMatch> FilteredCommands { get; } = [];

    public ObservableCollection<CommandPaletteEntry> DisplayEntries { get; } = [];

    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    public string SearchText
    {
        get => _searchText;
        private set => SetProperty(ref _searchText, value);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        private set
        {
            if (SetProperty(ref _selectedIndex, value))
            {
                OnPropertyChanged(nameof(SelectedCommand));
            }
        }
    }

    public CommandItem? SelectedCommand => SelectedIndex >= 0 && SelectedIndex < DisplayEntries.Count
        ? DisplayEntries[SelectedIndex].Command
        : null;

    public string StatusText => IsVisible && FilteredCommands.Count == 0
        ? "No matching commands found"
        : string.Empty;

    public void Show()
    {
        var commandsResult = _commandPaletteService.GetAllCommands();
        if (commandsResult.IsSuccess)
        {
            ReplaceCommands(AllCommands, commandsResult.Value);
            RefreshFilteredCommands();
        }

        IsVisible = true;
        OnPropertyChanged(nameof(StatusText));
    }

    public void Hide()
    {
        SearchText = string.Empty;
        IsVisible = false;
    }

    public void SetSearchText(string searchText)
    {
        SearchText = searchText;
        RefreshFilteredCommands();
    }

    public void SelectNext()
    {
        if (FilteredCommands.Count == 0)
        {
            SelectedIndex = -1;
            return;
        }

        var nextIndex = FindNextCommandIndex(SelectedIndex + 1);
        if (nextIndex >= 0)
        {
            SelectedIndex = nextIndex;
        }
    }

    public void SelectPrevious()
    {
        if (FilteredCommands.Count == 0)
        {
            SelectedIndex = -1;
            return;
        }

        var previousIndex = FindPreviousCommandIndex(SelectedIndex - 1);
        if (previousIndex >= 0)
        {
            SelectedIndex = previousIndex;
        }
    }

    public void SelectCommandById(string commandId)
    {
        var index = DisplayEntries
            .Select((entry, commandIndex) => new { entry, commandIndex })
            .FirstOrDefault(item => string.Equals(item.entry.Command?.Id, commandId, StringComparison.Ordinal))
            ?.commandIndex;

        if (index.HasValue)
        {
            SelectedIndex = index.Value;
        }
    }

    public async Task<Result> ExecuteSelectedCommandAsync()
    {
        if (SelectedCommand is null)
        {
            return Result.Failure(
                ViewerError.CommandPaletteError("ExecuteSelectedCommand", "No command is selected."));
        }

        var result = await _commandPaletteService.ExecuteCommandAsync(SelectedCommand.Id).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            Hide();
        }

        return result;
    }

    private static void ReplaceCommands(
        ObservableCollection<CommandItem> target,
        IEnumerable<CommandItem> commands)
    {
        target.Clear();
        foreach (var command in commands)
        {
            target.Add(command);
        }
    }

    private void RefreshFilteredCommands()
    {
        var result = _commandPaletteService.FilterCommands(SearchText);
        if (!result.IsSuccess)
        {
            return;
        }

        ReplaceMatches(FilteredCommands, result.Value);
        BuildDisplayEntries(result.Value);
        SelectedIndex = FindNextCommandIndex(0);
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(SelectedCommand));
    }

    private static void ReplaceMatches(
        ObservableCollection<CommandPaletteMatch> target,
        IEnumerable<CommandPaletteMatch> matches)
    {
        target.Clear();
        foreach (var match in matches)
        {
            target.Add(match);
        }
    }

    private void BuildDisplayEntries(IReadOnlyList<CommandPaletteMatch> matches)
    {
        DisplayEntries.Clear();
        if (matches.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            AddRecentEntries();
        }

        foreach (var group in matches
            .GroupBy(match => match.Category)
            .OrderBy(group => GetCategoryOrder(group.Key))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            DisplayEntries.Add(CommandPaletteEntry.Header(group.Key));
            foreach (var match in group)
            {
                DisplayEntries.Add(CommandPaletteEntry.CommandRow(match));
            }
        }
    }

    private void AddRecentEntries()
    {
        var recentResult = _commandPaletteService.GetRecentCommands();
        if (!recentResult.IsSuccess || recentResult.Value.Count == 0)
        {
            return;
        }

        DisplayEntries.Add(CommandPaletteEntry.Header("Recent"));
        foreach (var command in recentResult.Value)
        {
            DisplayEntries.Add(CommandPaletteEntry.CommandRow(new CommandPaletteMatch(command, [], int.MaxValue)));
        }
    }

    private int FindNextCommandIndex(int startIndex)
    {
        for (var index = Math.Max(0, startIndex); index < DisplayEntries.Count; index++)
        {
            if (!DisplayEntries[index].IsHeader)
            {
                return index;
            }
        }

        return -1;
    }

    private int FindPreviousCommandIndex(int startIndex)
    {
        for (var index = Math.Min(startIndex, DisplayEntries.Count - 1); index >= 0; index--)
        {
            if (!DisplayEntries[index].IsHeader)
            {
                return index;
            }
        }

        return -1;
    }

    private static int GetCategoryOrder(string category)
    {
        var index = Array.IndexOf(CategoryOrder, category);
        return index >= 0 ? index : CategoryOrder.Length;
    }
}
