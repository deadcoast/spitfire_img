# Phase 5 Professional UI And User QOL Development Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a professional, source-backed WinUI visual system and additive backend user-QOL improvements for `spitfire.img` without removing or narrowing existing behavior.

**Architecture:** UI work stays in `ImageViewer.App` XAML resources, views, and WinUI-specific services. Backend QOL work stays in `ImageViewer.Core` services and models, with app wiring in `ImageViewer.App` only where platform behavior is required. Evidence is written to `TestResults/phase5-evidence/`.

**Tech Stack:** C#/.NET 8, WinUI 3, Windows App SDK, XAML resource dictionaries, xUnit, PowerShell evidence scripts, UI Automation/manual screenshot evidence.

---

## File map

### Documentation

- `docs/phase-5-professional-ui-qol/source-material.md`
  - Source register for design references, libraries, assets, and implementation targets.
- `docs/phase-5-professional-ui-qol/visual-audit.md`
  - Current UI audit, surface gaps, screenshot requirements, and baseline notes.
- `docs/phase-5-professional-ui-qol/phase5-evidence-template.md`
  - Template for visual, motion, accessibility, QOL, build, test, and known-issue evidence.
- `docs/phase-5-professional-ui-qol/development-plan.md`
  - This implementation plan.
- `docs/phase-5-professional-ui-qol/tasks.md`
  - Required checklist. Mark only fully verified tasks complete.

### XAML resources

- `src/ImageViewer.App/App.xaml`
  - Add new resource dictionaries.
- `src/ImageViewer.App/Resources/ThemeTokens/DarkTheme.xaml`
  - Dark theme color/material tokens.
- `src/ImageViewer.App/Resources/ThemeTokens/LightTheme.xaml`
  - Light theme color/material tokens.
- `src/ImageViewer.App/Resources/Styles/TextStyles.xaml`
  - Phase 5 type ramp and text roles.
- `src/ImageViewer.App/Resources/Styles/LayoutTokens.xaml`
  - Spacing, sizing, density, radius, border, opacity, icon size tokens.
- `src/ImageViewer.App/Resources/Styles/MotionTokens.xaml`
  - Durations, easing names, transition constants.
- `src/ImageViewer.App/Resources/Styles/PanelStyles.xaml`
  - Chrome, overlay, pane, HUD, thumbnail, and status panel styles.
- `src/ImageViewer.App/Resources/Styles/ButtonStyles.xaml`
  - Icon buttons, command rows, media controls, toolbar buttons.

### App views

- `src/ImageViewer.App/Views/ImageCanvas.xaml`
  - Empty state, status layer, image transition host.
- `src/ImageViewer.App/Views/FadingChrome.xaml`
  - Chrome shell. Split if necessary.
- `src/ImageViewer.App/Views/BottomInfoBar.xaml`
  - Tokenized info bar and metadata hierarchy.
- `src/ImageViewer.App/Views/CommandPalette.xaml`
  - Professional command surface.
- `src/ImageViewer.App/Views/SearchOverlay.xaml`
  - Search panel, result row, empty state, transitions.
- `src/ImageViewer.App/Views/MetadataPanel.xaml`
  - Structured metadata surface.
- `src/ImageViewer.App/Views/ThumbnailStrip.xaml`
  - Current/hover/focus/loading/failure thumbnail states.
- `src/ImageViewer.App/Views/GridLayout.xaml`
  - Grid mode host and cell visual strategy.
- `src/ImageViewer.App/Views/FilmstripView.xaml`
  - Filmstrip mode and shared thumbnail styling.
- `src/ImageViewer.App/Views/ComparisonView.xaml`
  - Comparison HUD and active/sync/opacity states.
- `src/ImageViewer.App/Views/FrameControl.xaml`
  - GIF media control styling and accessibility.
- `src/ImageViewer.App/Views/DropIndicator.xaml`
  - Accepted/rejected drag state and overlay styling.
- `src/ImageViewer.App/MainWindow.xaml`
  - Loading, status, warning, error shell integration.

### App services

- `src/ImageViewer.App/Services/Motion/IViewerMotionService.cs`
  - Interface for code-triggered transitions and reduced-motion decisions.
- `src/ImageViewer.App/Services/Motion/ViewerMotionService.cs`
  - WinUI implementation.
- `src/ImageViewer.App/Services/Motion/ReducedMotionService.cs`
  - Platform/app setting resolver if needed.

### Core services and models

- `src/ImageViewer.Core/Models/UserExperienceSettings.cs`
  - Additive QOL settings if separate from `ViewerSettings`.
- `src/ImageViewer.Core/Models/OperationStatus.cs`
  - User-visible operation status model if selected.
- `src/ImageViewer.Core/Services/SessionRestoreService.cs`
  - Last session/folder/image restore candidate.
- `src/ImageViewer.Core/Services/ImagePreloadService.cs`
  - Bounded neighbor preload candidate.
- `src/ImageViewer.Core/Services/OperationStatusService.cs`
  - Progress/status candidate.

The exact QOL files depend on the selected QOL feature set, but each selected QOL feature must have tests and evidence.

### Tests

- `tests/ImageViewer.App.Tests/Phase5ResourceTests.cs`
  - Resource dictionary required-key validation.
- `tests/ImageViewer.App.Tests/Phase5DocumentationTests.cs`
  - Source register and evidence template validation.
- `tests/ImageViewer.Tests/Services/SessionRestoreServiceTests.cs`
  - If session continuity is selected.
