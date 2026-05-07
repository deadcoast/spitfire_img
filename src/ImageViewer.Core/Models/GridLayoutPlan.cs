namespace ImageViewer.Core.Models;

public sealed record GridLayoutPlan(
    int Rows,
    int Columns,
    double CellSpacing,
    IReadOnlyList<GridCell> Cells)
{
    public static GridLayoutPlan Empty { get; } = new(0, 0, LayoutConfig.DefaultGridSpacing, []);

    public int TotalCells => Rows * Columns;
}
