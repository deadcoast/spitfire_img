using System.Xml.Linq;

namespace ImageViewer.App.Tests;

public sealed class Phase5StatusSurfaceTests
{
    private static readonly XName NameAttribute = XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");
    private static readonly XName KeyAttribute = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");

    [Fact]
    public void ImageCanvas_DefinesFinishedEmptyStateSurface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "ImageCanvas.xaml"));
        var xaml = document.ToString(SaveOptions.DisableFormatting);

        AssertNamedElement(document, "EmptyStateSurface");
        AssertNamedElement(document, "EmptyStateKicker");
        AssertNamedElement(document, "EmptyStateTitle");
        AssertNamedElement(document, "EmptyStateBody");
        AssertNamedElement(document, "EmptyStateFormatHint");

        Assert.Contains("ViewerEmptyStatePanelBorderStyle", xaml);
        Assert.Contains("ViewerTextEmptyStateKickerStyle", xaml);
        Assert.Contains("ViewerTextEmptyStateTitleStyle", xaml);
        Assert.Contains("ViewerTextEmptyStateBodyStyle", xaml);
        Assert.Contains("AutomationProperties.Name=\"Empty image viewer state\"", xaml);
        Assert.DoesNotContain("x:Name=\"EmptyStateText\"", xaml);
    }

    [Fact]
    public void MainWindow_DefinesPolishedLoadingWarningAndErrorSurfaces()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "MainWindow.xaml"));
        var xaml = document.ToString(SaveOptions.DisableFormatting);

        AssertNamedElement(document, "ErrorInfoBar");
        AssertNamedElement(document, "ErrorRecoveryButton");
        AssertNamedElement(document, "LoadingOverlay");
        AssertNamedElement(document, "LoadingStatusTitle");
        AssertNamedElement(document, "LoadingStatusBody");
        AssertNamedElement(document, "CancelLoadingButton");

        Assert.Contains("ViewerLoadingStatusBorderStyle", xaml);
        Assert.Contains("ViewerStatusInfoBarMargin", xaml);
        Assert.Contains("ViewerTextStatusTitleStyle", xaml);
        Assert.Contains("ViewerTextStatusBodyStyle", xaml);
        Assert.Contains("AutomationProperties.Name=\"Image loading status\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"Image warning or error status\"", xaml);
    }

    [Fact]
    public void MainWindow_StatusCodeProvidesActionableCopyAndRecovery()
    {
        var repositoryRoot = FindRepositoryRoot();
        var code = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "MainWindow.xaml.cs"));

        Assert.Contains("LoadingStatusBody.Text", code);
        Assert.Contains("ErrorInfoBar.Title", code);
        Assert.Contains("ErrorRecoveryButton.Visibility", code);
        Assert.Contains("OnErrorRecoveryClicked", code);
        Assert.Contains("OpenFileFromPickerAsync", code);
        Assert.Contains("Unable to open image", code);
        Assert.Contains("Review needed", code);
    }

    [Fact]
    public void Phase5StatusSurfaceResources_DefineRequiredRoles()
    {
        var repositoryRoot = FindRepositoryRoot();
        var panelKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "PanelStyles.xaml"));
        var textKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "TextStyles.xaml"));
        var layoutKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "LayoutTokens.xaml"));

        Assert.Contains("ViewerEmptyStatePanelBorderStyle", panelKeys);
        Assert.Contains("ViewerLoadingStatusBorderStyle", panelKeys);

        Assert.Contains("ViewerTextEmptyStateKickerStyle", textKeys);
        Assert.Contains("ViewerTextStatusTitleStyle", textKeys);
        Assert.Contains("ViewerTextStatusBodyStyle", textKeys);

        Assert.Contains("ViewerEmptyStatePanelPadding", layoutKeys);
        Assert.Contains("ViewerEmptyStateMaxWidth", layoutKeys);
        Assert.Contains("ViewerStatusSurfacePadding", layoutKeys);
        Assert.Contains("ViewerStatusPanelMaxWidth", layoutKeys);
        Assert.Contains("ViewerStatusInfoBarMargin", layoutKeys);
    }

    private static void AssertNamedElement(XDocument document, string name)
    {
        Assert.True(
            document.Descendants().Any(element => string.Equals(element.Attribute(NameAttribute)?.Value, name, StringComparison.Ordinal)),
            $"Missing named Phase 5 status surface element: {name}");
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
