using System.Xml.Linq;

namespace ImageViewer.App.Tests;

public sealed class Phase5SearchMetadataTests
{
    private static readonly XName NameAttribute = XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");
    private static readonly XName KeyAttribute = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");

    [Fact]
    public void SearchOverlay_DefinesProfessionalSearchSurfaceAndStates()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "SearchOverlay.xaml"));
        var xaml = document.ToString(SaveOptions.DisableFormatting);

        AssertNamedElement(document, "SearchOverlayRoot");
        AssertNamedElement(document, "SearchOverlayPanel");
        AssertNamedElement(document, "SearchHeader");
        AssertNamedElement(document, "SearchTitle");
        AssertNamedElement(document, "SearchSummaryText");
        AssertNamedElement(document, "SearchInput");
        AssertNamedElement(document, "ResultsList");
        AssertNamedElement(document, "NoSearchResultsPanel");
        AssertNamedElement(document, "NoSearchResultsTitle");
        AssertNamedElement(document, "NoSearchResultsBody");

        Assert.Contains("ViewerCommandPalettePanelBorderStyle", xaml);
        Assert.Contains("ViewerSearchResultRowBorderStyle", xaml);
        Assert.Contains("ViewerTextCommandPaletteTitleStyle", xaml);
        Assert.Contains("AutomationProperties.Name=\"Search images\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"Search by filename\"", xaml);
    }

    [Fact]
    public void SearchOverlay_ResultRowsExposeFilenamePathAndExtension()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "SearchOverlay.xaml"));
        var xaml = document.ToString(SaveOptions.DisableFormatting);

        AssertNamedElement(document, "SearchResultRow");
        AssertNamedElement(document, "SearchResultFilename");
        AssertNamedElement(document, "SearchResultPath");
        AssertNamedElement(document, "SearchResultExtension");

        Assert.Contains("Text=\"{Binding Filename}\"", xaml);
        Assert.Contains("Text=\"{Binding FilePath}\"", xaml);
        Assert.Contains("Text=\"{Binding Extension}\"", xaml);
        Assert.Contains("ViewerTextSearchResultTitleStyle", xaml);
        Assert.Contains("ViewerTextSearchResultPathStyle", xaml);
        Assert.DoesNotContain("#66000000", xaml);
    }

    [Fact]
    public void SearchOverlay_CodeUpdatesSummaryAndEmptyState()
    {
        var repositoryRoot = FindRepositoryRoot();
        var code = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "SearchOverlay.xaml.cs"));

        Assert.Contains("UpdateSearchSummary", code);
        Assert.Contains("SearchSummaryText.Text", code);
        Assert.Contains("NoSearchResultsPanel.Visibility", code);
        Assert.DoesNotContain("ResultCountText.Text", code);
    }

    [Fact]
    public void MetadataPanel_DefinesStructuredSectionsAndRows()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "MetadataPanel.xaml"));
        var xaml = document.ToString(SaveOptions.DisableFormatting);

        AssertNamedElement(document, "MetadataPanelSurface");
        AssertNamedElement(document, "MetadataHeader");
        AssertNamedElement(document, "MetadataTitle");
        AssertNamedElement(document, "MetadataSummary");
        AssertNamedElement(document, "FileSection");
        AssertNamedElement(document, "ImageSection");
        AssertNamedElement(document, "CameraSection");
        AssertNamedElement(document, "FileNameValue");
        AssertNamedElement(document, "FileSizeValue");
        AssertNamedElement(document, "DimensionsValue");
        AssertNamedElement(document, "ColorSpaceValue");
        AssertNamedElement(document, "CameraMakeValue");
        AssertNamedElement(document, "CameraModelValue");
        AssertNamedElement(document, "CaptureDateValue");
        AssertNamedElement(document, "GpsValue");

        Assert.Contains("ViewerMetadataSectionBorderStyle", xaml);
        Assert.Contains("ViewerTextMetadataSectionTitleStyle", xaml);
        Assert.Contains("AutomationProperties.Name=\"Image metadata panel\"", xaml);
        Assert.DoesNotContain("x:Name=\"FileText\"", xaml);
        Assert.DoesNotContain("x:Name=\"ImageText\"", xaml);
        Assert.DoesNotContain("x:Name=\"CameraText\"", xaml);
    }

    [Fact]
    public void MetadataPanel_CodePopulatesStructuredRows()
    {
        var repositoryRoot = FindRepositoryRoot();
        var code = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "MetadataPanel.xaml.cs"));

        Assert.Contains("SetMetadataRow", code);
        Assert.Contains("MetadataSummary.Text", code);
        Assert.Contains("SetMetadataRow(FileNameValue", code);
        Assert.Contains("SetMetadataRow(FileSizeValue", code);
        Assert.Contains("SetMetadataRow(DimensionsValue", code);
        Assert.Contains("SetMetadataRow(ColorSpaceValue", code);
        Assert.Contains("SetMetadataRow(CameraMakeValue", code);
        Assert.Contains("SetMetadataRow(GpsValue", code);
        Assert.DoesNotContain("FileText.Text", code);
        Assert.DoesNotContain("CameraText.Text =", code);
    }

    [Fact]
    public void Phase5SearchMetadataResources_DefineRequiredRoles()
    {
        var repositoryRoot = FindRepositoryRoot();
        var panelKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "PanelStyles.xaml"));
        var textKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "TextStyles.xaml"));
        var layoutKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "LayoutTokens.xaml"));

        Assert.Contains("ViewerSearchResultRowBorderStyle", panelKeys);
        Assert.Contains("ViewerMetadataSectionBorderStyle", panelKeys);

        Assert.Contains("ViewerTextSearchResultTitleStyle", textKeys);
        Assert.Contains("ViewerTextSearchResultPathStyle", textKeys);
        Assert.Contains("ViewerTextMetadataSectionTitleStyle", textKeys);

        Assert.Contains("ViewerSearchHeaderPadding", layoutKeys);
        Assert.Contains("ViewerSearchResultRowPadding", layoutKeys);
        Assert.Contains("ViewerMetadataSectionPadding", layoutKeys);
        Assert.Contains("ViewerMetadataRowSpacing", layoutKeys);
    }

    private static void AssertNamedElement(XDocument document, string name)
    {
        Assert.True(
            document.Descendants().Any(element => string.Equals(element.Attribute(NameAttribute)?.Value, name, StringComparison.Ordinal)),
            $"Missing named Phase 5 search/metadata element: {name}");
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
