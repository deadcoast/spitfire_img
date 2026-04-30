using System.Collections.ObjectModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Core.ViewModels;

public sealed class CommandPaletteState : ObservableObject
{
    private readonly ICommandPaletteService _commandPaletteService;
    private bool _isOpen;
    private int _selectedCommandIndex;
    private string _searchQuery = string.Empty;

    public CommandPaletteState(ICommandPaletteService commandPaletteService)
    {
        _commandPaletteService = commandPaletteService;
        foreach (var command in commandPaletteService.GetRegisteredCommands())
        {
            Commands.Add(command);
        }

        RefreshFilteredCommands();
    }

    public bool IsOpen
    {
        get => _isOpen;
        private set => SetProperty(ref _isOpen, value);
    }

    public int SelectedCommandIndex
    {
        get => _selectedCommandIndex;
        private set
        {
            if (SetProperty(ref _selectedCommandIndex, value))
            {
                OnPropertyChanged(nameof(SelectedCommand));
            }
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        private set => SetProperty(ref _searchQuery, value);
    }

    public ObservableCollection<CommandItem> Commands { get; } = [];

    public ObservableCollection<CommandSearchResult> FilteredCommands { get; } = [];

    public ObservableCollection<CommandPaletteDisplayItem> DisplayItems { get; } = [];

    public ObservableCollection<CommandItem> RecentCommands { get; } = [];

    public CommandItem? SelectedCommand => SelectedResult?.Command;

    public CommandSearchResult? SelectedResult => FilteredCommands.Count > 0 &&
                                                  SelectedCommandIndex >= 0 &&
                                                  SelectedCommandIndex < FilteredCommands.Count
        ? FilteredCommands[SelectedCommandIndex]
        : null;

    public bool HasNoMatches => FilteredCommands.Count == 0;

    public string EmptyMessage => "No matching commands found";

    public int SelectedDisplayIndex
    {
        get
        {
            var selectedCommand = SelectedCommand;
            if (selectedCommand is null)
            {
                return -1;
            }

            for (var index = 0; index < DisplayItems.Count; index++)
            {
                if (DisplayItems[index].Command?.Id == selectedCommand.Id)
                {
                    return index;
                }
            }

            return -1;
        }
    }

    public void Open()
    {
        IsOpen = true;
        SetSearchQuery(string.Empty);
        SelectedCommandIndex = 0;
        OnPropertyChanged(nameof(SelectedDisplayIndex));
    }

    public void Close()
    {
        IsOpen = false;
        SelectedCommandIndex = 0;
        OnPropertyChanged(nameof(SelectedDisplayIndex));
    }

    public void SelectNext()
    {
        if (FilteredCommands.Count == 0)
        {
            return;
        }

        SelectedCommandIndex = (SelectedCommandIndex + 1) % FilteredCommands.Count;
        OnPropertyChanged(nameof(SelectedDisplayIndex));
    }

    public void SelectPrevious()
    {
        if (FilteredCommands.Count == 0)
        {
            return;
        }

        SelectedCommandIndex = (SelectedCommandIndex - 1 + FilteredCommands.Count) % FilteredCommands.Count;
        OnPropertyChanged(nameof(SelectedDisplayIndex));
    }

    public void SelectCommand(string commandId)
    {
        var index = FilteredCommands
            .Select((result, commandIndex) => new { result, commandIndex })
            .FirstOrDefault(item => string.Equals(item.result.Command.Id, commandId, StringComparison.Ordinal))
            ?.commandIndex;

        if (index is not null)
        {
            SelectedCommandIndex = index.Value;
            OnPropertyChanged(nameof(SelectedDisplayIndex));
        }
    }

    public void SelectDisplayItem(int displayIndex)
    {
        if (displayIndex < 0 || displayIndex >= DisplayItems.Count || DisplayItems[displayIndex].IsHeader)
        {
            return;
        }

        SelectCommand(DisplayItems[displayIndex].Command?.Id ?? string.Empty);
    }

    public void RecordCommandExecuted(CommandItem command)
    {
        var existing = RecentCommands.FirstOrDefault(recent => string.Equals(recent.Id, command.Id, StringComparison.Ordinal));
        if (existing is not null)
        {
            RecentCommands.Remove(existing);
        }

        RecentCommands.Insert(0, command);
        while (RecentCommands.Count > 10)
        {
            RecentCommands.RemoveAt(RecentCommands.Count - 1);
        }

        RefreshDisplayItems();
    }

    public void SetSearchQuery(string query)
    {
        SearchQuery = query;
        RefreshFilteredCommands();
        SelectedCommandIndex = 0;
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(SelectedCommand));
        OnPropertyChanged(nameof(SelectedResult));
        OnPropertyChanged(nameof(SelectedDisplayIndex));
    }

    private void RefreshFilteredCommands()
    {
        FilteredCommands.Clear();
        foreach (var result in _commandPaletteService.SearchCommands(SearchQuery))
        {
            FilteredCommands.Add(result);
        }

        OnPropertyChanged(nameof(HasNoMatches));
        RefreshDisplayItems();
    }

    private void RefreshDisplayItems()
    {
        DisplayItems.Clear();
        if (FilteredCommands.Count == 0)
        {
            OnPropertyChanged(nameof(SelectedDisplayIndex));
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchQuery) && RecentCommands.Count > 0)
        {
            DisplayItems.Add(CommandPaletteDisplayItem.HeaderItem("Recent"));
            foreach (var recentCommand in RecentCommands)
            {
                var result = FilteredCommands.FirstOrDefault(item => item.Command.Id == recentCommand.Id);
                if (result is not null)
                {
                    DisplayItems.Add(CommandPaletteDisplayItem.CommandItem(result));
                }
            }
        }

        foreach (var group in FilteredCommands.GroupBy(result => result.Command.Category))
        {
            DisplayItems.Add(CommandPaletteDisplayItem.HeaderItem(group.Key.ToString()));
            foreach (var result in group)
            {
                DisplayItems.Add(CommandPaletteDisplayItem.CommandItem(result));
            }
        }

        OnPropertyChanged(nameof(SelectedDisplayIndex));
    }
}
