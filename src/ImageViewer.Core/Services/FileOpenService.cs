using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class FileOpenService : IFileOpenService
{
    private readonly ICodecDetectionService? _codecDetectionService;

    public FileOpenService(ICodecDetectionService? codecDetectionService = null)
    {
        _codecDetectionService = codecDetectionService;
    }

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
        if (IsHeicExtension(extension))
        {
            if (_codecDetectionService is null)
            {
                return Result<string>.Failure(ViewerError.UnsupportedFormatError("ValidateFilePath", filePath));
            }

            return IsHeicCodecAvailable()
                ? Result<string>.Success(filePath)
                : Result<string>.Failure(ViewerError.CodecUnavailableError("ValidateFilePath", filePath));
        }

        if (IsAvifExtension(extension))
        {
            if (_codecDetectionService is null)
            {
                return Result<string>.Failure(ViewerError.UnsupportedFormatError("ValidateFilePath", filePath));
            }

            return IsAvifCodecAvailable()
                ? Result<string>.Success(filePath)
                : Result<string>.Failure(ViewerError.CodecUnavailableError("ValidateFilePath", filePath));
        }

        if (!ImageFormatCatalog.IsSupportedFormat(extension))
        {
            return Result<string>.Failure(ViewerError.UnsupportedFormatError("ValidateFilePath", filePath));
        }

        return Result<string>.Success(filePath);
    }

    private bool IsHeicCodecAvailable()
    {
        var result = _codecDetectionService?.IsHeicSupported();
        return result is { IsSuccess: true, Value: true };
    }

    private bool IsAvifCodecAvailable()
    {
        var result = _codecDetectionService?.IsAvifSupported();
        return result is { IsSuccess: true, Value: true };
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
