namespace ImageViewer.Core.Models;

public sealed record TransformedImage(
    DecodedImage SourceImage,
    ImageMetadata Metadata,
    IReadOnlyList<TransformOperation> Operations);
