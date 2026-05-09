# Phase 5 Current UI Visual Audit

## Purpose

This audit records the current Phase 5 UI baseline before professional UI implementation. It identifies what exists, what is unfinished, and what must be improved. It does not claim visual completion.

Screenshot evidence is still incomplete. This document is the source audit portion of task 2 and records the current evidence gaps without treating them as complete.

## Baseline summary

The application is functionally broad but visually underdeveloped. It has:

- A small theme brush set in `DarkTheme.xaml` and `LightTheme.xaml`.
- One shared chrome panel style in `PanelStyles.xaml`.
- Limited button styling in `ButtonStyles.xaml`.
- Sparse per-view XAML with many local sizes, margins, widths, and opacity values.
- A neutral canvas and basic chrome panels.
- Some transition usage, mostly `EntranceThemeTransition` or code-driven opacity/translation.

This baseline is valid for a functional app. It is not a professional UI system.

## Current token and style audit

### Theme dictionaries

Files:

- `src/ImageViewer.App/Resources/ThemeTokens/DarkTheme.xaml`
- `src/ImageViewer.App/Resources/ThemeTokens/LightTheme.xaml`

Current state:

- Tokens are mostly simple brushes: canvas, chrome, subtle panel, border, text, muted text, accent.
- There are no named semantic brushes for focus, selected, hover, pressed, warning, error, success, status, overlay scrim, image-safe chrome, or disabled state.
- There are no typography, spacing, radius, density, opacity, icon-size, or motion tokens.
- Light theme currently reads warm/beige compared with the dark theme's neutral charcoal. That may be acceptable as a deliberate choice only if Phase 5 validates it against the final visual direction; otherwise it risks becoming a one-note theme.

Required Phase 5 work:

- Expand token groups.
- Add validation tests for required token keys.
- Replace hard-coded repeated values in touched views.
- Define light and dark values intentionally instead of relying on parallel but incomplete dictionaries.

### Shared styles

Files:

- `src/ImageViewer.App/Resources/Styles/PanelStyles.xaml`
- `src/ImageViewer.App/Resources/Styles/ButtonStyles.xaml`

Current state:

- `ChromePanelBorderStyle` sets only corner radius and background.
- Border, shadow/elevation, material, padding, and active/inactive variants are not modeled.
- Most view-specific controls define widths, margins, padding, and corner radii locally.

Required Phase 5 work:

- Add panel roles: chrome, overlay, floating HUD, status, command palette, metadata pane, thumbnail rail.
- Add button roles: icon tool, command row, palette action, media/frame control, destructive/warning where needed.
- Add text styles and motion/layout token dictionaries.

## Surface audit

### Main image canvas and empty state

Files:

- `src/ImageViewer.App/Views/ImageCanvas.xaml`

Current state:

- Canvas is clean and image-first.
- Empty state is a single centered text string: `Open an image to begin`.
- No affordance hierarchy, drop/open action, supported format hint, recent path cue, or professional empty-state structure.
- No image navigation transition is declared.

Risks:

- Looks unfinished when no image is loaded.
- Does not express product identity beyond text.
- Does not guide first-run behavior.

Required Phase 5 work:

- Build a restrained empty state that is useful without becoming a marketing hero.
- Keep the image canvas neutral and inspection-focused.
- Add a status/progress layer that does not compete with loaded imagery.

### Top chrome and bottom info bar

Files:

- `src/ImageViewer.App/Views/FadingChrome.xaml`
- `src/ImageViewer.App/Views/FadingChrome.xaml.cs`
- `src/ImageViewer.App/Views/BottomInfoBar.xaml`

Current state:

- Top chrome is a simple `MenuBar` inside a panel.
- Bottom info bar uses a four-column grid with filename, position, dimensions, and folder.
- Unsaved badge uses `AccentFillColorDefaultBrush` and rounded badge styling outside the token system.
- Idle/active state is handled in code, but the visual language is minimal.

Risks:

- Top chrome reads like default desktop boilerplate rather than designed app chrome.
- Bottom bar lacks hierarchy for long filenames, metadata, and state.
- Unsaved badge is disconnected from the local token system.
- Chrome may compete with image content because no image-safe surface strategy exists.

Required Phase 5 work:

- Redesign chrome as a coherent shell with active, idle, hover, focus, warning, and unsaved states.
- Add density and spacing tokens.
- Consider extracting `AppChrome.xaml` if `FadingChrome` remains responsible for too many concerns.

### Command palette

Files:

- `src/ImageViewer.App/Views/CommandPalette.xaml`
- `src/ImageViewer.App/Views/CommandPalette.xaml.cs`

Current state:

