using System.Security;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.App.Services;

public sealed class WindowsCodecDetectionService : ICodecDetectionService
{
    public const string HeicStoreUrl = "ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG";
    public const string AvifStoreUrl = "ms-windows-store://pdp/?ProductId=9MVZQVXJBQ9V";

    private readonly ICodecRegistryProbe _registryProbe;
    private readonly Func<DateTimeOffset> _getUtcNow;
    private readonly object _cacheLock = new();
    private CodecAvailability? _cachedAvailability;

    public WindowsCodecDetectionService()
        : this(new WindowsCodecRegistryProbe(), () => DateTimeOffset.UtcNow)
    {
    }

    public WindowsCodecDetectionService(ICodecRegistryProbe registryProbe, Func<DateTimeOffset>? getUtcNow = null)
    {
        _registryProbe = registryProbe;
        _getUtcNow = getUtcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public Task<Result<CodecAvailability>> DetectCodecsAsync(CancellationToken cancellationToken = default)
    {
        lock (_cacheLock)
        {
            if (_cachedAvailability is not null)
            {
                return Task.FromResult(Result<CodecAvailability>.Success(_cachedAvailability));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        var availability = new CodecAvailability(
            DetectSafely(_registryProbe.IsHeicCodecRegistered),
            DetectSafely(_registryProbe.IsAvifCodecRegistered),
            _getUtcNow());

        lock (_cacheLock)
        {
            _cachedAvailability ??= availability;
            return Task.FromResult(Result<CodecAvailability>.Success(_cachedAvailability));
        }
    }

    public Result<bool> IsHeicSupported()
    {
        var cached = GetCachedAvailability();
        return cached is null
            ? Result<bool>.Failure(ViewerError.CodecDetectionError("IsHeicSupported", null))
            : Result<bool>.Success(cached.HeicSupported);
    }

    public Result<bool> IsAvifSupported()
    {
        var cached = GetCachedAvailability();
        return cached is null
            ? Result<bool>.Failure(ViewerError.CodecDetectionError("IsAvifSupported", null))
            : Result<bool>.Success(cached.AvifSupported);
    }

    public Result<string> GetHeicStoreUrl()
    {
        return Result<string>.Success(HeicStoreUrl);
    }

    public Result<string> GetAvifStoreUrl()
    {
        return Result<string>.Success(AvifStoreUrl);
    }

    private CodecAvailability? GetCachedAvailability()
    {
        lock (_cacheLock)
        {
            return _cachedAvailability;
        }
    }

    private static bool DetectSafely(Func<bool> detect)
    {
        try
        {
            return detect();
        }
        catch (Exception ex) when (ex is IOException
            or SecurityException
            or UnauthorizedAccessException
            or InvalidOperationException)
        {
            return false;
        }
    }
}
