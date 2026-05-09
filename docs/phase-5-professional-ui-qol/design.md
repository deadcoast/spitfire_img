# Design Document: Phase 5 Professional UI And User QOL

## Overview

Phase 5 upgrades `spitfire.img` from a functional Windows image viewer into a professionally designed, evidence-validated desktop application. It has two and only two workstreams:

1. A professional UI system for visual quality, sourced materials, coherent layout, state styling, transitions, and animation.
2. Backend user quality-of-life improvements that make existing workflows more convenient, responsive, continuous, and recoverable without removing or simplifying behavior.

The phase is intentionally strict. A polished UI is not a new gradient, a glow, or a decorative overlay. A QOL improvement is not deleting a feature, shrinking a workflow, or hiding a failure. The work must be source-backed, measurable, and visible in the app.

## Current baseline

The current app is a WinUI 3 / Windows App SDK application with:

- Presentation code under `src/ImageViewer.App/`.
- Platform-neutral workflow and services under `src/ImageViewer.Core/`.
- Theme dictionaries under `src/ImageViewer.App/Resources/ThemeTokens/`.
- Shared styles under `src/ImageViewer.App/Resources/Styles/`.
- Viewer surfaces under `src/ImageViewer.App/Views/`.
- Phase 4 release evidence under `TestResults/phase4-evidence/`.

The existing visual baseline is intentionally simple:

- `DarkTheme.xaml` and `LightTheme.xaml` define a small brush set.
- `PanelStyles.xaml` defines a single chrome panel border style.
- `FadingChrome.xaml` owns top menu chrome, unsaved badge, frame controls, and bottom info bar.
- Each overlay/view has local styling rather than a full design system.

Phase 5 should treat that baseline as functional, not finished.

## Source-backed design standards

Phase 5 design decisions must be grounded in source material and local validation.

Required source material:

- Fluent 2 design system for motion, typography, color, density, and interaction quality.
- Microsoft Learn Windows app motion guidance for connected animation and XAML animation patterns.
- WinUI Gallery for concrete WinUI control and sample patterns.
- Windows App SDK documentation for WinUI, system backdrop, composition, app-window, and XAML platform behavior.

Create:

```text
docs/phase-5-professional-ui-qol/source-material.md
```

The source-material register should record:

- Source name.
- URL or local path.
- License or usage status.
- What is being borrowed or referenced.
- Why it fits `spitfire.img`.
- Where it is implemented.
- Verification evidence path.

The register is mandatory. If Phase 5 uses a visual idea, asset, sample, library, animation package, or nontrivial control pattern, the source must be recorded.

## Architecture

```text
docs/phase-5-professional-ui-qol/
  requirements.md              Phase 5 user-visible and maintainer requirements
  design.md                    UI/QOL architecture and implementation direction
  tasks.md                     Required implementation checklist
  source-material.md           Source register created during implementation
  visual-audit.md              Current UI audit and final visual review
  design-token-plan.md         Token dictionary, naming, validation, and migration plan
  motion-system-plan.md        Motion categories, reduced-motion plan, and evidence targets
  surface-redesign-briefs.md   Per-surface redesign briefs and state requirements
  backend-qol-plan.md          Continuity, responsiveness, and error-recovery QOL plan
  phase5-evidence-template.md  Evidence template for visual, motion, accessibility, QOL, gate

src/ImageViewer.App/
  Resources/
    ThemeTokens/
      DarkTheme.xaml           Theme-specific token values
      LightTheme.xaml          Theme-specific token values
    Styles/
      ButtonStyles.xaml        Command, icon, panel, and tool button styles
      PanelStyles.xaml         Chrome, overlay, popover, and HUD panel styles
      TextStyles.xaml          Phase 5 type ramp and text roles
      MotionTokens.xaml        Duration, easing, transition keys
      LayoutTokens.xaml        Spacing, radius, density, sizing resources
  Services/
    Motion/
      IViewerMotionService.cs  Motion orchestration abstraction where code-behind needs coordination
      ViewerMotionService.cs   WinUI implementation for transitions and reduced motion
  Views/
    AppChrome.xaml             Extracted shell chrome if FadingChrome becomes too large
    StatusHud.xaml             Transient status/progress/error surface if the final UI design needs it
    ...existing views...

src/ImageViewer.Core/
  Models/
    UserExperienceSettings.cs  Additive local QOL settings if needed
  Services/
    SessionRestoreService.cs   Local continuity service if selected for QOL implementation
    ImagePreloadService.cs     Bounded preload/cache coordinator if selected for QOL implementation
    OperationStatusService.cs  Operation progress/status model if selected for QOL implementation

TestResults/
  phase5-evidence/
    visual/
    motion/
    accessibility/
    qol/
    phase5-gate/
```

