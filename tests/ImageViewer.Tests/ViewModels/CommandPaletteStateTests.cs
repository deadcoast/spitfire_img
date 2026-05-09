using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class CommandPaletteStateTests
{
    [Fact]
    public void Show_DisplaysAllCommandsAndSelectsFirstCommand()
    {
        var state = CreateState();

        state.Show();

        Assert.True(state.IsVisible);
        Assert.Equal(2, state.FilteredCommands.Count);
        Assert.Equal("View", state.DisplayEntries[0].HeaderText);
        Assert.Equal("view.fullscreen", state.SelectedCommand?.Id);
    }

    [Fact]
    public void Hide_ClosesPaletteAndClearsSearch()
    {
        var state = CreateState();
        state.Show();
        state.SetSearchText("zoom");

        state.Hide();

        Assert.False(state.IsVisible);
        Assert.Equal(string.Empty, state.SearchText);
    }

    [Fact]
    public async Task ExecuteSelectedCommandAsync_RunsCommandAndClosesPalette()
    {
        var executed = false;
        var command = new CommandItem("view.fullscreen", "Toggle Fullscreen", "View", "F11");
        var service = new CommandPaletteService(
            [new CommandRegistration(command, () =>
            {
                executed = true;
                return Task.FromResult(Result.Success());
            })]);
        var state = new CommandPaletteState(service);
        state.Show();

        var result = await state.ExecuteSelectedCommandAsync();

        Assert.True(result.IsSuccess);
        Assert.True(executed);
        Assert.False(state.IsVisible);
    }

    [Fact]
    public void SetSearchText_FiltersCommandsAndSelectsFirstMatch()
    {
        var state = CreateState();
        state.Show();

        state.SetSearchText("zoom");

        Assert.Equal("zoom", state.SearchText);
        Assert.Single(state.FilteredCommands);
        Assert.Equal("view.zoom-in", state.SelectedCommand?.Id);
        Assert.Equal(1, state.SelectedIndex);
        Assert.Equal(string.Empty, state.StatusText);
    }

    [Fact]
    public void SetSearchText_ClearingSearchDisplaysAllCommands()
    {
        var state = CreateState();
        state.Show();
        state.SetSearchText("zoom");

        state.SetSearchText(string.Empty);

        Assert.Equal(2, state.FilteredCommands.Count);
        Assert.Equal("view.fullscreen", state.SelectedCommand?.Id);
    }

    [Fact]
    public void SetSearchText_NoMatchesDisplaysNoMatchingCommandsMessage()
    {
        var state = CreateState();
        state.Show();

        state.SetSearchText("missing");

        Assert.Empty(state.FilteredCommands);
        Assert.Equal(-1, state.SelectedIndex);
        Assert.Equal("No matching commands found", state.StatusText);
    }

    [Fact]
    public void SelectNext_NavigatesFilteredResults()
    {
        var state = new CommandPaletteState(new CommandPaletteService(
            [
                new CommandRegistration(new CommandItem("view.fullscreen", "Toggle Fullscreen", "View", "F11"), () => Task.FromResult(Result.Success())),
                new CommandRegistration(new CommandItem("layout.filmstrip", "Filmstrip Layout", "Layout", "F"), () => Task.FromResult(Result.Success())),
                new CommandRegistration(new CommandItem("image.rotate-left", "Rotate Left", "Image", "Ctrl+L"), () => Task.FromResult(Result.Success()))
            ]));
        state.Show();
        state.SetSearchText("fs");
        var firstSelectedId = state.SelectedCommand?.Id;

        state.SelectNext();

        Assert.NotEqual(firstSelectedId, state.SelectedCommand?.Id);
        Assert.Contains(state.SelectedCommand?.Id, ["view.fullscreen", "layout.filmstrip"]);
        Assert.False(state.DisplayEntries[state.SelectedIndex].IsHeader);
    }

    [Fact]
    public async Task Show_DisplaysRecentCommandsAtTopOfPalette()
    {
        var fullscreen = new CommandItem("view.fullscreen", "Toggle Fullscreen", "View", "F11");
        var rotateLeft = new CommandItem("image.rotate-left", "Rotate Left", "Image", "Ctrl+L");
        var service = new CommandPaletteService(
            [
                new CommandRegistration(fullscreen, () => Task.FromResult(Result.Success())),
                new CommandRegistration(rotateLeft, () => Task.FromResult(Result.Success()))
            ]);
        await service.ExecuteCommandAsync(rotateLeft.Id);
        var state = new CommandPaletteState(service);

        state.Show();

        Assert.Equal("Recent", state.DisplayEntries[0].HeaderText);
        Assert.Equal(rotateLeft.Id, state.DisplayEntries[1].Command?.Id);
        Assert.Equal(rotateLeft.Id, state.SelectedCommand?.Id);
    }

    [Fact]
    public void Show_GroupsCommandsByCategoryHeaders()
    {
        var state = new CommandPaletteState(new CommandPaletteService(
            [
                new CommandRegistration(new CommandItem("file.open", "Open File", "File", "Ctrl+O"), () => Task.FromResult(Result.Success())),
                new CommandRegistration(new CommandItem("view.fullscreen", "Toggle Fullscreen", "View", "F11"), () => Task.FromResult(Result.Success())),
                new CommandRegistration(new CommandItem("image.rotate-left", "Rotate Left", "Image", "Ctrl+L"), () => Task.FromResult(Result.Success()))
            ]));

        state.Show();

        Assert.Equal(["File", "View", "Image"], state.DisplayEntries.Where(entry => entry.IsHeader).Select(entry => entry.HeaderText).ToArray());
    }

    [Fact]
    public void Show_CommandRowsExposeDescriptionForPaletteHierarchy()
    {
        var state = new CommandPaletteState(new CommandPaletteService(
            [
                new CommandRegistration(
                    new CommandItem(
                        "file.open",
                        "Open File",
                        "File",
                        "Ctrl+O",
                        Description: "Choose an image file and load its folder playlist."),
                    () => Task.FromResult(Result.Success()))
            ]));

        state.Show();

        var commandRow = Assert.Single(state.DisplayEntries.Where(entry => !entry.IsHeader));
        Assert.Equal("Choose an image file and load its folder playlist.", commandRow.Description);
        Assert.Equal(commandRow.Description, commandRow.Match?.Description);
    }

    [Fact]
    public void SelectNext_SkipsCategoryHeaders()
    {
        var state = new CommandPaletteState(new CommandPaletteService(
            [
                new CommandRegistration(new CommandItem("file.open", "Open File", "File", "Ctrl+O"), () => Task.FromResult(Result.Success())),
                new CommandRegistration(new CommandItem("view.fullscreen", "Toggle Fullscreen", "View", "F11"), () => Task.FromResult(Result.Success()))
            ]));
        state.Show();

        state.SelectNext();

        Assert.Equal("view.fullscreen", state.SelectedCommand?.Id);
    }

    private static CommandPaletteState CreateState()
    {
        return new CommandPaletteState(new CommandPaletteService(
            [
                new CommandRegistration(new CommandItem("view.fullscreen", "Toggle Fullscreen", "View", "F11"), () => Task.FromResult(Result.Success())),
                new CommandRegistration(new CommandItem("view.zoom-in", "Zoom In", "View", "Ctrl++"), () => Task.FromResult(Result.Success()))
            ]));
    }
}
