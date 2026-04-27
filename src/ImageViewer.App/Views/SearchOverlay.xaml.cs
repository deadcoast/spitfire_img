using System.ComponentModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace ImageViewer.App.Views;

public sealed partial class SearchOverlay : UserControl
{
    private MainViewModel? _viewModel;
    private bool _isUpdating;

    public SearchOverlay()
    {
        InitializeComponent();
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
        SearchInput.Focus(FocusState.Programmatic);
    }

    public void Hide()
    {
        Visibility = Visibility.Collapsed;
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
                if (_viewModel.SearchState.SelectedResult is not null)
                {
                    await _viewModel.OpenFileAsync(_viewModel.SearchState.SelectedResult.FilePath);
                    _viewModel.HideSearch();
                    Hide();
                }

                e.Handled = true;
                break;
        }
    }

    private async void OnResultSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating || _viewModel is null || ResultsList.SelectedItem is not ImageItem item)
        {
            return;
        }

        await _viewModel.OpenFileAsync(item.FilePath);
        _viewModel.HideSearch();
        Hide();
    }
}
