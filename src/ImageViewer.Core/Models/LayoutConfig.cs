namespace ImageViewer.Core.Models;

public sealed record LayoutConfig(
    LayoutMode Mode,
    int Rows,
    int Columns,
    double CellSpacing)
{
    public const int MinGridSize = 2;
    public const int MaxGridSize = 6;
    public const int FilmstripThumbnailCount = 9;

    public static LayoutConfig Default { get; } = new(LayoutMode.Grid, Rows: 3, Columns: 3, CellSpacing: 12);

    public static LayoutConfig Filmstrip { get; } = new(
        LayoutMode.Filmstrip,
        Rows: 1,
        Columns: FilmstripThumbnailCount,
        CellSpacing: 10);

    public int VisibleCellCount => Rows * Columns;

    public static LayoutConfig StepGridDensity(LayoutConfig config, int direction)
    {
        var currentSize = Math.Max(config.Rows, config.Columns);
        var nextSize = Math.Clamp(
            currentSize + Math.Sign(direction),
            MinGridSize,
            MaxGridSize);

        return config with
        {
            Mode = LayoutMode.Grid,
            Rows = nextSize,
            Columns = nextSize
        };
    }
}
