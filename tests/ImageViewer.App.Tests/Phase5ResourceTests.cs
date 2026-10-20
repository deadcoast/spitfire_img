using System.Xml.Linq;

namespace ImageViewer.App.Tests;

public sealed class Phase5ResourceTests
{
    private static readonly XName KeyAttribute = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");
    private static readonly string[] Phase5TouchedXamlFiles =
    [
        Path.Combine("src", "ImageViewer.App", "MainWindow.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "BottomInfoBar.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "CommandPalette.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "ComparisonView.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "DropIndicator.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "FadingChrome.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "FilmstripView.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "FrameControl.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "GridLayout.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "ImageCanvas.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "MetadataPanel.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "SearchOverlay.xaml"),
        Path.Combine("src", "ImageViewer.App", "Views", "ThumbnailStrip.xaml")
    ];

    private static readonly string[] InlineStylingAttributes =
    [
        "Background",
        "BorderBrush",
        "CornerRadius",
        "FontSize",
        "Foreground",
        "Height",
        "Margin",
        "MaxHeight",
        "MinWidth",
        "Opacity",
        "Padding",
        "Spacing",
        "ColumnSpacing",
        "RowSpacing",
        "Width"
    ];

    private static readonly string[] RequiredThemeKeys =
    [
        "ViewerCanvasBackgroundBrush",
        "ViewerTransparentBrush",
        "ViewerImageMatBrush",
        "ViewerChromeSurfaceBrush",
        "ViewerChromeSurfaceActiveBrush",
        "ViewerOverlaySurfaceBrush",
        "ViewerElevatedSurfaceBrush",
        "ViewerSubtleSurfaceBrush",
        "ViewerScrimBrush",
        "ViewerBorderSubtleBrush",
        "ViewerBorderStrongBrush",
        "ViewerDividerBrush",
        "ViewerFocusStrokeBrush",
        "ViewerKeyboardFocusStrokeBrush",
        "ViewerImageSafeStrokeBrush",
        "ViewerTextPrimaryBrush",
        "ViewerTextSecondaryBrush",
        "ViewerTextMutedBrush",
        "ViewerTextDisabledBrush",
        "ViewerTextOnAccentBrush",
        "ViewerTextOnSemanticBrush",
        "ViewerPathTextBrush",
        "ViewerAccentBrush",
        "ViewerAccentHoverBrush",
        "ViewerAccentPressedBrush",
        "ViewerSelectionBrush",
        "ViewerSelectionStrokeBrush",
        "ViewerHoverSurfaceBrush",
        "ViewerPressedSurfaceBrush",
        "ViewerDisabledSurfaceBrush",
        "ViewerInfoBrush",
        "ViewerInfoSurfaceBrush",
        "ViewerWarningBrush",
        "ViewerWarningSurfaceBrush",
        "ViewerErrorBrush",
        "ViewerErrorSurfaceBrush",
        "ViewerSuccessBrush",
        "ViewerSuccessSurfaceBrush",
        "CanvasBackgroundBrush",
        "ChromeBackgroundBrush",
        "SubtlePanelBackgroundBrush",
        "SubtleBorderBrush",
        "PrimaryTextBrush",
        "SecondaryTextBrush",
        "MutedTextBrush",
        "AccentBrush"
    ];

    private static readonly string[] RequiredTextStyleKeys =
    [
        "ViewerTextCaptionStyle",
        "ViewerTextMetadataLabelStyle",
        "ViewerTextMetadataValueStyle",
        "ViewerTextBodyStyle",
        "ViewerTextBodyStrongStyle",
        "ViewerTextPanelTitleStyle",
        "ViewerTextCommandTitleStyle",
        "ViewerTextCommandPaletteTitleStyle",
        "ViewerTextCommandHeaderStyle",
        "ViewerTextCommandDescriptionStyle",
        "ViewerTextSearchResultTitleStyle",
        "ViewerTextSearchResultPathStyle",
        "ViewerTextMetadataSectionTitleStyle",
        "ViewerTextCommandShortcutStyle",
        "ViewerTextStatusStyle",
        "ViewerTextPathStyle",
        "ViewerTextNumericStyle",
        "ViewerTextEmptyStateKickerStyle",
        "ViewerTextEmptyStateTitleStyle",
        "ViewerTextEmptyStateBodyStyle",
        "ViewerTextEmptyStatePromptStyle",
        "ViewerTextStatusTitleStyle",
        "ViewerTextStatusBodyStyle",
        "ViewerTextBadgeStyle"
    ];

    private static readonly string[] RequiredLayoutKeys =
    [
        "ViewerSpace0",
        "ViewerSpace2",
        "ViewerSpace4",
        "ViewerSpace6",
        "ViewerSpace8",
        "ViewerSpace10",
        "ViewerSpace12",
        "ViewerSpace16",
        "ViewerSpace18",
        "ViewerSpace20",
        "ViewerSpace24",
        "ViewerSpace28",
        "ViewerSpace32",
        "ViewerSpace40",
        "ViewerSpace48",
        "ViewerChromeOuterMargin",
        "ViewerChromePanelPadding",
        "ViewerMenuBarPadding",
        "ViewerBottomInfoBarPadding",
        "ViewerOverlayPanelPadding",
        "ViewerOverlayContentPadding",
        "ViewerCommandRowPadding",
        "ViewerCommandHeaderPadding",
        "ViewerCommandShortcutPillPadding",
        "ViewerCommandPanelHeaderPadding",
        "ViewerSearchHeaderPadding",
        "ViewerSearchResultRowPadding",
        "ViewerMetadataPanelPadding",
        "ViewerMetadataSectionPadding",
        "ViewerThumbnailItemPadding",
        "ViewerFilmstripThumbnailPadding",
        "ViewerThumbnailBadgePadding",
        "ViewerGridHudPadding",
        "ViewerGridImageFramePadding",
        "ViewerGridImageFooterPadding",
        "ViewerStatusHudPadding",
        "ViewerInfoBarMargin",
        "ViewerComparisonHudPadding",
        "ViewerEmptyStatePanelPadding",
        "ViewerStatusSurfacePadding",
        "ViewerControlCornerRadius",
        "ViewerPanelCornerRadius",
        "ViewerOverlayCornerRadius",
        "ViewerThumbnailCornerRadius",
        "ViewerBadgeCornerRadius",
        "ViewerComparisonHudCornerRadius",
        "ViewerFocusCornerRadius",
        "ViewerIconSizeSmall",
        "ViewerIconSize",
        "ViewerIconSizeLarge",
        "ViewerToolButtonSize",
        "ViewerMediaButtonSize",
        "ViewerFrameButtonWidth",
        "ViewerFrameButtonHeight",
        "ViewerThumbnailSize",
        "ViewerThumbnailSizeCompact",
        "ViewerFilmstripThumbnailWidth",
        "ViewerFilmstripThumbnailHeight",
        "ViewerCommandPaletteWidth",
        "ViewerCommandShortcutMinWidth",
        "ViewerMetadataPanelWidth",
        "ViewerStatusHudMaxWidth",
        "ViewerEmptyStateMaxWidth",
        "ViewerStatusPanelMaxWidth",
        "ViewerChromeIdleOpacity",
        "ViewerChromeActiveOpacity",
        "ViewerOverlayScrimOpacity",
        "ViewerDisabledOpacity",
        "ViewerMutedOpacity",
        "ViewerSelectionOverlayOpacity",
        "ViewerHiddenOpacity",
        "ViewerLoadingRingSize",
        "ViewerOverlayMaxHeight",
        "ViewerOverlayResultsMaxHeight",
        "ViewerComparisonHudWidth",
        "ViewerGridHudMinWidth",
        "ViewerFrameSliderWidth",
        "ViewerFrameCounterMinWidth",
        "ViewerThumbnailItemWidth",
        "ViewerThumbnailPreviewSize",
        "ViewerThumbnailBadgeMinWidth",
        "ViewerFilmstripPreviewSize",
        "ViewerChromeBadgePadding",
        "ViewerCompactPanelPadding",
        "ViewerMediaPanelPadding",
        "ViewerCommandSearchBoxPadding",
        "ViewerCommandShortcutPadding",
        "ViewerCommandEmptyStatePadding",
        "ViewerComparisonHudPadding",
        "ViewerComparisonHudMargin",
        "ViewerCommandPaletteMargin",
        "ViewerSearchOverlayMargin",
        "ViewerMetadataPanelMargin",
        "ViewerFilmstripTopMargin",
        "ViewerFilmstripBottomMargin",
        "ViewerFilmstripThumbnailOverlayMargin",
        "ViewerGridHudMargin",
        "ViewerTrailingItemMargin",
        "ViewerChromeFrameControlMargin",
        "ViewerLoadingOverlaySpacing",
        "ViewerPanelContentSpacing",
        "ViewerMetadataContentSpacing",
        "ViewerCommandRowColumnSpacing",
        "ViewerCommandPaletteHeaderSpacing",
        "ViewerCommandRowContentSpacing",
        "ViewerSearchResultContentSpacing",
        "ViewerMetadataRowSpacing",
        "ViewerMediaControlColumnSpacing",
        "ViewerFilmstripRowSpacing",
        "ViewerInfoBarColumnSpacing",
        "ViewerThumbnailContentSpacing",
        "ViewerGridHudSpacing",
        "ViewerEmptyStateContentSpacing",
        "ViewerStatusSurfaceSpacing",
        "ViewerStatusInfoBarMargin",
        "ControlCornerRadius"
    ];

    private static readonly string[] RequiredMotionKeys =
    [
        "ViewerMotionInstantDurationMs",
        "ViewerMotionFastDurationMs",
        "ViewerMotionNormalDurationMs",
        "ViewerMotionDeliberateDurationMs",
        "ViewerMotionEntranceDurationMs",
        "ViewerMotionExitDurationMs",
        "ViewerMotionRepositionDurationMs",
        "ViewerMotionImageTransitionDurationMs",
        "ViewerMotionReducedDurationMs",
        "ViewerMotionStandardEasing",
        "ViewerMotionEmphasizedEasing",
        "ViewerMotionExitEasing",
        "ViewerMotionLinearEasing",
        "ViewerMotionEaseOut",
        "ViewerMotionEaseIn",
        "ViewerMotionEaseInOut",
        "ViewerMotionLinear",
        "ViewerMotionOverlayEntranceOffset",
        "ViewerMotionChromeRevealOffset",
        "ViewerMotionPanelSlideOffset",
        "ViewerMotionSubtleScaleFrom",
        "ViewerMotionSubtleScaleTo",
        "ViewerMotionCategoryChromeReveal",
        "ViewerMotionCategoryChromeIdle",
        "ViewerMotionCategoryOverlayEntrance",
        "ViewerMotionCategoryOverlayExit",
        "ViewerMotionCategoryMetadataPanel",
        "ViewerMotionCategoryImageNavigation",
        "ViewerMotionCategoryLayoutModeChange",
        "ViewerMotionCategoryReposition",
        "ViewerMotionCategoryFeedback",
        "ViewerMotionCategoryStatus",
        "ViewerMotionCategoryDropIndicator",
        "ViewerMotionCategoryGifControls",
        "ViewerMotionCategoryComparison"
    ];

    private static readonly string[] RequiredPanelStyleKeys =
    [
        "ViewerChromePanelBorderStyle",
        "ViewerOverlayPanelBorderStyle",
        "ViewerCommandPalettePanelBorderStyle",
        "ViewerCommandRowBorderStyle",
        "ViewerCommandHeaderBorderStyle",
        "ViewerCommandShortcutPillBorderStyle",
        "ViewerCommandPaletteListViewItemStyle",
        "ViewerSearchResultRowBorderStyle",
        "ViewerMetadataSectionBorderStyle",
        "ViewerMediaThumbnailListViewItemStyle",
        "ViewerMediaThumbnailCardBorderStyle",
        "ViewerMediaThumbnailPreviewBorderStyle",
        "ViewerMediaThumbnailStateBadgeBorderStyle",
        "ViewerMediaThumbnailCurrentBadgeBorderStyle",
        "ViewerGridHudBorderStyle",
        "ViewerGridImageFrameBorderStyle",
        "ViewerGridImageActiveFrameBorderStyle",
        "ViewerGridImageFooterBorderStyle",
        "ViewerFilmstripFocusBorderStyle",
        "ViewerStatusHudBorderStyle",
        "ViewerEmptyStatePanelBorderStyle",
        "ViewerLoadingStatusBorderStyle",
        "ChromePanelBorderStyle"
    ];

    [Fact]
    public void Phase5ThemeDictionaries_DefineRequiredTokensInLightAndDark()
    {
        var repositoryRoot = FindRepositoryRoot();
        var darkKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "ThemeTokens", "DarkTheme.xaml"));
        var lightKeys = ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "ThemeTokens", "LightTheme.xaml"));

        AssertRequiredKeys(darkKeys, RequiredThemeKeys, "dark theme");
        AssertRequiredKeys(lightKeys, RequiredThemeKeys, "light theme");
        Assert.True(
            darkKeys.SetEquals(lightKeys),
            "Light and dark theme dictionaries must expose the same token keys.");
    }

    [Fact]
    public void Phase5StyleDictionaries_DefineRequiredTextLayoutMotionAndPanelTokens()
    {
        var repositoryRoot = FindRepositoryRoot();

        AssertRequiredKeys(
            ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "TextStyles.xaml")),
            RequiredTextStyleKeys,
            "text styles");
        AssertRequiredKeys(
            ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "LayoutTokens.xaml")),
            RequiredLayoutKeys,
            "layout tokens");
        AssertRequiredKeys(
            ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "MotionTokens.xaml")),
            RequiredMotionKeys,
            "motion tokens");
        AssertRequiredKeys(
            ReadResourceKeys(Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Resources", "Styles", "PanelStyles.xaml")),
            RequiredPanelStyleKeys,
            "panel styles");
    }

    [Fact]
    public void AppResources_MergePhase5TokenDictionariesBeforeComponentStyles()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appXamlPath = Path.Combine(repositoryRoot, "src", "ImageViewer.App", "App.xaml");
        var document = XDocument.Load(appXamlPath);
        var mergedSources = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ResourceDictionary")
            .Select(element => element.Attribute("Source")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();

        AssertMergeOrder(mergedSources, "Resources/Styles/LayoutTokens.xaml", "Resources/Styles/PanelStyles.xaml");
        AssertMergeOrder(mergedSources, "Resources/Styles/MotionTokens.xaml", "Resources/Styles/PanelStyles.xaml");
        AssertMergeOrder(mergedSources, "Resources/Styles/TextStyles.xaml", "Resources/Styles/PanelStyles.xaml");
        Assert.Contains("Resources/ThemeTokens/DarkTheme.xaml", mergedSources);
        Assert.Contains("Resources/ThemeTokens/LightTheme.xaml", mergedSources);
    }

    [Fact]
    public void DropIndicator_UsesPhase5TokensAsFirstMigratedSurface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var dropIndicatorPath = Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Views", "DropIndicator.xaml");
        var xaml = File.ReadAllText(dropIndicatorPath);

        Assert.Contains("ViewerScrimBrush", xaml);
        Assert.Contains("ViewerOverlayPanelPadding", xaml);
        Assert.Contains("ViewerOverlayPanelBorderStyle", xaml);
        Assert.Contains("ViewerTextEmptyStateTitleStyle", xaml);
        Assert.DoesNotContain("#66000000", xaml);
    }

    [Fact]
    public void Phase5TouchedXaml_UsesNamedResourcesForReusableStylingValues()
    {
        var repositoryRoot = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var relativePath in Phase5TouchedXamlFiles)
        {
            var path = Path.Combine(repositoryRoot, relativePath);
            Assert.True(File.Exists(path), $"Missing Phase 5-touched XAML file: {relativePath}");

            var document = XDocument.Load(path);
            foreach (var element in document.Descendants())
            {
                foreach (var attribute in element.Attributes())
                {
                    if (!InlineStylingAttributes.Contains(attribute.Name.LocalName, StringComparer.Ordinal) ||
                        IsAllowedStructuralGridSizing(element, attribute) ||
                        IsNamedResource(attribute.Value))
                    {
                        continue;
                    }

                    violations.Add($"{relativePath}: <{element.Name.LocalName}> {attribute.Name.LocalName}=\"{attribute.Value}\"");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Phase 5-touched XAML has inline reusable styling values: " + string.Join("; ", violations));
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

    private static void AssertRequiredKeys(
        IReadOnlySet<string> actualKeys,
        IEnumerable<string> requiredKeys,
        string dictionaryName)
    {
        var missing = requiredKeys
            .Where(requiredKey => !actualKeys.Contains(requiredKey))
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"{dictionaryName} is missing required Phase 5 resource keys: {string.Join(", ", missing)}");
    }

    private static void AssertMergeOrder(
        List<string> mergedSources,
        string earlierSource,
        string laterSource)
    {
        var earlierIndex = mergedSources.IndexOf(earlierSource);
        var laterIndex = mergedSources.IndexOf(laterSource);

        Assert.True(earlierIndex >= 0, $"Missing merged dictionary: {earlierSource}");
        Assert.True(laterIndex >= 0, $"Missing merged dictionary: {laterSource}");
        Assert.True(
            earlierIndex < laterIndex,
            $"{earlierSource} must be merged before {laterSource}.");
    }

    private static bool IsAllowedStructuralGridSizing(XElement element, XAttribute attribute)
    {
        return attribute.Name.LocalName is "Width" or "Height" &&
               element.Name.LocalName is "ColumnDefinition" or "RowDefinition";
    }

    private static bool IsNamedResource(string value)
    {
        return value.Contains("{StaticResource ", StringComparison.Ordinal) ||
               value.Contains("{ThemeResource ", StringComparison.Ordinal);
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
