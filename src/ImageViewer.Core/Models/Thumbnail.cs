namespace ImageViewer.Core.Models;

public sealed record Thumbnail(
    ImageItem Item,
    byte[] EncodedBytes,
    int PixelWidth,
    int PixelHeight,
    long SizeBytes,
    DateTimeOffset LastAccessed,
    bool IsPlaceholder = false);
