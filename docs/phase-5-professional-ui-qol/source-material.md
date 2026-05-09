# Phase 5 Source Material Register

## Purpose

This register records the professional source material allowed to guide Phase 5 UI and QOL work. It is not a mood board. Each source must have a concrete reason for use, a license or usage status, an implementation target, and evidence requirements.

Phase 5 implementation must not rely on improvised styling, blank gradients, glow-heavy filler, decorative orbs, or generic design trends. Source material informs the work; local app behavior, tests, screenshots, and evidence prove the work.

## Source rules

- Use WinUI-native controls, Microsoft Fluent guidance, and source-controlled app assets before adding a dependency.
- Do not hotlink runtime visual assets.
- Do not copy sample code blindly; adapt only patterns that fit the app architecture.
- Do not treat a design-system phrase as completion evidence.
- Record every external visual asset, animation asset, icon pack, or UI library before implementation.
- Keep generated screenshots and review evidence under `TestResults/phase5-evidence/`.

## Registered sources

| ID | Source | URL | Usage status | Phase 5 use | Implementation targets | Evidence requirement |
| --- | --- | --- | --- | --- | --- | --- |
| S1 | Fluent 2 motion guidance | https://fluent2.microsoft.design/motion | Microsoft design guidance; use as reference, not copied asset | Motion principles, duration/easing discipline, choreography, reduced-motion expectations | `MotionTokens.xaml`, overlay transitions, chrome reveal/hide, layout transitions, reduced-motion review | `TestResults/phase5-evidence/motion/motion-review.md` and reduced-motion notes |
| S2 | Fluent 2 typography guidance | https://fluent2.microsoft.design/typography | Microsoft design guidance; use as reference | Segoe UI Variable direction, type hierarchy, sentence case, contrast expectations | `TextStyles.xaml`, command palette text, metadata hierarchy, info bar text, empty state copy | Visual screenshots and accessibility review |
| S3 | Fluent 2 color guidance | https://fluent2.microsoft.design/color | Microsoft design guidance; use as reference | Neutral grounding, semantic color use, restrained accent use, state color roles | `DarkTheme.xaml`, `LightTheme.xaml`, semantic brushes, warning/error/success/focus resources | Token validation and visual review |
| S4 | Windows app XAML animation overview | https://learn.microsoft.com/en-us/windows/apps/develop/motion/xaml-animation | Microsoft Learn documentation; use APIs and design guidance | Theme transitions, theme animations, visual transitions, content/edge/popup/reposition animation categories | `MotionTokens.xaml`, `ViewerMotionService`, XAML transitions on overlays and lists | Motion evidence showing transitions do not block inspection |
| S5 | Windows content transition animation guidelines | https://learn.microsoft.com/en-us/windows/apps/design/motion/content-transition-animations | Microsoft Learn documentation; use guidance and APIs | Content replacement, entrance, fade, and guidance against misusing content transitions | Search results, command palette results, metadata updates, loading-to-content states | Before/after motion review |
| S6 | Windows connected animation guidance | https://learn.microsoft.com/en-us/windows/apps/design/motion/connected-animation | Microsoft Learn documentation; use APIs only if local UX supports continuity | Potential continuity between thumbnail/grid/filmstrip item and focused image | Grid to single image, filmstrip focus, thumbnail strip to canvas | Manual evidence if implemented; otherwise record decision not to use |
| S7 | Windows app accessibility development guidance | https://learn.microsoft.com/en-us/windows/apps/develop/accessibility | Microsoft Learn documentation; use as acceptance baseline | Programmatic access, keyboard navigation, color/contrast review | Automation names, keyboard traversal, focus indicators, contrast review | `TestResults/phase5-evidence/accessibility/accessibility-review.md` |
| S8 | Windows controls and patterns | https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/ | Microsoft Learn documentation; use as WinUI control pattern reference | Control selection, standard WinUI surfaces, InfoBar/menus/list controls, input expectations | Command palette, InfoBar/error surfaces, menus, panels, ListView styling | Source reference in visual audit and implementation notes |
| S9 | Windows typography guidance | https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography | Microsoft Learn documentation; use as Windows-specific typography reference | Windows type ramp, hierarchy, legibility, font roles | `TextStyles.xaml`, metadata panel, command palette, status HUD, bottom info bar | Visual review and token validation |
| S10 | Windows App SDK documentation | https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/ | Microsoft Learn documentation; use APIs compatible with app platform | WinUI 3, system backdrop, XAML resources, app-window/platform behavior | `MainWindow.xaml`, app resources, backdrop/material implementation if selected | Build/test and manual screenshot evidence |
| S11 | WinUI Gallery source | https://github.com/microsoft/WinUI-Gallery | MIT-licensed Microsoft sample repository; use as reference, not vendored code unless explicitly reviewed | Concrete WinUI control examples and Fluent implementation patterns | Control styling decisions, InfoBar, command/list patterns, sample comparison during audit | Record exact files/patterns if code is adapted |

