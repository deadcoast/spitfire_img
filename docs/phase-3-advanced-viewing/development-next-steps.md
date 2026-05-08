# Phase 3 development next steps

Date: 2026-05-08

## Current required status

No unchecked required tasks remain in the Phase 1, Phase 2, or Phase 3 task lists.

Phase 3 required tasks `1` through `35` are checked in `docs/phase-3-advanced-viewing/tasks.md`. The remaining unchecked Phase 3 task entries are starred optional property or integration tests, and they are not part of the current required scope.

## Verification evidence

Manual Phase 3 evidence is stored under `TestResults/phase3-evidence/`.

Primary evidence file:

```text
TestResults/phase3-evidence/phase3-manual-verification-2026-05-08.md
```

That evidence records:

- 1000-image playlist coverage.
- 3840 x 2160 image coverage.
- 100-frame animated GIF coverage.
- UI smoke evidence for 4K image display, command palette fuzzy search, comparison mode, grid layout, filmstrip layout, GIF frame controls, and command palette polish.
- Performance evidence for specified slideshow transition durations, command palette search under 50ms, and comparison visual response under 600ms.
- x64 build and test gate results: 355 total tests passed.

## Source and docs reconciliation

The Phase 3 checklist and design were reconciled with source behavior after manual validation:

- `Ctrl+F` remains search.
- `Ctrl+Shift+F` is filmstrip layout.
- Slideshow transition performance now matches the actual Phase 3 specification: Fade 300ms, Slide 400ms, Zoom 350ms.
- Command palette row recycling was fixed so filtered result titles, highlighted characters, and shortcut text update with the current data context.
- The missing `SubtleBorderBrush` theme token was added to both dark and light theme dictionaries so the WinUI shell launches successfully.

## Remaining non-required items

The unchecked starred tasks in `tasks.md` are optional property and integration tests. They remain available as hardening work, but they are explicitly outside the current required task scope.

Notable optional areas:

- Additional slideshow timing, looping, reset, and transition property tests.
- Additional comparison layout, sync zoom, and overlay property tests.
- Additional command palette property and integration tests.
- Additional GIF, codec, grid, filmstrip, and advanced-mode slideshow tests.

## Next required development step

There is no Phase 4 requirements, design, or task file in this repository yet. Do not start Phase 4 implementation from implied scope.

The next required development step is to create the Phase 4 specification set before code changes:

```text
docs/phase-4-*/requirements.md
docs/phase-4-*/design.md
docs/phase-4-*/tasks.md
```

After those documents exist, continue from the first unchecked required task in the new Phase 4 `tasks.md`.
