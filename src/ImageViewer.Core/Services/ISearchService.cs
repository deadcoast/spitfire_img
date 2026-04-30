using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface ISearchService
{
    Result<List<ImageItem>> FilterPlaylist(IReadOnlyList<ImageItem> playlist, string searchQuery);
}
