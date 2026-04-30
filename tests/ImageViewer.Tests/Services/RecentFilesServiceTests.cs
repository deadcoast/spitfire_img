using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class RecentFilesServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public RecentFilesServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "spitfire-recent-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task AddRecentFileAsync_NewFile_AddsToTop()
    {
        var service = new RecentFilesService(_tempDirectory);

        await service.AddRecentFileAsync(@"C:\Images\a.png");
        await service.AddRecentFileAsync(@"C:\Images\b.png");
        var result = await service.LoadRecentFilesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(@"C:\Images\b.png", result.Value[0].FilePath);
        Assert.Equal(@"C:\Images\a.png", result.Value[1].FilePath);
    }

    [Fact]
    public async Task AddRecentFileAsync_Duplicate_MovesExistingEntryToTopOnce()
    {
        var service = new RecentFilesService(_tempDirectory);

        await service.AddRecentFileAsync(@"C:\Images\a.png");
        await service.AddRecentFileAsync(@"C:\Images\b.png");
        await service.AddRecentFileAsync(@"C:\Images\a.png");
        var result = await service.LoadRecentFilesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(@"C:\Images\a.png", result.Value[0].FilePath);
        Assert.Single(result.Value.Where(file => file.FilePath == @"C:\Images\a.png"));
    }

    [Fact]
    public async Task AddRecentFileAsync_ExceedsTwenty_RemovesOldestEntry()
    {
        var service = new RecentFilesService(_tempDirectory);

        for (var index = 0; index < 21; index++)
        {
            await service.AddRecentFileAsync($@"C:\Images\{index}.png");
        }

        var result = await service.LoadRecentFilesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.Count);
        Assert.Equal(@"C:\Images\20.png", result.Value[0].FilePath);
        Assert.DoesNotContain(result.Value, file => file.FilePath == @"C:\Images\0.png");
    }

    [Fact]
    public async Task LoadRecentFilesAsync_CorruptedJson_ReturnsEmptyList()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDirectory, "recent-files.json"), "{ not json");
        var service = new RecentFilesService(_tempDirectory);

        var result = await service.LoadRecentFilesAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
