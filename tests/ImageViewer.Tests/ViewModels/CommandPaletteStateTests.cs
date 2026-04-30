using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class CommandPaletteStateTests
{
    [Fact]
    public void OpenAndClose_UpdatesVisibilityAndSelection()
    {
        var state = new CommandPaletteState(new CommandPaletteService());

        state.Open();
        state.SelectNext();
        state.Close();

        Assert.False(state.IsOpen);
        Assert.Equal(0, state.SelectedCommandIndex);
        Assert.NotNull(state.SelectedCommand);
    }

    [Fact]
    public void Commands_ExposeRegistryForDisplay()
    {
        var state = new CommandPaletteState(new CommandPaletteService());

        Assert.Contains(state.Commands, command => command.Category == CommandCategory.File);
        Assert.Contains(state.Commands, command => command.Category == CommandCategory.View);
        Assert.Contains(state.Commands, command => command.Category == CommandCategory.Comparison);
    }

    [Fact]
    public void SetSearchQuery_FiltersCommandsAndResetsSelection()
    {
        var state = new CommandPaletteState(new CommandPaletteService());
        state.SelectNext();

        state.SetSearchQuery("rotate");

        Assert.Equal("rotate", state.SearchQuery);
        Assert.Equal(0, state.SelectedCommandIndex);
        Assert.All(state.FilteredCommands, result => Assert.Contains("Rotate", result.Command.Title));
    }

    [Fact]
    public void SetSearchQuery_WithNoMatchShowsEmptyMessage()
    {
        var state = new CommandPaletteState(new CommandPaletteService());

        state.SetSearchQuery("zzzzzz");

        Assert.True(state.HasNoMatches);
        Assert.Equal("No matching commands found", state.EmptyMessage);
        Assert.Null(state.SelectedCommand);
    }

    [Fact]
    public void SelectNext_NavigatesThroughFilteredResults()
    {
        var state = new CommandPaletteState(new CommandPaletteService());
        state.SetSearchQuery("rotate");

        state.SelectNext();

        Assert.Equal(1, state.SelectedCommandIndex);
        Assert.Contains("Rotate", state.SelectedCommand?.Title);
    }

    [Fact]
    public void DisplayItems_WithNoQuery_GroupsCommandsByCategoryHeaders()
    {
        var state = new CommandPaletteState(new CommandPaletteService());

        var headers = state.DisplayItems
            .Where(item => item.IsHeader)
            .Select(item => item.Header)
            .ToArray();

        Assert.Contains("File", headers);
        Assert.Contains("View", headers);
        Assert.Contains("Image", headers);
        Assert.Contains("Navigation", headers);
        Assert.Contains("Layout", headers);
        Assert.Contains("Slideshow", headers);
        Assert.Contains("Comparison", headers);
        Assert.Contains("Animation", headers);
    }

    [Fact]
    public void RecordCommandExecuted_TracksMostRecentTenCommandsAtTop()
    {
        var commands = Enumerable.Range(0, 12)
            .Select(index => new CommandItem(
                $"test.command.{index}",
                $"Test Command {index}",
                CommandCategory.View,
                $"Ctrl+{index}"))
            .ToArray();
        var state = new CommandPaletteState(new CommandPaletteService(commands));

        foreach (var command in commands)
        {
            state.RecordCommandExecuted(command);
        }

        Assert.Equal(10, state.RecentCommands.Count);
        Assert.Equal("test.command.11", state.RecentCommands[0].Id);
        Assert.Equal("Recent", state.DisplayItems[0].Header);
        Assert.Equal("test.command.11", state.DisplayItems[1].Command?.Id);
    }

    [Fact]
    public void SelectNext_SkipsHeadersAndSelectsOnlyCommandRows()
    {
        var state = new CommandPaletteState(new CommandPaletteService());

        state.SelectNext();

        Assert.NotNull(state.SelectedCommand);
        Assert.False(state.DisplayItems[state.SelectedDisplayIndex].IsHeader);
        Assert.Equal(state.SelectedCommand?.Id, state.DisplayItems[state.SelectedDisplayIndex].Command?.Id);
    }
}
