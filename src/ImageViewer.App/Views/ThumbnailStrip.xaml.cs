using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace ImageViewer.App.Views;

public sealed partial class ThumbnailStrip : UserControl
{
    private MainViewModel? _viewModel;
    private int _generationVersion;
    private readonly ObservableCollection<ThumbnailStripItem> _items = [];

    public ThumbnailStrip()
    {
        InitializeComponent();
        RightTapped += OnRightTapped;
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.ThumbnailState.PropertyChanged += OnStateChanged;
        viewModel.PlaylistState.PropertyChanged += OnStateChanged;
        viewModel.ImageContentChanged += (_, _) => DispatcherQueue.TryEnqueue(() =>
        {
            _generationVersion++;
            _items.Clear();
            Refresh();
        });
        Refresh();
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        if (_viewModel is null)
        {
            return;
        }

        Visibility = _viewModel.ThumbnailState.IsStripVisible ? Visibility.Visible : Visibility.Collapsed;
        StripBorder.VerticalAlignment = _viewModel.ThumbnailState.StripPosition == Core.Models.ThumbnailPosition.Top
            ? VerticalAlignment.Top
            : VerticalAlignment.Bottom;
        StripBorder.Margin = _viewModel.ThumbnailState.StripPosition == Core.Models.ThumbnailPosition.Top
            ? new Thickness(8, 48, 8, 0)
            : new Thickness(8, 0, 8, 38);
        if (!_viewModel.ThumbnailState.IsStripVisible)
        {
            return;
        }

        if (_items.Select(item => item.FilePath).SequenceEqual(_viewModel.PlaylistState.Items.Select(item => item.FilePath)))
        {
            UpdateSelection();
            return;
        }
        _ = LoadThumbnailsAsync(++_generationVersion);
    }

    private void UpdateSelection()
    {
        foreach (var item in _items)
            item.SetCurrent(string.Equals(item.FilePath, _viewModel?.PlaylistState.CurrentItem?.FilePath, StringComparison.OrdinalIgnoreCase));
        if (_viewModel?.PlaylistState.CurrentIndex is >= 0 and var index && index < _items.Count)
            ThumbnailStripList.ScrollIntoView(_items[index]);
    }

    private async Task LoadThumbnailsAsync(int generationVersion)
    {
        if (_viewModel is null)
        {
            return;
        }

        var playlistItems = _viewModel.PlaylistState.Items.ToArray();
        var currentIndex = _viewModel.PlaylistState.CurrentIndex;
        _items.Clear();
        ThumbnailStripList.ItemsSource = _items;

        foreach (var item in playlistItems)
        {
            var isCurrent = item.FilePath == _viewModel.PlaylistState.CurrentItem?.FilePath;

            _items.Add(new ThumbnailStripItem(
                item.FilePath,
                item.Filename,
                imageSource: null,
                placeholderText: "Loading",
                new Thickness(2),
                isCurrent));
        }

        foreach (var index in ThumbnailLoadPlanner.PrioritizeVisibleFirst(playlistItems.Length, currentIndex))
        {
            if (generationVersion != _generationVersion)
            {
                return;
            }

            var item = playlistItems[index];
            var thumbnailResult = await _viewModel.GetThumbnailAsync(item);
            var imageSource = thumbnailResult.IsSuccess
                ? await CreateBitmapImageAsync(thumbnailResult.Value.EncodedBytes)
                : null;

            if (generationVersion != _generationVersion) return;
            _items[index].ImageSource = imageSource;
            _items[index].PlaceholderText = imageSource is null ? "No preview" : string.Empty;
        }
        UpdateSelection();
    }

    private static async Task<BitmapImage?> CreateBitmapImageAsync(byte[] encodedBytes)
    {
        if (encodedBytes.Length == 0)
        {
            return null;
        }

        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(encodedBytes.AsBuffer());
        stream.Seek(0);

        var image = new BitmapImage();
        await image.SetSourceAsync(stream);
        return image;
    }

    private void OnRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        var flyout = new MenuFlyout();
        var top = new MenuFlyoutItem { Text = "Top" };
        top.Click += async (_, _) => await _viewModel.SetThumbnailPosition(Core.Models.ThumbnailPosition.Top);
        var bottom = new MenuFlyoutItem { Text = "Bottom" };
        bottom.Click += async (_, _) => await _viewModel.SetThumbnailPosition(Core.Models.ThumbnailPosition.Bottom);
        flyout.Items.Add(top);
        flyout.Items.Add(bottom);
        flyout.ShowAt(this, e.GetPosition(this));
    }

    private async void OnThumbnailClicked(object sender, ItemClickEventArgs e)
    {
        if (_viewModel is null || e.ClickedItem is not ThumbnailStripItem item)
        {
            return;
        }

        var index = _viewModel.PlaylistState.Items.ToList().FindIndex(candidate =>
            string.Equals(candidate.FilePath, item.FilePath, StringComparison.OrdinalIgnoreCase));
        if (index >= 0) await _viewModel.NavigateToPlaylistIndexAsync(index);
    }

    public sealed class ThumbnailStripItem : ObservableObject
    {
        private BitmapImage? _imageSource;
        private string _placeholderText;
        private Thickness _borderThickness;
        private string _currentLabel;

        public ThumbnailStripItem(
            string filePath,
            string fileName,
            BitmapImage? imageSource,
            string placeholderText,
            Thickness borderThickness,
            bool isCurrent)
        {
            FilePath = filePath;
            FileName = fileName;
            _imageSource = imageSource;
            _placeholderText = placeholderText;
            _borderThickness = borderThickness;
            _currentLabel = isCurrent ? "Current" : string.Empty;
        }

        public string FilePath { get; }

        public string FileName { get; }

        public BitmapImage? ImageSource
        {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
        }

        public string PlaceholderText
        {
            get => _placeholderText;
            set
            {
                if (SetProperty(ref _placeholderText, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public Thickness BorderThickness { get => _borderThickness; private set => SetProperty(ref _borderThickness, value); }

        public string CurrentLabel { get => _currentLabel; private set => SetProperty(ref _currentLabel, value); }

        public void SetCurrent(bool isCurrent)
        {
            BorderThickness = new Thickness(2);
            CurrentLabel = isCurrent ? "Current" : string.Empty;
        }

        public string StatusText => string.IsNullOrWhiteSpace(PlaceholderText) ? string.Empty : PlaceholderText;
    }
}
