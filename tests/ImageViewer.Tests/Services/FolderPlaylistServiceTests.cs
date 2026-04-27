using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class FolderPlaylistServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly FolderPlaylistService _service = new();

    public FolderPlaylistServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "spitfire-playlist-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task BuildPlaylistAsync_SingleImageFolder_ReturnsOneItemPlaylist()
    {
        var opened = CreateFile("image.png");

        var result = await _service.BuildPlaylistAsync(opened);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(0, result.Value.CurrentIndex);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(_tempDirectory, result.Value.FolderPath);
    }

    [Fact]
    public async Task BuildPlaylistAsync_FiltersUnsupportedAndDeferredFormats()
    {
        var opened = CreateFile("b.jpg");
        CreateFile("a.png");
        CreateFile("notes.txt");
        CreateFile("future.gif");

        var result = await _service.BuildPlaylistAsync(opened);

        Assert.True(result.IsSuccess);
        Assert.Equal(["a.png", "b.jpg"], result.Value.Items.Select(item => item.Filename).ToArray());
        Assert.Equal(1, result.Value.CurrentIndex);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task BuildPlaylistAsync_UsesCaseInsensitiveNaturalSort()
    {
        var opened = CreateFile("img10.jpg");
        CreateFile("IMG2.jpg");
        CreateFile("img1.jpg");

        var result = await _service.BuildPlaylistAsync(opened);

        Assert.True(result.IsSuccess);
        Assert.Equal(["img1.jpg", "IMG2.jpg", "img10.jpg"], result.Value.Items.Select(item => item.Filename).ToArray());
        Assert.Equal(2, result.Value.CurrentIndex);
    }

    [Fact]
    public async Task BuildPlaylistAsync_SpecialCharactersAndLargeFolder_ReturnsSupportedFilesOnly()
    {
        var opened = CreateFile("vacation 050.png");
        CreateFile("résumé-001.jpg");
        CreateFile("family photo (2).bmp");

        for (var index = 0; index < 1000; index++)
        {
            CreateFile($"bulk-{index:0000}.png");
            CreateFile($"bulk-{index:0000}.txt");
        }

        var result = await _service.BuildPlaylistAsync(opened);

        Assert.True(result.IsSuccess);
        Assert.Equal(1003, result.Value.TotalCount);
        Assert.Contains(result.Value.Items, item => item.Filename == "résumé-001.jpg");
        Assert.Contains(result.Value.Items, item => item.Filename == "family photo (2).bmp");
        Assert.All(result.Value.Items, item => Assert.True(ImageFormatCatalog.IsSupportedFormat(item.Extension)));
    }

    [Fact]
    public async Task BuildPlaylistAsync_MissingFolder_ReturnsPlaylistScanError()
    {
        var missing = Path.Combine(_tempDirectory, "missing", "image.png");

        var result = await _service.BuildPlaylistAsync(missing);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.PlaylistScanError, result.Error.Category);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private string CreateFile(string filename)
    {
        var path = Path.Combine(_tempDirectory, filename);
        File.WriteAllText(path, "test");
        return path;
    }
}
