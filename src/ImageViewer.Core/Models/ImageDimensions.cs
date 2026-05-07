namespace ImageViewer.Core.Models;

public sealed record ImageDimensions(double Width, double Height)
{
    public static ImageDimensions Unknown { get; } = new(1, 1);

    public ImageDimensions Normalize()
    {
        return new ImageDimensions(
            Width <= 0 ? 1 : Width,
            Height <= 0 ? 1 : Height);
    }
}