## Current source-derived design constraints

### Motion

Fluent 2 frames motion as functional, natural, consistent, and appealing. Phase 5 maps that to:

- Motion must explain a state change or preserve spatial context.
- Small overlays should enter and leave quickly.
- Large top-level mode changes should avoid disorienting slide motion.
- Linear motion is allowed for truly constant-rate cases such as looping progress or rotation, not general UI transitions.
- Reduced-motion behavior is required; motion cannot be the only state communication.

Implementation targets:

- `src/ImageViewer.App/Resources/Styles/MotionTokens.xaml`
- `src/ImageViewer.App/Services/Motion/IViewerMotionService.cs`
- `src/ImageViewer.App/Services/Motion/ViewerMotionService.cs`
- `src/ImageViewer.App/Views/FadingChrome.xaml`
- `src/ImageViewer.App/Views/CommandPalette.xaml`
- `src/ImageViewer.App/Views/SearchOverlay.xaml`
- `src/ImageViewer.App/Views/MetadataPanel.xaml`
- `src/ImageViewer.App/Views/GridLayout.xaml`
- `src/ImageViewer.App/Views/FilmstripView.xaml`
- `src/ImageViewer.App/Views/ComparisonView.xaml`

### Typography

The app should use a coherent Windows type system instead of arbitrary local font sizes. Phase 5 maps Fluent and Windows typography guidance to:

- Segoe UI Variable as the default Windows-facing family when explicitly needed.
- Type roles: caption, metadata, body, body strong, panel title, command title, command shortcut, status, numeric.
- Sentence case by default.
- No all-caps emphasis for normal app UI.
- Metadata and status text must be scannable and should not compete with the image.

Implementation targets:

- `src/ImageViewer.App/Resources/Styles/TextStyles.xaml`
- `BottomInfoBar.xaml`
- `MetadataPanel.xaml`
- `CommandPalette.xaml`
- `SearchOverlay.xaml`
- `FrameControl.xaml`

### Color and material

The app is an image viewer. The canvas must stay neutral, and chrome must remain readable over varied image content.

Phase 5 token direction:

- Neutral canvas and layered neutral surfaces.
- Accent color reserved for focus, selection, active state, and targeted commands.
- Semantic warning/error/success/info brushes.
- Distinct border/focus rings for light and dark themes.
- No single-hue wash.
- No blank gradient or glow-based substitute for layout.

Implementation targets:

- `DarkTheme.xaml`
- `LightTheme.xaml`
- `PanelStyles.xaml`
- `ButtonStyles.xaml`
- `LayoutTokens.xaml`

### Accessibility

Phase 5 maps Windows accessibility guidance to:

- Programmatic names for new or materially changed controls.
- Keyboard operation for every touched interactive surface.
- Contrast review for text, icons, focus, selected, warning, and error states.
- State communication through shape, text, icon, and structure, not color alone.
- Reduced-motion review.

Implementation targets:

- All Phase 5-touched views.
- `tests/ImageViewer.App.Tests` static resource/automation checks where practical.
- `TestResults/phase5-evidence/accessibility/accessibility-review.md`.

## Library and dependency policy

No third-party UI package is approved by this register yet.

Before adding a package, implementation must record:

- Package name and version.
- License.
- Maintenance signal.
- Why WinUI-native controls or source-controlled assets are insufficient.
- Bundle/runtime impact.
- Which exact task requires it.

Allowed without further dependency approval:

- WinUI and Windows App SDK APIs already in the app platform.
- Source-controlled XAML resources.
- Source-controlled image/icon assets created for this repository.
- Microsoft Learn and WinUI Gallery patterns used as reference.

## Asset policy

Runtime remote assets are disallowed. Any visual asset used by Phase 5 must be:

- Created in the repository,
- checked into an approved source asset folder,
- generated deterministically from a source-controlled asset, or
- documented as an external asset with license status before use.

## Verification

Task 1 verification commands:

```powershell
rg -n "https?://" src docs -g "*.xaml" -g "*.cs" -g "*.md"
rg -n "http://|https://" src\ImageViewer.App src\ImageViewer.Core
git diff --check -- docs\phase-5-professional-ui-qol
```

Task 1 completion requires:

- This file exists.
- The required source set is registered.
- No runtime source file introduces remote hotlinked visual assets.
- The Phase 5 task list remains unchecked except tasks verified as fully complete.