- Panel is fixed width with default `TextBox` and `ListView`.
- Command grouping is implemented in code-behind using visibility changes.
- Shortcut text exists but has limited hierarchy and no dedicated command row style.
- Empty state is a plain text row.
- There is no explicit entrance/exit choreography in XAML.

Risks:

- Looks like a basic sample rather than a finished power-user surface.
- Keyboard and selected states depend heavily on default ListView visuals.
- Command hierarchy and row density need design.

Required Phase 5 work:

- Design command row, group header, shortcut chip, match highlight, selected row, empty state, and keyboard focus.
- Keep all existing command registry shortcuts.
- Add motion and reduced-motion behavior.

### Search overlay

Files:

- `src/ImageViewer.App/Views/SearchOverlay.xaml`
- `src/ImageViewer.App/Views/SearchOverlay.xaml.cs`

Current state:

- Uses hard-coded scrim `#66000000`.
- Uses `EntranceThemeTransition`.
- Results are bare filename `TextBlock` items.
- Result count exists but lacks status hierarchy.

Risks:

- Sparse results do not communicate current selection, context, or folder.
- Hard-coded scrim bypasses token strategy.
- Search and command palette share patterns but not a shared design system.

Required Phase 5 work:

- Introduce overlay/scrim tokens.
- Unify search and command palette panel behavior where appropriate.
- Add selected/focused result states, empty state, keyboard traversal evidence, and motion review.

### Metadata panel

Files:

- `src/ImageViewer.App/Views/MetadataPanel.xaml`
- `src/ImageViewer.App/Views/MetadataPanel.xaml.cs`

Current state:

- Right panel with title and three wrapping text blocks.
- Translation transform exists for slide behavior.
- No explicit section rows, labels, icons, copy affordances, or overflow treatment.

Risks:

- Metadata is technically present but visually flat.
- Long metadata can become hard to scan.
- The panel width and margins are local literals.

Required Phase 5 work:

- Create metadata row/section styles.
- Add typography hierarchy and truncation/wrap strategy.
- Add consistent panel transition and reduced-motion behavior.

### Thumbnail strip

Files:

- `src/ImageViewer.App/Views/ThumbnailStrip.xaml`
- `src/ImageViewer.App/Views/ThumbnailStrip.xaml.cs`

Current state:

- Uses horizontal `ListView`.
- Item cards have local widths, padding, radius, and margin.
- Selected/current state appears through `BorderThickness`.
- Placeholder text is centered over the thumbnail box.

Risks:

- Selection state is too subtle or visually dependent on one border property.
- Loading and failure states are not visually distinct enough.
- Item spacing and density are local rather than tokenized.

Required Phase 5 work:

- Add current, hover, focus, loading, failed, and keyboard states.
- Use density and thumbnail sizing tokens.
- Preserve large-folder behavior and thumbnail loading performance.

### Grid layout

Files:

- `src/ImageViewer.App/Views/GridLayout.xaml`
- `src/ImageViewer.App/Views/GridLayout.xaml.cs`

Current state:

- XAML is minimal: canvas host on themed background.
- Most visual composition is created in code-behind.
- No visible grid-level state, mode badge, active cell style, loading state, or transition is declared in XAML.

Risks:

- Layout can feel mechanically placed rather than designed.
- Harder to validate visual states because visual elements are produced programmatically.
- Mode transition needs a clear motion plan.

Required Phase 5 work:

- Introduce grid cell style resources or a focused cell component if needed.
- Add selected/current/focus state.
- Add grid entry/exit transition evidence.

### Filmstrip layout

Files:

- `src/ImageViewer.App/Views/FilmstripView.xaml`
- `src/ImageViewer.App/Views/FilmstripView.xaml.cs`

Current state:

- Primary image region plus bottom thumbnail panel.
- Thumbnail row repeats visual patterns from `ThumbnailStrip` with different dimensions.
- Focus image uses transform but no explicit transition resources.

Risks:

- Duplicate thumbnail styling will drift.
- Focus image transition and selection state are not strongly designed.

Required Phase 5 work:

- Share thumbnail visual language with thumbnail strip.
- Add filmstrip-specific focus/active treatment.
- Verify keyboard and pointer selection evidence.

### Comparison mode

Files:

- `src/ImageViewer.App/Views/ComparisonView.xaml`
- `src/ImageViewer.App/Views/ComparisonView.xaml.cs`

Current state:

- Image host is dynamic.
- Overlay opacity slider sits in a simple bottom-centered border.
- Active image, sync zoom, overlay layer, and opacity states need stronger visual feedback.

Risks:

- Comparison controls do not yet look like a professional image-review tool.
- Overlay slider lacks label/context.
- State may rely on user memory rather than visible structure.

