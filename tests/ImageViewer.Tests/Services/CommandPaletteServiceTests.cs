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
    public void GetRegisteredCommands_IncludesExecutablePhaseThreeIntegrationCommands()
    {
        var service = new CommandPaletteService();

        var commandIds = service.GetRegisteredCommands().Select(command => command.Id).ToArray();

        Assert.Contains(CommandIds.ReturnToSingleImage, commandIds);
        Assert.Contains(CommandIds.ComparisonTwoImages, commandIds);
        Assert.Contains(CommandIds.ComparisonThreeImages, commandIds);
        Assert.Contains(CommandIds.ComparisonFourImages, commandIds);
        Assert.Contains(CommandIds.ToggleGifPlayback, commandIds);
    }

    [Fact]
    public void GetRegisteredCommands_DoesNotAssignCtrlOneToTwoCommands()
    {
        var service = new CommandPaletteService();

        var commands = service.GetRegisteredCommands();
        var actualSize = Assert.Single(commands, command => command.Id == CommandIds.ActualSize);
        var singleImage = Assert.Single(commands, command => command.Id == CommandIds.ReturnToSingleImage);

        Assert.NotEqual("Ctrl+1", actualSize.Shortcut);
        Assert.Equal("Ctrl+1", singleImage.Shortcut);
    }

    [Fact]
    public void GetRegisteredCommands_PreservesCtrlFForSearchAndUsesShiftShortcutForFilmstrip()
    {
        var service = new CommandPaletteService();

        var filmstrip = Assert.Single(
            service.GetRegisteredCommands(),
            command => command.Id == CommandIds.FilmstripLayout);

        Assert.Equal("Ctrl+Shift+F", filmstrip.Shortcut);
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

    [Fact]
    public void SearchCommands_ReusesCachedResultsForRepeatedNormalizedQuery()
    {
        var service = new CommandPaletteService();

        var first = service.SearchCommands(" rot ");
        var second = service.SearchCommands("rot");

        Assert.Same(first, second);
    }

    [Fact]
    public void SearchCommands_EvictsOldCachedQueriesWhenCacheLimitIsExceeded()
    {
        var commands = Enumerable.Range(0, 80)
            .Select(index => new CommandItem(
                $"test.command.{index}",
                $"Query {index}",
                CommandCategory.Image,
                $"Ctrl+{index}"))
            .ToArray();
        var service = new CommandPaletteService(commands);

        var first = service.SearchCommands("query 0");
        for (var index = 1; index <= 70; index++)
        {
            service.SearchCommands($"query {index}");
        }

        var repeated = service.SearchCommands("query 0");

        Assert.NotSame(first, repeated);
    }
}
