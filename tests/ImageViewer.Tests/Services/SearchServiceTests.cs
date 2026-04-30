using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class SearchServiceTests
{
    [Fact]
    public void FilterPlaylist_EmptyQuery_ReturnsFullPlaylist()
    {
        var playlist = CreatePlaylist("cat.png", "dog.jpg");

        var result = new SearchService().FilterPlaylist(playlist, string.Empty);

        Assert.True(result.IsSuccess);
        Assert.Equal(playlist, result.Value);
    }

    [Fact]
    public void FilterPlaylist_Query_ReturnsCaseInsensitiveFilenameMatchesInOriginalOrder()
    {
        var playlist = CreatePlaylist("Beach-001.png", "mountain.jpg", "beach-002.webp");

        var result = new SearchService().FilterPlaylist(playlist, "BEACH");

        Assert.True(result.IsSuccess);
        Assert.Equal(["Beach-001.png", "beach-002.webp"], result.Value.Select(item => item.Filename).ToArray());
    }

    [Fact]
    public void FilterPlaylist_QueryWithUnicodeAndSpecialCharacters_MatchesFilename()
    {
        var playlist = CreatePlaylist("résumé photo.png", "family (copy).jpg");

        var result = new SearchService().FilterPlaylist(playlist, "résumé");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("résumé photo.png", result.Value[0].Filename);
    }

    [Fact]
    public void FilterPlaylist_Property_AllResultsContainQuery()
    {
        var random = new Random(1234);
        var service = new SearchService();

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var playlist = Enumerable.Range(0, random.Next(10, 100))
                .Select(index => new ImageItem($@"C:\Images\{(index % 3 == 0 ? "target" : "other")}-{index}.png"))
                .ToList();

            var result = service.FilterPlaylist(playlist, "TARGET");

            Assert.True(result.IsSuccess);
            Assert.All(result.Value, item => Assert.Contains("target", item.Filename, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(playlist, service.FilterPlaylist(playlist, "").Value);
        }
    }

    [Fact]
    public void FilterPlaylist_RepeatedQueriesAgainstSamePlaylist_ReuseCachedFilenames()
    {
        var playlist = CreatePlaylist("Alpha.png", "beta.jpg", "alphabet.webp");
        var service = new SearchService();

        service.FilterPlaylist(playlist, "alpha");
        service.FilterPlaylist(playlist, "ALPHA");

        Assert.Equal(1, service.CacheMissCount);
        Assert.Equal(1, service.CachedPlaylistCount);
    }

    private static List<ImageItem> CreatePlaylist(params string[] filenames)
    {
        return filenames.Select(filename => new ImageItem($@"C:\Images\{filename}")).ToList();
    }
}
