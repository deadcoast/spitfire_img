using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public SettingsServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "spitfire-settings-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task LoadSettingsAsync_MissingFile_ReturnsDefaultSettings()
    {
        var service = new SettingsService(_tempDirectory);

        var result = await service.LoadSettingsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(ViewerSettings.Default, result.Value);
    }

    [Fact]
    public async Task LoadSettingsAsync_OlderPartialSettings_ReturnsNormalizedSettings()
    {
        var settingsPath = Path.Combine(_tempDirectory, "settings.json");
        await File.WriteAllTextAsync(
            settingsPath,
            """
            {
              "theme": 1,
              "defaultDisplayMode": 0,
              "windowWidth": 320,
              "windowHeight": 200,
              "windowX": -1,
              "windowY": -1,
              "fadeDelayMs": 10,
              "fadeDurationMs": 10
            }
            """);
        var service = new SettingsService(_tempDirectory);

        var result = await service.LoadSettingsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(ViewerTheme.Light, result.Value.Theme);
        Assert.Equal(ViewerSettings.MinimumWindowWidth, result.Value.WindowWidth);
        Assert.Equal(ViewerSettings.MinimumWindowHeight, result.Value.WindowHeight);
        Assert.Equal(ViewerSettings.MinimumFadeDelayMs, result.Value.FadeDelayMs);
        Assert.Equal(ViewerSettings.MinimumFadeDurationMs, result.Value.FadeDurationMs);
        Assert.Equal(SlideshowConfig.DefaultInterval, result.Value.SlideshowInterval);
        Assert.Equal(LayoutMode.Single, result.Value.PreferredLayout);
        Assert.Equal(LayoutConfig.DefaultGridRows, result.Value.PreferredGridRows);
        Assert.Equal(LayoutConfig.DefaultGridColumns, result.Value.PreferredGridColumns);
        Assert.Equal(LayoutConfig.DefaultFilmstripThumbnailCount, result.Value.PreferredFilmstripThumbnailCount);
        Assert.True(result.Value.ComparisonSyncZoom);
        Assert.True(result.Value.GifAutoPlay);
    }

    [Fact]
    public async Task SaveThenLoadSettingsAsync_RoundTripsSettings()
    {
        var service = new SettingsService(_tempDirectory);
        var settings = new ViewerSettings(
            ViewerTheme.Dark,
            DisplayMode.ActualSize,
            1400,
            900,
            10,
            20,
            2500,
            350,
            SlideshowInterval: TimeSpan.FromSeconds(5),
            PreferredLayout: LayoutMode.Grid,
            PreferredGridRows: 4,
            PreferredGridColumns: 4,
            PreferredFilmstripThumbnailCount: 9,
            ComparisonSyncZoom: false,
            GifAutoPlay: false);

        var saveResult = await service.SaveSettingsAsync(settings);
        var loadResult = await service.LoadSettingsAsync();

        Assert.True(saveResult.IsSuccess);
        Assert.True(loadResult.IsSuccess);
        Assert.Equal(settings, loadResult.Value);
    }

    [Fact]
    public async Task LoadSettingsAsync_CorruptedJson_ReturnsSettingsPersistenceError()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDirectory, "settings.json"), "{ not json");
        var service = new SettingsService(_tempDirectory);

        var result = await service.LoadSettingsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.SettingsPersistenceError, result.Error.Category);
        Assert.Equal(ErrorSeverity.Warning, result.Error.Severity);
        Assert.Equal("LoadSettings", result.Error.Operation);
        Assert.Equal(nameof(System.Text.Json.JsonException), result.Error.ExceptionType);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
