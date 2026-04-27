using System.Collections.ObjectModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Core.ViewModels;

public sealed class SearchState : ObservableObject
{
    private readonly ISearchService _searchService;
    private readonly List<ImageItem> _sourcePlaylist = [];
    private string _searchQuery = string.Empty;
    private bool _isSearchActive;
    private int _selectedResultIndex;

    public SearchState(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public ObservableCollection<ImageItem> FilteredPlaylist { get; } = [];

    public string SearchQuery
    {
        get => _searchQuery;
        private set => SetProperty(ref _searchQuery, value);
    }

    public bool IsSearchActive
    {
        get => _isSearchActive;
        private set => SetProperty(ref _isSearchActive, value);
    }

    public int ResultCount => FilteredPlaylist.Count;

    public string ResultCountText => ResultCount == 1 ? "1 match" : $"{ResultCount} matches";

    public int SelectedResultIndex
    {
        get => _selectedResultIndex;
        private set => SetProperty(ref _selectedResultIndex, value);
    }

    public ImageItem? SelectedResult => SelectedResultIndex >= 0 && SelectedResultIndex < FilteredPlaylist.Count
        ? FilteredPlaylist[SelectedResultIndex]
        : null;

    public void SetPlaylist(IEnumerable<ImageItem> items)
    {
        _sourcePlaylist.Clear();
        _sourcePlaylist.AddRange(items);
        ApplyFilter();
    }

    public void SetSearchQuery(string query)
    {
        SearchQuery = query;
        IsSearchActive = !string.IsNullOrWhiteSpace(query);
        ApplyFilter();
    }

    public void ClearSearch()
    {
        SearchQuery = string.Empty;
        IsSearchActive = false;
        ApplyFilter();
    }

    public void SelectNext()
    {
        if (FilteredPlaylist.Count == 0)
        {
            SelectedResultIndex = -1;
            return;
        }

        SelectedResultIndex = (SelectedResultIndex + 1) % FilteredPlaylist.Count;
    }

    public void SelectPrevious()
    {
        if (FilteredPlaylist.Count == 0)
        {
            SelectedResultIndex = -1;
            return;
        }

        SelectedResultIndex = SelectedResultIndex <= 0
            ? FilteredPlaylist.Count - 1
            : SelectedResultIndex - 1;
    }

    public void SelectResult(int index)
    {
        SelectedResultIndex = FilteredPlaylist.Count == 0 ? -1 : Math.Clamp(index, 0, FilteredPlaylist.Count - 1);
    }

    private void ApplyFilter()
    {
        var result = _searchService.FilterPlaylist(_sourcePlaylist, SearchQuery);
        FilteredPlaylist.Clear();
        foreach (var item in result.Value)
        {
            FilteredPlaylist.Add(item);
        }

        SelectedResultIndex = FilteredPlaylist.Count > 0 ? 0 : -1;
        OnPropertyChanged(nameof(ResultCount));
        OnPropertyChanged(nameof(ResultCountText));
        OnPropertyChanged(nameof(SelectedResult));
    }
}
