namespace ImageViewer.App.Services.Motion;

public sealed class ViewerMotionService(IViewerMotionPreferenceProvider preferenceProvider) : IViewerMotionService
{
    private static readonly TimeSpan InstantDuration = TimeSpan.Zero;
    private static readonly TimeSpan FastDuration = TimeSpan.FromMilliseconds(90);
    private static readonly TimeSpan NormalDuration = TimeSpan.FromMilliseconds(160);
    private static readonly TimeSpan DeliberateDuration = TimeSpan.FromMilliseconds(240);
    private static readonly TimeSpan EntranceDuration = TimeSpan.FromMilliseconds(180);
    private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(120);
    private static readonly TimeSpan RepositionDuration = TimeSpan.FromMilliseconds(180);
    private static readonly TimeSpan ImageTransitionDuration = TimeSpan.FromMilliseconds(140);

    private const double NoOffset = 0;
    private const double ChromeRevealOffset = 8;
    private const double OverlayEntranceOffset = 12;
    private const double PanelSlideOffset = 24;
    private const double NoScale = 1;
    private const double SubtleScaleFrom = 0.98;

    public bool IsReducedMotionEnabled => preferenceProvider.Preference switch
    {
        ViewerMotionPreference.Full => false,
        ViewerMotionPreference.Reduced => true,
        _ => preferenceProvider.IsSystemReducedMotionEnabled()
    };

    public ViewerMotionPlan CreatePlan(ViewerMotionCategory category)
    {
        return CreateDefaultPlan(category, IsReducedMotionEnabled);
    }

    public static ViewerMotionPlan CreateDefaultPlan(ViewerMotionCategory category, bool isReducedMotion)
    {
        if (isReducedMotion)
        {
            return new ViewerMotionPlan(
                category,
                InstantDuration,
                ViewerMotionEasing.Linear,
                NoOffset,
                NoScale,
                NoScale,
                AllowsOpacity: true,
                AllowsTranslation: false,
                AllowsScale: false,
                AllowsLooping: false,
                IsReducedMotion: true);
        }

        return category switch
        {
            ViewerMotionCategory.ChromeReveal => CreateNormalPlan(category, FastDuration, ViewerMotionEasing.EaseOut, ChromeRevealOffset, allowsTranslation: true),
            ViewerMotionCategory.ChromeIdle => CreateNormalPlan(category, NormalDuration, ViewerMotionEasing.EaseIn, NoOffset),
            ViewerMotionCategory.OverlayEntrance => CreateNormalPlan(category, EntranceDuration, ViewerMotionEasing.EaseOut, OverlayEntranceOffset, allowsTranslation: true, allowsScale: true),
            ViewerMotionCategory.OverlayExit => CreateNormalPlan(category, ExitDuration, ViewerMotionEasing.EaseIn, OverlayEntranceOffset, allowsTranslation: true),
            ViewerMotionCategory.MetadataPanel => CreateNormalPlan(category, NormalDuration, ViewerMotionEasing.EaseInOut, PanelSlideOffset, allowsTranslation: true),
            ViewerMotionCategory.ImageNavigation => CreateNormalPlan(category, ImageTransitionDuration, ViewerMotionEasing.EaseInOut, NoOffset),
            ViewerMotionCategory.LayoutModeChange => CreateNormalPlan(category, DeliberateDuration, ViewerMotionEasing.EaseInOut, NoOffset),
            ViewerMotionCategory.Reposition => CreateNormalPlan(category, RepositionDuration, ViewerMotionEasing.EaseInOut, NoOffset, allowsTranslation: true),
            ViewerMotionCategory.Feedback => CreateNormalPlan(category, FastDuration, ViewerMotionEasing.EaseOut, NoOffset),
            ViewerMotionCategory.Status => CreateNormalPlan(category, NormalDuration, ViewerMotionEasing.EaseOut, OverlayEntranceOffset, allowsTranslation: true),
            ViewerMotionCategory.DropIndicator => CreateNormalPlan(category, FastDuration, ViewerMotionEasing.EaseOut, NoOffset, allowsScale: true),
            ViewerMotionCategory.GifControls => CreateNormalPlan(category, FastDuration, ViewerMotionEasing.EaseOut, NoOffset),
            ViewerMotionCategory.Comparison => CreateNormalPlan(category, FastDuration, ViewerMotionEasing.EaseOut, NoOffset),
            _ => CreateNormalPlan(category, NormalDuration, ViewerMotionEasing.EaseInOut, NoOffset)
        };
    }

    private static ViewerMotionPlan CreateNormalPlan(
        ViewerMotionCategory category,
        TimeSpan duration,
        ViewerMotionEasing easing,
        double offset,
        bool allowsTranslation = false,
        bool allowsScale = false)
    {
        return new ViewerMotionPlan(
            category,
            duration,
            easing,
            offset,
            allowsScale ? SubtleScaleFrom : NoScale,
            NoScale,
            AllowsOpacity: true,
            AllowsTranslation: allowsTranslation,
            AllowsScale: allowsScale,
            AllowsLooping: false,
            IsReducedMotion: false);
    }
}

