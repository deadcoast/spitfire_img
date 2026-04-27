using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface ITransformService
{
    Task<Result<TransformedImage>> ApplyTransformAsync(
        DecodedImage sourceImage,
        IReadOnlyList<TransformOperation> operations,
        CancellationToken cancellationToken = default);

    Task<Result> SaveTransformsAsync(
        string filePath,
        IReadOnlyList<TransformOperation> operations,
        CancellationToken cancellationToken = default);
}
