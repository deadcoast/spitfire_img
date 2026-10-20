using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Core.Services;

public sealed record ImageViewportGeometry(
    double NaturalWidth,
    double NaturalHeight,
    double Scale,
    double FitZoomPercent,
    double PanLimitX,
    double PanLimitY,
    PanOffset Pan)
{
    public bool CanPan => PanLimitX > 0 || PanLimitY > 0;

    public static ImageViewportGeometry Calculate(
        ImageMetadata metadata, double viewportWidth, double viewportHeight, double rasterizationScale,
        DisplayMode mode, double zoomPercent, double rotationDegrees, PanOffset requestedPan)
    {
        var dpi = double.IsFinite(rasterizationScale) && rasterizationScale > 0 ? rasterizationScale : 1;
        var width = metadata.PixelWidth / dpi;
        var height = metadata.PixelHeight / dpi;
        var quarterTurn = Math.Abs(rotationDegrees % 180) > 45;
        var rotatedWidth = quarterTurn ? height : width;
        var rotatedHeight = quarterTurn ? width : height;
        var fitScale = Math.Min(Math.Max(1, viewportWidth) / Math.Max(1, rotatedWidth),
            Math.Max(1, viewportHeight) / Math.Max(1, rotatedHeight));
        var scale = mode switch
        {
            DisplayMode.FitToWindow => fitScale,
            DisplayMode.ActualSize => 1,
            _ => Math.Clamp(zoomPercent, 10, 3200) / 100
        };
        var limitX = Math.Max(0, (rotatedWidth * scale - viewportWidth) / 2);
        var limitY = Math.Max(0, (rotatedHeight * scale - viewportHeight) / 2);
        return new(width, height, scale, fitScale * 100, limitX, limitY,
            new PanOffset(Math.Clamp(requestedPan.X, -limitX, limitX), Math.Clamp(requestedPan.Y, -limitY, limitY)));
    }
}
