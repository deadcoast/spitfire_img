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
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
