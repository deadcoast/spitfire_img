using System.ComponentModel;
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

    public void Show()
    {
        Visibility = Visibility.Visible;
        AnimateOpacity(1, 120);
        SearchInput.Focus(FocusState.Programmatic);
    }

    public void Hide()
    {
        AnimateOpacity(0, 80);
        Visibility = Visibility.Collapsed;
    }

    private void AnimateOpacity(double opacity, int durationMs)
    {
        var animation = new DoubleAnimation
        {
            To = opacity,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EnableDependentAnimation = true
        };

        Storyboard.SetTarget(animation, this);
        Storyboard.SetTargetProperty(animation, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin();
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

        ResultCountText.Text = _viewModel.SearchState.ResultCount == 0
            ? "No matching images found"
            : _viewModel.SearchState.ResultCountText;
        ResultsList.ItemsSource = _viewModel.SearchState.FilteredPlaylist;
        ResultsList.SelectedIndex = _viewModel.SearchState.SelectedResultIndex;
        _isUpdating = false;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        _searchDebounceTimer.Stop();
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
