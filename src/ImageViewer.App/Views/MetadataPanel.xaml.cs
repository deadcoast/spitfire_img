using System.ComponentModel;
using ImageViewer.App.Services.Motion;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace ImageViewer.App.Views;

public sealed partial class MetadataPanel : UserControl
{
    private MainViewModel? _viewModel;
    private IViewerMotionService? _motionService;

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

    public void AttachMotionService(IViewerMotionService motionService)
    {
        _motionService = motionService;
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
        var motionPlan = GetMotionPlan();
        if (show)
        {
            Visibility = Visibility.Visible;
        }

        ViewerMotionStoryboard.AnimateHorizontalPanel(
            this,
            PanelTransform,
            motionPlan,
            isEntering: show,
            hiddenOffset: GetDoubleResource("ViewerMetadataPanelWidth", 360),
            completed: () =>
            {
                if (!show)
                {
                    Visibility = Visibility.Collapsed;
                }
            });
    }

    private ViewerMotionPlan GetMotionPlan()
    {
        return _motionService?.CreatePlan(ViewerMotionCategory.MetadataPanel)
            ?? ViewerMotionService.CreateDefaultPlan(ViewerMotionCategory.MetadataPanel, isReducedMotion: false);
    }

    private double GetDoubleResource(string key, double fallback)
    {
        return Application.Current.Resources.TryGetValue(key, out var value) && value is double doubleValue
            ? doubleValue
            : fallback;
    }
}
