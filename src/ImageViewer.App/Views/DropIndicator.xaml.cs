using ImageViewer.App.Services.Motion;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace ImageViewer.App.Views;

public sealed partial class DropIndicator : UserControl
{
    private IViewerMotionService? _motionService;

    public DropIndicator()
    {
        InitializeComponent();
    }

    public void AttachMotionService(IViewerMotionService motionService)
    {
        _motionService = motionService;
    }

    public void Show(bool isValid)
    {
        MessageText.Text = isValid ? "Drop image to open" : "Unsupported file format";
        Visibility = Visibility.Visible;
        ViewerMotionStoryboard.AnimateOpacity(this, GetMotionPlan(ViewerMotionCategory.DropIndicator), 1);
    }

    public void Hide()
    {
        ViewerMotionStoryboard.AnimateOpacity(
            this,
            GetMotionPlan(ViewerMotionCategory.OverlayExit),
            0,
            completed: () => Visibility = Visibility.Collapsed);
    }

    private ViewerMotionPlan GetMotionPlan(ViewerMotionCategory category)
    {
        return _motionService?.CreatePlan(category) ?? ViewerMotionService.CreateDefaultPlan(category, isReducedMotion: false);
    }
}
