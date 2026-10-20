using System.Xml.Linq;

namespace ImageViewer.App.Tests;

public sealed class Phase5ChromeShellTests
{
    private static readonly XName KeyAttribute = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");

    // Rendered geometry, idle timing, repeat activity and feature access are
    // covered by tests/ImageViewer.UiTests/ViewerUiSmoke.cs. The former string
    // checks required the oversized cards and translated chrome being repaired.

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
