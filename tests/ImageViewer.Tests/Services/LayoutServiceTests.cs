using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.Services;

public sealed class LayoutServiceTests
{
    [Fact]
    public void CalculateGridLayout_DefaultGridCreatesNineCellsWithSpacing()
    {
        var service = new LayoutService();

        var result = service.CalculateGridLayout(3, 3, new ImageDimensions(900, 600));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Rows);
        Assert.Equal(3, result.Value.Columns);
        Assert.Equal(9, result.Value.TotalCells);
        Assert.Equal(8, result.Value.CellSpacing);
        Assert.Equal(9, result.Value.Cells.Count);
        Assert.All(result.Value.Cells, cell => Assert.True(cell.PreservesAspectRatio));
        Assert.Equal(new GridCell(0, 0, 0, 0, 294.6666666666667, 194.66666666666666, true), result.Value.Cells[0]);
        Assert.Equal(new GridCell(0, 1, 302.6666666666667, 0, 294.6666666666667, 194.66666666666666, true), result.Value.Cells[1]);
        Assert.Equal(new GridCell(2, 2, 605.3333333333334, 405.3333333333333, 294.6666666666667, 194.66666666666666, true), result.Value.Cells[8]);
    }

    [Fact]
    public void LoadGridImages_UsesPlaylistItemsFromStartIndexAndAssignsCells()
    {
        var service = new LayoutService();
        var playlist = CreatePlaylistState(12, currentIndex: 2);
        var layout = service.CalculateGridLayout(3, 3, new ImageDimensions(900, 600)).Value;

        var result = service.LoadGridImages(playlist, startIndex: 2, layout);

        Assert.True(result.IsSuccess);
        Assert.Equal(9, result.Value.Count);
        Assert.Equal(2, result.Value[0].PlaylistIndex);
        Assert.Equal(@"C:\Photos\image-02.png", result.Value[0].Item.FilePath);
        Assert.Equal(layout.Cells[0], result.Value[0].Cell);
        Assert.Equal(10, result.Value[8].PlaylistIndex);
        Assert.Equal(@"C:\Photos\image-10.png", result.Value[8].Item.FilePath);
        Assert.Equal(layout.Cells[8], result.Value[8].Cell);
    }

    [Fact]
    public void LoadGridImages_StopsAtPlaylistEndWhenFewerImagesRemain()
    {
        var service = new LayoutService();
        var playlist = CreatePlaylistState(5, currentIndex: 3);
        var layout = service.CalculateGridLayout(3, 3, new ImageDimensions(900, 600)).Value;

        var result = service.LoadGridImages(playlist, startIndex: 3, layout);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal([3, 4], result.Value.Select(image => image.PlaylistIndex));
    }

    [Fact]
    public void CalculateGridLayout_RejectsInvalidDimensions()
    {
        var service = new LayoutService();

        var result = service.CalculateGridLayout(0, 3, new ImageDimensions(900, 600));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.LayoutError, result.Error.Category);
    }

    private static PlaylistState CreatePlaylistState(int count, int currentIndex)
    {
        var playlist = new Playlist(
            Enumerable.Range(0, count)
                .Select(index => new ImageItem($@"C:\Photos\image-{index:00}.png"))
                .ToArray(),
            currentIndex,
            @"C:\Photos");
        var state = new PlaylistState();
        state.SetPlaylist(playlist);
        return state;
    }
}
