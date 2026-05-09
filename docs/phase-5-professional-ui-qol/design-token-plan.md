# Phase 5 Design Token Plan

## Purpose

This document defines the Phase 5 token system before implementation. It converts the professional UI requirements into named resource groups, validation targets, and surface ownership rules.

The current app has only a small brush set and one chrome panel style. Phase 5 must replace that with a complete, source-controlled UI vocabulary that can be applied across the viewer without per-view improvisation.

## Source basis

- Fluent 2 layout: use a four-pixel spacing ramp, proximity to express grouping, and alignment to create predictable scanning.
- Fluent 2 elevation: use elevation and strokes to communicate surface hierarchy; on Windows, strokes are part of the elevation language.
- Fluent 2 color: use neutral structure, semantic color, and restrained accent.
- Fluent 2 typography and Windows typography: use a coherent text ramp based on Windows-appropriate type roles.
- Microsoft Learn InfoBar: use built-in semantic severity when possible, and do not communicate severity only through color.

## Required resource dictionaries

Phase 5 implementation must add these dictionaries unless an implementation review proves an existing dictionary is a better location:

```text
src/ImageViewer.App/Resources/Styles/TextStyles.xaml
src/ImageViewer.App/Resources/Styles/LayoutTokens.xaml
src/ImageViewer.App/Resources/Styles/MotionTokens.xaml
```

The existing dictionaries remain:

```text
src/ImageViewer.App/Resources/ThemeTokens/DarkTheme.xaml
src/ImageViewer.App/Resources/ThemeTokens/LightTheme.xaml
src/ImageViewer.App/Resources/Styles/PanelStyles.xaml
src/ImageViewer.App/Resources/Styles/ButtonStyles.xaml
```

`App.xaml` must merge the new dictionaries in an order that lets style dictionaries reference theme tokens and layout tokens without circular assumptions.

## Token naming rules

- Prefix app-owned tokens with `Viewer`.
- Use semantic names, not visual trivia.
- Theme-specific color tokens live in `DarkTheme.xaml` and `LightTheme.xaml`.
- Theme-neutral geometry and duration tokens live in `LayoutTokens.xaml` and `MotionTokens.xaml`.
- Component styles live in style dictionaries and reference tokens.
- Do not create a token for a one-off value unless it belongs to a repeated visual grammar.

Examples:

```xaml
<SolidColorBrush x:Key="ViewerCanvasBackgroundBrush" Color="#FF111111" />
<Thickness x:Key="ViewerChromePanelPadding">12,8</Thickness>
<CornerRadius x:Key="ViewerPanelCornerRadius">8</CornerRadius>
<x:Double x:Key="ViewerChromeIdleOpacity">0.72</x:Double>
```

## Color token groups

Theme dictionaries must include both dark and light values for these roles:

### Canvas and surface

- `ViewerCanvasBackgroundBrush`
- `ViewerImageMatBrush`
- `ViewerChromeSurfaceBrush`
- `ViewerChromeSurfaceActiveBrush`
- `ViewerOverlaySurfaceBrush`
- `ViewerElevatedSurfaceBrush`
- `ViewerSubtleSurfaceBrush`
- `ViewerScrimBrush`

### Border and divider

- `ViewerBorderSubtleBrush`
- `ViewerBorderStrongBrush`
- `ViewerDividerBrush`
- `ViewerFocusStrokeBrush`
- `ViewerKeyboardFocusStrokeBrush`
- `ViewerImageSafeStrokeBrush`

### Text

- `ViewerTextPrimaryBrush`
- `ViewerTextSecondaryBrush`
- `ViewerTextMutedBrush`
- `ViewerTextDisabledBrush`
- `ViewerTextOnAccentBrush`
- `ViewerTextOnSemanticBrush`
- `ViewerPathTextBrush`

### Accent and state

- `ViewerAccentBrush`
- `ViewerAccentHoverBrush`
- `ViewerAccentPressedBrush`
- `ViewerSelectionBrush`
- `ViewerSelectionStrokeBrush`
- `ViewerHoverSurfaceBrush`
- `ViewerPressedSurfaceBrush`
- `ViewerDisabledSurfaceBrush`

### Semantic

- `ViewerInfoBrush`
- `ViewerInfoSurfaceBrush`
- `ViewerWarningBrush`
- `ViewerWarningSurfaceBrush`
- `ViewerErrorBrush`
- `ViewerErrorSurfaceBrush`
- `ViewerSuccessBrush`
- `ViewerSuccessSurfaceBrush`

Semantic roles must be visible through text, icon, shape, and layout as well as color.

## Typography token groups

`TextStyles.xaml` must define these text roles:

- `ViewerTextCaptionStyle`
- `ViewerTextMetadataLabelStyle`
- `ViewerTextMetadataValueStyle`
- `ViewerTextBodyStyle`
- `ViewerTextBodyStrongStyle`
- `ViewerTextPanelTitleStyle`
- `ViewerTextCommandTitleStyle`
- `ViewerTextCommandShortcutStyle`
- `ViewerTextStatusStyle`
- `ViewerTextPathStyle`
- `ViewerTextNumericStyle`
- `ViewerTextEmptyStateTitleStyle`
- `ViewerTextEmptyStateBodyStyle`

Typography rules:

