# Phase 5 Motion System Plan

## Purpose

This document defines how Phase 5 motion should behave before implementation. Motion must explain changes, preserve spatial context, and keep image inspection fast. It must not act as decoration or disguise incomplete layout.

## Source basis

- Fluent 2 motion guidance: motion should be functional, natural, consistent, and responsive.
- Microsoft Learn XAML animation guidance: prefer platform animation and transition primitives when they fit.
- Microsoft Learn content transition guidance: use content transitions for content replacement and entrance, not arbitrary movement.
- Microsoft Learn connected animation guidance: use connected animation only when the source and destination are clearly the same object.
- Windows accessibility guidance: motion must not be the only state signal and must have reduced-motion behavior.

## Motion token dictionary

`MotionTokens.xaml` must define named values for:

### Duration

- `ViewerMotionInstantDuration`: `0`
- `ViewerMotionFastDuration`: short feedback such as hover, focus, badge reveal.
- `ViewerMotionNormalDuration`: overlay entrance, command palette/search/metadata panel.
- `ViewerMotionDeliberateDuration`: large layout mode transitions where continuity matters.
- `ViewerMotionExitDuration`: exit should generally be equal to or shorter than entrance.
- `ViewerMotionReducedDuration`: zero or near-zero reduced-motion substitution.

### Easing

- `ViewerMotionEaseOut`: entrance and reveal.
- `ViewerMotionEaseIn`: exit and dismiss.
- `ViewerMotionEaseInOut`: reposition and mode continuity.
- `ViewerMotionLinear`: constant-rate progress only.

### Distance and scale

- `ViewerMotionOverlayEntranceOffset`
- `ViewerMotionChromeRevealOffset`
- `ViewerMotionPanelSlideOffset`
- `ViewerMotionSubtleScaleFrom`
- `ViewerMotionSubtleScaleTo`

## Motion categories

### Chrome reveal and idle

Surfaces:

- `FadingChrome.xaml`
- `BottomInfoBar.xaml`
- `FrameControl.xaml`

Behavior:

- Active chrome appears quickly and feels immediately responsive.
- Idle chrome fades to a readable but non-dominant state.
- Keyboard focus forces active chrome.
- Pointer movement and command invocation must not be delayed by animation.
- Reduced motion swaps movement for opacity or instant state change.

### Overlay entrance and exit

Surfaces:

- `CommandPalette.xaml`
- `SearchOverlay.xaml`
- `MetadataPanel.xaml`
- `DropIndicator.xaml`
- `MainWindow.xaml` loading and error surfaces

Behavior:

- Command palette and search share a coherent entrance pattern.
- Metadata panel can slide from the edge because it is spatially anchored.
- Drop indicator may fade and slightly scale only if accepted/rejected state remains readable.
- Error and warning surfaces must not flash rapidly.
- Reduced motion makes overlays appear without travel.

### Image navigation

Surfaces:

- `ImageCanvas.xaml`
- `ThumbnailStrip.xaml`
- `GridLayout.xaml`
- `FilmstripView.xaml`

Behavior:

- Navigation should remain inspection-first.
- Crossfade is allowed only if it does not delay next/previous input.
- Connected animation from thumbnail/grid/filmstrip to canvas is allowed only when it can be stable and performant.
- Large folders must not animate every item when only one current item changes.

### Layout mode changes

Surfaces:

- Single image
- Grid
- Filmstrip
- Comparison side-by-side
- Comparison overlay

Behavior:

- Use transition to clarify where the user went.
- Do not animate large item collections with unbounded per-item work.
- Preserve current image context across mode switches.
- Use short status or mode feedback only when useful.

### Feedback and status

Surfaces:

- Loading overlay
- Status HUD
- Warning and error InfoBar/status surface
- Unsaved badge
- GIF play/pause state

Behavior:

- Progress animation is acceptable when operation duration justifies it.
- Warning and error state should be stable enough to read.
- Unsaved badge changes should be visible but not attention-seeking.
- GIF controls should communicate play/pause through icon and text/accessibility, not animation alone.

## Reduced-motion requirements

Phase 5 must provide one of these, in order of preference:

1. Platform-respecting reduced-motion detection if available through WinUI/Windows APIs.
2. A local app setting that disables nonessential transitions.
3. A focused motion service that can be overridden by tests and user settings.

Reduced motion does not mean visual state disappears. It means:

- No travel animation for overlays.
- No scale animation.
- No long crossfades.
- Focus, selected, warning, and error states remain visible through structure, text, icon, and stroke.

## Implementation ownership

Use XAML transitions when they are enough:

- `EntranceThemeTransition`
- `ContentThemeTransition`
- `RepositionThemeTransition`
- `PopupThemeTransition`
- Storyboards for local opacity/translation where already used

Use `ViewerMotionService` only when:

- Multiple controls must coordinate.
- Reduced-motion decisions must be centralized.
- Code-behind currently duplicates animation timing.
- Tests need to validate motion decisions independent of WinUI rendering.

Proposed files:

```text
src/ImageViewer.App/Services/Motion/IViewerMotionService.cs
src/ImageViewer.App/Services/Motion/ViewerMotionService.cs
src/ImageViewer.App/Services/Motion/ReducedMotionService.cs
```

## Surface-specific motion decisions

| Surface | Initial Phase 5 motion | Reduced-motion substitute | Evidence |
| --- | --- | --- | --- |
| Top chrome | Opacity plus slight vertical reveal | Instant or opacity only | `motion/chrome-reveal.md` |
| Bottom info bar | Opacity synchronized with chrome | Instant or opacity only | `motion/chrome-reveal.md` |
| Command palette | Fade plus short downward/scale settle | Instant open, visible focus | `motion/overlays.md` |
| Search overlay | Fade plus short downward settle | Instant open, visible focus | `motion/overlays.md` |
| Metadata panel | Edge slide plus fade | Instant open | `motion/metadata-panel.md` |
| Thumbnail strip | Reposition current item only | Selection stroke change | `motion/thumbnails.md` |
| Grid | Current cell emphasis; avoid unbounded item animation | Selection stroke change | `motion/layout-modes.md` |
| Filmstrip | Focus image continuity; thumbnail selection | Selection stroke change | `motion/layout-modes.md` |
| Comparison | HUD fade and active pane state | Instant state change | `motion/comparison.md` |
| GIF controls | Play/pause state update; no decorative looping | State update only | `motion/gif-controls.md` |
| Loading | Indeterminate progress only after delay | Static loading state if needed | `motion/status.md` |
| Warning/error | Stable entrance; no repeated flashing | Static severity surface | `motion/status.md` |
| Drop indicator | Fade, accepted/rejected state change | Static state surface | `motion/drop-indicator.md` |

## Validation

Motion work is complete only when evidence records:

- Normal-motion screen recording or screenshot sequence for each touched surface.
- Reduced-motion behavior for each broad category.
- Keyboard responsiveness during command palette/search/metadata transitions.
- No transition blocks next/previous image navigation.
- No state is communicated only by motion.

Automated tests should cover service decisions, reduced-motion configuration, and state routing. Visual timing itself can remain manual evidence unless a stable UI automation harness exists.
