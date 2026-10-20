using System.Xml.Linq;

namespace ImageViewer.App.Tests;

public sealed class Phase5InputErgonomicsTests
{
    [Fact]
    public void MainWindow_UsesCentralInputRoutingBeforeGlobalNavigationShortcuts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var code = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "MainWindow.xaml.cs"));

        Assert.Contains("ViewerInputRouting.ResolveTopLayer", code);
        Assert.Contains("TryHandleTopLayerKeyAsync", code);
        Assert.Contains("ViewerInputRouting.ShouldSuppressRootNavigation", code);
        Assert.Contains("_viewModel.HideSearch()", code);
        Assert.True(
            code.IndexOf("TryHandleTopLayerKeyAsync", StringComparison.Ordinal) <
            code.IndexOf("case VirtualKey.Right:", StringComparison.Ordinal),
            "Top-layer routing must run before global image navigation.");
    }

    [Fact]
    public void TextOverlays_TrapKeyboardTraversalAndKeepFocusInsideActiveOverlay()
    {
        var repositoryRoot = FindRepositoryRoot();
        var commandPalette = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "CommandPalette.xaml"));
        var searchOverlay = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "SearchOverlay.xaml"));
        var metadataPanel = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "MetadataPanel.xaml"));

        Assert.Contains("TabFocusNavigation=\"Cycle\"", commandPalette);
        Assert.Contains("TabFocusNavigation=\"Cycle\"", searchOverlay);
        Assert.Contains("TabFocusNavigation=\"Cycle\"", metadataPanel);
        Assert.Contains("IsTabStop=\"True\"", metadataPanel);
    }

    [Fact]
    public void ModeAndMediaSurfaces_ExposeKeyboardFocusAndPointerAffordances()
    {
        var repositoryRoot = FindRepositoryRoot();
        var gridXaml = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "GridLayout.xaml"));
        var filmstripXaml = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "FilmstripView.xaml"));
        var comparisonXaml = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "ComparisonView.xaml"));
        var frameControlXaml = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "FrameControl.xaml"));
        var gridCode = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "GridLayout.xaml.cs"));
        var comparisonCode = File.ReadAllText(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "ComparisonView.xaml.cs"));

        Assert.Contains("IsTabStop=\"True\"", gridXaml);
        Assert.Contains("IsTabStop=\"True\"", filmstripXaml);
        Assert.Contains("IsTabStop=\"True\"", comparisonXaml);
        Assert.Contains("TabFocusNavigation=\"Cycle\"", frameControlXaml);
        Assert.Contains("InputSystemCursorShape.Hand", gridCode);
        Assert.Contains("InputSystemCursorShape.Hand", comparisonCode);
    }

    [Fact]
    public void PointerAndDisabledStates_AreVisibleInPhase5TouchedStyles()
    {
        var repositoryRoot = FindRepositoryRoot();
        var panelStyles = XDocument.Load(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "PanelStyles.xaml"));
        var styleText = panelStyles.ToString(SaveOptions.DisableFormatting);

        Assert.Contains("ViewerCommandPaletteListViewItemStyle", styleText);
        Assert.Contains("ViewerMediaThumbnailListViewItemStyle", styleText);
        Assert.Contains("UseSystemFocusVisuals", styleText);
        Assert.Contains("ViewerInteractiveImageFrameBorderStyle", styleText);
        Assert.Contains("ViewerInteractiveImageFrameActiveBorderStyle", styleText);
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
