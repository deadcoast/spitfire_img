namespace ImageViewer.Core.Services;

public static class ImageFormatCatalog
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".webp"
    };

    private static readonly HashSet<string> DeferredExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gif",
        ".tiff",
        ".tif",
        ".heic",
        ".avif"
    };

    public static bool IsSupportedFormat(string? extension)
    {
        return !string.IsNullOrWhiteSpace(extension) && SupportedExtensions.Contains(extension);
    }

    public static bool IsDeferredFormat(string? extension)
    {
        return !string.IsNullOrWhiteSpace(extension) && DeferredExtensions.Contains(extension);
    }
}
