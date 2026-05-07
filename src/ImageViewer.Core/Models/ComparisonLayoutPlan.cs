namespace ImageViewer.Core.Models;

public sealed record ComparisonLayoutPlan(
    ComparisonLayout Layout,
    int Rows,
    int Columns,
    IReadOnlyList<ComparisonCell> Cells);
