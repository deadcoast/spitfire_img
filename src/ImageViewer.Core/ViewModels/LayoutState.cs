using System.Collections.ObjectModel;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class LayoutState : ObservableObject
{
    private LayoutConfig _config = LayoutConfig.Default;
    private GridLayoutPlan _gridLayout = GridLayoutPlan.Empty;
    private int _gridStartIndex;
    private int _activeGridImageIndex;
    private int _filmstripFocusIndex;

    public LayoutConfig Config
    {
        get => _config;
        private set
        {
            if (SetProperty(ref _config, value))
            {
                OnPropertyChanged(nameof(CurrentLayout));
                OnPropertyChanged(nameof(IsGridActive));
                OnPropertyChanged(nameof(GridRows));
                OnPropertyChanged(nameof(GridColumns));
            }
        }
    }

    public LayoutMode CurrentLayout => Config.Mode;

    public bool IsGridActive => CurrentLayout == LayoutMode.Grid;

    public int GridRows => Config.GridRows;

    public int GridColumns => Config.GridColumns;

    public int GridStartIndex
    {
        get => _gridStartIndex;
        private set => SetProperty(ref _gridStartIndex, value);
    }

    public GridLayoutPlan GridLayout
    {
        get => _gridLayout;
        private set => SetProperty(ref _gridLayout, value);
    }

    public int FilmstripFocusIndex
    {
        get => _filmstripFocusIndex;
        private set => SetProperty(ref _filmstripFocusIndex, value);
    }

    public int ActiveGridImageIndex
    {
        get => _activeGridImageIndex;
        private set
        {
            if (SetProperty(ref _activeGridImageIndex, value))
            {
                OnPropertyChanged(nameof(ActiveGridPlaylistIndex));
            }
        }
    }

    public int ActiveGridPlaylistIndex => ActiveGridImageIndex >= 0 && ActiveGridImageIndex < GridImages.Count
        ? GridImages[ActiveGridImageIndex].PlaylistIndex
        : -1;

    public ObservableCollection<GridImage> GridImages { get; } = [];

    public void EnterGrid(
        GridLayoutPlan layout,
        IEnumerable<GridImage> images,
        int startIndex)
    {
        ApplyGrid(layout, images, startIndex, preferredActivePlaylistIndex: startIndex);
    }

    public void UpdateGrid(
        GridLayoutPlan layout,
        IEnumerable<GridImage> images,
        int startIndex)
    {
        ApplyGrid(layout, images, startIndex, ActiveGridPlaylistIndex);
    }

    public void SelectGridImage(int visibleIndex)
    {
        if (GridImages.Count == 0)
        {
            ActiveGridImageIndex = 0;
            return;
        }

        ActiveGridImageIndex = Math.Clamp(visibleIndex, 0, GridImages.Count - 1);
    }

    public void ExitLayout()
    {
        Config = Config.WithMode(LayoutMode.Single);
        GridLayout = GridLayoutPlan.Empty;
        GridStartIndex = 0;
        GridImages.Clear();
        ActiveGridImageIndex = 0;
        OnPropertyChanged(nameof(GridImages));
        OnPropertyChanged(nameof(ActiveGridPlaylistIndex));
    }

    private void ApplyGrid(
        GridLayoutPlan layout,
        IEnumerable<GridImage> images,
        int startIndex,
        int preferredActivePlaylistIndex)
    {
        Config = Config with
        {
            Mode = LayoutMode.Grid,
            GridRows = layout.Rows,
            GridColumns = layout.Columns
        };
        GridLayout = layout;
        GridStartIndex = startIndex;
        ReplaceGridImages(images);
        SelectGridImageByPlaylistIndex(preferredActivePlaylistIndex);
    }

    private void ReplaceGridImages(IEnumerable<GridImage> images)
    {
        GridImages.Clear();
        foreach (var image in images)
        {
            GridImages.Add(image);
        }

        OnPropertyChanged(nameof(GridImages));
        OnPropertyChanged(nameof(ActiveGridPlaylistIndex));
    }

    private void SelectGridImageByPlaylistIndex(int playlistIndex)
    {
        var visibleIndex = GridImages
            .Select((image, index) => (image.PlaylistIndex, Index: index))
            .FirstOrDefault(candidate => candidate.PlaylistIndex == playlistIndex)
            .Index;

        if (GridImages.Count == 0)
        {
            ActiveGridImageIndex = 0;
            return;
        }

        if (visibleIndex == 0 && GridImages[0].PlaylistIndex != playlistIndex)
        {
            ActiveGridImageIndex = 0;
            return;
        }

        ActiveGridImageIndex = visibleIndex;
    }
}
