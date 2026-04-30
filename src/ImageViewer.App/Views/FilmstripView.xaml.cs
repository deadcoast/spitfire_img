using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    private readonly ObservableCollection<FilmstripThumbnailItem> _thumbnails = [];
    private MainViewModel? _viewModel;
    private int _renderVersion;

    public FilmstripView()
    {
        InitializeComponent();
        ThumbnailItems.ItemsSource = _thumbnails;
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.LayoutState.PropertyChanged += OnLayoutStateChanged;
        viewModel.LayoutState.VisibleImages.CollectionChanged += OnVisibleImagesChanged;
        Update();
    }

    private void OnLayoutStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Update);
    }

    private void OnVisibleImagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Update);
    }

    private void Update()
    {
        var state = _viewModel?.LayoutState;
        if (state is null || !state.IsActive || state.Mode != LayoutMode.Filmstrip)
        {
            Visibility = Visibility.Collapsed;
            FocusImage.Source = null;
            _thumbnails.Clear();
            _renderVersion++;
            return;
        }

        Visibility = Visibility.Visible;
        _ = RenderFilmstripAsync(state, ++_renderVersion);
    }

    private async Task RenderFilmstripAsync(LayoutState state, int renderVersion)
    {
        if (state.FocusImage is { } focusImage)
        {
            await LoadImageAsync(FocusImage, focusImage.Image.FilePath, renderVersion);
        }

        _thumbnails.Clear();
        foreach (var image in state.VisibleImages)
        {
            var isFocus = image.PlaylistIndex == state.FocusImage?.PlaylistIndex;
            _thumbnails.Add(new FilmstripThumbnailItem(
                _thumbnails.Count,
                image.Image.FilePath,
                image.Image.FilePath,
                ImageSource: null,
                PlaceholderText: "Loading",
                BorderThickness: isFocus ? new Thickness(2) : new Thickness(0)));
        }

        for (var index = 0; index < state.VisibleImages.Count; index++)
        {
            if (renderVersion != _renderVersion || _viewModel is null)
            {
                return;
            }

            var image = state.VisibleImages[index];
            var thumbnail = await _viewModel.GetThumbnailAsync(new ImageItem(image.Image.FilePath));
            var imageSource = thumbnail.IsSuccess
                ? await CreateBitmapImageAsync(thumbnail.Value.EncodedBytes)
                : null;

            _thumbnails[index].ImageSource = imageSource;
            _thumbnails[index].PlaceholderText = imageSource is null ? "No preview" : string.Empty;
        }
    }

    private async void OnThumbnailClicked(object sender, ItemClickEventArgs e)
    {
        if (_viewModel is null || e.ClickedItem is not FilmstripThumbnailItem item)
        {
            return;
        }

        await _viewModel.SelectFilmstripImageAsync(item.VisibleIndex);
    }

    private static async Task LoadImageAsync(Image target, string filePath, int renderVersion)
    {
        try
        {
            await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var randomAccessStream = fileStream.AsRandomAccessStream();
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(randomAccessStream);

            target.Source = bitmap;
        }
        catch
        {
            target.Source = null;
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

    public sealed class FilmstripThumbnailItem : ObservableObject
    {
        private BitmapImage? _imageSource;
        private string _placeholderText;

        public FilmstripThumbnailItem(
            int visibleIndex,
            string filePath,
            string fileName,
            BitmapImage? ImageSource,
            string PlaceholderText,
            Thickness BorderThickness)
        {
            VisibleIndex = visibleIndex;
            FilePath = filePath;
            FileName = fileName;
            _imageSource = ImageSource;
            _placeholderText = PlaceholderText;
            this.BorderThickness = BorderThickness;
        }

        public int VisibleIndex { get; }

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
            set => SetProperty(ref _placeholderText, value);
        }

        public Thickness BorderThickness { get; }
    }
}
