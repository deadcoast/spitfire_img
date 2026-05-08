using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using System.Diagnostics;

namespace ImageViewer.Tests.Services;

public sealed class CommandPaletteServiceTests
{
    [Fact]
    public void GetAllCommands_ReturnsRegisteredCommandsWithShortcuts()
    {
        var command = new CommandItem("view.fullscreen", "Toggle Fullscreen", "View", "F11");
        var service = new CommandPaletteService([new CommandRegistration(command, () => Task.FromResult(Result.Success()))]);

        var result = service.GetAllCommands();

        Assert.True(result.IsSuccess);
        Assert.Equal(command, result.Value.Single());
        Assert.Equal("F11", result.Value.Single().KeyboardShortcut);
    }

    [Fact]
    public async Task ExecuteCommandAsync_RunsRegisteredCommand()
    {
        var executed = false;
        var command = new CommandItem("view.fullscreen", "Toggle Fullscreen", "View", "F11");
        var service = new CommandPaletteService(
            [new CommandRegistration(command, () =>
            {
                executed = true;
                return Task.FromResult(Result.Success());
            })]);

        var result = await service.ExecuteCommandAsync(command.Id);

        Assert.True(result.IsSuccess);
        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteCommandAsync_AddsSuccessfulCommandToRecentCommands()
    {
        var fullscreen = new CommandItem("view.fullscreen", "Toggle Fullscreen", "View", "F11");
        var rotateLeft = new CommandItem("image.rotate-left", "Rotate Left", "Image", "Ctrl+L");
        var service = new CommandPaletteService(
            [
                CreateRegistration(fullscreen),
                CreateRegistration(rotateLeft)
            ]);

        await service.ExecuteCommandAsync(fullscreen.Id);
        await service.ExecuteCommandAsync(rotateLeft.Id);
        await service.ExecuteCommandAsync(fullscreen.Id);

        var result = service.GetRecentCommands();

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value,
            command => Assert.Equal(fullscreen.Id, command.Id),
            command => Assert.Equal(rotateLeft.Id, command.Id));
    }

