using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.App.Services;

public sealed class WindowsImageDecodeService : IImageDecodeService
{
    private readonly ICodecDetectionService _codecDetectionService;
    private readonly IWindowsImageMetadataReader _metadataReader;
    private readonly ImageDecodeService _fallbackDecodeService;

    public WindowsImageDecodeService(
        ICodecDetectionService codecDetectionService,
        IWindowsImageMetadataReader metadataReader,
        ImageDecodeService? fallbackDecodeService = null)
    {
        _codecDetectionService = codecDetectionService;
        _metadataReader = metadataReader;
        _fallbackDecodeService = fallbackDecodeService ?? new ImageDecodeService();
    }

    public async Task<Result<DecodedImage>> DecodeImageAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!IsCodecDependentFile(filePath))
        {
            return await _fallbackDecodeService.DecodeImageAsync(filePath, cancellationToken).ConfigureAwait(false);
        }

        if (!IsCodecAvailableForFile(filePath))
        {
            return Result<DecodedImage>.Failure(ViewerError.CodecUnavailableError("DecodeImage", filePath));
        }

        var metadataResult = await _metadataReader.ReadMetadataAsync(filePath, cancellationToken).ConfigureAwait(false);
        return metadataResult.IsSuccess
            ? Result<DecodedImage>.Success(new DecodedImage(filePath, metadataResult.Value))
            : Result<DecodedImage>.Failure(metadataResult.Error);
    }

    public bool IsSupportedFormat(string extension)
    {
        if (IsHeicExtension(extension))
        {
            return IsHeicCodecAvailable();
        }

        if (IsAvifExtension(extension))
        {
            return IsAvifCodecAvailable();
        }

        return _fallbackDecodeService.IsSupportedFormat(extension);
    }

    public bool IsDeferredFormat(string extension)
    {
        return _fallbackDecodeService.IsDeferredFormat(extension);
    }

    public Task<Result<int>> GetFrameCountAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!IsCodecDependentFile(filePath))
        {
            return _fallbackDecodeService.GetFrameCountAsync(filePath, cancellationToken);
        }

        return Task.FromResult(IsCodecAvailableForFile(filePath)
            ? Result<int>.Success(1)
            : Result<int>.Failure(ViewerError.CodecUnavailableError("GetFrameCount", filePath)));
    }

    private bool IsHeicCodecAvailable()
    {
        var result = _codecDetectionService.IsHeicSupported();
        return result is { IsSuccess: true, Value: true };
    }

    private bool IsAvifCodecAvailable()
    {
        var result = _codecDetectionService.IsAvifSupported();
        return result is { IsSuccess: true, Value: true };
    }

    private bool IsCodecAvailableForFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (IsHeicExtension(extension))
        {
            return IsHeicCodecAvailable();
        }

        if (IsAvifExtension(extension))
        {
            return IsAvifCodecAvailable();
        }

        return false;
    }

    private static bool IsCodecDependentFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return IsHeicExtension(extension) || IsAvifExtension(extension);
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
