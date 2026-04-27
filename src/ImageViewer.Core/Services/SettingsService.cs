using System.Text.Json;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _localDataFolder;

    public SettingsService(string localDataFolder)
    {
        _localDataFolder = localDataFolder;
    }

    private string SettingsPath => Path.Combine(_localDataFolder, "settings.json");

    public async Task<Result<ViewerSettings>> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return Result<ViewerSettings>.Success(ViewerSettings.Default);
            }

            await using var stream = File.OpenRead(SettingsPath);
            var settings = await JsonSerializer.DeserializeAsync<ViewerSettings>(
                    stream,
                    SerializerOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return settings is null
                ? Result<ViewerSettings>.Failure(ViewerError.SettingsPersistenceError("LoadSettings", SettingsPath))
                : Result<ViewerSettings>.Success(settings.Normalize());
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return Result<ViewerSettings>.Failure(
                ViewerError.SettingsPersistenceError("LoadSettings", SettingsPath, ex));
        }
    }

    public async Task<Result> SaveSettingsAsync(
        ViewerSettings settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_localDataFolder);
            await using var stream = File.Create(SettingsPath);
            await JsonSerializer.SerializeAsync(
                    stream,
                    settings.Normalize(),
                    SerializerOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return Result.Success();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return Result.Failure(ViewerError.SettingsPersistenceError("SaveSettings", SettingsPath, ex));
        }
    }
}
