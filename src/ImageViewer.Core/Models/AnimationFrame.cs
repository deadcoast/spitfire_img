namespace ImageViewer.Core.Models;

public sealed record AnimationFrame(
    byte[] EncodedBytes,
    int PixelWidth,
    int PixelHeight,
    TimeSpan Delay);
