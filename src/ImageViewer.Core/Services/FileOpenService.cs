using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class FileOpenService : IFileOpenService
{
    public Result<string> ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return Result<string>.Failure(ViewerError.FileAccessError("ValidateFilePath", filePath));
        }

        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result<string>.Failure(ViewerError.FileAccessError("ValidateFilePath", filePath, ex));
        }

        var extension = Path.GetExtension(filePath);
        if (!ImageFormatCatalog.IsSupportedFormat(extension))
        {
            return Result<string>.Failure(ViewerError.UnsupportedFormatError("ValidateFilePath", filePath));
        }

        return Result<string>.Success(filePath);
    }
}
