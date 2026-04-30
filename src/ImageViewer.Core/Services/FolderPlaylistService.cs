using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class FolderPlaylistService : IFolderPlaylistService
{
    private readonly ICodecDetectionService? _codecDetectionService;

    public FolderPlaylistService(ICodecDetectionService? codecDetectionService = null)
    {
        _codecDetectionService = codecDetectionService;
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
                .Where(IsPlaylistFormat)
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

    private bool IsPlaylistFormat(string path)
    {
        var extension = Path.GetExtension(path);
        if (ImageFormatCatalog.IsSupportedFormat(extension))
        {
            return true;
        }

        if (IsHeicExtension(extension))
        {
            var result = _codecDetectionService?.IsHeicSupported();
            return result is { IsSuccess: true, Value: true };
        }

        if (IsAvifExtension(extension))
        {
            var result = _codecDetectionService?.IsAvifSupported();
            return result is { IsSuccess: true, Value: true };
        }

        return false;
    }

    private static bool IsHeicExtension(string? extension)
    {
        return extension is not null
            && (extension.Equals(".heic", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".heif", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAvifExtension(string? extension)
    {
        return extension is not null
            && extension.Equals(".avif", StringComparison.OrdinalIgnoreCase);
    }
}
