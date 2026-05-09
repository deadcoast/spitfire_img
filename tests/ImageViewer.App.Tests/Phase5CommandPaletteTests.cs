using System.Xml.Linq;

namespace ImageViewer.App.Tests;

public sealed class Phase5CommandPaletteTests
{
    private static readonly XName NameAttribute = XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");
    private static readonly XName KeyAttribute = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");

    [Fact]
    public void CommandPalette_DefinesProfessionalPowerUserSurface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "CommandPalette.xaml"));
        var xaml = document.ToString(SaveOptions.DisableFormatting);

        AssertNamedElement(document, "PalettePanel");
        AssertNamedElement(document, "PaletteHeader");
        AssertNamedElement(document, "PaletteTitle");
        AssertNamedElement(document, "ResultSummaryText");
        AssertNamedElement(document, "SearchInput");
        AssertNamedElement(document, "CommandList");
        AssertNamedElement(document, "NoMatchesPanel");
        AssertNamedElement(document, "NoMatchesTitle");
        AssertNamedElement(document, "NoMatchesBody");

        Assert.Contains("ViewerCommandPalettePanelBorderStyle", xaml);
        Assert.Contains("ViewerCommandPaletteListViewItemStyle", xaml);
        Assert.Contains("ViewerTextCommandPaletteTitleStyle", xaml);
        Assert.Contains("AutomationProperties.Name=\"Command palette\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"Search commands\"", xaml);
    }

    [Fact]
    public void CommandPalette_CommandRowsExposeHierarchyShortcutAndDescription()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "CommandPalette.xaml"));
        var xaml = document.ToString(SaveOptions.DisableFormatting);

        AssertNamedElement(document, "CommandHeaderSurface");
        AssertNamedElement(document, "CommandRowSurface");
        AssertNamedElement(document, "CommandTitleText");
        AssertNamedElement(document, "CommandDescriptionText");
        AssertNamedElement(document, "CommandCategoryText");
        AssertNamedElement(document, "CommandShortcutPill");
        AssertNamedElement(document, "CommandShortcutText");

        Assert.Contains("Text=\"{Binding Match.Description}\"", xaml);
        Assert.Contains("Text=\"{Binding Match.Category}\"", xaml);
        Assert.Contains("ViewerCommandHeaderBorderStyle", xaml);
        Assert.Contains("ViewerCommandRowBorderStyle", xaml);
        Assert.Contains("ViewerCommandShortcutPillBorderStyle", xaml);
        Assert.Contains("ViewerTextCommandDescriptionStyle", xaml);
    }

    [Fact]
    public void CommandPalette_CodeUpdatesSummaryAndUsesTokenizedHighlight()
    {
        var repositoryRoot = FindRepositoryRoot();
        var code = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "CommandPalette.xaml.cs"));

        Assert.Contains("UpdateResultSummary", code);
        Assert.Contains("ResultSummaryText.Text", code);
        Assert.Contains("ViewerAccentBrush", code);
        Assert.DoesNotContain("Colors.Yellow", code);
    }

    [Fact]
    public void Phase5CommandPaletteResources_DefineRequiredRoles()
    {
        var repositoryRoot = FindRepositoryRoot();
        var panelKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "PanelStyles.xaml"));
        var textKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "TextStyles.xaml"));
        var layoutKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "LayoutTokens.xaml"));

        Assert.Contains("ViewerCommandPalettePanelBorderStyle", panelKeys);
        Assert.Contains("ViewerCommandRowBorderStyle", panelKeys);
        Assert.Contains("ViewerCommandHeaderBorderStyle", panelKeys);
        Assert.Contains("ViewerCommandShortcutPillBorderStyle", panelKeys);

        Assert.Contains("ViewerTextCommandPaletteTitleStyle", textKeys);
        Assert.Contains("ViewerTextCommandHeaderStyle", textKeys);
        Assert.Contains("ViewerTextCommandDescriptionStyle", textKeys);

        Assert.Contains("ViewerCommandPaletteHeaderSpacing", layoutKeys);
        Assert.Contains("ViewerCommandRowContentSpacing", layoutKeys);
        Assert.Contains("ViewerCommandShortcutPillPadding", layoutKeys);
    }

    private static void AssertNamedElement(XDocument document, string name)
    {
        Assert.True(
            document.Descendants().Any(element => string.Equals(element.Attribute(NameAttribute)?.Value, name, StringComparison.Ordinal)),
            $"Missing named command palette element: {name}");
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
