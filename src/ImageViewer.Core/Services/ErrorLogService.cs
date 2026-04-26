using System.Text;
using System.Text.RegularExpressions;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed partial class ErrorLogService : IErrorLogService
{
    private const int RetentionDays = 30;
    private readonly string _localDataFolder;

    public ErrorLogService(string localDataFolder)
    {
        _localDataFolder = localDataFolder;
    }

    private string LogsDirectory => Path.Combine(_localDataFolder, "logs");

    public async Task<Result> LogErrorAsync(
        ViewerError error,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(LogsDirectory);
            var logPath = Path.Combine(LogsDirectory, $"viewer-{DateTimeOffset.Now:yyyy-MM-dd}.log");
            await File.AppendAllTextAsync(logPath, Format(error), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failure(ViewerError.SettingsPersistenceError("LogError", LogsDirectory, ex));
        }
    }

    public Task<Result> CleanupOldLogsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(LogsDirectory))
            {
                return Task.FromResult(Result.Success());
            }

            var cutoff = DateOnly.FromDateTime(DateTimeOffset.Now.AddDays(-RetentionDays).Date);
            foreach (var file in Directory.EnumerateFiles(LogsDirectory, "viewer-*.log"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var logDate = TryGetLogDate(file);
                if (logDate is not null && logDate < cutoff)
                {
                    File.Delete(file);
                }
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result.Failure(
                ViewerError.SettingsPersistenceError("CleanupOldLogs", LogsDirectory, ex)));
        }
    }

    private static string Format(ViewerError error)
    {
        var builder = new StringBuilder();
        builder.Append('[')
            .Append(error.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append("] [")
            .Append(error.Severity.ToString().ToUpperInvariant())
            .Append("] [")
            .Append(error.Category)
            .AppendLine("]");

        builder.Append("Operation: ").AppendLine(error.Operation);

        if (!string.IsNullOrWhiteSpace(error.FilePath))
        {
            builder.Append("File: ").AppendLine(error.FilePath);
        }

        builder.Append("Message: ").AppendLine(error.UserMessage);

        if (!string.IsNullOrWhiteSpace(error.ExceptionType))
        {
            builder.Append("Exception: ").AppendLine(error.ExceptionType);
        }

        if (!string.IsNullOrWhiteSpace(error.StackTrace))
        {
            builder.AppendLine("Stack Trace:");
            builder.AppendLine(error.StackTrace);
        }

        builder.AppendLine("---");
        return builder.ToString();
    }

    private static DateOnly? TryGetLogDate(string filePath)
    {
        var match = LogFileRegex().Match(Path.GetFileName(filePath));
        return match.Success && DateOnly.TryParse(match.Groups["date"].Value, out var date)
            ? date
            : null;
    }

    [GeneratedRegex(@"^viewer-(?<date>\d{4}-\d{2}-\d{2})\.log$", RegexOptions.CultureInvariant)]
    private static partial Regex LogFileRegex();
}
