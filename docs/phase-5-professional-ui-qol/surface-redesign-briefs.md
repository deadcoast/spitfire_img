# Phase 5 Surface Redesign Briefs

## Purpose

This document translates the Phase 5 audit into concrete design briefs for each visible surface. It defines what each surface must become, what it must preserve, and what evidence proves it.

The briefs are implementation targets. They are not completion claims.

## Global visual direction

The viewer must feel like a native Windows image-inspection tool:

- Dense, calm, and image-first.
- Neutral canvas and controlled surface hierarchy.
- Accent used for state, selection, and focus rather than broad decoration.
- Semantic warning/error/success treatment with text and structure.
- No blank gradients, glow-heavy filler, decorative orbs, bokeh, or one-note hue wash.
- No nested cards as a substitute for hierarchy.

## Main image canvas and empty state

Files:

- `src/ImageViewer.App/Views/ImageCanvas.xaml`

Design target:

- Keep the loaded image dominant.
- Replace the single-line empty state with a restrained first-use surface.
- Add drop/open affordance and supported-format hint without a marketing hero.
- Provide a status layer that can host loading, no-image, missing-file, and recoverable failure context.

Required states:

- Empty.
- Loaded image.
- Loading delayed state.
- Recoverable error context.
- Keyboard focus where actions exist.
- Drag-over accepted/rejected feedback if integrated with drop state.

Evidence:

- Empty state screenshot.
- Loaded image screenshot.
- Loading screenshot.
- Error/recovery screenshot.

## Top chrome and bottom information bar

Files:

- `src/ImageViewer.App/Views/FadingChrome.xaml`
- `src/ImageViewer.App/Views/FadingChrome.xaml.cs`
- `src/ImageViewer.App/Views/BottomInfoBar.xaml`

Design target:

- Turn top and bottom chrome into one coherent app shell.
- Make idle/active states readable over varied image content.
- Keep menus discoverable without dominating the canvas.
- Strengthen filename, count, dimensions, folder, comparison sync, and unsaved states.

Required states:

- Idle.
- Active.
- Hover.
- Keyboard focused.
- Disabled command where applicable.
- Unsaved badge.
- Long filename/folder.
- Comparison sync indicator.

Preservation:

- Existing menu commands.
- Phase 1 through Phase 4 shortcuts.
- Chrome fade behavior.
- Folder-as-playlist state display.

Evidence:

- Active chrome screenshot.
- Idle chrome screenshot.
- Long filename screenshot.
- Keyboard-only focus evidence.

## Command palette

Files:

- `src/ImageViewer.App/Views/CommandPalette.xaml`
- `src/ImageViewer.App/Views/CommandPalette.xaml.cs`
- `src/ImageViewer.Core/ViewModels/CommandPaletteState.cs`

Design target:

- Treat command palette as a professional power-user surface.
- Add type hierarchy for command title, group, shortcut, description, and recent entries.
- Add selected, hover, focus, disabled/unavailable, no-results, and loading-ready visual states.
- Keep command filtering and registry behavior intact.

Required states:

- Open with recent commands.
- Filtered results.
- No results.
- Selected row.
- Keyboard traversal.
- Long command title/shortcut.

Preservation:

- Existing command registry.
- Existing shortcuts.
- Search debounce semantics unless tests justify changing them.

Evidence:

- Before/after screenshot.
- Keyboard traversal notes.
- Search/no-results screenshot.

## Search overlay

Files:

- `src/ImageViewer.App/Views/SearchOverlay.xaml`
- `src/ImageViewer.App/Views/SearchOverlay.xaml.cs`
- `src/ImageViewer.Core/ViewModels/SearchState.cs`

Design target:

- Unify visual treatment with command palette where appropriate.
- Replace hard-coded scrim with overlay tokens.
- Show result count, selected result, filename, and folder context when useful.
- Make empty state informative without clutter.

Required states:

- Empty query.
- Matching results.
- No matching results.
- Selected result.
- Keyboard next/previous result.
- Dismissal.

Preservation:

- Existing playlist filtering.
- Existing selected-result navigation.
- Existing search shortcuts.

Evidence:

- Search active screenshot.
- No-results screenshot.
- Keyboard evidence.

## Metadata panel

Files:

- `src/ImageViewer.App/Views/MetadataPanel.xaml`
- `src/ImageViewer.App/Views/MetadataPanel.xaml.cs`

Design target:

- Replace flat text blocks with structured metadata sections.
- Add labels, values, hierarchy, wrapping/trimming rules, and copy-friendly layout where appropriate.
- Keep panel dense and readable.

Required states:

- Basic metadata.
- EXIF metadata present.
- Missing metadata.
- Long paths/values.
- Keyboard focus.

Preservation:

- Existing metadata extraction and display behavior.
- Panel toggle shortcut.

Evidence:

- Metadata panel screenshot.
- Long-value screenshot.
- Keyboard/focus notes.

## Thumbnail strip

Files:

- `src/ImageViewer.App/Views/ThumbnailStrip.xaml`
- `src/ImageViewer.App/Views/ThumbnailStrip.xaml.cs`
- `src/ImageViewer.App/Views/ThumbnailLoadPlanner.cs`

Design target:

