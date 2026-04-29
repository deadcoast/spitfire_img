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
        if (!_viewModel.ThumbnailState.IsStripVisible)
        {
            return;
        }

        _ = LoadThumbnailsAsync(++_generationVersion);
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
        Items.ItemsSource = _items;

        foreach (var item in playlistItems)
        {
            var borderThickness = item.FilePath == _viewModel.PlaylistState.CurrentItem?.FilePath
                ? new Thickness(2)
                : new Thickness(0);

            _items.Add(new ThumbnailStripItem(
                item.Filename,
                imageSource: null,
                placeholderText: "Loading",
                borderThickness));
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

            _items[index].ImageSource = imageSource;
            _items[index].PlaceholderText = imageSource is null ? "No preview" : string.Empty;
        }
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

    public sealed class ThumbnailStripItem : ObservableObject
    {
        private BitmapImage? _imageSource;
        private string _placeholderText;

        public ThumbnailStripItem(
            string fileName,
            BitmapImage? imageSource,
            string placeholderText,
            Thickness borderThickness)
        {
            FileName = fileName;
            _imageSource = imageSource;
            _placeholderText = placeholderText;
            BorderThickness = borderThickness;
        }

        public string FileName { get; }

        public BitmapImage? ImageSource
        {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
        }

        public string PlaceholderText
        {
            get => _placeholderText;
            set => SetProperty(ref _placeholderText, value);
        }

        public Thickness BorderThickness { get; }
    }
}
