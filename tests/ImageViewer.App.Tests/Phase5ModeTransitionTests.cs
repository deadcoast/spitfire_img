using System.Xml.Linq;

namespace ImageViewer.App.Tests;

public sealed class Phase5ModeTransitionTests
{
    private static readonly XName NameAttribute = XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");

    [Fact]
    public void ImageCanvas_UsesMotionServiceForInspectionFirstImageNavigation()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "ImageCanvas.xaml"));
        var code = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "ImageCanvas.xaml.cs"));
        var mainWindowCode = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "MainWindow.xaml.cs"));

        AssertNamedElement(document, "DisplayedImage");
        Assert.Contains("Opacity=\"{StaticResource ViewerHiddenOpacity}\"", document.ToString(SaveOptions.DisableFormatting));
        Assert.Contains("IViewerMotionService", code);
        Assert.Contains("AttachMotionService", code);
        Assert.Contains("ViewerMotionCategory.ImageNavigation", code);
        Assert.Contains("ViewerMotionStoryboard.AnimateOpacity", code);
        Assert.Contains("ImageCanvas.AttachMotionService(motionService)", mainWindowCode);
    }

    [Fact]
    public void LayoutModeSurfaces_DefineWholeSurfaceTransformsAndUseLayoutModeMotion()
    {
        var repositoryRoot = FindRepositoryRoot();
        var gridDocument = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "GridLayout.xaml"));
        var filmstripDocument = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "FilmstripView.xaml"));
        var comparisonDocument = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "ComparisonView.xaml"));
        var gridCode = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "GridLayout.xaml.cs"));
        var filmstripCode = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "FilmstripView.xaml.cs"));
        var comparisonCode = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "ComparisonView.xaml.cs"));
        var mainWindowCode = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "MainWindow.xaml.cs"));

        AssertNamedElement(gridDocument, "GridLayoutTransform");
        AssertNamedElement(filmstripDocument, "FilmstripLayoutTransform");
        AssertNamedElement(comparisonDocument, "ComparisonLayoutTransform");

        foreach (var code in new[] { gridCode, filmstripCode, comparisonCode })
        {
            Assert.Contains("IViewerMotionService", code);
            Assert.Contains("AttachMotionService", code);
            Assert.Contains("ViewerMotionCategory.LayoutModeChange", code);
            Assert.Contains("ViewerMotionStoryboard.AnimateOverlay", code);
        }

        Assert.Contains("GridLayout.AttachMotionService(motionService)", mainWindowCode);
        Assert.Contains("FilmstripView.AttachMotionService(motionService)", mainWindowCode);
        Assert.Contains("ComparisonView.AttachMotionService(motionService)", mainWindowCode);
    }

    [Fact]
    public void ComparisonAndGifControls_UseFocusedMotionWithoutDecorativeLooping()
    {
        var repositoryRoot = FindRepositoryRoot();
        var comparisonDocument = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "ComparisonView.xaml"));
        var frameDocument = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "FrameControl.xaml"));
        var comparisonCode = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "ComparisonView.xaml.cs"));
        var frameCode = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "FrameControl.xaml.cs"));
        var chromeCode = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "FadingChrome.xaml.cs"));

        AssertNamedElement(comparisonDocument, "OverlayControlsTransform");
        AssertNamedElement(frameDocument, "FrameControlPanel");
        Assert.Contains("ViewerMotionCategory.Comparison", comparisonCode);
        Assert.Contains("ViewerMotionCategory.GifControls", frameCode);
        Assert.Contains("ViewerMotionStoryboard.AnimateOpacity", frameCode);
        Assert.Contains("FrameControl.AttachMotionService(motionService)", chromeCode);
        Assert.DoesNotContain("RepeatBehavior=\"Forever\"", comparisonDocument.ToString(SaveOptions.DisableFormatting));
        Assert.DoesNotContain("RepeatBehavior=\"Forever\"", frameDocument.ToString(SaveOptions.DisableFormatting));
    }

    [Fact]
    public void ModeTransitionCode_DoesNotBlockNavigationOrAnimateEveryCollectionItem()
    {
        var repositoryRoot = FindRepositoryRoot();
        var modeSurfaceFiles = new[]
        {
            Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "ImageCanvas.xaml.cs"),
            Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "GridLayout.xaml.cs"),
            Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "FilmstripView.xaml.cs"),
            Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "ComparisonView.xaml.cs"),
            Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "FrameControl.xaml.cs")
        };

        foreach (var file in modeSurfaceFiles)
        {
            var code = File.ReadAllText(file);

            Assert.DoesNotContain("Thread.Sleep", code);
            Assert.DoesNotContain(".Wait()", code);
            Assert.DoesNotContain(".Result", code);
            Assert.DoesNotContain("foreach (var child in ImageHost.Children)", code);
            Assert.DoesNotContain("foreach (var frame in frames)\r\n        {\r\n            ViewerMotionStoryboard", code);
        }
    }

    private static void AssertNamedElement(XDocument document, string name)
    {
        Assert.True(
            document.Descendants().Any(element => string.Equals(element.Attribute(NameAttribute)?.Value, name, StringComparison.Ordinal)),
            $"Missing named mode transition element: {name}");
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
