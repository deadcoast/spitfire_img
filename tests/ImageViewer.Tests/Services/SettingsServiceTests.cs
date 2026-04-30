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
            350)
        {
            PreferredLayout = LayoutMode.Grid,
            GridRows = 4,
            GridColumns = 5,
            ComparisonSyncZoom = false,
            GifAutoPlay = false
        };

        var saveResult = await service.SaveSettingsAsync(settings);
        var loadResult = await service.LoadSettingsAsync();

        Assert.True(saveResult.IsSuccess);
        Assert.True(loadResult.IsSuccess);
        Assert.Equal(settings, loadResult.Value);
    }

    [Fact]
    public async Task SaveThenLoadSettingsAsync_RoundTripsAllPhaseThreePreferences()
    {
        var service = new SettingsService(_tempDirectory);
        var settings = ViewerSettings.Default with
        {
            SlideshowIntervalSeconds = 15,
            SlideshowTransition = TransitionEffect.Zoom,
            PreferredLayout = LayoutMode.Filmstrip,
            GridRows = 6,
            GridColumns = 4,
            ComparisonSyncZoom = false,
            GifAutoPlay = false
        };

        var saveResult = await service.SaveSettingsAsync(settings);
        var loadResult = await service.LoadSettingsAsync();

        Assert.True(saveResult.IsSuccess);
        Assert.True(loadResult.IsSuccess);
        Assert.Equal(15, loadResult.Value.SlideshowIntervalSeconds);
        Assert.Equal(TransitionEffect.Zoom, loadResult.Value.SlideshowTransition);
        Assert.Equal(LayoutMode.Filmstrip, loadResult.Value.PreferredLayout);
        Assert.Equal(6, loadResult.Value.GridRows);
        Assert.Equal(4, loadResult.Value.GridColumns);
        Assert.False(loadResult.Value.ComparisonSyncZoom);
        Assert.False(loadResult.Value.GifAutoPlay);
    }

    [Fact]
    public async Task LoadSettingsAsync_CorruptedJson_ReturnsSettingsPersistenceError()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDirectory, "settings.json"), "{ not json");
        var service = new SettingsService(_tempDirectory);

        var result = await service.LoadSettingsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.SettingsPersistenceError, result.Error.Category);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
