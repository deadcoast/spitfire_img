namespace ImageViewer.App.Services.Motion;

public interface IViewerMotionPreferenceProvider
{
    ViewerMotionPreference Preference { get; }

    bool IsSystemReducedMotionEnabled();
}