Required Phase 5 work:

- Add comparison mode HUD pattern.
- Add active image and sync-state indicator.
- Improve overlay controls without occluding image inspection.

### Animated GIF frame controls

Files:

- `src/ImageViewer.App/Views/FrameControl.xaml`
- `src/ImageViewer.App/Views/FrameControl.xaml.cs`

Current state:

- Compact control with previous, play/pause, next, frame text, and slider.
- Uses default button visuals and local dimensions.
- Frame text has min width but no complete text style.

Risks:

- Controls are functional but not integrated with the rest of the shell.
- Slider and button states need focus/keyboard/accessibility review.

Required Phase 5 work:

- Tokenize media control dimensions.
- Add motion/state review for play/pause, paused frame, frame stepping.
- Improve accessibility naming and evidence.

### Drop indicator

Files:

- `src/ImageViewer.App/Views/DropIndicator.xaml`

Current state:

- Uses hard-coded scrim `#66000000`.
- Center panel has large text and local padding/radius.
- No icon, supported-file feedback, or invalid drop state.

Risks:

- Looks like a basic overlay.
- Does not provide differentiated feedback for accepted versus rejected files.

Required Phase 5 work:

- Introduce drop overlay state styles.
- Add accepted/rejected/pending state if backend supports it.
- Keep visual treatment restrained.

### Loading and error states

Files:

- `src/ImageViewer.App/MainWindow.xaml`
- `src/ImageViewer.App/MainWindow.xaml.cs`

Current state:

- Loading overlay uses a centered `ProgressRing` and cancel button.
- Error state uses `InfoBar` at top with basic severity.
- Warning dismissal and loading delay are code-driven.

Risks:

- Loading is functional but generic.
- Error/warning UI is not integrated with app chrome.
- Recoverability and next-step affordances are limited.

Required Phase 5 work:

- Create status HUD or polished inline status surface.
- Improve copy and recovery affordances without hiding errors.
- Preserve structured logging and support bundle behavior.

## Current motion audit

Observed motion hooks:

- `SearchOverlay.xaml` uses `EntranceThemeTransition`.
- `ThumbnailStrip.xaml` uses `EntranceThemeTransition`.
- `MetadataPanel.xaml` has opacity and translation transform controlled by code.
- `DropIndicator.xaml` uses opacity.
- `FadingChrome.xaml.cs` controls chrome fade/show behavior.
- `FrameControl` and layout surfaces do not define a coherent motion system.

Gaps:

- No named duration/easing resources.
- No reduced-motion strategy.
- No unified overlay choreography.
- No documented mode-transition strategy.
- No explicit motion evidence.

Required Phase 5 work:

- Add `MotionTokens.xaml`.
- Define motion categories.
- Implement transition behavior in a way that does not block image navigation.
- Capture normal-motion and reduced-motion evidence.

## Backend QOL audit

Current relevant services:

- `SettingsService`
- `RecentFilesService`
- `FolderPlaylistService`
- `ThumbnailService`
- `MetadataService`
- `SearchService`
- `ErrorLogService`
- `NavigationService`
- `ImageDecodeService`

Current QOL strengths:

- Folder-as-playlist behavior exists.
- Recent files exist.
- Settings persistence exists.
- Search exists.
- Thumbnail loading exists.
- Structured errors exist.

Candidate QOL improvements for Phase 5:

1. Session continuity
   - Restore last opened image or folder.
   - Remember last active mode only when safe.
   - Safer recent-file recovery when files disappear.

2. Responsiveness
   - Bounded neighbor preload around current playlist index.
   - Smarter thumbnail queue prioritizing visible/current-nearby items.
   - Progress/status reporting for long playlist scans or metadata work.

3. Error recovery
   - Continue to next valid image after recoverable decode/navigation failure.
   - Improve missing-file recovery from recent files.
   - Add user-visible recovery actions while preserving logs.

Restrictions:

- No catalog database.
- No tagging.
- No albums.
- No cloud sync.
- No telemetry.
- No disabling features to make performance appear better.

## Baseline screenshot evidence

Screenshot evidence folder:

```text
TestResults/phase5-evidence/visual/before/
```

Manifest:

```text
TestResults/phase5-evidence/visual/before/before-screenshot-manifest.json
```

Existing baseline screenshot files:

