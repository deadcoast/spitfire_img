using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class ErrorLogServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public ErrorLogServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "spitfire-error-log-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task LogErrorAsync_CreatesDailyLogFileWithStructuredEntry()
    {
        var service = new ErrorLogService(_tempDirectory);
        var error = ViewerError.DecodeError("DecodeImage", @"C:\Photos\bad.png", new InvalidDataException("bad image"));

        var result = await service.LogErrorAsync(error);

        Assert.True(result.IsSuccess);
        var logPath = Path.Combine(_tempDirectory, "logs", $"viewer-{DateTimeOffset.Now:yyyy-MM-dd}.log");
        Assert.True(File.Exists(logPath));
        var log = await File.ReadAllTextAsync(logPath);
        Assert.Contains("[DecodeError]", log);
        Assert.Contains("Operation: DecodeImage", log);
        Assert.Contains(@"File: C:\Photos\bad.png", log);
        Assert.Contains("Message: Unable to decode this image.", log);
        Assert.Contains("Exception: InvalidDataException", log);
    }

    [Fact]
    public async Task CleanupOldLogsAsync_DeletesLogsOlderThanRetentionWindow()
    {
        var logsDirectory = Path.Combine(_tempDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);
        var oldLog = Path.Combine(logsDirectory, $"viewer-{DateTimeOffset.Now.AddDays(-45):yyyy-MM-dd}.log");
        var recentLog = Path.Combine(logsDirectory, $"viewer-{DateTimeOffset.Now.AddDays(-5):yyyy-MM-dd}.log");
        await File.WriteAllTextAsync(oldLog, "old");
        await File.WriteAllTextAsync(recentLog, "recent");
        var service = new ErrorLogService(_tempDirectory);

        await service.CleanupOldLogsAsync();

        Assert.False(File.Exists(oldLog));
        Assert.True(File.Exists(recentLog));
    }

    [Fact]
    public async Task LogErrorAsync_Phase3Errors_RecordsCategoryAndActionableContext()
    {
        var service = new ErrorLogService(_tempDirectory);
        var codecError = ViewerError.CodecUnavailableError("HEIC", "HEIF Image Extensions");
        var layoutError = ViewerError.LayoutError("EnterGrid", new InvalidOperationException("raw layout failure"));

        Assert.True((await service.LogErrorAsync(codecError)).IsSuccess);
        Assert.True((await service.LogErrorAsync(layoutError)).IsSuccess);

        var logPath = Path.Combine(_tempDirectory, "logs", $"viewer-{DateTimeOffset.Now:yyyy-MM-dd}.log");
        var log = await File.ReadAllTextAsync(logPath);
        Assert.Contains("[CodecUnavailableError]", log);
        Assert.Contains("HEIC", log);
        Assert.Contains("HEIF Image Extensions", log);
        Assert.Contains("[LayoutError]", log);
        Assert.Contains("Operation: EnterGrid", log);
        Assert.Contains("Message: Unable to load custom layout.", log);
        Assert.Contains("Exception: InvalidOperationException", log);
        Assert.DoesNotContain("raw layout failure", log);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
