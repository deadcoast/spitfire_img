using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface ICodecDetectionService
{
    Task<Result<CodecAvailability>> DetectCodecsAsync(CancellationToken cancellationToken = default);

    Result<bool> IsHeicSupported();

    Result<bool> IsAvifSupported();

    Result<string> GetHeicStoreUrl();

    Result<string> GetAvifStoreUrl();
}
