namespace ImageViewer.Core.Models;

public enum LayoutMode
{
    Single,
    Grid,
    Filmstrip
}

public sealed record LayoutConfig(
    LayoutMode Mode = LayoutMode.Single,
    int GridRows = LayoutConfig.DefaultGridRows,
    int GridColumns = LayoutConfig.DefaultGridColumns,
    int FilmstripThumbnailCount = LayoutConfig.DefaultFilmstripThumbnailCount)
{
    public const int DefaultGridRows = 3;
    public const int DefaultGridColumns = 3;
    public const int MinimumGridDensity = 2;
    public const int MaximumGridDensity = 6;
    public const int DefaultFilmstripThumbnailCount = 7;
    public const int MinimumFilmstripThumbnailCount = 7;
    public const int MaximumFilmstripThumbnailCount = 11;
    public const int DefaultGridSpacing = 8;

    public static LayoutConfig Default { get; } = new();

    public LayoutConfig WithMode(LayoutMode mode)
    {
        return this with { Mode = mode };
    }
}