The exact files should be finalized during implementation tasks. The design goal is clear boundaries:

- XAML resource dictionaries own reusable visual values.
- Views own layout composition and control templates.
- App services own WinUI-specific motion or platform behavior.
- Core services own platform-neutral QOL behavior.
- Tests verify service behavior, settings compatibility, token availability, and input/mode regressions.

## Visual design system

Phase 5 should introduce a coherent Professional_UI_System rather than isolated style edits.

The detailed token plan is maintained in:

```text
docs/phase-5-professional-ui-qol/design-token-plan.md
```

### Token groups

Required token groups:

- Color: canvas, surface, elevated surface, toolbar, command, selection, focus, warning, error, success, border, divider, text, muted text, accent, image-safe overlay.
- Typography: title, panel heading, body, caption, metadata, command, status, numeric, monospace/path text where useful.
- Spacing: compact desktop spacing, panel padding, command row padding, thumbnail gaps, overlay margins, metadata row spacing.
- Radius: tool button, panel, thumbnail, focus ring, status HUD.
- Border and elevation: restrained strokes, panel separation, focused/selected states.
- Opacity: idle chrome, active chrome, disabled state, muted metadata, overlay dim.
- Motion: instant, fast, normal, deliberate, entrance, exit, reposition, image transition, reduced-motion substitutions.
- Density: compact, normal, presentation mode control density.

Hard-coded styling in Phase 5-touched surfaces should move into named resources unless a value is truly local and non-reusable.

### Palette direction

The app should feel like a professional imaging tool:

- Image canvas stays neutral and inspection-friendly.
- Chrome uses layered neutrals with enough contrast to be readable over varied images.
- Accent color is used for selection, focus, and active command state, not as a broad wash.
- Warning and error colors are semantic and distinct.
- Light and dark themes must both be designed, not mirror-inverted afterthoughts.

Prohibited as completion strategies:

- Blank gradient backgrounds.
- Overuse of glow effects.
- Decorative orbs, bokeh, floating blobs, and stock decorative filler.
- One-note palettes dominated by a single hue family.
- Styling that hides weak layout with blur, glow, or opacity.

## Component and surface redesign

Phase 5 should review every primary surface:

- Main viewer canvas and empty state.
- Top chrome/menu surface.
- Bottom info bar.
- Unsaved-change and transient status badges.
- Command palette.
- Search overlay.
- Metadata panel.
- Thumbnail strip.
- Grid layout.
- Filmstrip layout.
- Comparison side-by-side and overlay modes.
- GIF frame controls.
- Drop indicator.
- Loading, cancel, warning, and error states.

Each surface must have:

- Idle and active appearance.
- Keyboard focus appearance.
- Pointer hover/pressed appearance where interactive.
- Loading/progress behavior when applicable.
- Error or empty behavior where applicable.
- Screenshot evidence.

The design should favor dense, purposeful desktop utility. This is not a landing page, marketing page, or illustrative dashboard.

The detailed per-surface redesign briefs are maintained in:

```text
docs/phase-5-professional-ui-qol/surface-redesign-briefs.md
```

## Motion system

Motion should clarify state changes. It should never make image inspection harder.

The detailed motion plan is maintained in:

```text
docs/phase-5-professional-ui-qol/motion-system-plan.md
```

Motion categories:

- Chrome reveal/hide: short opacity and translation transition that makes controls feel responsive.
- Overlay entrance/exit: command palette, search, metadata, and status HUD should use consistent entrance/exit choreography.
- Image navigation: restrained crossfade or connected image continuity if it preserves inspection speed.
- Layout mode changes: grid, filmstrip, comparison, and single-image transitions should preserve spatial context.
- Repositioning: thumbnails, grid cells, comparison cells, and metadata changes may use reposition transitions when performance allows.
- Feedback: save/export/status/error should use short, semantic, non-decorative motion.

Implementation guidance:

- Prefer WinUI and Windows App SDK primitives before adding dependencies.
- Use connected animation where source and destination UI elements are clearly shared.
- Use entrance, reposition, opacity, scale, and translation transitions deliberately.
- Avoid unbounded animations on large item collections.
- Respect platform accessibility and add an app setting if platform detection is insufficient.
- Record reduced-motion behavior in manual evidence.

