using System.Collections.ObjectModel;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class ComparisonState : ObservableObject
{
    private bool _isActive;
    private ComparisonLayout _layout = ComparisonLayout.SideBySide;
    private int _activeImageIndex;
    private double _overlayOpacity = 50;
    private bool _syncZoomEnabled;

    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    public ComparisonLayout Layout
    {
        get => _layout;
        private set => SetProperty(ref _layout, value);
    }

    public ObservableCollection<ImageItem> DisplayedItems { get; } = [];

    public ObservableCollection<int> DisplayedIndices { get; } = [];

    public ObservableCollection<ComparisonImageViewport> ImageViewports { get; } = [];

    public int ImageCount => DisplayedItems.Count;

    public int ActiveImageIndex
    {
        get => _activeImageIndex;
        private set
        {
            if (SetProperty(ref _activeImageIndex, value))
            {
                OnPropertyChanged(nameof(ActivePlaylistIndex));
            }
        }
    }

    public int ActivePlaylistIndex => ActiveImageIndex >= 0 && ActiveImageIndex < DisplayedIndices.Count
        ? DisplayedIndices[ActiveImageIndex]
        : -1;

    public double OverlayOpacity
    {
        get => _overlayOpacity;
        private set => SetProperty(ref _overlayOpacity, value);
    }

    public bool SyncZoomEnabled
    {
        get => _syncZoomEnabled;
        private set => SetProperty(ref _syncZoomEnabled, value);
    }

    public string FormattedDisplayedIndices => string.Join(", ", DisplayedIndices.Select(index => index + 1));

    public void EnterComparison(
        ComparisonLayout layout,
        IEnumerable<ImageItem> displayedItems,
        IEnumerable<int> displayedIndices)
    {
        Layout = layout;
        if (layout == ComparisonLayout.Overlay)
        {
            OverlayOpacity = 50;
        }

        ReplaceDisplayedItems(displayedItems, displayedIndices);
        IsActive = true;
    }

    public void ExitComparison()
    {
        IsActive = false;
        ActiveImageIndex = 0;
        DisplayedItems.Clear();
        DisplayedIndices.Clear();
        ImageViewports.Clear();
        OnDisplayedCollectionsChanged();
    }

    public void ShiftImages(IEnumerable<ImageItem> displayedItems, IEnumerable<int> displayedIndices)
    {
        ReplaceDisplayedItems(displayedItems, displayedIndices);
    }

    public void SetActiveImage(int visibleIndex)
    {
        if (DisplayedItems.Count == 0)
        {
            ActiveImageIndex = 0;
            return;
        }

        ActiveImageIndex = Math.Clamp(visibleIndex, 0, DisplayedItems.Count - 1);
    }

    public void CycleActiveImage()
    {
        if (DisplayedItems.Count == 0)
        {
            ActiveImageIndex = 0;
            return;
        }

        ActiveImageIndex = (ActiveImageIndex + 1) % DisplayedItems.Count;
    }

    public void SetOverlayOpacity(double opacity)
    {
        OverlayOpacity = Math.Clamp(opacity, 0, 100);
    }

    public void AdjustOverlayOpacity(double delta)
    {
        SetOverlayOpacity(OverlayOpacity + delta);
    }

    public void SwapLayers()
    {
        if (Layout != ComparisonLayout.Overlay || DisplayedItems.Count < 2)
        {
            return;
        }

        var reversedItems = DisplayedItems.Reverse().ToArray();
        var reversedIndices = DisplayedIndices.Reverse().ToArray();
        var reversedViewports = ImageViewports.Reverse().ToArray();
        ReplaceDisplayedItems(reversedItems, reversedIndices);
        ReplaceViewports(reversedViewports);
    }

    public void ToggleSyncZoom()
    {
        SyncZoomEnabled = !SyncZoomEnabled;
    }

    public void SetSyncZoom(bool isEnabled)
    {
        SyncZoomEnabled = isEnabled;
    }

    public void ApplyViewportUpdate(int visibleIndex, ComparisonImageViewport viewport)
    {
        if (visibleIndex < 0 || visibleIndex >= ImageViewports.Count)
        {
            return;
        }

        if (viewport.ZoomLevel != 100 || viewport.PanOffset != new ImagePoint(0, 0))
        {
            viewport = viewport with { DisplayMode = DisplayMode.Custom };
        }

        if (ImageViewports[visibleIndex] == viewport)
        {
            return;
        }

        ImageViewports[visibleIndex] = viewport;
        OnPropertyChanged(nameof(ImageViewports));
    }

    public void ApplyViewportUpdates(IReadOnlyList<ComparisonImageViewport> viewports)
    {
        ReplaceViewports(viewports);
    }

    public void SetImageDimensions(int visibleIndex, ImageDimensions dimensions)
    {
        if (visibleIndex < 0 || visibleIndex >= ImageViewports.Count)
        {
            return;
        }

        var nextViewport = ImageViewports[visibleIndex].WithDimensions(dimensions);
        ApplyViewportUpdate(visibleIndex, nextViewport);
    }

    private void ReplaceDisplayedItems(
        IEnumerable<ImageItem> displayedItems,
        IEnumerable<int> displayedIndices)
    {
        DisplayedItems.Clear();
        foreach (var item in displayedItems)
        {
            DisplayedItems.Add(item);
        }

        DisplayedIndices.Clear();
        foreach (var index in displayedIndices)
        {
            DisplayedIndices.Add(index);
        }

        EnsureViewportCount(DisplayedItems.Count);
        SetActiveImage(Math.Min(ActiveImageIndex, DisplayedItems.Count - 1));
        OnDisplayedCollectionsChanged();
    }

    private void EnsureViewportCount(int count)
    {
        while (ImageViewports.Count < count)
        {
            ImageViewports.Add(ComparisonImageViewport.Default);
        }

        while (ImageViewports.Count > count)
        {
            ImageViewports.RemoveAt(ImageViewports.Count - 1);
        }
    }

    private void ReplaceViewports(IReadOnlyList<ComparisonImageViewport> viewports)
    {
        ImageViewports.Clear();
        foreach (var viewport in viewports)
        {
            ImageViewports.Add(viewport);
        }

        OnPropertyChanged(nameof(ImageViewports));
    }

    private void OnDisplayedCollectionsChanged()
    {
        OnPropertyChanged(nameof(ImageCount));
        OnPropertyChanged(nameof(FormattedDisplayedIndices));
        OnPropertyChanged(nameof(DisplayedItems));
        OnPropertyChanged(nameof(DisplayedIndices));
        OnPropertyChanged(nameof(ImageViewports));
        OnPropertyChanged(nameof(ActivePlaylistIndex));
    }
}
