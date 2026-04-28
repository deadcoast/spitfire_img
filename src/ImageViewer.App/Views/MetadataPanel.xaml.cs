using System.ComponentModel;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ImageViewer.App.Views;

public sealed partial class MetadataPanel : UserControl
{
    private MainViewModel? _viewModel;

    public MetadataPanel()
    {
        InitializeComponent();
    }

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.MetadataState.PropertyChanged += OnMetadataChanged;
        viewModel.ViewerState.PropertyChanged += OnMetadataChanged;
        Refresh();
    }

    private void OnMetadataChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        if (_viewModel is null)
        {
            return;
        }

        Visibility = _viewModel.MetadataState.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        var image = _viewModel.ViewerState.CurrentImage;
        var metadata = _viewModel.ViewerState.Metadata;
        var exif = _viewModel.MetadataState.CurrentExifMetadata;

        FileText.Text = image is null
            ? "File\nNo image loaded"
            : $"File\n{Path.GetFileName(image.FilePath)}\n{metadata?.FormattedFileSize}";
        ImageText.Text = metadata is null
            ? "Image\nNo image metadata"
            : $"Image\n{metadata.FormattedDimensions}\nColor space: {exif.ColorSpace ?? "Unknown"}";
        CameraText.Text =
            $"Camera\nMake: {exif.CameraMake ?? "Unknown"}\nModel: {exif.CameraModel ?? "Unknown"}\nCaptured: {exif.FormattedCaptureDate ?? "Unknown"}\nISO: {exif.Iso?.ToString() ?? "Unknown"}\nAperture: {exif.FormattedAperture ?? "Unknown"}\nShutter: {exif.FormattedShutterSpeed ?? "Unknown"}\nFocal length: {exif.FormattedFocalLength ?? "Unknown"}\nFlash: {exif.FormattedFlashStatus ?? "Unknown"}\nGPS: {exif.FormattedGpsCoordinates ?? "Unknown"}\nAltitude: {exif.FormattedAltitude ?? "Unknown"}";
    }
}
