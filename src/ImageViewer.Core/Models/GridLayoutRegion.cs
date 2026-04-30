namespace ImageViewer.Core.Models;

public sealed record GridLayoutRegion(
    int VisibleIndex,
    int Row,
    int Column,
    double X,
    double Y,
    double Width,
    double Height);
