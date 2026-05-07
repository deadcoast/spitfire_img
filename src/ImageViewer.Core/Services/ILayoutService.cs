using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Core.Services;

public interface ILayoutService
{
    Result<GridLayoutPlan> CalculateGridLayout(
        int rows,
        int columns,
        ImageDimensions canvasSize);

    Result<IReadOnlyList<GridImage>> LoadGridImages(
        PlaylistState playlist,
        int startIndex,
        GridLayoutPlan layout);

    Result<int> CalculateGridStartIndex(
        int currentStartIndex,
        int offset,
        int playlistSize,
        int visibleCapacity);
}
