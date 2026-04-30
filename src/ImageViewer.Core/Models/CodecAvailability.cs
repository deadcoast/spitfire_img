namespace ImageViewer.Core.Models;

public sealed record CodecAvailability(
    bool HeicSupported,
    bool AvifSupported,
    DateTimeOffset DetectionTime);
