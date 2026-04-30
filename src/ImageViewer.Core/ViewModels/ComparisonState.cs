using System.Collections.ObjectModel;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class ComparisonState : ObservableObject
{
    private bool _isActive;
    private ComparisonLayout _layout = ComparisonLayout.SideBySide;
    private int _activeImageIndex;
    private double _overlayOpacity = 0.5;
    private bool _isSyncZoomEnabled = true;

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

    public int ActiveImageIndex
    {
        get => _activeImageIndex;
        private set
        {
            if (SetProperty(ref _activeImageIndex, value))
            {
                OnPropertyChanged(nameof(ActiveImage));
            }
        }
    }

    public double OverlayOpacity
    {
        get => _overlayOpacity;
        private set => SetProperty(ref _overlayOpacity, value);
    }

    public bool IsSyncZoomEnabled
    {
        get => _isSyncZoomEnabled;
        private set => SetProperty(ref _isSyncZoomEnabled, value);
    }

    public ObservableCollection<ComparisonImage> VisibleImages { get; } = [];

    public ObservableCollection<ComparisonViewport> Viewports { get; } = [];

    public void EnterComparison(ComparisonSet comparison)
    {
        VisibleImages.Clear();
        Viewports.Clear();
        var visibleIndex = 0;
        foreach (var image in comparison.Images)
        {
            VisibleImages.Add(image);
            Viewports.Add(CreateDefaultViewport(visibleIndex));
            visibleIndex++;
        }

        Layout = comparison.Layout;
        ActiveImageIndex = 0;
        IsActive = true;
        OnPropertyChanged(nameof(VisibleIndicesText));
    }

    public void ExitComparison()
    {
        VisibleImages.Clear();
        Viewports.Clear();
        IsActive = false;
        ActiveImageIndex = 0;
        OnPropertyChanged(nameof(VisibleIndicesText));
    }

    public void SelectImage(int visibleIndex)
    {
        if (!IsActive || VisibleImages.Count == 0)
        {
            return;
        }

        ActiveImageIndex = Math.Clamp(visibleIndex, 0, VisibleImages.Count - 1);
    }

    public void SelectNextImage()
    {
        if (!IsActive || VisibleImages.Count == 0)
        {
            return;
        }

        ActiveImageIndex = (ActiveImageIndex + 1) % VisibleImages.Count;
    }

    public void SetOverlayOpacity(double opacity)
    {
        OverlayOpacity = Math.Clamp(opacity, 0, 1);
    }

    public void AdjustOverlayOpacity(double delta)
    {
        SetOverlayOpacity(OverlayOpacity + delta);
    }

    public void SwapOverlayLayers()
    {
        if (!IsActive || Layout != ComparisonLayout.Overlay || VisibleImages.Count < 2)
        {
            return;
        }

        var first = VisibleImages[0];
        var second = VisibleImages[1];
        VisibleImages[0] = second;
        VisibleImages[1] = first;
        ActiveImageIndex = 0;
        OnPropertyChanged(nameof(VisibleIndicesText));
    }

    public void ToggleSyncZoom()
    {
        IsSyncZoomEnabled = !IsSyncZoomEnabled;
    }

    public void SetSyncZoom(bool isEnabled)
    {
        IsSyncZoomEnabled = isEnabled;
    }

    public void SetSynchronizedZoom(double zoomLevel)
    {
        if (!IsSyncZoomEnabled)
        {
            SetActiveViewportZoom(zoomLevel);
            return;
        }

        var normalizedZoom = NormalizeZoom(zoomLevel);
        for (var index = 0; index < Viewports.Count; index++)
        {
            Viewports[index] = Viewports[index] with { ZoomLevel = normalizedZoom };
        }
    }

    public void SetActiveViewportZoom(double zoomLevel)
    {
        if (!IsActive || ActiveImageIndex < 0 || ActiveImageIndex >= Viewports.Count)
        {
            return;
        }

        Viewports[ActiveImageIndex] = Viewports[ActiveImageIndex] with
        {
            ZoomLevel = NormalizeZoom(zoomLevel)
        };
    }

    public void SetSynchronizedRelativePan(RelativePanOffset relativePan)
    {
        if (!IsSyncZoomEnabled)
        {
            SetActiveViewportRelativePan(relativePan);
            return;
        }

        var clampedPan = relativePan.Clamp();
        for (var index = 0; index < Viewports.Count; index++)
        {
            Viewports[index] = Viewports[index] with { RelativePan = clampedPan };
        }
    }

    public void SetActiveViewportRelativePan(RelativePanOffset relativePan)
    {
        if (!IsActive || ActiveImageIndex < 0 || ActiveImageIndex >= Viewports.Count)
        {
            return;
        }

        Viewports[ActiveImageIndex] = Viewports[ActiveImageIndex] with
        {
            RelativePan = relativePan.Clamp()
        };
    }

    public void ResetSynchronizedZoom()
    {
        if (!IsSyncZoomEnabled)
        {
            if (ActiveImageIndex < 0 || ActiveImageIndex >= Viewports.Count)
            {
                return;
            }

            Viewports[ActiveImageIndex] = CreateDefaultViewport(ActiveImageIndex);
            return;
        }

        for (var index = 0; index < Viewports.Count; index++)
        {
            Viewports[index] = CreateDefaultViewport(index);
        }
    }

    public ComparisonImage? ActiveImage => IsActive && ActiveImageIndex >= 0 && ActiveImageIndex < VisibleImages.Count
        ? VisibleImages[ActiveImageIndex]
        : null;

    public ComparisonViewport? ActiveViewport => IsActive && ActiveImageIndex >= 0 && ActiveImageIndex < Viewports.Count
        ? Viewports[ActiveImageIndex]
        : null;

    public string VisibleIndicesText => string.Join(
        "  ",
        VisibleImages.Select(image => $"{image.PlaylistIndex + 1}"));

    private static ComparisonViewport CreateDefaultViewport(int visibleIndex)
    {
        return new ComparisonViewport(visibleIndex, 100, new RelativePanOffset(0.5, 0.5));
    }

    private static double NormalizeZoom(double zoomLevel)
    {
        return Math.Max(10, zoomLevel);
    }
}
