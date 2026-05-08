using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.Properties;

public sealed class Phase3PropertyTests
{
    private readonly Random _random = new(24680);

    [Fact]
    public void GridLayoutCellDistribution_DisplaysExpectedContiguousImages_ForRandomGridsAndPlaylists()
    {
        var service = new LayoutService();

        // Feature: phase-3-advanced-viewing, Property 15: Grid Layout Cell Distribution
        for (var iteration = 0; iteration < 150; iteration++)
        {
            var rows = _random.Next(2, 7);
            var columns = _random.Next(2, 7);
            var playlistSize = _random.Next(0, 80);
            var requestedStartIndex = playlistSize == 0 ? 0 : _random.Next(-10, playlistSize + 10);
            var canvasSize = new ImageDimensions(
                _random.Next(200, 5000),
                _random.Next(200, 5000));
            var playlist = CreatePlaylistState(playlistSize, playlistSize == 0 ? 0 : _random.Next(playlistSize));

            var layoutResult = service.CalculateGridLayout(rows, columns, canvasSize);

            Assert.True(layoutResult.IsSuccess);
            Assert.Equal(rows * columns, layoutResult.Value.TotalCells);
            Assert.Equal(rows * columns, layoutResult.Value.Cells.Count);

            var imagesResult = service.LoadGridImages(playlist, requestedStartIndex, layoutResult.Value);

            Assert.True(imagesResult.IsSuccess);
            if (playlistSize == 0)
            {
                Assert.Empty(imagesResult.Value);
                continue;
            }

            var clampedStartIndex = Math.Clamp(requestedStartIndex, 0, playlistSize - 1);
            var expectedCount = Math.Min(layoutResult.Value.TotalCells, playlistSize - clampedStartIndex);

            Assert.Equal(expectedCount, imagesResult.Value.Count);
            Assert.Equal(
                Enumerable.Range(clampedStartIndex, expectedCount),
                imagesResult.Value.Select(image => image.PlaylistIndex));
            Assert.Equal(
                layoutResult.Value.Cells.Take(expectedCount),
                imagesResult.Value.Select(image => image.Cell));
            Assert.All(layoutResult.Value.Cells, cell =>
            {
                Assert.InRange(cell.Row, 0, rows - 1);
                Assert.InRange(cell.Column, 0, columns - 1);
                Assert.True(cell.PreservesAspectRatio);
                Assert.True(cell.Width > 0);
                Assert.True(cell.Height > 0);
            });
        }
    }

    private static PlaylistState CreatePlaylistState(int count, int currentIndex)
    {
        var playlist = new Playlist(
            Enumerable.Range(0, count)
                .Select(index => new ImageItem($@"C:\Photos\phase3-{index:00}.png"))
                .ToArray(),
            currentIndex,
            @"C:\Photos");
        var state = new PlaylistState();
        state.SetPlaylist(playlist);
        return state;
    }
}
