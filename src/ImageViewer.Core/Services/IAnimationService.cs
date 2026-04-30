using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IAnimationService
{
    Task<Result<AnimatedGif>> LoadAnimatedGifAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> IsAnimatedGifAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
