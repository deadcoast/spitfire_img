using System.ComponentModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;

namespace ImageViewer.App.Views;

public sealed partial class CommandPalette : UserControl
{
    private static readonly TimeSpan SearchDebounceInterval = TimeSpan.FromMilliseconds(50);
    private MainViewModel? _viewModel;
    private bool _isUpdating;
    private readonly DispatcherTimer _searchDebounceTimer;
    private string _pendingSearchQuery = string.Empty;

    public CommandPalette()
    {
        InitializeComponent();
        _searchDebounceTimer = new DispatcherTimer { Interval = SearchDebounceInterval };
        _searchDebounceTimer.Tick += OnSearchDebounceTick;
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.CommandPaletteState.PropertyChanged += OnCommandPaletteStateChanged;
        Refresh();
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

        _isUpdating = true;
        if (!string.Equals(CommandInput.Text, _viewModel.CommandPaletteState.SearchQuery, StringComparison.Ordinal))
        {
            CommandInput.Text = _viewModel.CommandPaletteState.SearchQuery;
        }

        RebuildCommandList();
        CommandList.SelectedIndex = _viewModel.CommandPaletteState.SelectedDisplayIndex;
        EmptyStateText.Text = _viewModel.CommandPaletteState.EmptyMessage;
        EmptyStateText.Visibility = _viewModel.CommandPaletteState.HasNoMatches
            ? Visibility.Visible
            : Visibility.Collapsed;
        CommandList.Visibility = _viewModel.CommandPaletteState.HasNoMatches
            ? Visibility.Collapsed
            : Visibility.Visible;
        _isUpdating = false;

        if (_viewModel.CommandPaletteState.IsOpen)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void RebuildCommandList()
    {
        if (_viewModel is null)
        {
            return;
        }

        CommandList.Items.Clear();
        foreach (var item in _viewModel.CommandPaletteState.DisplayItems)
        {
            CommandList.Items.Add(item.IsHeader
                ? CreateHeaderListItem(item.Header ?? string.Empty)
                : CreateCommandListItem(item.Result!));
        }
    }

    private static ListViewItem CreateHeaderListItem(string header)
    {
        return new ListViewItem
        {
            Content = new TextBlock
            {
                Text = header,
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(180, 220, 220, 220)),
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            },
            IsEnabled = false,
            Tag = CommandPaletteDisplayItem.HeaderItem(header)
        };
    }

    private ListViewItem CreateCommandListItem(CommandSearchResult result)
    {
        var titleText = new TextBlock
        {
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
        };
        AddHighlightedTitle(titleText, result);

        var categoryText = new TextBlock
        {
            Text = result.Category.ToString(),
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(190, 220, 220, 220)),
            FontSize = 12
        };

        var shortcutText = new TextBlock
        {
            Text = result.Shortcut,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(220, 230, 230, 230))
        };

        var textStack = new StackPanel();
        textStack.Children.Add(titleText);
        textStack.Children.Add(categoryText);

        var grid = new Grid
        {
            ColumnSpacing = 12,
            Padding = new Thickness(2)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(textStack);
        Grid.SetColumn(shortcutText, 1);
        grid.Children.Add(shortcutText);

        return new ListViewItem
        {
            Content = grid,
            Tag = CommandPaletteDisplayItem.CommandItem(result)
        };
    }

    private static void AddHighlightedTitle(TextBlock textBlock, CommandSearchResult result)
    {
        var matched = result.MatchIndexes.ToHashSet();
        for (var index = 0; index < result.Command.Title.Length; index++)
        {
            var run = new Run { Text = result.Command.Title[index].ToString() };
            if (matched.Contains(index))
            {
                textBlock.Inlines.Add(new Bold { Inlines = { run } });
            }
            else
            {
                textBlock.Inlines.Add(run);
            }
        }
    }

    private void Show()
    {
        Visibility = Visibility.Visible;
        AnimateOpacity(1, 100);
        CommandInput.Focus(FocusState.Programmatic);
    }

    private void Hide()
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

    private async void OnCommandInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        FlushPendingSearchQuery();
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
                await _viewModel.ExecuteSelectedCommandAsync();
                e.Handled = true;
                break;
        }
    }

    private void OnCommandInputTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        _pendingSearchQuery = CommandInput.Text;
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private void OnSearchDebounceTick(object? sender, object e)
    {
        _searchDebounceTimer.Stop();
        ApplyPendingSearchQuery();
    }

    private void FlushPendingSearchQuery()
    {
        if (!_searchDebounceTimer.IsEnabled)
        {
            return;
        }

        _searchDebounceTimer.Stop();
        ApplyPendingSearchQuery();
    }

    private void ApplyPendingSearchQuery()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.CommandPaletteState.SetSearchQuery(_pendingSearchQuery);
    }

    private async void OnCommandClicked(object sender, ItemClickEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        var result = e.ClickedItem switch
        {
            ListViewItem { Tag: CommandPaletteDisplayItem { Result: not null } clickedItem } => clickedItem.Result,
            CommandPaletteDisplayItem { Result: not null } clickedItem => clickedItem.Result,
            _ => null
        };
        if (result is null)
        {
            return;
        }

        _viewModel.CommandPaletteState.SelectCommand(result.Command.Id);
        await _viewModel.ExecuteSelectedCommandAsync();
    }

    private void OnOverlayTapped(object sender, TappedRoutedEventArgs e)
    {
        _viewModel?.HideCommandPalette();
    }

    private void OnPanelTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }
}
