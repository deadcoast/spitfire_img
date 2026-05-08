# Codex Onboarding

This document is the handoff guide for the next Codex instance working on `spitfire.img`. Follow it before changing code. The goal is continuity: preserve the spec-driven workflow, understand the source/docs relationship, and avoid false completion claims.

## Operating Workflow

Work from local evidence, not assumptions.

1. Read `AGENTS.md` first, then the relevant phase docs under `docs/`.
2. Identify the active phase from `docs/roadmap.md` and the phase tasklist.
3. Cross-reference requirements, design, and tasks before implementation.
4. For code changes, use test-first development where practical: add or update a focused test, watch it fail for the expected reason, implement the smallest fix, then rerun it.
5. Keep the tasklist current. Do not check a task unless the implementation and validation evidence exist.
6. Before claiming completion, run fresh verification and read the output.
7. Commit and push completed work when requested or when finishing a defined phase/checkpoint.

Use short progress updates while working. If a bug appears during manual validation, stop treating the task as complete, write a regression test where possible, fix it, and revalidate the original behavior.

## Verification Rules

For code changes, the standard final gate is:

```powershell
Stop-Process -Name ImageViewer.App -Force -ErrorAction SilentlyContinue
dotnet build SpitfireImg.sln -p:Platform=x64
dotnet test SpitfireImg.sln -p:Platform=x64
```

The app executable can lock build outputs. Always stop `ImageViewer.App` before build/test. Do not report success from an old run.

For UI work, add manual evidence under ignored `TestResults/`, usually `TestResults/phaseN-evidence/`. Screenshots and fixture images are acceptable because `TestResults/` is ignored. Manual validation still needs automated tests for service/model regressions where feasible.

## Documentation Map

The docs are the source of truth for scope.

- `docs/roadmap.md`: phase order and deferred scope.
- `docs/phase-1-mvp-viewer/requirements.md`: Phase 1 user-visible behavior.
- `docs/phase-1-mvp-viewer/design.md`: Phase 1 architecture and service boundaries.
- `docs/phase-1-mvp-viewer/tasks.md`: Phase 1 implementation checklist.
- `docs/phase-2-browse-qol/requirements.md`: Phase 2 acceptance criteria.
- `docs/phase-2-browse-qol/design.md`: Phase 2 architecture, UI components, services, and acceptance criteria.
- `docs/phase-2-browse-qol/tasks.md`: Phase 2 task tracking; currently complete.
- `docs/phase-3-advanced-viewing/requirements.md`: next-phase scope.
- `docs/phase-3-advanced-viewing/design.md`: planned Phase 3 architecture.
- `docs/phase-3-advanced-viewing/tasks.md`: Phase 3 execution checklist.
- `docs/phase-4-release-readiness/requirements.md`: release readiness scope.
- `docs/phase-4-release-readiness/design.md`: release artifact, distribution, support, and evidence architecture.
- `docs/phase-4-release-readiness/tasks.md`: Phase 4 execution checklist.

For any phase, read the three files together:

```text
requirements.md -> what must be true
design.md       -> how the repo intends to satisfy it
tasks.md        -> implementation order and completion tracking
```

Do not introduce out-of-phase features unless the docs are updated with rationale.

## Product Direction

`spitfire.img` is a Windows-native, presentation-first image viewer. Core principles:

- Fast viewing, not editing/cataloging/cloud sync.
- Folder-as-playlist behavior: opening one image builds navigation from its parent folder.
- Minimal chrome that fades, with explicit UI when users interact.
- WinUI 3 and Windows App SDK are the primary app platform.
- Phase 2 added search, thumbnails, recent files, drag/drop, export, transforms, metadata, GIF, and TIFF.
- GIF/TIFF are Phase 2. GIF animation, multi-page TIFF navigation, HEIC, AVIF, slideshow, comparison, and command palette belong to later docs unless explicitly moved.

## Source Structure

The solution is split into app-specific WinUI code and platform-neutral core logic.

