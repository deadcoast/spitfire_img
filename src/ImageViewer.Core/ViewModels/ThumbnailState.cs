using System.Collections.ObjectModel;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class ThumbnailState : ObservableObject
{
    private bool _isStripVisible;
    private ThumbnailPosition _stripPosition = ThumbnailPosition.Bottom;
    private long _cacheSize;
    private int _generationProgress;

    public ObservableCollection<Thumbnail> Thumbnails { get; } = [];

    public bool IsStripVisible
    {
        get => _isStripVisible;
        private set => SetProperty(ref _isStripVisible, value);
    }

    public ThumbnailPosition StripPosition
    {
        get => _stripPosition;
        private set => SetProperty(ref _stripPosition, value);
    }

    public long CacheSize
    {
        get => _cacheSize;
        private set => SetProperty(ref _cacheSize, value);
    }

    public int GenerationProgress
    {
        get => _generationProgress;
        private set => SetProperty(ref _generationProgress, value);
    }

    public void ToggleVisibility()
    {
        IsStripVisible = !IsStripVisible;
    }

    public void SetVisibility(bool isVisible)
    {
        IsStripVisible = isVisible;
    }

    public void SetStripPosition(ThumbnailPosition position)
    {
        StripPosition = position;
    }

    public void SetThumbnails(IEnumerable<Thumbnail> thumbnails, long cacheSize)
    {
        Thumbnails.Clear();
        foreach (var thumbnail in thumbnails)
        {
            Thumbnails.Add(thumbnail);
        }

        CacheSize = cacheSize;
        GenerationProgress = Thumbnails.Count;
    }
}