- Create a shared thumbnail visual language for strip and filmstrip.
- Make current, hover, focus, loading, and failure states distinct.
- Use density tokens and fixed dimensions so thumbnails do not shift layout.

Required states:

- Current thumbnail.
- Hover.
- Keyboard focus.
- Loading placeholder.
- Failed thumbnail.
- Large playlist.
- Top and bottom positions.

Preservation:

- Existing visible-first thumbnail load planning.
- Existing thumbnail cache behavior.
- Existing top/bottom position setting.

Evidence:

- Normal strip screenshot.
- Large playlist screenshot.
- Failure placeholder evidence.

## Grid layout

Files:

- `src/ImageViewer.App/Views/GridLayout.xaml`
- `src/ImageViewer.App/Views/GridLayout.xaml.cs`
- `src/ImageViewer.Core/Services/LayoutService.cs`

Design target:

- Make grid mode feel designed while preserving image density.
- Add current/active image state, focus, index context, and layout-mode status.
- Avoid unbounded animation or expensive visual work for large playlists.

Required states:

- 2x2 through 6x6 density.
- Active image.
- Keyboard focus/selection.
- Loading or missing image.
- Page boundary.

Preservation:

- Existing density bounds.
- Existing paging and clamping behavior.
- Existing layout shortcuts.

Evidence:

- Each density representative screenshot.
- Large playlist behavior notes.

## Filmstrip layout

Files:

- `src/ImageViewer.App/Views/FilmstripView.xaml`
- `src/ImageViewer.App/Views/FilmstripView.xaml.cs`

Design target:

- Share thumbnail styling with thumbnail strip.
- Strengthen focused image state without drawing attention away from the image.
- Keep thumbnail row stable and dense.

Required states:

- Focused image.
- Current thumbnail.
- Failed thumbnail.
- Large playlist.
- Keyboard focus.

Preservation:

- Existing filmstrip focus behavior.
- Existing thumbnail count settings.
- Existing shortcuts.

Evidence:

- Filmstrip screenshot.
- 100-plus image playlist notes where feasible.

## Comparison mode

Files:

- `src/ImageViewer.App/Views/ComparisonView.xaml`
- `src/ImageViewer.App/Views/ComparisonView.xaml.cs`
- `src/ImageViewer.Core/ViewModels/ComparisonState.cs`

Design target:

- Make side-by-side and overlay modes feel like image review tools.
- Add active image, sync zoom, displayed indices, and overlay opacity state.
- Keep controls compact and nonblocking.

Required states:

- Side-by-side.
- Overlay.
- Active left/right image.
- Sync zoom on/off.
- Overlay opacity adjustment.
- Missing comparison target.

Preservation:

- Existing comparison shortcuts.
- Existing sync zoom behavior.
- Existing overlay opacity behavior.

Evidence:

- Side-by-side screenshot.
- Overlay screenshot.
- Keyboard/shortcut evidence.

## GIF frame controls

Files:

- `src/ImageViewer.App/Views/FrameControl.xaml`
- `src/ImageViewer.App/Views/FrameControl.xaml.cs`

Design target:

- Bring GIF controls into the same tool-button and HUD style system.
- Improve frame number, play/pause, previous/next, slider, focus, and disabled states.
- Ensure the controls remain compact.

Required states:

- Playing.
- Paused.
- First frame.
- Last frame.
- Slider focused.
- 100-frame GIF.

Preservation:

- Existing frame stepping.
- Existing play/pause.
- Existing animation shortcuts.

Evidence:

- GIF controls screenshot.
- 100-frame GIF screenshot.

## Drop indicator

Files:

- `src/ImageViewer.App/Views/DropIndicator.xaml`
- `src/ImageViewer.App/Views/DropIndicator.xaml.cs`
- `src/ImageViewer.App/MainWindow.xaml.cs`

Design target:

- Replace hard-coded scrim and generic panel with accepted/rejected drag feedback.
- Use icon/text/semantic surface, not color alone.
- Preserve current validation behavior for supported image file extensions.

Required states:

- Pending drag.
- Accepted drop.
- Rejected drop.
- Drop complete or dismissed.

Evidence:

- Accepted drag screenshot.
- Rejected drag screenshot.

## Loading, warning, and error states

Files:

- `src/ImageViewer.App/MainWindow.xaml`
- `src/ImageViewer.App/MainWindow.xaml.cs`
- `src/ImageViewer.Core/Models/ViewerError.cs`

Design target:

- Replace generic status presentation with coherent status surfaces.
- Keep InfoBar semantics or equivalent accessible severity behavior.
- Provide recovery actions where recovery exists.
- Keep warning/error readable long enough and avoid repeated flashing.

Required states:

- Loading with cancel.
- Warning.
- Error.
- Recoverable error with action.
- Nonrecoverable error.

Preservation:

- Structured logging.
- Support bundle redaction behavior.
- Warning auto-dismiss behavior only if still appropriate and accessible.

Evidence:

- Loading screenshot.
- Warning screenshot.
- Error screenshot.
- Recovery behavior notes.

## Surface completion rule

A surface is not complete until it has:

- Tokenized styling.
- Required visual states.
- Keyboard/focus behavior verified.
- Light and dark theme review where applicable.
- Before/after screenshot evidence.
- No feature removal or simplification used as a shortcut.
