using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IThumbnailService
{
    Task<Result<Thumbnail>> GetThumbnailAsync(ImageItem item, CancellationToken cancellationToken = default);

    void ClearCache();

    long GetCacheSize();
}
