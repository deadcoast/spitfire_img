using ImageViewer.App.Services;

namespace ImageViewer.App.Tests;

public sealed class WindowPlacementServiceTests
{
    [Fact]
    public void IsSavedWindowPositionVisible_AcceptsSecondaryDisplayCoordinates()
    {
        var displays = new[]
        {
            new WindowPlacementService.DisplayWorkArea(0, 0, 2560, 1392),
            new WindowPlacementService.DisplayWorkArea(3840, 0, 2560, 1392)
        };

        var result = WindowPlacementService.IsSavedWindowPositionVisible(3960, 20, displays);

        Assert.True(result);
    }

    [Fact]
    public void IsSavedWindowPositionVisible_RejectsCoordinatesOutsideEveryDisplay()
    {
        var displays = new[]
        {
            new WindowPlacementService.DisplayWorkArea(0, 0, 2560, 1392),
            new WindowPlacementService.DisplayWorkArea(3840, 0, 2560, 1392)
        };

        var result = WindowPlacementService.IsSavedWindowPositionVisible(8000, 20, displays);

        Assert.False(result);
    }
}
