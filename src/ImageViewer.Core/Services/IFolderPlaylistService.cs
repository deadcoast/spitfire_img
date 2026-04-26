using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IFolderPlaylistService
{
    Task<Result<Playlist>> BuildPlaylistAsync(string openedFilePath, CancellationToken cancellationToken = default);
}
