using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class Phase2StateTests
{
    [Fact]
    public void SearchState_SetSearchQuery_FiltersAndClearsPlaylist()
    {
        var searchState = new SearchState(new SearchService());
        var playlist = CreatePlaylist("cat.png", "dog.jpg", "catalog.webp");
        searchState.SetPlaylist(playlist);

        searchState.SetSearchQuery("cat");

        Assert.True(searchState.IsSearchActive);
        Assert.Equal(2, searchState.ResultCount);
        Assert.Equal("2 matches", searchState.ResultCountText);

        searchState.ClearSearch();

        Assert.False(searchState.IsSearchActive);
        Assert.Equal(3, searchState.ResultCount);
    }

    [Fact]
    public void SearchState_SelectNextAndPrevious_WrapsAtBoundaries()
    {
        var searchState = new SearchState(new SearchService());
        searchState.SetPlaylist(CreatePlaylist("a.png", "b.png"));

        searchState.SelectPrevious();
        Assert.Equal(1, searchState.SelectedResultIndex);

        searchState.SelectNext();
        Assert.Equal(0, searchState.SelectedResultIndex);
    }

    [Fact]
    public void SearchState_NoMatches_UsesRequiredEmptyResultMessage()
    {
        var searchState = new SearchState(new SearchService());
        searchState.SetPlaylist(CreatePlaylist("a.png", "b.png"));

        searchState.SetSearchQuery("missing");

        Assert.Equal(0, searchState.ResultCount);
        Assert.Equal("No matching images found", searchState.ResultCountText);
        Assert.Equal(-1, searchState.SelectedResultIndex);
    }

    [Fact]
    public void TransformState_TracksUndoRedoAndUnsavedChanges()
    {
        var state = new TransformState();

        state.ApplyTransform(TransformOperation.RotateRight);
        state.ApplyTransform(TransformOperation.FlipHorizontal);
        state.Undo();

        Assert.True(state.HasUnsavedChanges);
        Assert.True(state.CanRedo);
        Assert.Equal([TransformOperation.RotateRight], state.PendingTransforms);

        state.Redo();
        Assert.Equal([TransformOperation.RotateRight, TransformOperation.FlipHorizontal], state.PendingTransforms);
    }

    [Fact]
    public void MetadataState_TogglesVisibilityAndStoresExif()
    {
        var state = new MetadataState();
        var metadata = new ExifMetadata(CameraMake: "Canon");

        state.ShowPanel(metadata);

        Assert.True(state.IsVisible);
        Assert.True(state.HasExifData);

        state.HidePanel();
        Assert.False(state.IsVisible);
    }

    [Fact]
    public void ThumbnailState_TogglesVisibilityAndPosition()
    {
        var state = new ThumbnailState();

        state.ToggleVisibility();
        state.SetStripPosition(ThumbnailPosition.Top);

        Assert.True(state.IsStripVisible);
        Assert.Equal(ThumbnailPosition.Top, state.StripPosition);
    }

    private static List<ImageItem> CreatePlaylist(params string[] filenames)
    {
        return filenames.Select(filename => new ImageItem($@"C:\Images\{filename}")).ToList();
    }
}