```text
src/ImageViewer.Core/
  Models/       Result types, image metadata, playlist items, settings, errors
  Services/     Core service interfaces and cross-platform/default implementations
  ViewModels/   MainViewModel and state objects used by the app UI

src/ImageViewer.App/
  App.xaml(.cs)       DI setup, startup argument parsing
  MainWindow.xaml(.cs) Window shell, shortcuts, picker dialogs, drag/drop
  Services/           Windows-specific imaging, metadata, thumbnail implementations
  Views/              WinUI controls: canvas, chrome, search, thumbnails, metadata, drop indicator
  Resources/          Theme tokens and control styles

tests/ImageViewer.Tests/
  Core model/service/viewmodel tests, property tests

tests/ImageViewer.App.Tests/
  Windows imaging and WinUI-adjacent integration tests
```

Core should not depend on WinUI. App services may use Windows APIs, WIC, WinUI streams, and ImageSharp where required.

## Important Code Relationships

`App.xaml.cs` wires dependency injection and parses startup file arguments. `MainWindow.xaml.cs` owns UI routing: keyboard shortcuts, file pickers, drag/drop, fullscreen, error display, and dialog presentation.

`MainViewModel` coordinates the workflows. It owns `ViewerState`, `PlaylistState`, `SearchState`, `ThumbnailState`, `TransformState`, `MetadataState`, and `RecentFiles`. It calls services and exposes operations used by the WinUI controls.

`ImageDecodeService` and `ImageFormatCatalog` centralize supported extensions. Keep format detection there; do not scatter extension lists through UI code. Phase 2 supported formats are PNG, JPG/JPEG, BMP, WEBP, GIF, TIFF/TIF.

`WindowsImageProcessingService` implements export and transform persistence. `WindowsMetadataService` extracts EXIF and includes an ImageSharp GPS fallback because WIC did not reliably surface decimal GPS fields during validation. `WindowsThumbnailService` creates WinUI-compatible thumbnails.

`ErrorLogService` writes structured logs under `%LOCALAPPDATA%\spitfire.img\logs`. User-facing messages are defined by `ViewerError`; keep them calm and non-technical.

## Testing Strategy

Use the right test layer:

- Model tests for computed properties and formatting.
- Service tests for file validation, playlist scanning, format detection, search, recent files, settings, errors, thumbnails, transforms, metadata, and export.
- ViewModel tests for state transitions and command coordination.
- App integration tests for Windows-specific image handling, EXIF preservation, GPS extraction, startup argument parsing, and thumbnail planning.
- Manual tests for WinUI behavior, dialogs, animations, focus, drag/drop visuals, shortcut conflicts, and fullscreen.

Existing full verification currently passes `179/179` tests when run with the x64 platform flag.

## Manual Validation Pattern

Create temporary fixtures under `TestResults/`, launch the built app, and capture evidence. Useful launch pattern:

```powershell
Stop-Process -Name ImageViewer.App -Force -ErrorAction SilentlyContinue
dotnet build SpitfireImg.sln -p:Platform=x64
$exe = Resolve-Path src\ImageViewer.App\bin\x64\Debug\net8.0-windows10.0.19041.0\ImageViewer.App.exe
$image = Resolve-Path TestResults\phase2-fixtures\phase2-alpha.png
Start-Process -FilePath $exe.Path -ArgumentList @($image.Path) -WorkingDirectory (Split-Path $exe.Path)
```

If you alter `%LOCALAPPDATA%\spitfire.img\settings.json` for deterministic UI validation, restore defaults before handoff unless the test is specifically about persistence.

## Git And Workspace Rules

Check status before editing. The worktree may include user changes or local tool artifacts. Do not revert unrelated changes. `.serena/` has appeared as an untracked local tool directory; leave it alone unless the user asks.

Use focused commits with imperative messages. Do not amend unless explicitly requested. Push only after verification for completed work.

Common commands:

```powershell
git status --short
git status --branch --short
git log --oneline --decorate -8
git add <changed files>
git commit -m "Implement focused change"
git push
```

## Handoff Discipline

A completion report must include the actual verification command output summary, not confidence language. State what passed, what was manually validated, what was committed/pushed, and any remaining risks or untracked files. If any required check was not run, say that directly.

Never say a phase is complete because code “looks done.” A phase is complete only when its tasklist is checked, acceptance criteria have evidence, build/test verification is fresh, and the branch has been synchronized if pushing is part of the task.