- Use sentence case unless the source string is a proper name, shortcut, file extension, or system value.
- Do not use all caps for general emphasis.
- Avoid viewport-scaled font sizes.
- Maintain `TextTrimming` or wrapping rules for long filenames, folder names, dimensions, and metadata.
- Do not let metadata text compete visually with the image.

## Layout token groups

`LayoutTokens.xaml` must define:

### Spacing

- `ViewerSpace0`: `0`
- `ViewerSpace2`: `2`
- `ViewerSpace4`: `4`
- `ViewerSpace6`: `6`
- `ViewerSpace8`: `8`
- `ViewerSpace10`: `10`
- `ViewerSpace12`: `12`
- `ViewerSpace16`: `16`
- `ViewerSpace20`: `20`
- `ViewerSpace24`: `24`
- `ViewerSpace28`: `28`
- `ViewerSpace32`: `32`
- `ViewerSpace40`: `40`
- `ViewerSpace48`: `48`

### Thickness aliases

- `ViewerChromeOuterMargin`
- `ViewerChromePanelPadding`
- `ViewerOverlayPanelPadding`
- `ViewerCommandRowPadding`
- `ViewerMetadataPanelPadding`
- `ViewerThumbnailItemPadding`
- `ViewerStatusHudPadding`
- `ViewerInfoBarMargin`

### Radius

- `ViewerControlCornerRadius`
- `ViewerPanelCornerRadius`
- `ViewerOverlayCornerRadius`
- `ViewerThumbnailCornerRadius`
- `ViewerBadgeCornerRadius`
- `ViewerFocusCornerRadius`

### Sizing and density

- `ViewerIconSizeSmall`
- `ViewerIconSize`
- `ViewerIconSizeLarge`
- `ViewerToolButtonSize`
- `ViewerMediaButtonSize`
- `ViewerThumbnailSize`
- `ViewerThumbnailSizeCompact`
- `ViewerFilmstripThumbnailWidth`
- `ViewerFilmstripThumbnailHeight`
- `ViewerCommandPaletteWidth`
- `ViewerMetadataPanelWidth`
- `ViewerStatusHudMaxWidth`

### Opacity

- `ViewerChromeIdleOpacity`
- `ViewerChromeActiveOpacity`
- `ViewerOverlayScrimOpacity`
- `ViewerDisabledOpacity`
- `ViewerMutedOpacity`
- `ViewerSelectionOverlayOpacity`

## Elevation and border strategy

WinUI does not need decorative heavy shadows for the viewer. Phase 5 uses layered surfaces, strokes, and restrained shadow where supported by the platform.

Required roles:

- Canvas: no shadow, image-first.
- Chrome: semi-opaque surface plus subtle stroke.
- Overlay panels: elevated surface plus stroke; optional shadow only if it improves separation.
- HUD/status: compact elevated surface with strong text contrast.
- Thumbnail/current item: selection stroke plus structure, not glow.
- Warning/error: semantic icon/copy plus severity surface, not color alone.

`PanelStyles.xaml` must grow beyond `ChromePanelBorderStyle` into role-based styles.

## Validation tests

Create `tests/ImageViewer.App.Tests/Phase5ResourceTests.cs` with static resource validation for:

- Required dark theme color keys.
- Required light theme color keys.
- Required text style keys.
- Required layout token keys.
- Required motion token keys.
- Required panel and button style keys after they are introduced.

Tests should parse XAML dictionaries or load them through a stable app-test helper already used by the repository. The test must fail if a required key exists in one theme but not the other.

## Migration from current tokens

The current keys remain temporarily as compatibility aliases during the first implementation slice:

- `CanvasBackgroundBrush`
- `ChromeBackgroundBrush`
- `SubtlePanelBackgroundBrush`
- `SubtleBorderBrush`
- `PrimaryTextBrush`
- `SecondaryTextBrush`
- `MutedTextBrush`
- `AccentBrush`
- `ControlCornerRadius`
- `ChromePanelBorderStyle`

Phase 5-touched views should move to `Viewer*` tokens. Compatibility aliases may remain only to avoid breaking untouched views during incremental implementation.

## Completion evidence

Task 3 is complete only when:

- The token dictionaries exist and are merged.
- Light and dark values are both present.
- Static validation tests pass.
- At least one Phase 5-touched surface is using the new token system.
- No required token category is represented only by prose.

## Implementation status

Status: implemented for Phase 5 task 3.

Implemented dictionaries:

- `src/ImageViewer.App/Resources/ThemeTokens/DarkTheme.xaml`
- `src/ImageViewer.App/Resources/ThemeTokens/LightTheme.xaml`
- `src/ImageViewer.App/Resources/Styles/TextStyles.xaml`
- `src/ImageViewer.App/Resources/Styles/LayoutTokens.xaml`
- `src/ImageViewer.App/Resources/Styles/MotionTokens.xaml`
- `src/ImageViewer.App/Resources/Styles/PanelStyles.xaml`

Application merge point:

- `src/ImageViewer.App/App.xaml`

First migrated surface:

- `src/ImageViewer.App/Views/DropIndicator.xaml`

Validation:

- `tests/ImageViewer.App.Tests/Phase5ResourceTests.cs`
- `dotnet test tests\ImageViewer.App.Tests\ImageViewer.App.Tests.csproj -p:Platform=x64 -c Release --filter Phase5ResourceTests`
- `dotnet build SpitfireImg.sln -p:Platform=x64 -c Release`

Evidence:

- `TestResults/phase5-evidence/tokens/token-validation.md`
