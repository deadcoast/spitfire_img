namespace ImageViewer.App.Services.Motion;

public interface IViewerMotionService
{
    bool IsReducedMotionEnabled { get; }

    ViewerMotionPlan CreatePlan(ViewerMotionCategory category);
}

