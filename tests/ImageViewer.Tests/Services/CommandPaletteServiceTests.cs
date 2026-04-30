using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class CommandPaletteServiceTests
{
    [Fact]
    public void GetRegisteredCommands_IncludesCommandsFromViewerPhasesWithShortcuts()
    {
        var service = new CommandPaletteService();

        var commands = service.GetRegisteredCommands();

        Assert.Contains(commands, command => command.Id == CommandIds.OpenFile && command.Shortcut == "Ctrl+O");
        Assert.Contains(commands, command => command.Id == CommandIds.RotateLeft && command.Category == CommandCategory.Image);
        Assert.Contains(commands, command => command.Id == CommandIds.ToggleSlideshow && command.Category == CommandCategory.Slideshow);
        Assert.Contains(commands, command => command.Id == CommandIds.EnterComparison && command.Category == CommandCategory.Comparison);
        Assert.All(commands, command => Assert.False(string.IsNullOrWhiteSpace(command.Shortcut)));
    }

    [Fact]
    public void SearchCommands_MatchesCharactersInOrderAndHighlightsMatchedIndexes()
    {
        var service = new CommandPaletteService();

        var results = service.SearchCommands("rot");

        var rotateLeft = Assert.Single(results, result => result.Command.Id == CommandIds.RotateLeft);
        Assert.Equal([0, 1, 2], rotateLeft.MatchIndexes);
        Assert.DoesNotContain(results, result => result.Command.Title.Contains("Export", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SearchCommands_ShowsExpectedShortcutStyleMatchesAndRanksByScore()
    {
        var service = new CommandPaletteService();

        var results = service.SearchCommands("fs");

        Assert.Contains(results, result => result.Command.Id == CommandIds.ToggleFullscreen);
        Assert.Contains(results, result => result.Command.Id == CommandIds.FilmstripLayout);
        Assert.Equal(results.Select(result => result.Score), results.Select(result => result.Score).OrderByDescending(score => score));
    }

    [Fact]
    public void SearchCommands_EmptyQueryReturnsAllCommands()
    {
        var service = new CommandPaletteService();

        var results = service.SearchCommands(string.Empty);

        Assert.Equal(service.GetRegisteredCommands().Count, results.Count);
    }

    [Fact]
    public void SearchCommands_WithNoMatchesReturnsEmptyResult()
    {
        var service = new CommandPaletteService();

        var results = service.SearchCommands("zzzzzz");

        Assert.Empty(results);
    }

    [Fact]
    public void SearchCommands_ReturnsResultsWithinFiftyMilliseconds_ForTwoHundredCommands()
    {
        var commands = Enumerable.Range(0, 220)
            .Select(index => new CommandItem(
                $"test.command.{index}",
                $"Rotate Test Command {index}",
                CommandCategory.Image,
                $"Ctrl+{index}"))
            .ToArray();
        var service = new CommandPaletteService(commands);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = service.SearchCommands("rot");
        stopwatch.Stop();

        Assert.Equal(220, results.Count);
        Assert.InRange(stopwatch.ElapsedMilliseconds, 0, 50);
    }

    [Fact]
    public void SearchCommands_WhenQueryIsCategoryName_ReturnsCommandsInThatCategory()
    {
        var service = new CommandPaletteService();

        var results = service.SearchCommands("Comparison");

        Assert.NotEmpty(results);
        Assert.All(results, result => Assert.Equal(CommandCategory.Comparison, result.Command.Category));
    }
}
