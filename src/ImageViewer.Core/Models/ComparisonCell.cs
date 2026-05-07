namespace ImageViewer.Core.Models;

public sealed record ComparisonCell(
    int Row,
    int Column,
    double WidthWeight,
    double HeightWeight,
    bool PreservesAspectRatio);
