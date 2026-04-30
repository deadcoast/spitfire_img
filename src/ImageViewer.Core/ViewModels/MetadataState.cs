using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class MetadataState : ObservableObject
{
    private bool _isVisible;
    private ExifMetadata _currentExifMetadata = ExifMetadata.Empty;

    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    public ExifMetadata CurrentExifMetadata
    {
        get => _currentExifMetadata;
        private set
        {
            if (SetProperty(ref _currentExifMetadata, value))
            {
                OnPropertyChanged(nameof(HasExifData));
            }
        }
    }

    public bool HasExifData =>
        CurrentExifMetadata.HasCameraInfo ||
        CurrentExifMetadata.HasCaptureSettings ||
        CurrentExifMetadata.HasGpsInfo ||
        !string.IsNullOrWhiteSpace(CurrentExifMetadata.ColorSpace);

    public void ShowPanel(ExifMetadata metadata)
    {
        CurrentExifMetadata = metadata;
        IsVisible = true;
    }

    public void HidePanel()
    {
        IsVisible = false;
    }
}
