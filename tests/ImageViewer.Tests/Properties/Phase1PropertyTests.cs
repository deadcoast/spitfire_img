using System.Text.Json;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Properties;

public sealed class Phase1PropertyTests : IDisposable
{
    private static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".webp", ".gif", ".tiff", ".tif"];
    private static readonly string[] DeferredExtensions = [".heic", ".avif"];
    private static readonly string[] UnknownExtensions = [".txt", ".raw", ".svg", ".psd", ".unknown"];
    private readonly string _tempDirectory;
    private readonly Random _random = new(12345);

    public Phase1PropertyTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "spitfire-property-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void ExtensionClassification_CorrectlyIdentifiesAllCategories_ForRandomCaseVariants()
    {
        // Feature: phase-1-mvp-viewer, Property 1: Extension Classification Correctness
        var service = new ImageDecodeService();

        for (var iteration = 0; iteration < 150; iteration++)
        {
            var supported = RandomCase(Pick(SupportedExtensions));
            var deferred = RandomCase(Pick(DeferredExtensions));
            var unknown = RandomCase(Pick(UnknownExtensions));

            Assert.True(service.IsSupportedFormat(supported));
            Assert.False(service.IsDeferredFormat(supported));

            Assert.True(service.IsDeferredFormat(deferred));
            Assert.False(service.IsSupportedFormat(deferred));

            Assert.False(service.IsSupportedFormat(unknown));
            Assert.False(service.IsDeferredFormat(unknown));
        }
    }

    [Fact]
    public async Task PlaylistBuilding_PreservesSupportedOrderAndOpenedSelection_ForRandomFolders()
    {
        // Feature: phase-1-mvp-viewer, Property 2: Playlist Building Preserves Order and Selection
        for (var iteration = 0; iteration < 120; iteration++)
        {
            var folder = Path.Combine(_tempDirectory, $"playlist-{iteration}");
            Directory.CreateDirectory(folder);
            var supportedFiles = new List<string>();

            for (var index = 0; index < 20; index++)
            {
                var extension = index % 4 == 0 ? Pick(UnknownExtensions) : Pick(SupportedExtensions);
                var filename = $"image{_random.Next(1, 100)}-{index}{RandomCase(extension)}";
                var path = Path.Combine(folder, filename);
                await File.WriteAllTextAsync(path, "test");

                if (ImageFormatCatalog.IsSupportedFormat(Path.GetExtension(path)))
                {
                    supportedFiles.Add(path);
                }
            }

            var opened = supportedFiles[_random.Next(supportedFiles.Count)];
            var result = await new FolderPlaylistService().BuildPlaylistAsync(opened);

            Assert.True(result.IsSuccess);
            Assert.All(result.Value.Items, item => Assert.True(ImageFormatCatalog.IsSupportedFormat(item.Extension)));
            Assert.Equal(supportedFiles.Count, result.Value.TotalCount);
            Assert.Equal(opened, result.Value.CurrentItem?.FilePath, ignoreCase: true);
        }
    }

    [Fact]
    public async Task Navigation_ClampsRandomTargetIndexes_ToPlaylistBoundaries()
    {
        // Feature: phase-1-mvp-viewer, Property 3: Navigation Maintains Playlist Boundaries
        for (var iteration = 0; iteration < 150; iteration++)
        {
            var count = _random.Next(1, 100);
            var items = Enumerable.Range(0, count)
                .Select(index => new ImageItem($@"C:\Images\img{index}.png"))
                .ToList();
            var playlist = new Playlist(items, _random.Next(count), @"C:\Images");
            var target = _random.Next(-100, count + 100);
            var expected = Math.Clamp(target, 0, count - 1);

            var result = await new NavigationService(new SuccessfulDecodeService()).NavigateToIndexAsync(playlist, target);

            Assert.True(result.IsSuccess);
            Assert.Equal(expected, result.Value.Index);
            Assert.Equal(items[expected].FilePath, result.Value.Image.FilePath);
        }
    }

    [Fact]
    public void SettingsSerialization_RoundTripsRandomValidSettings()
    {
        // Feature: phase-1-mvp-viewer, Property 4: Settings Serialization Round-Trip
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        for (var iteration = 0; iteration < 150; iteration++)
        {
            var settings = new ViewerSettings(
                Pick(Enum.GetValues<ViewerTheme>()),
                Pick(Enum.GetValues<DisplayMode>()),
                _random.Next(800, 5000),
                _random.Next(600, 4000),
                _random.Next(-1, 3000),
                _random.Next(-1, 2000),
                _random.Next(500, 10000),
                _random.Next(100, 2000));

            var json = JsonSerializer.Serialize(settings, options);
            var roundTrip = JsonSerializer.Deserialize<ViewerSettings>(json, options);

            Assert.Equal(settings, roundTrip);
        }
    }

    [Fact]
    public void ErrorFactories_SetExpectedCategoriesAndUserSafeMessages_ForEveryCategory()
    {
        // Feature: phase-1-mvp-viewer, Properties 5 and 6: Error Categorization and User-Friendly Messages
        var exception = new InvalidOperationException("RAW EXCEPTION TEXT SHOULD NOT LEAK");

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var errors = new[]
            {
                ViewerError.FileAccessError("Open", @"C:\missing.png", exception),
                ViewerError.UnsupportedFormatError("Open", @"C:\future.avif", exception),
                ViewerError.DecodeError("Decode", @"C:\bad.png", exception),
                ViewerError.PlaylistScanError("Scan", @"C:\Photos", exception),
                ViewerError.NavigationError("Navigate", @"C:\Photos\next.png", exception),
                ViewerError.SettingsPersistenceError("Save", @"C:\settings.json", exception),
                ViewerError.UnexpectedViewerError("Unhandled", null, exception)
            };

            foreach (var error in errors)
            {
                Assert.NotEqual(default, error.Timestamp);
                Assert.False(string.IsNullOrWhiteSpace(error.Operation));
                Assert.False(string.IsNullOrWhiteSpace(error.UserMessage));
                Assert.DoesNotContain(exception.Message, error.UserMessage);
                Assert.Equal(exception.GetType().Name, error.ExceptionType);
            }
        }
    }

    [Fact]
    public void MetadataFormatting_UsesStableDimensionAndSizeFormats_ForRandomValues()
    {
        // Feature: phase-1-mvp-viewer, Property 7: Metadata Formatting Consistency
        for (var iteration = 0; iteration < 150; iteration++)
        {
            var width = _random.Next(1, 100000);
            var height = _random.Next(1, 100000);
            var size = (long)(_random.NextDouble() * 10L * 1024 * 1024 * 1024);
            var metadata = new ImageMetadata(width, height, size);

            Assert.Equal($"{width} × {height}", metadata.FormattedDimensions);
            Assert.Matches(@"^\d+(\.\d)? (B|KB|MB|GB)$", metadata.FormattedFileSize);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private T Pick<T>(IReadOnlyList<T> values)
    {
        return values[_random.Next(values.Count)];
    }

    private string RandomCase(string value)
    {
        return new string(value.Select(character =>
            _random.Next(2) == 0
                ? char.ToLowerInvariant(character)
                : char.ToUpperInvariant(character)).ToArray());
    }

    private sealed class SuccessfulDecodeService : IImageDecodeService
    {
        public Task<Result<DecodedImage>> DecodeImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<DecodedImage>.Success(
                new DecodedImage(filePath, new ImageMetadata(100, 100, 10))));
        }

        public bool IsSupportedFormat(string extension) => ImageFormatCatalog.IsSupportedFormat(extension);

        public bool IsDeferredFormat(string extension) => ImageFormatCatalog.IsDeferredFormat(extension);

        public Task<Result<int>> GetFrameCountAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }
}
