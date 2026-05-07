namespace ImageViewer.Core.Services;

public static class ImageFormatCatalog
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".webp",
        ".gif",
        ".tiff",
        ".tif"
    };

    private static readonly HashSet<string> DeferredExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
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

    public static bool IsHeicFormat(string? extension)
    {
        return string.Equals(extension, ".heic", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAvifFormat(string? extension)
    {
        return string.Equals(extension, ".avif", StringComparison.OrdinalIgnoreCase);
    }
}
