namespace ImageViewer.Core.Models;

public sealed record GridCell(
    int Row,
    int Column,
    double X,
    double Y,
    double Width,
    double Height,
    bool PreservesAspectRatio);
