using System.Collections.ObjectModel;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class LayoutState : ObservableObject
{
    private bool _isActive;
    private LayoutMode _mode = LayoutMode.SingleImage;
    private LayoutConfig _config = LayoutConfig.Default;
    private int _windowStartIndex;
    private int _activeImageIndex;
    private int _totalImageCount;

    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    public LayoutMode Mode
    {
        get => _mode;
        private set => SetProperty(ref _mode, value);
    }

    public LayoutConfig Config
    {
        get => _config;
        private set => SetProperty(ref _config, value);
    }

    public int WindowStartIndex
    {
        get => _windowStartIndex;
        private set => SetProperty(ref _windowStartIndex, value);
    }

    public int ActiveImageIndex
    {
        get => _activeImageIndex;
        private set
        {
            if (SetProperty(ref _activeImageIndex, value))
            {
                OnPropertyChanged(nameof(ActiveImage));
                OnPropertyChanged(nameof(FocusImage));
                OnPropertyChanged(nameof(PositionText));
            }
        }
    }

    public int TotalImageCount
    {
        get => _totalImageCount;
        private set
        {
            if (SetProperty(ref _totalImageCount, value))
            {
                OnPropertyChanged(nameof(PositionText));
            }
        }
    }

    public ObservableCollection<LayoutImage> VisibleImages { get; } = [];

    public LayoutImage? ActiveImage => IsActive && ActiveImageIndex >= 0 && ActiveImageIndex < VisibleImages.Count
        ? VisibleImages[ActiveImageIndex]
        : null;

    public LayoutImage? FocusImage => Mode == LayoutMode.Filmstrip ? ActiveImage : null;

    public string PositionText => Mode switch
    {
        LayoutMode.Grid when IsActive && VisibleImages.Count > 0 && TotalImageCount > 0 =>
            $"Grid {Config.Rows}x{Config.Columns} {GetGridRangeText()} / {TotalImageCount}",
        LayoutMode.Filmstrip when FocusImage is not null && TotalImageCount > 0 =>
            $"Filmstrip {FocusImage.PlaylistIndex + 1} / {TotalImageCount}",
        _ => string.Empty
    };

    public void EnterLayout(LayoutSet layout)
    {
        var previousActivePlaylistIndex = ActiveImage?.PlaylistIndex;
        VisibleImages.Clear();
        foreach (var image in layout.VisibleImages)
        {
            VisibleImages.Add(image);
        }

        Config = layout.Config;
        Mode = layout.Mode;
        WindowStartIndex = layout.WindowStartIndex;
        TotalImageCount = layout.TotalImageCount;
        ActiveImageIndex = layout.Mode == LayoutMode.Filmstrip || previousActivePlaylistIndex is null
            ? Math.Clamp(layout.ActiveImageIndex, 0, Math.Max(0, VisibleImages.Count - 1))
            : FindVisibleIndex(previousActivePlaylistIndex);
        IsActive = true;
        OnPropertyChanged(nameof(ActiveImage));
        OnPropertyChanged(nameof(FocusImage));
        OnPropertyChanged(nameof(PositionText));
    }

    public void ExitLayout()
    {
        VisibleImages.Clear();
        Mode = LayoutMode.SingleImage;
        IsActive = false;
        WindowStartIndex = 0;
        ActiveImageIndex = 0;
        TotalImageCount = 0;
        OnPropertyChanged(nameof(ActiveImage));
        OnPropertyChanged(nameof(FocusImage));
        OnPropertyChanged(nameof(PositionText));
    }

    public void SelectImage(int visibleIndex)
    {
        if (!IsActive || VisibleImages.Count == 0)
        {
            return;
        }

        ActiveImageIndex = Math.Clamp(visibleIndex, 0, VisibleImages.Count - 1);
    }

    private int FindVisibleIndex(int? playlistIndex)
    {
        if (VisibleImages.Count == 0)
        {
            return 0;
        }

        if (playlistIndex is null)
        {
            return 0;
        }

        var index = VisibleImages
            .Select((image, visibleIndex) => (image, visibleIndex))
            .FirstOrDefault(item => item.image.PlaylistIndex == playlistIndex.Value)
            .visibleIndex;

        return index;
    }

    private string GetGridRangeText()
    {
        var ranges = new List<string>();
        var rangeStart = VisibleImages[0].PlaylistIndex;
        var previous = rangeStart;

        foreach (var playlistIndex in VisibleImages.Select(image => image.PlaylistIndex).Skip(1))
        {
            if (playlistIndex == previous + 1)
            {
                previous = playlistIndex;
                continue;
            }

            ranges.Add(FormatRange(rangeStart, previous));
            rangeStart = playlistIndex;
            previous = playlistIndex;
        }

        ranges.Add(FormatRange(rangeStart, previous));
        return string.Join(", ", ranges);
    }

    private static string FormatRange(int startIndex, int endIndex)
    {
        return startIndex == endIndex
            ? (startIndex + 1).ToString()
            : $"{startIndex + 1}-{endIndex + 1}";
    }
}
