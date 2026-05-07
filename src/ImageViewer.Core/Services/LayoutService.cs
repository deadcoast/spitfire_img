using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Core.Services;

public sealed class LayoutService : ILayoutService
{
    public Result<GridLayoutPlan> CalculateGridLayout(
        int rows,
        int columns,
        ImageDimensions canvasSize)
    {
        if (rows <= 0 || columns <= 0)
        {
            return Result<GridLayoutPlan>.Failure(
                ViewerError.LayoutError("CalculateGridLayout", "Grid layout requires positive row and column counts."));
        }

        var normalizedSize = canvasSize.Normalize();
        var spacing = LayoutConfig.DefaultGridSpacing;
        var totalSpacingWidth = (columns - 1) * spacing;
        var totalSpacingHeight = (rows - 1) * spacing;
        var cellWidth = Math.Max(1, (normalizedSize.Width - totalSpacingWidth) / columns);
        var cellHeight = Math.Max(1, (normalizedSize.Height - totalSpacingHeight) / rows);
        var cells = new List<GridCell>(rows * columns);

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                cells.Add(new GridCell(
                    row,
                    column,
                    column * (cellWidth + spacing),
                    row * (cellHeight + spacing),
                    cellWidth,
                    cellHeight,
                    PreservesAspectRatio: true));
            }
        }

        return Result<GridLayoutPlan>.Success(new GridLayoutPlan(rows, columns, spacing, cells));
    }

    public Result<IReadOnlyList<GridImage>> LoadGridImages(
        PlaylistState playlist,
        int startIndex,
        GridLayoutPlan layout)
    {
        if (layout.TotalCells <= 0 || layout.Cells.Count == 0)
        {
            return Result<IReadOnlyList<GridImage>>.Failure(
                ViewerError.LayoutError("LoadGridImages", "Grid layout requires at least one cell."));
        }

        if (playlist.TotalCount == 0)
        {
            return Result<IReadOnlyList<GridImage>>.Success([]);
        }

        var clampedStartIndex = Math.Clamp(startIndex, 0, playlist.TotalCount - 1);
        var visibleCount = Math.Min(layout.TotalCells, playlist.TotalCount - clampedStartIndex);
        var images = new List<GridImage>(visibleCount);

        for (var offset = 0; offset < visibleCount; offset++)
        {
            var playlistIndex = clampedStartIndex + offset;
            images.Add(new GridImage(
                playlist.Items[playlistIndex],
                playlistIndex,
                layout.Cells[offset]));
        }

        return Result<IReadOnlyList<GridImage>>.Success(images);
    }
}
