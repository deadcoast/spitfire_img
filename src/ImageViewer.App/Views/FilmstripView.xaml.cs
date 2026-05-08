using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace ImageViewer.App.Views;

public sealed partial class FilmstripView : UserControl
{
    private readonly ObservableCollection<FilmstripThumbnailItem> _items = [];
    private MainViewModel? _viewModel;
    private int _loadVersion;

    public FilmstripView()
    {
        InitializeComponent();
        ThumbnailItems.ItemsSource = _items;
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.LayoutState.PropertyChanged += OnLayoutStateChanged;
        viewModel.ViewerState.PropertyChanged += OnViewerStateChanged;
        Refresh();
        UpdateDisplayMode();
    }

    private void OnLayoutStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Refresh);
    }

    private void OnViewerStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewerState.DisplayMode)
            or nameof(ViewerState.ZoomLevel)
            or nameof(ViewerState.PanOffset))
        {
            DispatcherQueue.TryEnqueue(UpdateDisplayMode);
        }
    }

    private void Refresh()
    {
        if (_viewModel is null)
        {
            return;
        }

        Visibility = _viewModel.LayoutState.IsFilmstripActive ? Visibility.Visible : Visibility.Collapsed;
        if (!_viewModel.LayoutState.IsFilmstripActive)
        {
            FocusImage.Source = null;
            _items.Clear();
            return;
        }

        _ = LoadFilmstripAsync(++_loadVersion);
        UpdateDisplayMode();
    }

    private async Task LoadFilmstripAsync(int loadVersion)
    {
        if (_viewModel is null)
        {
            return;
        }

        var focusItem = _viewModel.LayoutState.FilmstripFocusItem;
        if (focusItem is null)
        {
            return;
        }

        var focusBitmap = await TryLoadBitmapAsync(focusItem.FilePath);
        if (loadVersion != _loadVersion)
        {
            return;
        }

        FocusImage.Source = focusBitmap;
        _items.Clear();

        var thumbnails = _viewModel.LayoutState.FilmstripThumbnails.ToArray();
        foreach (var thumbnail in thumbnails)
        {
            _items.Add(new FilmstripThumbnailItem(
                thumbnail,
                imageSource: null,
                placeholderText: "Loading",
                thumbnail.IsFocused ? new Thickness(3) : new Thickness(0)));
        }

        var thumbnailTasks = thumbnails
            .Select((thumbnail, index) => LoadThumbnailImageAsync(index, thumbnail.Item))
            .ToList();

        while (thumbnailTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(thumbnailTasks).ConfigureAwait(true);
            thumbnailTasks.Remove(completedTask);
            var loadedThumbnail = await completedTask.ConfigureAwait(true);
            if (loadVersion != _loadVersion)
            {
                return;
            }

            _items[loadedThumbnail.VisibleIndex].ImageSource = loadedThumbnail.ImageSource;
            _items[loadedThumbnail.VisibleIndex].PlaceholderText = loadedThumbnail.ImageSource is null
                ? "No preview"
                : string.Empty;
        }
    }

    private async Task<LoadedFilmstripThumbnail> LoadThumbnailImageAsync(int visibleIndex, ImageItem item)
    {
        if (_viewModel is null)
        {
            return new LoadedFilmstripThumbnail(visibleIndex, null);
        }

        var thumbnailResult = await _viewModel.GetThumbnailAsync(item).ConfigureAwait(true);
        var imageSource = thumbnailResult.IsSuccess
            ? await CreateBitmapImageAsync(thumbnailResult.Value.EncodedBytes).ConfigureAwait(true)
            : null;
        return new LoadedFilmstripThumbnail(visibleIndex, imageSource);
    }

    private static async Task<BitmapImage?> TryLoadBitmapAsync(string filePath)
    {
        try
        {
            await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var randomAccessStream = fileStream.AsRandomAccessStream();
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(randomAccessStream);
            return bitmap;
        }
        catch
        {
            return null;
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

    private async void OnThumbnailClicked(object sender, ItemClickEventArgs e)
    {
        if (_viewModel is null || e.ClickedItem is not FilmstripThumbnailItem item)
        {
            return;
        }

        await _viewModel.SetFilmstripFocusAsync(item.PlaylistIndex);
    }

    private void UpdateDisplayMode()
    {
        if (_viewModel is null)
        {
            return;
        }

        FocusImage.Stretch = _viewModel.ViewerState.DisplayMode == DisplayMode.FitToWindow
            ? Microsoft.UI.Xaml.Media.Stretch.Uniform
            : Microsoft.UI.Xaml.Media.Stretch.None;

        var scale = _viewModel.ViewerState.DisplayMode == DisplayMode.Custom
            ? _viewModel.ViewerState.ZoomLevel / 100d
            : 1d;

        FocusImageTransform.ScaleX = scale;
        FocusImageTransform.ScaleY = scale;
        FocusImageTransform.TranslateX = _viewModel.ViewerState.PanOffset.X;
        FocusImageTransform.TranslateY = _viewModel.ViewerState.PanOffset.Y;
    }

    public sealed class FilmstripThumbnailItem : ObservableObject
    {
        private BitmapImage? _imageSource;
        private string _placeholderText;

        public FilmstripThumbnailItem(
            FilmstripImage image,
            BitmapImage? imageSource,
            string placeholderText,
            Thickness borderThickness)
        {
            Item = image.Item;
            PlaylistIndex = image.PlaylistIndex;
            _imageSource = imageSource;
            _placeholderText = placeholderText;
            BorderThickness = borderThickness;
        }

        public ImageItem Item { get; }

        public int PlaylistIndex { get; }

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

    private sealed record LoadedFilmstripThumbnail(int VisibleIndex, BitmapImage? ImageSource);
}
