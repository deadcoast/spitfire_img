namespace ImageViewer.App.Tests;

public sealed class Phase5LayoutSurfaceTests
{
    [Fact]
    public void ThumbnailStrip_UsesSharedDesignedThumbnailStates()
    {
        var xaml = ReadViewXaml("ThumbnailStrip.xaml");

        Assert.Contains("AutomationProperties.Name=\"Thumbnail strip\"", xaml);
        Assert.Contains("x:Name=\"ThumbnailStripList\"", xaml);
        Assert.Contains("ViewerMediaThumbnailListViewItemStyle", xaml);
        Assert.Contains("ViewerMediaThumbnailCardBorderStyle", xaml);
        Assert.Contains("ViewerMediaThumbnailPreviewBorderStyle", xaml);
        Assert.Contains("x:Name=\"ThumbnailCurrentBadge\"", xaml);
        Assert.Contains("x:Name=\"ThumbnailStatusBadge\"", xaml);
        Assert.Contains("CurrentLabel", xaml);
        Assert.Contains("StatusText", xaml);
    }

    [Fact]
    public void GridLayout_DeclaresModeHudAndDesignedCellHost()
    {
        var xaml = ReadViewXaml("GridLayout.xaml");
        var code = ReadViewCode("GridLayout.xaml.cs");

        Assert.Contains("AutomationProperties.Name=\"Grid layout surface\"", xaml);
        Assert.Contains("x:Name=\"GridModeHud\"", xaml);
        Assert.Contains("x:Name=\"GridDensityText\"", xaml);
        Assert.Contains("x:Name=\"GridPageText\"", xaml);
        Assert.Contains("x:Name=\"GridStatusText\"", xaml);
        Assert.Contains("x:Name=\"ImageHost\"", xaml);
        Assert.Contains("CreateGridFrame", code);
        Assert.Contains("ViewerGridImageFrameBorderStyle", code);
        Assert.Contains("ViewerGridImageActiveFrameBorderStyle", code);
        Assert.Contains("Loading", code);
        Assert.Contains("No preview", code);
        Assert.DoesNotContain("Colors.White", code);
        Assert.DoesNotContain("Colors.Transparent", code);
    }

    [Fact]
    public void FilmstripView_UsesSharedThumbnailLanguageAndFocusTreatment()
    {
        var xaml = ReadViewXaml("FilmstripView.xaml");

        Assert.Contains("AutomationProperties.Name=\"Filmstrip layout surface\"", xaml);
        Assert.Contains("x:Name=\"FilmstripFocusSurface\"", xaml);
        Assert.Contains("x:Name=\"FilmstripModeHud\"", xaml);
        Assert.Contains("x:Name=\"FilmstripThumbnailList\"", xaml);
        Assert.Contains("ViewerFilmstripFocusBorderStyle", xaml);
        Assert.Contains("ViewerMediaThumbnailListViewItemStyle", xaml);
        Assert.Contains("ViewerMediaThumbnailCardBorderStyle", xaml);
        Assert.Contains("ViewerMediaThumbnailPreviewBorderStyle", xaml);
        Assert.Contains("FilmstripCurrentBadge", xaml);
        Assert.Contains("FilmstripStatusBadge", xaml);
        Assert.Contains("CurrentLabel", xaml);
        Assert.Contains("StatusText", xaml);
    }

    [Fact]
    public void FilmstripView_DefersInitialRefreshUntilLoadedForSavedLayoutRestore()
    {
        var code = ReadViewCode("FilmstripView.xaml.cs");

        Assert.Contains("Loaded += OnLoaded", code);
        Assert.Contains("private bool _isLoaded", code);
        Assert.Contains("private bool _refreshQueued", code);
        Assert.Contains("QueueRefresh", code);
        Assert.Contains("for (var thumbnailIndex", code);
        Assert.DoesNotContain("Task.WhenAny(thumbnailTasks)", code);
        Assert.Contains("if (!_isLoaded)", code);
    }

    // Layout captures and bounds checks run in tests/ImageViewer.UiTests.
    // They no longer depend on an untracked script in ignored TestResults.

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
