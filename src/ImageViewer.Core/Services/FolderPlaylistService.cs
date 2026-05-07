using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class FolderPlaylistService : IFolderPlaylistService
{
    private readonly Func<string, bool> _isSupportedFormat;

    public FolderPlaylistService()
        : this(ImageFormatCatalog.IsSupportedFormat)
    {
    }

    public FolderPlaylistService(IImageDecodeService imageDecodeService)
        : this(imageDecodeService.IsSupportedFormat)
    {
    }

    private FolderPlaylistService(Func<string, bool> isSupportedFormat)
    {
        _isSupportedFormat = isSupportedFormat;
    }

    public Task<Result<Playlist>> BuildPlaylistAsync(
        string openedFilePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var folderPath = Path.GetDirectoryName(openedFilePath);
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return Task.FromResult(Result<Playlist>.Failure(
                    ViewerError.PlaylistScanError("BuildPlaylist", openedFilePath)));
            }

            var files = Directory.EnumerateFiles(folderPath)
                .Where(path => _isSupportedFormat(Path.GetExtension(path)))
                .Select(path => new ImageItem(path))
                .OrderBy(item => item.Filename, NaturalStringComparer.OrdinalIgnoreCase)
                .ToList();

            var currentIndex = files.FindIndex(item =>
                string.Equals(item.FilePath, openedFilePath, StringComparison.OrdinalIgnoreCase));

            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            var playlist = new Playlist(files, currentIndex, folderPath);
            return Task.FromResult(Result<Playlist>.Success(playlist));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result<Playlist>.Failure(
                ViewerError.PlaylistScanError("BuildPlaylist", openedFilePath, ex)));
        }
    }
}