- `tests/ImageViewer.Tests/Services/ImagePreloadServiceTests.cs`
  - If preload/cache is selected.
- `tests/ImageViewer.Tests/Services/OperationStatusServiceTests.cs`
  - If status/progress is selected.
- Existing view model/service tests updated only when behavior changes.

## Implementation sequence

### Task A: Source and audit foundation

- [ ] Create or update `source-material.md`.
- [ ] Run:

```powershell
rg -n "http://|https://" src\ImageViewer.App src\ImageViewer.Core
```

Expected: no runtime hotlinked visual assets.

- [ ] Create or update `visual-audit.md`.
- [ ] Capture before screenshots for all required surfaces.
- [ ] Do not mark task 2 complete until screenshot evidence exists.

### Task B: Token system

- [ ] Add `TextStyles.xaml`, `LayoutTokens.xaml`, and `MotionTokens.xaml`.
- [ ] Merge them in `App.xaml`.
- [ ] Expand light and dark theme dictionaries.
- [ ] Write failing resource tests for required keys.
- [ ] Implement token keys.
- [ ] Run:

```powershell
dotnet test tests\ImageViewer.App.Tests\ImageViewer.App.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~Phase5ResourceTests"
```

Expected: all Phase 5 resource tests pass.

### Task C: Shared component styles

- [ ] Redesign `PanelStyles.xaml` around chrome, overlay, pane, HUD, thumbnail, and status roles.
- [ ] Redesign `ButtonStyles.xaml` around icon tool, command row, media control, and toolbar roles.
- [ ] Add text roles in `TextStyles.xaml`.
- [ ] Replace duplicated local values in the first touched surface.
- [ ] Build after every resource integration.

### Task D: Chrome shell

- [ ] Refactor `FadingChrome.xaml` only as far as required for clarity.
- [ ] Tokenize top chrome, bottom info, unsaved badge, frame-control host, and overlay margins.
- [ ] Preserve all existing events and bindings.
- [ ] Add focus/hover/active/idle evidence.
- [ ] Run affected app tests and full build.

### Task E: Overlay and panel surfaces

- [ ] Redesign command palette.
- [ ] Redesign search overlay.
- [ ] Redesign metadata panel.
- [ ] Redesign drop indicator.
- [ ] Add entrance/exit transitions after token system exists.
- [ ] Verify keyboard traversal and dismissal.

### Task F: Viewing mode surfaces

- [ ] Redesign thumbnail strip.
- [ ] Redesign grid visual strategy.
- [ ] Redesign filmstrip.
- [ ] Redesign comparison HUD.
- [ ] Redesign GIF frame controls.
- [ ] Verify large playlist, 4K, and 100-frame GIF scenarios still work.

### Task G: Motion system

- [ ] Implement named motion resources.
- [ ] Implement `IViewerMotionService` only if XAML-only transitions are insufficient.
- [ ] Add reduced-motion behavior.
- [ ] Validate:
  - normal motion,
  - reduced motion,
  - keyboard responsiveness during transitions,
  - no image inspection obstruction.

### Task H: Backend QOL continuity

- [ ] Select exactly one continuity feature for the first implementation slice.
- [ ] Recommended first slice: local last-session restore with safe missing-file fallback.
- [ ] Write failing tests for missing/partial/invalid settings and missing last file.
- [ ] Implement additively.
- [ ] Verify existing recent-file and folder-as-playlist behavior remains intact.

### Task I: Backend QOL responsiveness

- [ ] Select exactly one responsiveness feature for the first implementation slice.
- [ ] Recommended first slice: bounded neighbor preload or smarter thumbnail prioritization.
- [ ] Write tests for bounds, cancellation, and ordering.
- [ ] Add before/after evidence using a large playlist.

### Task J: Backend QOL error recovery

- [ ] Select exactly one recoverable error path.
- [ ] Recommended first slice: failed navigation target keeps playlist active and offers next valid image path.
- [ ] Preserve structured logging.
- [ ] Add tests for the modified path.

### Task K: Evidence and gate

- [ ] Create `phase5-evidence-template.md`.
- [ ] Capture visual after screenshots.
- [ ] Complete motion, reduced-motion, accessibility, QOL, and final scope evidence.
- [ ] Run:

```powershell
Stop-Process -Name ImageViewer.App -Force -ErrorAction SilentlyContinue
dotnet build SpitfireImg.sln -p:Platform=x64
dotnet test SpitfireImg.sln -p:Platform=x64
```

Expected:

- Build succeeds with `0 Error(s)`.
- Tests pass with `0` failures.
- Evidence exists under `TestResults/phase5-evidence/`.

## Non-negotiable completion rules

- Do not mark a task complete until all bullets under that task are verified.
- Do not claim visual completion without before/after screenshots.
- Do not claim motion completion without normal and reduced-motion evidence.
- Do not claim QOL completion without tests and before/after behavior evidence.
- Do not remove, disable, hide, or narrow features to satisfy QOL or polish.
- Do not introduce cloud sync, accounts, telemetry, cataloging, albums, tagging, social features, or new editing workflows.

## Recommended first implementation slice

The first implementation slice after planning should be:

1. Resource/token validation tests.
2. Token dictionaries.
3. Shared text/panel/button styles.
4. Chrome shell redesign.

Reason: every later visual surface depends on the token and shared-style foundation. Starting with surface-by-surface styling before tokens would create more churn and weaker consistency.
