# Viewer repair

## Review

The user's 2026-10-19 review rejects the current application. Earlier phase checkmarks and source-string tests do not establish usable behavior.

Confirmed defects:

| Failure | Evidence | Repair |
|---|---|---|
| Chrome jumps during movement | Rendered baseline: repeated `ShowChrome` moves top surface from Y=12 to Y=4 | One visibility transition per state change; no spatial animation |
| Idle bars never disappear | Rendered baseline: opacity remains 0.72, with additional child opacity | Fade to zero; remove hidden controls from pointer hit testing |
| Oversized, unhelpful status panels | Rendered baseline; labeled chips duplicate ordinary metadata | Compact edge-aligned toolbar and single-line status |
| Features absent from visible menus | FadingChrome exposes only basic viewing and theme | Expose existing browse, layout, comparison, slideshow, transform and export actions |
| No empty-state action | ImageCanvas contains text only | Open button connected to the native picker |
| GIF/TIFF missing from picker | Picker hardcodes five formats | Use the same format catalog as decoding |
| GIF frames repeatedly fade | Frame loader calls image navigation animation | Replace frames without opacity animation |
| Zoom/pan incomplete | No wheel handler; panning requires custom zoom >100 | Pixel-aware viewport, wheel zoom, bounded pan at actual size |
| Overlay interruptions race | New storyboards leave old completion callbacks active | Cancel replaced animations and stale completions |
| Thumbnails obscure status and rebuild on navigation | Bottom margins overlap; refresh clears every item | Separate geometry and update selection without reloading the folder |

## Design

The image owns the viewport. Controls occupy stationary, compact bands at its edges, and fade completely during viewing. Menus expose the existing workflows; secondary features do not become permanent dashboard panels. Native WinUI controls retain platform keyboard and focus behavior. Empty state stays usable.

Existing phase requirements remain the product contract. This repair does not add cataloging, cloud services or new editing scope.

## Plan

1. Preserve failing rendered baseline and introduce opt-in rendered-app verification.
2. Repair chrome timing, interruption and hit testing; simplify shell geometry.
3. Connect visible actions to existing view-model services and native picker flows.
4. Repair image display, GIF updates, zoom/pan and overlapping thumbnail geometry.
5. Run service regressions, build, and real XAML rendering checks across empty, loaded, idle, menus, layouts, search, metadata, GIF and narrow-window states.
6. Relaunch the ordinary build for review; record exact verification limits.

## Evidence boundaries

Baseline lives under ignored `TestResults/viewer-repair-20261019/before/`. The verification build uses the real application, WinUI rendering and image services with isolated settings. Its fixtures test rendering and behavior; they do not establish visual quality on the user's image collection.

Desktop input automation is currently unavailable: the Computer Use native pipe failed after recovery. Programmatic invocation and rendered captures do not substitute for physical pointer, keyboard, drag/drop or multi-monitor acceptance.

## Development

In progress. No repaired workflow is accepted solely because a build or source-string test passes.
