using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class PlaylistStateTests
{
    [Fact]
    public void SetPlaylist_UpdatesItemsCurrentIndexAndNavigationFlags()
    {
        var state = new PlaylistState();
        var playlist = CreatePlaylist(3, currentIndex: 1);

        state.SetPlaylist(playlist);

        Assert.Equal(3, state.TotalCount);
        Assert.Equal(1, state.CurrentIndex);
        Assert.Equal(playlist.Items[1], state.CurrentItem);
        Assert.True(state.CanNavigateNext);
        Assert.True(state.CanNavigatePrevious);
    }

    [Fact]
    public void MoveNext_AtLastItem_RemainsAtLastItem()
    {
        var state = new PlaylistState();
        state.SetPlaylist(CreatePlaylist(2, currentIndex: 1));

        state.MoveNext();

        Assert.Equal(1, state.CurrentIndex);
        Assert.False(state.CanNavigateNext);
    }

    [Fact]
    public void MovePrevious_AtFirstItem_RemainsAtFirstItem()
    {
        var state = new PlaylistState();
        state.SetPlaylist(CreatePlaylist(2, currentIndex: 0));

        state.MovePrevious();

        Assert.Equal(0, state.CurrentIndex);
        Assert.False(state.CanNavigatePrevious);
    }

    private static Playlist CreatePlaylist(int count, int currentIndex)
    {
        var items = Enumerable.Range(1, count)
            .Select(index => new ImageItem($@"C:\Photos\img{index}.png"))
            .ToList();

        return new Playlist(items, currentIndex, @"C:\Photos");
    }
}
