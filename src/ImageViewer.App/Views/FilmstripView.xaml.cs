using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.App.Services.Motion;
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
    private IViewerMotionService? _motionService;
    private int _loadVersion;
    private bool _isLoaded;
    private bool _refreshQueued;
    private bool _isFilmstripVisible;

    public FilmstripView()
    {
        InitializeComponent();
        FocusCanvas.SetActive(false);
        FilmstripThumbnailList.ItemsSource = _items;
        Loaded += OnLoaded;
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.LayoutState.PropertyChanged += OnLayoutStateChanged;
        viewModel.ImageContentChanged += (_, _) => QueueRefresh();
        FocusCanvas.Attach(viewModel);
        if (_isLoaded)
        {
            QueueRefresh();
        }
    }

    public void AttachMotionService(IViewerMotionService motionService)
    {
        _motionService = motionService;
        FocusCanvas.AttachMotionService(motionService);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        QueueRefresh();
    }

    private void OnLayoutStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        QueueRefresh();
    }

    private void Refresh()
    {
        if (_viewModel is null)
        {
            return;
        }

        if (!_isLoaded)
        {
            return;
        }

        if (!_viewModel.LayoutState.IsFilmstripActive)
        {
            HideFilmstrip();
            return;
        }

        ShowFilmstrip();
        _ = LoadFilmstripAsync(++_loadVersion);
    }

    private void ShowFilmstrip()
    {
        if (_isFilmstripVisible)
        {
            return;
        }

        _isFilmstripVisible = true;
        FocusCanvas.SetActive(true);
        Visibility = Visibility.Visible;
        ViewerMotionStoryboard.AnimateOverlay(
            LayoutRoot,
            FilmstripLayoutTransform,
            GetMotionPlan(ViewerMotionCategory.LayoutModeChange),
            isEntering: true);
    }

    private void HideFilmstrip()
    {
        if (!_isFilmstripVisible && Visibility == Visibility.Collapsed)
        {
            FocusCanvas.SetActive(false);
            _items.Clear();
            return;
        }

        _isFilmstripVisible = false;
        _loadVersion++;
        ViewerMotionStoryboard.AnimateOverlay(
            LayoutRoot,
            FilmstripLayoutTransform,
            GetMotionPlan(ViewerMotionCategory.LayoutModeChange),
            isEntering: false,
            completed: () =>
            {
                Visibility = Visibility.Collapsed;
                FocusCanvas.SetActive(false);
                _items.Clear();
            });
    }

    private void QueueRefresh()
    {
        if (_refreshQueued)
        {
            return;
        }

        _refreshQueued = true;
        if (!DispatcherQueue.TryEnqueue(() =>
        {
            _refreshQueued = false;
            Refresh();
        }))
        {
            _refreshQueued = false;
        }
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

        FilmstripFocusText.Text = $"{focusItem.Filename} - {(_viewModel.LayoutState.FilmstripFocusIndex + 1)} of {_viewModel.PlaylistState.TotalCount}";
        _items.Clear();

        var thumbnails = _viewModel.LayoutState.FilmstripThumbnails.ToArray();
        foreach (var thumbnail in thumbnails)
        {
            _items.Add(new FilmstripThumbnailItem(
                thumbnail,
                imageSource: null,
                placeholderText: "Loading",
                thumbnail.IsFocused ? new Thickness(3) : new Thickness(0),
                thumbnail.IsFocused));
        }

        for (var thumbnailIndex = 0; thumbnailIndex < thumbnails.Length; thumbnailIndex++)
        {
            var loadedThumbnail = await LoadThumbnailImageAsync(
                    thumbnailIndex,
                    thumbnails[thumbnailIndex].Item)
                .ConfigureAwait(true);
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

    private ViewerMotionPlan GetMotionPlan(ViewerMotionCategory category)
    {
        return _motionService?.CreatePlan(category) ?? ViewerMotionService.CreateDefaultPlan(category, isReducedMotion: false);
    }

    public sealed class FilmstripThumbnailItem : ObservableObject
    {
        private BitmapImage? _imageSource;
        private string _placeholderText;

        public FilmstripThumbnailItem(
            FilmstripImage image,
            BitmapImage? imageSource,
            string placeholderText,
            Thickness borderThickness,
            bool isCurrent)
        {
            Item = image.Item;
            PlaylistIndex = image.PlaylistIndex;
            _imageSource = imageSource;
            _placeholderText = placeholderText;
            BorderThickness = borderThickness;
            CurrentLabel = isCurrent ? "Focus" : string.Empty;
        }

        public ImageItem Item { get; }

        public string FileName => Item.Filename;

        public int PlaylistIndex { get; }

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

        public Thickness BorderThickness { get; }

        public string CurrentLabel { get; }

        public string StatusText => string.IsNullOrWhiteSpace(PlaceholderText) ? string.Empty : PlaceholderText;
    }

    private sealed record LoadedFilmstripThumbnail(int VisibleIndex, BitmapImage? ImageSource);
}
