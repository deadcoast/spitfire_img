namespace ImageViewer.Core.Models;

public sealed record AnimatedGif(
    string FilePath,
    ImageMetadata Metadata,
    IReadOnlyList<AnimationFrame> Frames)
{
    public int TotalFrames => Frames.Count;

    public bool IsAnimated => TotalFrames > 1;
}
