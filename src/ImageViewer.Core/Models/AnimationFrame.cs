namespace ImageViewer.Core.Models;

public sealed record AnimationFrame(
    int Index,
    TimeSpan Delay,
    byte[] EncodedBytes,
    int PixelWidth,
    int PixelHeight);
