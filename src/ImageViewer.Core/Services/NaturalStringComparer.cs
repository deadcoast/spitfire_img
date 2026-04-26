using System.Text.RegularExpressions;

namespace ImageViewer.Core.Services;

public sealed partial class NaturalStringComparer : IComparer<string>
{
    public static NaturalStringComparer OrdinalIgnoreCase { get; } = new(StringComparer.OrdinalIgnoreCase);

    private readonly StringComparer _stringComparer;

    private NaturalStringComparer(StringComparer stringComparer)
    {
        _stringComparer = stringComparer;
    }

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var xParts = NaturalSortRegex().Matches(x);
        var yParts = NaturalSortRegex().Matches(y);
        var count = Math.Min(xParts.Count, yParts.Count);

        for (var index = 0; index < count; index++)
        {
            var xPart = xParts[index].Value;
            var yPart = yParts[index].Value;
            var comparison = ComparePart(xPart, yPart);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return xParts.Count.CompareTo(yParts.Count);
    }

    private int ComparePart(string x, string y)
    {
        var xIsNumber = long.TryParse(x, out var xNumber);
        var yIsNumber = long.TryParse(y, out var yNumber);

        if (xIsNumber && yIsNumber)
        {
            return xNumber.CompareTo(yNumber);
        }

        return _stringComparer.Compare(x, y);
    }

    [GeneratedRegex(@"\d+|\D+", RegexOptions.CultureInvariant)]
    private static partial Regex NaturalSortRegex();
}
