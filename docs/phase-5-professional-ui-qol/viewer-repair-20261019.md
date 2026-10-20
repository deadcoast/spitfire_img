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
| Grid and filmstrip use the wrong viewport | Rendered inspection showed clipped grid cells and filmstrip thumbnails across the image | Calculate cells from the live canvas; give filmstrip focus and thumbnails separate rows |
| Slideshow choices do not change playback | Three rendered transition checks failed; preference updated but live state did not | Update playback state and wire the transition renderer to actual outgoing/incoming image layers |
| Rotate/flip preview differs from saved image | Rendered pixel comparison failed, mean channel difference 31.37 | Compose flips in operation order |
| Saving transforms leaves stale pixels and dimensions | Rendered pixel comparison failed, mean channel difference 49.70 | Reload the saved image, invalidate caches, prevent concurrent edits, preserve newer navigation |

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

The repair replaces the moving card-style chrome with a 41-DIP toolbar and one-line status band. Idle controls fade fully and stop intercepting pointer input. Menus expose the shared command registry, including layouts, comparison, search, metadata, transforms, export, animation and slideshow controls. The empty window offers a native Open image action.

Single view and filmstrip share the image viewport. Actual size accounts for display scaling, wheel zoom anchors at the pointer, pan clamps to image bounds, and GIF frames swap without navigation fades. Replaced animations cancel their obsolete completion callbacks. Thumbnail navigation retains existing items; hidden strips can finish pending preview loads.

Saving transforms reloads the actual saved pixels and invalidates thumbnail, metadata and slideshow caches. Reopening files with equal metadata still notifies the renderer. A save cannot overwrite newer navigation state, and overlapping save/edit requests are guarded. Existing overwrite confirmation remains.

## Reproducing verification

From the repository root on Windows with the existing .NET and Windows App SDK prerequisites:

```powershell
dotnet test SpitfireImg.sln -p:Platform=x64 -c Release
.\scripts\verify-viewer.ps1
```

The rendered runner is opt-in at compile time with `ViewerUiTest=true`; ordinary builds exclude it. The script creates its own fixture images and settings directory, launches the actual WinUI application, writes captures and `results.json`, then rebuilds the ordinary executable even if verification fails. It stops only the matching executable from this checkout.

The fixtures cover landscape, portrait, panorama, square, 4K, GIF and corrupt inputs. The transform check writes only generated scratch images and compares rendered preview, post-save and reopened pixels. This tests the real rendering/save pipeline for those fixtures, not acceptance on the user's collection.

## Verification record

- Full Release regression suite: **350 core tests and 93 Windows app/integration tests passed**, zero failures or skips. Final log: `TestResults/phase5-evidence/viewer-repair-20261019/final-regression.log`.
- Rendered baseline: idle opacity 0.72 and repeated activity moving the toolbar by 8 DIP. Preserved in `TestResults/viewer-repair-20261019/before/`.
- Transform baseline: both rendered preview/save checks failed. Preserved in `TestResults/viewer-repair-20261019/transform-baseline/`.
- Final rendered verification: **39 of 39 checks passed**. Evidence: `TestResults/phase5-evidence/viewer-repair-20261019/final-rendered-2/`. Captures were visually inspected; the final empty-state button uses the readable native neutral style.
- Rotate/flip preview-to-reopened mean channel difference fell from 31.37 to approximately 0.00073 on the generated calibration image; post-save-to-reopened difference was below 0.000001. This establishes pixel continuity for that operation sequence and fixture.
- Ordinary Release application rebuilt successfully after the test host, with zero build errors. Normal builds exclude the verification runner.
- Design document lint: zero errors and warnings. Static design audit: zero findings; its native-XAML coverage is limited and does not establish runtime quality.
- Existing ImageSharp 3.1.12 dependency audit warnings remain, including high-severity advisories. This repair does not claim release security qualification.

Still unverified: physical pointer and keyboard operation, native Open/Save dialogs and overwrite confirmation, drag/drop, screen readers/high contrast, mixed-DPI monitor transitions, codec-dependent HEIC/AVIF on this machine, and the user's real image collection. Desktop automation failed at its native connection; these checks were not replaced with claims based on programmatic button invocation.

Phase 5 acceptance remains open. Earlier checked implementation tasks do not supersede the user's rejected review.
