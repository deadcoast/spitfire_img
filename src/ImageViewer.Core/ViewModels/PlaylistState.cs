using System.Collections.ObjectModel;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class PlaylistState : ObservableObject
{
    private int _currentIndex = -1;
    private string _folderPath = string.Empty;

    public ObservableCollection<ImageItem> Items { get; } = [];

    public int CurrentIndex
    {
        get => _currentIndex;
        private set
        {
            if (SetProperty(ref _currentIndex, value))
            {
                OnPropertyChanged(nameof(CurrentItem));
                OnPropertyChanged(nameof(CanNavigateNext));
                OnPropertyChanged(nameof(CanNavigatePrevious));
            }
        }
    }

    public int TotalCount => Items.Count;

    public ImageItem? CurrentItem => CurrentIndex >= 0 && CurrentIndex < Items.Count
        ? Items[CurrentIndex]
        : null;

    public bool CanNavigateNext => CurrentIndex >= 0 && CurrentIndex < Items.Count - 1;

    public bool CanNavigatePrevious => CurrentIndex > 0;

    public string FolderPath
    {
        get => _folderPath;
        private set => SetProperty(ref _folderPath, value);
    }

    public void SetPlaylist(Playlist playlist)
    {
        Items.Clear();
        foreach (var item in playlist.Items)
        {
            Items.Add(item);
        }

        FolderPath = playlist.FolderPath;
        CurrentIndex = playlist.CurrentIndex;
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(CurrentItem));
        OnPropertyChanged(nameof(CanNavigateNext));
        OnPropertyChanged(nameof(CanNavigatePrevious));
    }

    public void MoveNext()
    {
        if (CanNavigateNext)
        {
            CurrentIndex++;
        }
    }

    public void MovePrevious()
    {
        if (CanNavigatePrevious)
        {
            CurrentIndex--;
        }
    }

    public void MoveFirst()
    {
        if (Items.Count > 0)
        {
            CurrentIndex = 0;
        }
    }

    public void MoveLast()
    {
        if (Items.Count > 0)
        {
            CurrentIndex = Items.Count - 1;
        }
    }

    public void SetCurrentIndex(int index)
    {
        if (Items.Count == 0)
        {
            CurrentIndex = -1;
            return;
        }

        CurrentIndex = Math.Clamp(index, 0, Items.Count - 1);
    }
}
