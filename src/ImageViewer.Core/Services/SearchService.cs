using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class SearchService : ISearchService
{
    private readonly Dictionary<int, IReadOnlyList<SearchIndexEntry>> _indexCache = [];

    public int CacheMissCount { get; private set; }

    public int CachedPlaylistCount => _indexCache.Count;

    public Result<List<ImageItem>> FilterPlaylist(IReadOnlyList<ImageItem> playlist, string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return Result<List<ImageItem>>.Success(playlist.ToList());
        }

        var normalizedQuery = searchQuery.ToLowerInvariant();
        var filtered = GetSearchIndex(playlist)
            .Where(entry => entry.NormalizedFilename.Contains(normalizedQuery, StringComparison.Ordinal))
            .Select(entry => entry.Item)
            .ToList();

        return Result<List<ImageItem>>.Success(filtered);
    }

    private IReadOnlyList<SearchIndexEntry> GetSearchIndex(IReadOnlyList<ImageItem> playlist)
    {
        var cacheKey = HashCode.Combine(playlist.Count, playlist.Count > 0 ? playlist[0].FilePath : string.Empty);
        if (_indexCache.TryGetValue(cacheKey, out var index))
        {
            return index;
        }

        CacheMissCount++;
        index = playlist
            .Select(item => new SearchIndexEntry(item, item.Filename.ToLowerInvariant()))
            .ToArray();
        _indexCache[cacheKey] = index;
        return index;
    }

    private sealed record SearchIndexEntry(ImageItem Item, string NormalizedFilename);
}
