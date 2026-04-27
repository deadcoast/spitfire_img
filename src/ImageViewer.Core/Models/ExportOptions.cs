namespace ImageViewer.Core.Models;

public enum ImageExportFormat
{
    Png,
    Jpeg,
    Bmp,
    Webp
}

public sealed record ExportOptions(
    ImageExportFormat TargetFormat,
    int JpegQuality,
    bool PreserveExif,
    bool OverwriteExisting)
{
    public static ExportOptions Default { get; } = new(
        ImageExportFormat.Png,
        JpegQuality: 90,
        PreserveExif: true,
        OverwriteExisting: false);

    public ExportOptions Normalize()
    {
        return this with
        {
            JpegQuality = Math.Clamp(JpegQuality, 1, 100)
        };
    }
}
