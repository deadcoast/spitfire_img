using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class LayoutStateTests
{
    [Fact]
    public void DefaultStateStartsInSingleModeWithDefaultGridConfig()
    {
        var state = new LayoutState();

        Assert.Equal(LayoutMode.Single, state.CurrentLayout);
        Assert.False(state.IsGridActive);
        Assert.Equal(3, state.Config.GridRows);
        Assert.Equal(3, state.Config.GridColumns);
        Assert.Equal(7, state.Config.FilmstripThumbnailCount);
    }

    [Fact]
    public void EnterGrid_ActivatesDefaultGridAndVisiblePlaylistImages()
    {
        var state = new LayoutState();
        var service = new LayoutService();
        var playlist = CreatePlaylistState(12, currentIndex: 2);
        var layout = service.CalculateGridLayout(3, 3, new ImageDimensions(900, 600)).Value;
        var images = service.LoadGridImages(playlist, playlist.CurrentIndex, layout).Value;

        state.EnterGrid(layout, images, playlist.CurrentIndex);

        Assert.Equal(LayoutMode.Grid, state.CurrentLayout);
        Assert.True(state.IsGridActive);
        Assert.Equal(3, state.GridRows);
        Assert.Equal(3, state.GridColumns);
        Assert.Equal(2, state.GridStartIndex);
        Assert.Equal(9, state.GridImages.Count);
        Assert.Equal([2, 3, 4, 5, 6, 7, 8, 9, 10], state.GridImages.Select(image => image.PlaylistIndex));
        Assert.Equal(0, state.ActiveGridImageIndex);
        Assert.Equal(2, state.ActiveGridPlaylistIndex);
    }

    [Fact]
    public void SelectGridImage_TracksActiveVisibleAndPlaylistIndex()
    {
        var state = new LayoutState();
        var service = new LayoutService();
        var playlist = CreatePlaylistState(12, currentIndex: 2);
        var layout = service.CalculateGridLayout(3, 3, new ImageDimensions(900, 600)).Value;
        state.EnterGrid(layout, service.LoadGridImages(playlist, playlist.CurrentIndex, layout).Value, playlist.CurrentIndex);

        state.SelectGridImage(4);

        Assert.Equal(4, state.ActiveGridImageIndex);
        Assert.Equal(6, state.ActiveGridPlaylistIndex);
    }

    [Fact]
    public void SelectGridImage_ClampsToVisibleGridImages()
    {
        var state = new LayoutState();
        var service = new LayoutService();
        var playlist = CreatePlaylistState(5, currentIndex: 3);
        var layout = service.CalculateGridLayout(3, 3, new ImageDimensions(900, 600)).Value;
        state.EnterGrid(layout, service.LoadGridImages(playlist, playlist.CurrentIndex, layout).Value, playlist.CurrentIndex);

        state.SelectGridImage(8);

        Assert.Equal(1, state.ActiveGridImageIndex);
        Assert.Equal(4, state.ActiveGridPlaylistIndex);
    }

    [Fact]
    public void UpdateGrid_ReplacesImagesAndPreservesActivePlaylistWhenStillVisible()
    {
        var state = new LayoutState();
        var service = new LayoutService();
        var playlist = CreatePlaylistState(20, currentIndex: 0);
        var initialLayout = service.CalculateGridLayout(3, 3, new ImageDimensions(900, 600)).Value;
        state.EnterGrid(initialLayout, service.LoadGridImages(playlist, 0, initialLayout).Value, startIndex: 0);
        state.SelectGridImage(4);
        var expandedLayout = service.CalculateGridLayout(4, 4, new ImageDimensions(900, 600)).Value;

        state.UpdateGrid(expandedLayout, service.LoadGridImages(playlist, 0, expandedLayout).Value, startIndex: 0);

        Assert.Equal(4, state.GridRows);
        Assert.Equal(4, state.GridColumns);
        Assert.Equal(4, state.ActiveGridImageIndex);
        Assert.Equal(4, state.ActiveGridPlaylistIndex);
    }

    [Fact]
    public void ExitLayout_ReturnsToSingleModeAndClearsGridImages()
    {
        var state = new LayoutState();
        var service = new LayoutService();
        var playlist = CreatePlaylistState(4, currentIndex: 0);
        var layout = service.CalculateGridLayout(3, 3, new ImageDimensions(900, 600)).Value;
        state.EnterGrid(layout, service.LoadGridImages(playlist, 0, layout).Value, startIndex: 0);

        state.ExitLayout();

        Assert.Equal(LayoutMode.Single, state.CurrentLayout);
        Assert.False(state.IsGridActive);
        Assert.Empty(state.GridImages);
        Assert.Empty(state.GridLayout.Cells);
        Assert.Equal(0, state.ActiveGridImageIndex);
        Assert.Equal(-1, state.ActiveGridPlaylistIndex);
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
