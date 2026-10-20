using ImageViewer.App.Services.Motion;

namespace ImageViewer.App.Tests;

public sealed class ViewerMotionServiceTests
{
    private static readonly ViewerMotionCategory[] RequiredCategories =
    [
        ViewerMotionCategory.ChromeReveal,
        ViewerMotionCategory.ChromeIdle,
        ViewerMotionCategory.OverlayEntrance,
        ViewerMotionCategory.OverlayExit,
        ViewerMotionCategory.MetadataPanel,
        ViewerMotionCategory.ImageNavigation,
        ViewerMotionCategory.LayoutModeChange,
        ViewerMotionCategory.Reposition,
        ViewerMotionCategory.Feedback,
        ViewerMotionCategory.Status,
        ViewerMotionCategory.DropIndicator,
        ViewerMotionCategory.GifControls,
        ViewerMotionCategory.Comparison
    ];

    [Fact]
    public void MotionCategories_AreExplicitlyLimitedToPhase5AllowedMotion()
    {
        Assert.Equal(
            RequiredCategories.OrderBy(category => category.ToString()),
            Enum.GetValues<ViewerMotionCategory>().OrderBy(category => category.ToString()));
    }

    [Fact]
    public void CreatePlan_ReturnsPurposefulNormalMotionForEachAllowedCategory()
    {
        var service = new ViewerMotionService(new FixedMotionPreferenceProvider(ViewerMotionPreference.Full, systemReducedMotion: false));

        foreach (var category in RequiredCategories)
        {
            var plan = service.CreatePlan(category);

            Assert.Equal(category, plan.Category);
            Assert.False(plan.IsReducedMotion);
            Assert.True(plan.AllowsOpacity);
            Assert.False(plan.AllowsLooping);
            Assert.True(plan.Duration >= TimeSpan.Zero);
        }

        var overlayPlan = service.CreatePlan(ViewerMotionCategory.OverlayEntrance);
        Assert.Equal(TimeSpan.FromMilliseconds(180), overlayPlan.Duration);
        Assert.Equal(ViewerMotionEasing.EaseOut, overlayPlan.Easing);
        Assert.True(overlayPlan.AllowsTranslation);
        Assert.True(overlayPlan.AllowsScale);

        var imagePlan = service.CreatePlan(ViewerMotionCategory.ImageNavigation);
        Assert.Equal(TimeSpan.FromMilliseconds(140), imagePlan.Duration);
        Assert.False(imagePlan.AllowsTranslation);
        Assert.False(imagePlan.AllowsScale);
    }

    [Fact]
    public void CreatePlan_WhenReducedMotionIsEnabled_SuppressesTravelScaleAndLongCrossfade()
    {
        var service = new ViewerMotionService(new FixedMotionPreferenceProvider(ViewerMotionPreference.Reduced, systemReducedMotion: false));

        foreach (var category in RequiredCategories)
        {
            var plan = service.CreatePlan(category);

            Assert.True(plan.IsReducedMotion);
            Assert.Equal(TimeSpan.Zero, plan.Duration);
            Assert.Equal(ViewerMotionEasing.Linear, plan.Easing);
            Assert.True(plan.AllowsOpacity);
            Assert.False(plan.AllowsTranslation);
            Assert.False(plan.AllowsScale);
            Assert.False(plan.AllowsLooping);
        }
    }

    [Fact]
    public void IsReducedMotionEnabled_RespectsSystemPreferenceProvider()
    {
        var systemReducedService = new ViewerMotionService(new FixedMotionPreferenceProvider(ViewerMotionPreference.System, systemReducedMotion: true));
        var systemFullService = new ViewerMotionService(new FixedMotionPreferenceProvider(ViewerMotionPreference.System, systemReducedMotion: false));

        Assert.True(systemReducedService.IsReducedMotionEnabled);
        Assert.False(systemFullService.IsReducedMotionEnabled);
    }

    private sealed class FixedMotionPreferenceProvider(
        ViewerMotionPreference preference,
        bool systemReducedMotion) : IViewerMotionPreferenceProvider
    {
        public ViewerMotionPreference Preference => preference;

        public bool IsSystemReducedMotionEnabled() => systemReducedMotion;
    }
}
