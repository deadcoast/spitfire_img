using System.ComponentModel;
using ImageViewer.App.Services.Motion;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;

namespace ImageViewer.App.Views;

public sealed partial class SearchOverlay : UserControl
{
    private MainViewModel? _viewModel;
    private bool _isUpdating;
    private readonly DispatcherTimer _searchDebounceTimer;
    private IViewerMotionService? _motionService;

    public SearchOverlay()
    {
        InitializeComponent();
        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _searchDebounceTimer.Tick += OnSearchDebounceElapsed;
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.SearchState.PropertyChanged += OnSearchStateChanged;
        RefreshResults();
    }

    public void AttachMotionService(IViewerMotionService motionService)
    {
        _motionService = motionService;
    }

    public void Show()
    {
        Visibility = Visibility.Visible;
        ViewerMotionStoryboard.AnimateOverlay(
            this,
            SearchOverlayPanelTransform,
            GetMotionPlan(ViewerMotionCategory.OverlayEntrance),
            isEntering: true);
        SearchInput.Focus(FocusState.Programmatic);
    }

    public void Hide()
    {
        _searchDebounceTimer.Stop();
        ViewerMotionStoryboard.AnimateOverlay(
            this,
            SearchOverlayPanelTransform,
            GetMotionPlan(ViewerMotionCategory.OverlayExit),
            isEntering: false,
            completed: () => Visibility = Visibility.Collapsed);
    }

    private ViewerMotionPlan GetMotionPlan(ViewerMotionCategory category)
    {
        return _motionService?.CreatePlan(category) ?? ViewerMotionService.CreateDefaultPlan(category, isReducedMotion: false);
    }

    private void OnSearchStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(RefreshResults);
    }

    private void RefreshResults()
    {
        if (_viewModel is null)
        {
            return;
        }

        _isUpdating = true;
        if (!string.Equals(SearchInput.Text, _viewModel.SearchState.SearchQuery, StringComparison.Ordinal))
        {
            SearchInput.Text = _viewModel.SearchState.SearchQuery;
        }

        UpdateSearchSummary(_viewModel.SearchState);
        ResultsList.ItemsSource = _viewModel.SearchState.FilteredPlaylist;
        ResultsList.SelectedIndex = _viewModel.SearchState.SelectedResultIndex;
        ResultsList.Visibility = _viewModel.SearchState.ResultCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        NoSearchResultsPanel.Visibility = _viewModel.SearchState.ResultCount == 0 && _viewModel.SearchState.IsSearchActive
            ? Visibility.Visible
            : Visibility.Collapsed;
        _isUpdating = false;
    }

    private void UpdateSearchSummary(SearchState state)
    {
        if (!state.IsSearchActive)
        {
            SearchSummaryText.Text = state.ResultCount == 0
                ? "Ready"
                : $"{state.ResultCountText} in folder";
            return;
        }

        SearchSummaryText.Text = state.ResultCount == 0
            ? "No results"
            : $"{state.ResultCountText} - Up/Down to select";
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        _searchDebounceTimer.Stop();
        if (SearchInput.Text.Length == 0)
            _viewModel?.SetSearchQuery(string.Empty);
        else
            _searchDebounceTimer.Start();
    }

    private void OnSearchDebounceElapsed(object? sender, object e)
    {
        _searchDebounceTimer.Stop();
        _viewModel?.SetSearchQuery(SearchInput.Text);
    }

    private async void OnSearchKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.Escape:
                _viewModel.HideSearch();
                Hide();
                e.Handled = true;
                break;
            case VirtualKey.Down:
                _viewModel.SearchState.SelectNext();
                e.Handled = true;
                break;
            case VirtualKey.Up:
                _viewModel.SearchState.SelectPrevious();
                e.Handled = true;
                break;
            case VirtualKey.Enter:
                var selectedResult = _viewModel.SearchState.SelectedResult;
                if (selectedResult is not null)
                {
                    _viewModel.HideSearch();
                    Hide();
                    await _viewModel.OpenFileAsync(selectedResult.FilePath);
                }

                e.Handled = true;
                break;
        }
    }

    private async void OnResultClicked(object sender, ItemClickEventArgs e)
    {
        if (_viewModel is null || e.ClickedItem is not ImageItem item)
        {
            return;
        }

        _viewModel.HideSearch();
        Hide();
        await _viewModel.OpenFileAsync(item.FilePath);
    }
}
