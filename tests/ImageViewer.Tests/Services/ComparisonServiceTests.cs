using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class ComparisonServiceTests
{
    [Fact]
    public void CalculateComparisonIndices_UsesCurrentAndNextImage()
    {
        var service = new ComparisonService();

        var result = service.CalculateComparisonIndices(currentIndex: 1, imageCount: 2, playlistSize: 4);

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2], result.Value);
    }

    [Fact]
    public void CalculateComparisonIndices_ClampsWindowAtEndOfPlaylist()
    {
        var service = new ComparisonService();

        var result = service.CalculateComparisonIndices(currentIndex: 3, imageCount: 2, playlistSize: 4);

        Assert.True(result.IsSuccess);
        Assert.Equal([2, 3], result.Value);
    }

    [Fact]
    public void CalculateComparisonIndices_SupportsThreeImageWindow()
    {
        var service = new ComparisonService();

        var result = service.CalculateComparisonIndices(currentIndex: 1, imageCount: 3, playlistSize: 5);

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2, 3], result.Value);
    }

    [Fact]
    public void CalculateComparisonIndices_SupportsFourImageWindow()
    {
        var service = new ComparisonService();

        var result = service.CalculateComparisonIndices(currentIndex: 2, imageCount: 4, playlistSize: 6);

        Assert.True(result.IsSuccess);
        Assert.Equal([2, 3, 4, 5], result.Value);
    }

    [Theory]
    [InlineData(2, 1, 2)]
    [InlineData(3, 1, 3)]
    [InlineData(4, 2, 2)]
    public void CalculateSideBySideLayout_ReturnsDocumentedGridShape(
        int imageCount,
        int expectedRows,
        int expectedColumns)
    {
        var service = new ComparisonService();

        var result = service.CalculateSideBySideLayout(imageCount);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedRows, result.Value.Rows);
        Assert.Equal(expectedColumns, result.Value.Columns);
        Assert.Equal(imageCount, result.Value.Cells.Count);
        Assert.All(result.Value.Cells, cell => Assert.True(cell.PreservesAspectRatio));
    }

    [Fact]
    public void CalculateComparisonIndices_RequiresAtLeastTwoImages()
    {
        var service = new ComparisonService();

        var result = service.CalculateComparisonIndices(currentIndex: 0, imageCount: 2, playlistSize: 1);

        Assert.True(result.IsFailure);
        Assert.Equal("Comparison mode requires at least two images", result.Error.UserMessage);
    }

    [Fact]
    public void ConvertToRelativePosition_ClampsToUnitRange()
    {
        var service = new ComparisonService();

        var result = service.ConvertToRelativePosition(
            new ImagePoint(2400, -100),
            new ImageDimensions(1200, 800));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.X);
        Assert.Equal(0, result.Value.Y);
    }

    [Fact]
    public void SynchronizeZoom_UsesRelativePanForDifferentImageDimensions()
    {
        var service = new ComparisonService();
        var viewports = new[]
        {
            new ComparisonImageViewport(new ImageDimensions(1000, 500), ZoomLevel: 100, new ImagePoint(0, 0)),
            new ComparisonImageViewport(new ImageDimensions(2000, 1000), ZoomLevel: 100, new ImagePoint(0, 0))
        };

        var result = service.SynchronizeZoom(
            viewports,
            sourceIndex: 0,
            zoomLevel: 175,
            panOffset: new ImagePoint(500, 250));

        Assert.True(result.IsSuccess);
        Assert.Equal(175, result.Value[0].ZoomLevel);
        Assert.Equal(175, result.Value[1].ZoomLevel);
        Assert.Equal(new ImagePoint(500, 250), result.Value[0].PanOffset);
        Assert.Equal(new ImagePoint(1000, 500), result.Value[1].PanOffset);
    }
}
