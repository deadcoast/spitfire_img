using System.ComponentModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Text;

namespace ImageViewer.App.Views;

public sealed partial class CommandPalette : UserControl
{
    private MainViewModel? _viewModel;
    private bool _isUpdating;

    public CommandPalette()
    {
        InitializeComponent();
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.CommandPaletteState.PropertyChanged += OnCommandPaletteStateChanged;
        Refresh();
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

    private void OnCommandPaletteStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        if (_viewModel is null)
        {
            return;
        }

        var state = _viewModel.CommandPaletteState;
        Visibility = state.IsVisible ? Visibility.Visible : Visibility.Collapsed;

        _isUpdating = true;
        if (!string.Equals(SearchInput.Text, state.SearchText, StringComparison.Ordinal))
        {
            SearchInput.Text = state.SearchText;
        }

        CommandList.ItemsSource = state.DisplayEntries;
        CommandList.SelectedIndex = state.SelectedIndex;
        CommandList.Visibility = state.DisplayEntries.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        NoMatchesText.Visibility = string.IsNullOrEmpty(state.StatusText) ? Visibility.Collapsed : Visibility.Visible;
        _isUpdating = false;

        if (state.IsVisible)
        {
            SearchInput.Focus(FocusState.Programmatic);
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        _viewModel?.CommandPaletteState.SetSearchText(SearchInput.Text);
    }

    private async void OnPaletteKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.Escape:
                _viewModel.HideCommandPalette();
                e.Handled = true;
                break;
            case VirtualKey.Down:
                _viewModel.CommandPaletteState.SelectNext();
                e.Handled = true;
                break;
            case VirtualKey.Up:
                _viewModel.CommandPaletteState.SelectPrevious();
                e.Handled = true;
                break;
            case VirtualKey.Enter:
                await _viewModel.ExecuteSelectedCommandPaletteCommandAsync();
                e.Handled = true;
                break;
        }
    }

    private async void OnCommandClicked(object sender, ItemClickEventArgs e)
    {
        if (_viewModel is null || e.ClickedItem is not CommandPaletteEntry entry || entry.Command is null)
        {
            return;
        }

        _viewModel.CommandPaletteState.SelectCommandById(entry.Command.Id);
        await _viewModel.ExecuteSelectedCommandPaletteCommandAsync();
    }

    private void OnCommandHeaderLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock textBlock || textBlock.Tag is not CommandPaletteEntry entry)
        {
            return;
        }

        textBlock.Text = entry.HeaderText;
        textBlock.Visibility = entry.IsHeader ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnCommandRowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not CommandPaletteEntry entry)
        {
            return;
        }

        element.Visibility = entry.IsHeader ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnCommandTitleLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock textBlock || textBlock.Tag is not CommandPaletteMatch match)
        {
            return;
        }

        textBlock.Inlines.Clear();
        var highlightedIndices = match.MatchedTitleIndices.ToHashSet();

        for (var index = 0; index < match.Title.Length; index++)
        {
            var run = new Run { Text = match.Title[index].ToString() };
            if (highlightedIndices.Contains(index))
            {
                run.FontWeight = new FontWeight { Weight = 600 };
                run.Foreground = new SolidColorBrush(Colors.Yellow);
            }

            textBlock.Inlines.Add(run);
        }
    }

    private void OnPaletteRootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_viewModel is null || e.OriginalSource is not Grid)
        {
            return;
        }

        _viewModel.HideCommandPalette();
        e.Handled = true;
    }
}
