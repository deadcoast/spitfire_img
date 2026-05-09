using System.Xml.Linq;

namespace ImageViewer.App.Tests;

public sealed class Phase5ChromeShellTests
{
    private static readonly XName NameAttribute = XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");
    private static readonly XName KeyAttribute = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");

    [Fact]
    public void FadingChrome_DefinesProfessionalChromeShellStatesAndSurfaces()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "FadingChrome.xaml"));
        var xaml = document.ToString(SaveOptions.DisableFormatting);

        Assert.Contains("ChromeActivityStates", xaml);
        Assert.Contains("Active", GetVisualStateNames(document));
        Assert.Contains("Idle", GetVisualStateNames(document));

        AssertNamedElement(document, "TopChromeSurface");
        AssertNamedElement(document, "TopChromeContent");
        AssertNamedElement(document, "UnsavedChangesBadge");
        AssertNamedElement(document, "ChromeStatusText");

        Assert.Contains("ViewerChromeShellBorderStyle", xaml);
        Assert.Contains("ViewerChromeUnsavedBadgeBorderStyle", xaml);
        Assert.Contains("ViewerTextChromeStatusStyle", xaml);
        Assert.Contains("AutomationProperties.Name=\"Application chrome\"", xaml);
    }

    [Fact]
    public void BottomInfoBar_UsesStructuredShellStatusRegions()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "BottomInfoBar.xaml"));
        var xaml = document.ToString(SaveOptions.DisableFormatting);

        AssertNamedElement(document, "InfoBarSurface");
        AssertNamedElement(document, "FilenameText");
        AssertNamedElement(document, "PositionChip");
        AssertNamedElement(document, "DimensionsChip");
        AssertNamedElement(document, "ModeChip");
        AssertNamedElement(document, "FolderText");

        Assert.Contains("ViewerChromeInfoBarBorderStyle", xaml);
        Assert.Contains("ViewerStatusChipBorderStyle", xaml);
        Assert.Contains("ViewerTextChromeLabelStyle", xaml);
        Assert.Contains("ViewerTextChromeValueStyle", xaml);
        Assert.Contains("AutomationProperties.Name=\"Image status\"", xaml);
    }

    [Fact]
    public void Phase5ChromeStyles_DefineShellBadgeAndStatusRoles()
    {
        var repositoryRoot = FindRepositoryRoot();
        var panelKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "PanelStyles.xaml"));
        var textKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "TextStyles.xaml"));
        var layoutKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "LayoutTokens.xaml"));

        Assert.Contains("ViewerChromeShellBorderStyle", panelKeys);
        Assert.Contains("ViewerChromeInfoBarBorderStyle", panelKeys);
        Assert.Contains("ViewerChromeUnsavedBadgeBorderStyle", panelKeys);
        Assert.Contains("ViewerStatusChipBorderStyle", panelKeys);

        Assert.Contains("ViewerTextChromeLabelStyle", textKeys);
        Assert.Contains("ViewerTextChromeValueStyle", textKeys);
        Assert.Contains("ViewerTextChromeStatusStyle", textKeys);

        Assert.Contains("ViewerChromeShellColumnSpacing", layoutKeys);
        Assert.Contains("ViewerStatusChipPadding", layoutKeys);
        Assert.Contains("ViewerChromeStatusMinWidth", layoutKeys);
    }

    private static string[] GetVisualStateNames(XDocument document)
    {
        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "VisualState")
            .Select(element => element.Attribute(NameAttribute)?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
    }

    private static void AssertNamedElement(XDocument document, string name)
    {
        Assert.True(
            document.Descendants().Any(element => string.Equals(element.Attribute(NameAttribute)?.Value, name, StringComparison.Ordinal)),
            $"Missing named chrome shell element: {name}");
    }

    private static HashSet<string> ReadResourceKeys(string path)
    {
        Assert.True(File.Exists(path), $"Missing resource dictionary: {path}");

        return XDocument
            .Load(path)
            .Descendants()
            .Select(element => element.Attribute(KeyAttribute)?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
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
