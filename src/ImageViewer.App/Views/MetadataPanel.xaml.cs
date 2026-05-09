using System.ComponentModel;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

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

        var shouldShow = _viewModel.MetadataState.IsVisible;
        Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
        AnimatePanel(shouldShow);
        var image = _viewModel.ViewerState.CurrentImage;
        var metadata = _viewModel.ViewerState.Metadata;
        var exif = _viewModel.MetadataState.CurrentExifMetadata;

        MetadataSummary.Text = image is null
            ? "No image loaded"
            : Path.GetFileName(image.FilePath);

        SetMetadataRow(FileNameValue, image is null ? "No image loaded" : Path.GetFileName(image.FilePath));
        SetMetadataRow(FileSizeValue, metadata?.FormattedFileSize);
        SetMetadataRow(DimensionsValue, metadata?.FormattedDimensions);
        SetMetadataRow(ColorSpaceValue, exif.ColorSpace);
        SetMetadataRow(CameraMakeValue, exif.CameraMake);
        SetMetadataRow(CameraModelValue, exif.CameraModel);
        SetMetadataRow(CaptureDateValue, exif.FormattedCaptureDate);
        SetMetadataRow(IsoValue, exif.Iso?.ToString());
        SetMetadataRow(ApertureValue, exif.FormattedAperture);
        SetMetadataRow(ShutterValue, exif.FormattedShutterSpeed);
        SetMetadataRow(FocalLengthValue, exif.FormattedFocalLength);
        SetMetadataRow(FlashValue, exif.FormattedFlashStatus);
        SetMetadataRow(GpsValue, FormatGpsValue(exif));
    }

    private static void SetMetadataRow(TextBlock valueBlock, string? value)
    {
        valueBlock.Text = string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    }

    private static string? FormatGpsValue(ExifMetadata exif)
    {
        if (string.IsNullOrWhiteSpace(exif.FormattedGpsCoordinates))
        {
            return exif.FormattedAltitude;
        }

        return string.IsNullOrWhiteSpace(exif.FormattedAltitude)
            ? exif.FormattedGpsCoordinates
            : $"{exif.FormattedGpsCoordinates} / {exif.FormattedAltitude}";
    }

    private void AnimatePanel(bool show)
    {
        var opacityAnimation = new DoubleAnimation
        {
            To = show ? 1 : 0,
            Duration = TimeSpan.FromMilliseconds(220),
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(opacityAnimation, this);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

        var slideAnimation = new DoubleAnimation
        {
            To = show ? 0 : 360,
            Duration = TimeSpan.FromMilliseconds(220),
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(slideAnimation, PanelTransform);
        Storyboard.SetTargetProperty(slideAnimation, "X");

        var storyboard = new Storyboard();
        storyboard.Children.Add(opacityAnimation);
        storyboard.Children.Add(slideAnimation);
        storyboard.Begin();
    }
}
