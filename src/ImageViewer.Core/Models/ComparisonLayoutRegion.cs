namespace ImageViewer.Core.Models;

public sealed record ComparisonLayoutRegion(
    int VisibleIndex,
    int Row,
    int Column,
    double X,
    double Y,
    double Width,
    double Height);
