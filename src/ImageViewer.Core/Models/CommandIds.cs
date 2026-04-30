namespace ImageViewer.Core.Models;

public static class CommandIds
{
    public const string OpenFile = "file.open";
    public const string ToggleFullscreen = "view.toggleFullscreen";
    public const string FitToWindow = "view.fitToWindow";
    public const string ActualSize = "view.actualSize";
    public const string RotateLeft = "image.rotateLeft";
    public const string RotateRight = "image.rotateRight";
    public const string NavigateNext = "navigation.next";
    public const string NavigatePrevious = "navigation.previous";
    public const string ToggleSlideshow = "slideshow.toggle";
    public const string EnterComparison = "comparison.enterSideBySide";
    public const string EnterOverlayComparison = "comparison.enterOverlay";
    public const string ToggleComparisonSyncZoom = "comparison.toggleSyncZoom";
    public const string ComparisonTwoImages = "comparison.twoImages";
    public const string ComparisonThreeImages = "comparison.threeImages";
    public const string ComparisonFourImages = "comparison.fourImages";
    public const string ReturnToSingleImage = "layout.singleImage";
    public const string GridLayout = "layout.grid";
    public const string FilmstripLayout = "layout.filmstrip";
    public const string ToggleGifPlayback = "animation.togglePlayback";
}
