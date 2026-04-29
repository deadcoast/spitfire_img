namespace ImageViewer.Core.Models;

public sealed class ImageItem : IEquatable<ImageItem>
{
    public ImageItem(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        FilePath = filePath;
        Filename = Path.GetFileName(filePath);
        Extension = Path.GetExtension(filePath).ToLowerInvariant();
    }

    public string FilePath { get; }

    public string Filename { get; }

    public string Extension { get; }

    public bool Equals(ImageItem? other)
    {
        return other is not null &&
               string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ImageItem);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath);
    }

    public override string ToString()
    {
        return Filename;
    }
}
