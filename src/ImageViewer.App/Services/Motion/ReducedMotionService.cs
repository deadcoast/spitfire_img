using Windows.UI.ViewManagement;

namespace ImageViewer.App.Services.Motion;

public sealed class ReducedMotionService : IViewerMotionPreferenceProvider
{
    public ViewerMotionPreference Preference => ViewerMotionPreference.System;

    public bool IsSystemReducedMotionEnabled()
    {
        try
        {
            return !new UISettings().AnimationsEnabled;
        }
        catch
        {
            return false;
        }
    }
}

