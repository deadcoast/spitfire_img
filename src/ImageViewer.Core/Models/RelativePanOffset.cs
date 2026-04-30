namespace ImageViewer.Core.Models;

public sealed record RelativePanOffset(double X, double Y)
{
    public RelativePanOffset Clamp()
    {
        return new RelativePanOffset(
            Math.Clamp(X, 0, 1),
            Math.Clamp(Y, 0, 1));
    }
}
