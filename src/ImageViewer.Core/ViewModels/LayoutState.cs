using System.Collections.ObjectModel;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class LayoutState : ObservableObject
{
    private LayoutConfig _config = LayoutConfig.Default;
    private GridLayoutPlan _gridLayout = GridLayoutPlan.Empty;
    private int _gridStartIndex;
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

    public ObservableCollection<GridImage> GridImages { get; } = [];

    public void EnterGrid(
        GridLayoutPlan layout,
        IEnumerable<GridImage> images,
        int startIndex)
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
    }

    public void ExitLayout()
    {
        Config = Config.WithMode(LayoutMode.Single);
        GridLayout = GridLayoutPlan.Empty;
        GridStartIndex = 0;
        GridImages.Clear();
        OnPropertyChanged(nameof(GridImages));
    }

    private void ReplaceGridImages(IEnumerable<GridImage> images)
    {
        GridImages.Clear();
        foreach (var image in images)
        {
            GridImages.Add(image);
        }

        OnPropertyChanged(nameof(GridImages));
    }
}