- `01-empty-state.png`
- `02-main-image-chrome.png`
- `03-command-palette.png`
- `04-search-overlay.png`
- `05-metadata-panel.png`
- `06-thumbnail-strip.png`
- `07-grid-layout.png`
- `08-filmstrip-layout.png`
- `09-comparison-side-by-side.png`
- `10-comparison-overlay.png`
- `11-gif-frame-controls.png`
- `12-error-state-corrupt-file.png`
- `13-warning-state-settings-load.png`
- `14-loading-state-large-bmp.png`
- `15-drop-indicator-accepted-file.png`
- `stable-contact-sheet.png`

Validated current baseline screenshots:

- `01-empty-state.png`
- `02-main-image-chrome.png`
- `03-command-palette.png`
- `04-search-overlay.png`
- `05-metadata-panel.png`
- `06-thumbnail-strip.png`
- `07-grid-layout.png`
- `08-filmstrip-layout.png`
- `09-comparison-side-by-side.png`
- `10-comparison-overlay.png`
- `11-gif-frame-controls.png`
- `12-error-state-corrupt-file.png`
- `13-warning-state-settings-load.png`
- `14-loading-state-large-bmp.png`
- `15-drop-indicator-accepted-file.png`

Validation notes:

- The accepted screenshots above were captured from the current source Release executable at `src/ImageViewer.App/bin/x64/Release/net8.0-windows10.0.19041.0/ImageViewer.App.exe`.
- Latest full manifest: `before-screenshot-manifest.json`, `createdAtUtc` `2026-05-09T01:46:24.6453468Z`, 15 captured, 0 failed.
- Capture window: 1600 x 1200 at `(120, 20)`.
- Theme: current system dark rendering from clean local settings.
- `01-empty-state.png` shows the current empty viewer.
- `02-main-image-chrome.png` shows the loaded baseline image, top chrome, and bottom information bar.
- `03-command-palette.png` shows the command palette opened from the running app.
- `04-search-overlay.png` shows the search overlay with query, result count, and filename results. Capturing this required fixing the existing search-visibility integration path so `MainViewModel.ShowSearch()` requests `SearchOverlay.Show()` through `MainWindow`.
- `05-metadata-panel.png` shows the metadata panel with file, image, and camera sections.
- `06-thumbnail-strip.png` shows the thumbnail strip with loaded thumbnails, filenames, current-item treatment, and bottom information bar.
- `07-grid-layout.png`, `08-filmstrip-layout.png`, `09-comparison-side-by-side.png`, `10-comparison-overlay.png`, and `11-gif-frame-controls.png` show running-app layout, comparison, and animated GIF control surfaces.
- `12-error-state-corrupt-file.png` shows the decode error InfoBar from a corrupt PNG fixture.
- `13-warning-state-settings-load.png` was captured from the running source Release executable through window-content capture. Scenario: invalid `%LOCALAPPDATA%/spitfire.img/settings.json` with settings backup and restore. It shows the warning InfoBar message `Unable to load settings. Default preferences were restored.`
- `14-loading-state-large-bmp.png` shows the loading overlay with progress ring and cancel button. Scenario: large BMP startup file with `SPITFIRE_EVIDENCE_IMAGE_DECODE_DELAY_MS=5000` set only for the capture process.
- `15-drop-indicator-accepted-file.png` shows the actual `DropIndicator` control in accepted-file mode. Scenario: running app with `SPITFIRE_EVIDENCE_SHOW_DROP_INDICATOR=accepted` set only for the capture process. This is visual-state evidence for the audit, not drag/drop behavior proof.
- The capture script now backs up and restores local app settings, recent files, and clipboard state.
- Command-palette-dependent captures use UI Automation `ValuePattern.SetValue` against the palette `SearchInput`.
- `stable-contact-sheet.png` is an inspection aid and is not itself a required surface.

Required screenshot set:

- Empty state.
- Main image loaded.
- Top chrome and bottom info bar.
- Command palette.
- Search overlay.
- Metadata panel.
- Thumbnail strip.
- Grid layout.
- Filmstrip layout.
- Side-by-side comparison.
- Overlay comparison.
- GIF frame controls.
- Drop indicator.
- Loading state.
- Warning/error state.

Each accepted screenshot is backed by `before-evidence-validation.md`, which records source executable, capture method, input file or scenario, visible state, latest manifest timestamp, theme, and window size.
- Whether the screenshot represents a current limitation or a passing baseline.

## Audit conclusion

The current UI is serviceable and consistent enough to support Phase 5, but it is not yet a professional design system. The highest-leverage Phase 5 order is:

1. Source register and visual audit.
2. Token system.
3. Shared text/panel/button/motion resources.
4. Chrome shell redesign.
5. Overlay and panel redesign.
6. Layout and media-control redesign.
7. Backend QOL continuity, responsiveness, and error recovery.
8. Evidence-driven visual, motion, accessibility, QOL, and final scope checkpoints.