    [Fact]
    public async Task ExecuteCommandAsync_TrimsRecentCommandsToLastTen()
    {
        var registrations = Enumerable.Range(0, 12)
            .Select(index => CreateRegistration(new CommandItem(
                $"command.{index}",
                $"Command {index}",
                "View",
                $"Ctrl+{index}")))
            .ToArray();
        var service = new CommandPaletteService(registrations);

        foreach (var registration in registrations)
        {
            await service.ExecuteCommandAsync(registration.Command.Id);
        }

        var result = service.GetRecentCommands();

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.Count);
        Assert.Equal("command.11", result.Value[0].Id);
        Assert.Equal("command.2", result.Value[^1].Id);
    }

    [Fact]
    public async Task ExecuteCommandAsync_UnknownCommandReturnsCommandPaletteError()
    {
        var service = new CommandPaletteService([]);

        var result = await service.ExecuteCommandAsync("missing.command");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.CommandPaletteError, result.Error.Category);
    }

    [Fact]
    public void FilterCommands_MatchesCharactersInOrder()
    {
        var service = new CommandPaletteService(CreateSearchRegistrations());

        var rotateResult = service.FilterCommands("rot");
        var layoutResult = service.FilterCommands("fs");

        Assert.True(rotateResult.IsSuccess);
        Assert.Contains(rotateResult.Value, match => match.Command.Id == "image.rotate-left");
        Assert.Contains(rotateResult.Value, match => match.Command.Id == "image.rotate-right");
        Assert.DoesNotContain(rotateResult.Value, match => match.Command.Id == "image.flip-image");

        Assert.True(layoutResult.IsSuccess);
        Assert.Contains(layoutResult.Value, match => match.Command.Id == "view.fullscreen");
        Assert.Contains(layoutResult.Value, match => match.Command.Id == "layout.filmstrip");
    }

    [Fact]
    public void FilterCommands_RanksResultsByMatchQuality()
    {
        var service = new CommandPaletteService(
            [
                CreateRegistration(new CommandItem("view.zoom-mode", "Toggle Zoom Mode", "View", "Z")),
                CreateRegistration(new CommandItem("view.zoom-in", "Zoom In", "View", "Ctrl++")),
                CreateRegistration(new CommandItem("view.reset-zoom", "Reset Zoom", "View", "0"))
            ]);

        var result = service.FilterCommands("zoom");

        Assert.True(result.IsSuccess);
        Assert.Equal("view.zoom-in", result.Value[0].Command.Id);
        Assert.True(result.Value[0].Score > result.Value[1].Score);
    }

    [Fact]
    public void FilterCommands_ReturnsMatchedCharacterIndicesForHighlighting()
    {
        var service = new CommandPaletteService(CreateSearchRegistrations());

        var result = service.FilterCommands("rot");

        Assert.True(result.IsSuccess);
        var rotateLeft = result.Value.Single(match => match.Command.Id == "image.rotate-left");
        Assert.Equal([0, 1, 2], rotateLeft.MatchedTitleIndices);
    }

    [Fact]
    public void FilterCommands_EmptyQueryReturnsAllCommands()
    {
        var service = new CommandPaletteService(CreateSearchRegistrations());

        var result = service.FilterCommands(string.Empty);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
        Assert.All(result.Value, match => Assert.Empty(match.MatchedTitleIndices));
    }

    [Fact]
    public void FilterCommands_CategoryNameReturnsAllCommandsInThatCategory()
    {
        var service = new CommandPaletteService(CreateSearchRegistrations());

        var result = service.FilterCommands("image");

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value, match => match.Command.Id == "image.rotate-left");
        Assert.Contains(result.Value, match => match.Command.Id == "image.rotate-right");
        Assert.Contains(result.Value, match => match.Command.Id == "image.flip-image");
        Assert.DoesNotContain(result.Value, match => match.Command.Category != "Image");
    }

    [Fact]
    public void FilterCommands_ReturnsWithinFiftyMillisecondsForTwoHundredCommands()
    {
        var registrations = Enumerable.Range(0, 200)
            .Select(index => CreateRegistration(new CommandItem(
                $"command.{index}",
                $"Command {index:000} Rotate Fullscreen Filmstrip",
                "Generated",
                string.Empty)))
            .ToArray();
        var service = new CommandPaletteService(registrations);

        var stopwatch = Stopwatch.StartNew();
        var result = service.FilterCommands("c199");
        stopwatch.Stop();

        Assert.True(result.IsSuccess);
        Assert.True(
            stopwatch.ElapsedMilliseconds < 50,
            $"Expected fuzzy search under 50ms, but took {stopwatch.ElapsedMilliseconds}ms.");
    }

    [Fact]
    public void FilterCommands_RepeatedQueryReusesCachedFuzzyMatches()
    {
        var service = new CommandPaletteService(
            [
                CreateRegistration(new CommandItem("view.zoom-mode", "Toggle Zoom Mode", "View", "Z")),
                CreateRegistration(new CommandItem("view.zoom-in", "Zoom In", "View", "Ctrl++")),
                CreateRegistration(new CommandItem("view.reset-zoom", "Reset Zoom", "View", "0"))
            ]);

        var firstResult = service.FilterCommands("zoom");
        var secondResult = service.FilterCommands("zoom");

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Same(firstResult.Value[0], secondResult.Value[0]);
        Assert.Same(firstResult.Value[1], secondResult.Value[1]);
    }

    private static CommandRegistration[] CreateSearchRegistrations()
    {
        return
        [
            CreateRegistration(new CommandItem("image.rotate-left", "Rotate Left", "Image", "Ctrl+L")),
            CreateRegistration(new CommandItem("image.rotate-right", "Rotate Right", "Image", "Ctrl+R")),
            CreateRegistration(new CommandItem("image.flip-image", "Flip Image", "Image", "H")),
            CreateRegistration(new CommandItem("view.fullscreen", "Toggle Fullscreen", "View", "F11")),
            CreateRegistration(new CommandItem("layout.filmstrip", "Filmstrip Layout", "Layout", "F"))
        ];
    }

    private static CommandRegistration CreateRegistration(CommandItem command)
    {
        return new CommandRegistration(command, () => Task.FromResult(Result.Success()));
    }
}
