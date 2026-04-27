using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class SearchService : ISearchService
{
    public Result<List<ImageItem>> FilterPlaylist(IReadOnlyList<ImageItem> playlist, string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return Result<List<ImageItem>>.Success(playlist.ToList());
        }

        var filtered = playlist
            .Where(item => item.Filename.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Result<List<ImageItem>>.Success(filtered);
    }
}
