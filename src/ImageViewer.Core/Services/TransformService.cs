using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class TransformService : ITransformService
{
    public Task<Result<TransformedImage>> ApplyTransformAsync(
        DecodedImage sourceImage,
        IReadOnlyList<TransformOperation> operations,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var width = sourceImage.Metadata.PixelWidth;
            var height = sourceImage.Metadata.PixelHeight;

            foreach (var operation in operations)
            {
                if (operation is TransformOperation.RotateLeft or TransformOperation.RotateRight)
                {
                    (width, height) = (height, width);
                }
            }

            var metadata = sourceImage.Metadata with
            {
                PixelWidth = width,
                PixelHeight = height
            };

            return Task.FromResult(Result<TransformedImage>.Success(
                new TransformedImage(sourceImage, metadata, operations.ToArray())));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<TransformedImage>.Failure(
                ViewerError.TransformError("ApplyTransform", sourceImage.FilePath, ex)));
        }
    }

    public Task<Result> SaveTransformsAsync(
        string filePath,
        IReadOnlyList<TransformOperation> operations,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return Task.FromResult(Result.Failure(
                ViewerError.TransformSaveError("SaveTransforms", filePath)));
        }

        // Pixel-level persistence is handled in the app layer where Windows imaging APIs are available.
        return Task.FromResult(Result.Success());
    }
}