## Backend QOL design

Backend QOL work must be additive and user-centered. It should improve existing workflows without becoming a catalog, account system, cloud system, or telemetry system.

The detailed backend QOL plan is maintained in:

```text
docs/phase-5-professional-ui-qol/backend-qol-plan.md
```

Candidate QOL areas:

1. Session continuity
   - Restore last opened image or last folder on startup.
   - Remember layout mode per local folder.
   - Preserve local zoom/layout preference safely.
   - Keep behavior local and settings-backed.

2. Responsiveness
   - Bounded image or metadata preloading near the current playlist index.
   - Smarter thumbnail scheduling and cancellation.
   - Progress state for long playlist scans or metadata batches.
   - Avoid blocking UI during large-folder work.

3. Error recovery
   - Continue navigation after corrupt or missing image failures where possible.
   - Offer next valid image behavior without hiding the error.
   - Make codec-missing flows clearer and recoverable.
   - Preserve structured logs and support-bundle redaction.

4. Command and workflow continuity
   - Improve recent command quality without telemetry.
   - Make recent files safer when files moved or disappeared.
   - Add local-only convenience state, such as remembering the last export folder.

Implementation must choose a concrete subset during tasks and verify it. "QOL" is open to interpretation, but completion is not open-ended: each selected QOL item must have acceptance criteria, tests, evidence, and no feature removal.

## Accessibility and quality checks

Phase 5 should include:

- Contrast review for all Phase 5 surfaces.
- Keyboard-only navigation review.
- Focus-state review.
- Text clipping/overflow review at small and normal desktop window sizes.
- Display scale review when feasible.
- Reduced-motion review.
- Screen-reader/automation-name review for new or heavily changed controls.

Evidence belongs under:

```text
TestResults/phase5-evidence/accessibility/
```

## Testing strategy

Use automated tests for:

- Required token keys in light/dark dictionaries.
- Settings migrations and QOL continuity behavior.
- Preload/cache/cancellation behavior if implemented.
- Error recovery paths.
- Input routing and mode conflict behavior changed by Phase 5.
- Source-material register existence and required sections.

Use visual/manual evidence for:

- Primary surface screenshots before and after.
- Motion behavior and reduced-motion behavior.
- Keyboard traversal and focus states.
- Contrast/readability notes.
- Large playlist and QOL responsiveness before/after comparison.

Use the final Phase 5 gate:

```powershell
Stop-Process -Name ImageViewer.App -Force -ErrorAction SilentlyContinue
dotnet build SpitfireImg.sln -p:Platform=x64
dotnet test SpitfireImg.sln -p:Platform=x64
```

Additional release-artifact verification may be run, but Phase 5 is not a packaging phase.

## Evidence model

The reusable evidence template is maintained in:

```text
docs/phase-5-professional-ui-qol/phase5-evidence-template.md
```

```text
TestResults/phase5-evidence/
  visual/
    before/
    after/
    visual-review.md
  motion/
    motion-review.md
    reduced-motion-review.md
  accessibility/
    accessibility-review.md
  qol/
    before-after-qol.md
    perf-or-behavior-measurements.json
  phase5-gate/
    build-x64.log
    test-x64.log
    final-checkpoint.md
```

Evidence must be specific. A screenshot or checklist must identify the app state, viewport/window size, theme, file/scenario, and result.

## Acceptance criteria mapping

- Requirement 1 maps to visual audit, token system, surface redesign, and screenshot evidence.
- Requirement 2 maps to `source-material.md`, asset discipline, and dependency review.
- Requirement 3 maps to XAML resource dictionaries and token validation tests.
- Requirement 4 maps to chrome, overlay, panel, and state redesign.
- Requirement 5 maps to motion resources, motion service, transition implementation, and reduced-motion evidence.
- Requirement 6 maps to input routing, focus states, overlay behavior, and shortcut regression tests.
- Requirement 7 maps to contrast, automation names, keyboard traversal, and accessibility evidence.
- Requirement 8 maps to local settings, session continuity, and migration tests.
- Requirement 9 maps to preload/cache/progress/cancellation services and before/after evidence.
- Requirement 10 maps to recoverable error flows and structured logging tests.
- Requirement 11 maps to the Phase 5 gate and `TestResults/phase5-evidence/`.
- Requirement 12 maps to final scope review before Phase 5 closure.
