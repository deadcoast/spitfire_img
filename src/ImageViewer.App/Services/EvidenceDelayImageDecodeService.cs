using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.App.Services;

public sealed class EvidenceDelayImageDecodeService : IImageDecodeService
{
    public const string DelayEnvironmentVariable = "SPITFIRE_EVIDENCE_IMAGE_DECODE_DELAY_MS";

    private readonly IImageDecodeService _inner;
    private readonly TimeSpan _delay;

    private EvidenceDelayImageDecodeService(IImageDecodeService inner, TimeSpan delay)
    {
        _inner = inner;
        _delay = delay;
    }

    public static IImageDecodeService CreateIfConfigured(IImageDecodeService inner)
    {
        return CreateIfConfigured(
            inner,
            Environment.GetEnvironmentVariable(DelayEnvironmentVariable));
    }

    public static IImageDecodeService CreateIfConfigured(IImageDecodeService inner, string? delayMilliseconds)
    {
        if (!int.TryParse(delayMilliseconds, out var parsedDelayMilliseconds) ||
            parsedDelayMilliseconds <= 0)
        {
            return inner;
        }

        return new EvidenceDelayImageDecodeService(
            inner,
            TimeSpan.FromMilliseconds(parsedDelayMilliseconds));
    }

    public async Task<Result<DecodedImage>> DecodeImageAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
        return await _inner.DecodeImageAsync(filePath, cancellationToken).ConfigureAwait(false);
    }

    public bool IsSupportedFormat(string extension)
    {
        return _inner.IsSupportedFormat(extension);
    }

    public bool IsDeferredFormat(string extension)
    {
        return _inner.IsDeferredFormat(extension);
    }

    public Task<Result<int>> GetFrameCountAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        return _inner.GetFrameCountAsync(filePath, cancellationToken);
    }
}
