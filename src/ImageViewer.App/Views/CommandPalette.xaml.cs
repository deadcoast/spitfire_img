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
    private static readonly TimeSpan SearchDebounceInterval = TimeSpan.FromMilliseconds(50);
    private readonly DispatcherTimer _searchDebounceTimer;
    private MainViewModel? _viewModel;
    private bool _isUpdating;

    public CommandPalette()
    {
        InitializeComponent();
        _searchDebounceTimer = new DispatcherTimer { Interval = SearchDebounceInterval };
        _searchDebounceTimer.Tick += OnSearchDebounceElapsed;
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
        _searchDebounceTimer.Stop();
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

        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private void OnSearchDebounceElapsed(object? sender, object e)
    {
        _searchDebounceTimer.Stop();
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
        if (sender is TextBlock textBlock)
        {
            UpdateCommandHeader(textBlock);
        }
    }

    private void OnCommandHeaderDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (sender is TextBlock textBlock)
        {
            UpdateCommandHeader(textBlock);
        }
    }

    private void OnCommandRowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            UpdateCommandRow(element);
        }
    }

    private void OnCommandRowDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        UpdateCommandRow(sender);
    }

    private void OnCommandTitleLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            UpdateCommandTitle(textBlock);
        }
    }

    private void OnCommandTitleDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (sender is TextBlock textBlock)
        {
            UpdateCommandTitle(textBlock);
        }
    }

    private static void UpdateCommandHeader(TextBlock textBlock)
    {
        if (GetEntry(textBlock) is not { } entry)
        {
            return;
        }

        textBlock.Text = entry.HeaderText;
        textBlock.Visibility = entry.IsHeader ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void UpdateCommandRow(FrameworkElement element)
    {
        if (GetEntry(element) is not { } entry)
        {
            return;
        }

        element.Visibility = entry.IsHeader ? Visibility.Collapsed : Visibility.Visible;
    }

    private static void UpdateCommandTitle(TextBlock textBlock)
    {
        if (GetMatch(textBlock) is not { } match)
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

    private static CommandPaletteEntry? GetEntry(FrameworkElement element)
    {
        return element.DataContext as CommandPaletteEntry ?? element.Tag as CommandPaletteEntry;
    }

    private static CommandPaletteMatch? GetMatch(FrameworkElement element)
    {
        return element.Tag as CommandPaletteMatch ??
               (element.DataContext as CommandPaletteEntry)?.Match;
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
