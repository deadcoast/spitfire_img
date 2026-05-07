using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.App.Services;

public sealed class WindowsCodecDetectionService : ICodecDetectionService
{
    private const string HeicStoreUrl = "ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG";
    private const string AvifStoreUrl = "ms-windows-store://pdp/?ProductId=9MVZQVXJBQ9V";

    private readonly IWindowsCodecRegistry _registry;
    private readonly Func<DateTimeOffset> _clock;
    private CodecAvailability? _cachedAvailability;

    public WindowsCodecDetectionService()
        : this(new WindowsCodecRegistry(), () => DateTimeOffset.UtcNow)
    {
    }

    public WindowsCodecDetectionService(IWindowsCodecRegistry registry, Func<DateTimeOffset> clock)
    {
        _registry = registry;
        _clock = clock;
    }

    public Task<Result<CodecAvailability>> DetectCodecsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedAvailability is not null)
        {
            return Task.FromResult(Result<CodecAvailability>.Success(_cachedAvailability));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<Result<CodecAvailability>>(cancellationToken);
        }

        var availability = new CodecAvailability(
            HeicSupported: DetectOrUnavailable(_registry.IsHeicCodecInstalled),
            AvifSupported: DetectOrUnavailable(_registry.IsAvifCodecInstalled),
            DetectionTime: _clock());

        _cachedAvailability = availability;
        return Task.FromResult(Result<CodecAvailability>.Success(availability));
    }

    public Result<bool> IsHeicSupported()
    {
        return _cachedAvailability is null
            ? Result<bool>.Failure(ViewerError.CodecDetectionError("IsHeicSupported"))
            : Result<bool>.Success(_cachedAvailability.HeicSupported);
    }

    public Result<bool> IsAvifSupported()
    {
        return _cachedAvailability is null
            ? Result<bool>.Failure(ViewerError.CodecDetectionError("IsAvifSupported"))
            : Result<bool>.Success(_cachedAvailability.AvifSupported);
    }

    public Result<string> GetHeicStoreUrl()
    {
        return Result<string>.Success(HeicStoreUrl);
    }

    public Result<string> GetAvifStoreUrl()
    {
        return Result<string>.Success(AvifStoreUrl);
    }

    private static bool DetectOrUnavailable(Func<bool> detect)
    {
        try
        {
            return detect();
        }
        catch (Exception)
        {
            return false;
        }
    }
}
