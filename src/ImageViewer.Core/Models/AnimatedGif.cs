namespace ImageViewer.Core.Models;

public sealed record AnimatedGif(string FilePath, IReadOnlyList<AnimationFrame> Frames)
{
    public int FrameCount => Frames.Count;

    public bool IsAnimated => FrameCount > 1;

    public IReadOnlyList<int> FrameDelays => Frames
        .Select(frame => checked((int)frame.Delay.TotalMilliseconds))
        .ToArray();
}
