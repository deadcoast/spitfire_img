using System.Text.Json;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class RecentFilesService : IRecentFilesService
{
    private const int MaxRecentFiles = 20;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _localDataFolder;

    public RecentFilesService(string localDataFolder)
    {
        _localDataFolder = localDataFolder;
    }

    private string RecentFilesPath => Path.Combine(_localDataFolder, "recent-files.json");

    public async Task<Result<IReadOnlyList<RecentFile>>> LoadRecentFilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(RecentFilesPath))
            {
                return Result<IReadOnlyList<RecentFile>>.Success([]);
            }

            await using var stream = File.OpenRead(RecentFilesPath);
            var recentFiles = await JsonSerializer.DeserializeAsync<List<RecentFile>>(
                    stream,
                    SerializerOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return Result<IReadOnlyList<RecentFile>>.Success(Normalize(recentFiles ?? []));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return Result<IReadOnlyList<RecentFile>>.Success([]);
        }
    }

    public async Task<Result<IReadOnlyList<RecentFile>>> AddRecentFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var loadResult = await LoadRecentFilesAsync(cancellationToken).ConfigureAwait(false);
        var recentFiles = loadResult.Value.ToList();
        recentFiles.RemoveAll(file => string.Equals(file.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        recentFiles.Insert(0, new RecentFile(filePath, DateTimeOffset.UtcNow));

        if (recentFiles.Count > MaxRecentFiles)
        {
            recentFiles.RemoveRange(MaxRecentFiles, recentFiles.Count - MaxRecentFiles);
        }

        await SaveAsync(recentFiles, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<RecentFile>>.Success(recentFiles);
    }

    public async Task<Result<IReadOnlyList<RecentFile>>> RemoveRecentFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var loadResult = await LoadRecentFilesAsync(cancellationToken).ConfigureAwait(false);
        var recentFiles = loadResult.Value.ToList();
        recentFiles.RemoveAll(file => string.Equals(file.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        await SaveAsync(recentFiles, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<RecentFile>>.Success(recentFiles);
    }

    public Task<Result> ClearRecentFilesAsync(CancellationToken cancellationToken = default)
    {
        return SaveAsync([], cancellationToken);
    }

    private async Task<Result> SaveAsync(IReadOnlyList<RecentFile> recentFiles, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(_localDataFolder);
            await using var stream = File.Create(RecentFilesPath);
            await JsonSerializer.SerializeAsync(stream, recentFiles, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return Result.Failure(ViewerError.SettingsPersistenceError("SaveRecentFiles", RecentFilesPath, ex));
        }
    }

    private static List<RecentFile> Normalize(IEnumerable<RecentFile> recentFiles)
    {
        return recentFiles
            .Where(file => !string.IsNullOrWhiteSpace(file.FilePath))
            .GroupBy(file => file.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(file => file.LastOpened).First())
            .OrderByDescending(file => file.LastOpened)
            .Take(MaxRecentFiles)
            .ToList();
    }
}
