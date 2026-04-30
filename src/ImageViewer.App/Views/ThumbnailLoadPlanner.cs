namespace ImageViewer.App.Views;

public static class ThumbnailLoadPlanner
{
    public static IReadOnlyList<int> PrioritizeVisibleFirst(int itemCount, int currentIndex)
    {
        if (itemCount <= 0)
        {
            return [];
        }

        var clampedCurrentIndex = Math.Clamp(currentIndex, 0, itemCount - 1);
        var indexes = new List<int>(itemCount) { clampedCurrentIndex };

        for (var distance = 1; indexes.Count < itemCount; distance++)
        {
            var previous = clampedCurrentIndex - distance;
            if (previous >= 0)
            {
                indexes.Add(previous);
            }

            var next = clampedCurrentIndex + distance;
            if (next < itemCount)
            {
                indexes.Add(next);
            }
        }

        return indexes;
    }
}
