using ImageViewer.App.Services;

namespace ImageViewer.App.Tests;

public sealed class Phase5ComparisonGifSurfaceTests
{
    [Fact]
    public void ComparisonView_DeclaresProfessionalHudAndOverlayControls()
    {
        var xaml = ReadViewXaml("ComparisonView.xaml");
        var code = ReadViewCode("ComparisonView.xaml.cs");

        Assert.Contains("AutomationProperties.Name=\"Comparison image review surface\"", xaml);
        Assert.Contains("x:Name=\"ComparisonModeHud\"", xaml);
        Assert.Contains("x:Name=\"ComparisonModeText\"", xaml);
        Assert.Contains("x:Name=\"ComparisonCountText\"", xaml);
        Assert.Contains("x:Name=\"ComparisonActiveText\"", xaml);
        Assert.Contains("x:Name=\"ComparisonSyncText\"", xaml);
        Assert.Contains("x:Name=\"OverlayOpacityText\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"Overlay opacity\"", xaml);
        Assert.Contains("ViewerComparisonHudBorderStyle", xaml);
        Assert.Contains("ViewerComparisonImageFrameBorderStyle", code);
        Assert.Contains("ViewerComparisonImageActiveFrameBorderStyle", code);
        Assert.Contains("CreateImageStateBadge", code);
        Assert.Contains("UpdateComparisonHud", code);
        Assert.DoesNotContain("Colors.White", code);
        Assert.DoesNotContain("Colors.Transparent", code);
    }

    [Fact]
    public void FrameControl_DeclaresReadableMediaStateAndAccessibleControls()
    {
        var xaml = ReadViewXaml("FrameControl.xaml");
        var code = ReadViewCode("FrameControl.xaml.cs");

        Assert.Contains("AutomationProperties.Name=\"Animated GIF frame controls\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"Previous GIF frame\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"Play or pause GIF\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"Next GIF frame\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"GIF frame timeline\"", xaml);
        Assert.Contains("x:Name=\"PlaybackStateText\"", xaml);
        Assert.Contains("x:Name=\"FrameRangeText\"", xaml);
        Assert.Contains("ViewerFrameControlPanelBorderStyle", xaml);
        Assert.Contains("ViewerFrameTimelineBorderStyle", xaml);
        Assert.Contains("UpdateAutomationNames", code);
        Assert.Contains("PlaybackStateText.Text", code);
        Assert.Contains("FrameRangeText.Text", code);
        Assert.Contains("PreviousButton.IsEnabled", code);
        Assert.Contains("NextButton.IsEnabled", code);
    }

    [Fact]
    public void CaptureHarness_CanScopeEvidenceToComparisonAndGifSurfaces()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "TestResults", "phase5-evidence", "visual", "capture-before-states.ps1"));

        Assert.Contains("[switch]$OnlyComparisonGif", script);
        Assert.Contains("09-comparison-side-by-side.png", script);
        Assert.Contains("10-comparison-overlay.png", script);
        Assert.Contains("11-gif-frame-controls.png", script);
        Assert.Contains("100-frame", script);
        Assert.Contains("Get-EvidenceWindowOrigin", script);
        Assert.DoesNotContain("MoveWindow($handle, 120, 20", script);
    }

    [Fact]
    public async Task AnimationService_LoadsPhase5HundredFrameGifFixture()
    {
        var fixture = Path.Combine(FindRepositoryRoot(), "TestResults", "phase5-evidence", "fixtures", "baseline-100-frame.gif");
        var service = new WindowsAnimationService();

        var result = await service.LoadAnimatedGifAsync(fixture);

        if (!result.IsSuccess)
        {
            Assert.Fail(result.Error.ToString());
        }

        Assert.True(result.Value.IsAnimated);
        Assert.Equal(100, result.Value.FrameCount);
    }

    private static string ReadViewXaml(string fileName)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ImageViewer.App", "Views", fileName));
    }

    private static string ReadViewCode(string fileName)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ImageViewer.App", "Views", fileName));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SpitfireImg.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing SpitfireImg.sln.");
    }
}
