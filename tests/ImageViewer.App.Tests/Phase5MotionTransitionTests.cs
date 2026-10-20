using System.Xml.Linq;

namespace ImageViewer.App.Tests;

public sealed class Phase5MotionTransitionTests
{
    private static readonly XName NameAttribute = XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");

    [Fact]
    public void CommandPalette_DefinesMotionTargetsAndUsesMotionService()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "CommandPalette.xaml"));
        var code = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "CommandPalette.xaml.cs"));
        var mainWindowCode = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "MainWindow.xaml.cs"));

        AssertNamedElement(document, "PalettePanelTransform");
        Assert.Contains("Opacity=\"{StaticResource ViewerHiddenOpacity}\"", document.ToString(SaveOptions.DisableFormatting));
        Assert.Contains("IViewerMotionService", code);
        Assert.Contains("ViewerMotionCategory.OverlayEntrance", code);
        Assert.Contains("ViewerMotionCategory.OverlayExit", code);
        Assert.Contains("ViewerMotionStoryboard.AnimateOverlay", code);
        Assert.Contains("CommandPalette.AttachMotionService(motionService)", mainWindowCode);
    }

    [Fact]
    public void OverlayAndStatusSurfaces_DefineNamedTransitionTransforms()
    {
        var repositoryRoot = FindRepositoryRoot();
        var searchDocument = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "SearchOverlay.xaml"));
        var mainWindowDocument = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "MainWindow.xaml"));
        var fadingChromeDocument = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "FadingChrome.xaml"));

        AssertNamedElement(searchDocument, "SearchOverlayPanelTransform");
        AssertNamedElement(mainWindowDocument, "ErrorInfoBarTransform");
        AssertNamedElement(mainWindowDocument, "LoadingOverlayTransform");
        AssertNamedElement(fadingChromeDocument, "TopChromeTransform");
        AssertNamedElement(fadingChromeDocument, "BottomInfoBarTransform");
    }

    [Fact]
    public void MainWindow_StatusAndLoadingTransitionsUseMotionServiceWithoutBlockingInput()
    {
        var repositoryRoot = FindRepositoryRoot();
        var code = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "MainWindow.xaml.cs"));

        Assert.Contains("private readonly IViewerMotionService _motionService;", code);
        Assert.Contains("ViewerMotionCategory.Status", code);
        Assert.Contains("ViewerMotionStoryboard.AnimateOverlay", code);
        Assert.Contains("HideStatusInfoBar", code);
        Assert.Contains("HideLoadingOverlay", code);
        Assert.Contains("OnRootKeyDown", code);
        Assert.DoesNotContain("Thread.Sleep", code);
        Assert.DoesNotContain(".Wait()", code);
        Assert.DoesNotContain(".Result", code);
    }

    private static void AssertNamedElement(XDocument document, string name)
    {
        Assert.True(
            document.Descendants().Any(element => string.Equals(element.Attribute(NameAttribute)?.Value, name, StringComparison.Ordinal)),
            $"Missing named motion transition element: {name}");
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
