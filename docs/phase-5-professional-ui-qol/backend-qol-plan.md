# Phase 5 Backend QOL Plan

## Purpose

This document defines backend user quality-of-life work for Phase 5. QOL means the app becomes easier, safer, faster, or more recoverable for the user without removing, hiding, disabling, narrowing, or simplifying existing behavior.

Phase 5 backend QOL is local-only. It must not introduce accounts, cloud sync, telemetry, cataloging, albums, tagging, social features, or new editing workflows.

## Current backend baseline

Current relevant services and state:

- `SettingsService`: local settings persistence under the app data folder.
- `RecentFilesService`: local recent-file list.
- `FolderPlaylistService`: automatic folder-as-playlist behavior.
- `NavigationService`: playlist navigation.
- `ThumbnailService`: bounded thumbnail cache.
- `ThumbnailLoadPlanner`: visible-first thumbnail load ordering.
- `MetadataService`: EXIF metadata extraction.
- `SearchService`: cached playlist search index.
- `ErrorLogService`: structured error logging.
- `MainViewModel`: app workflow orchestration and user-facing error state.

Current strengths:

- Folder-as-playlist already exists.
- Recent files already exist.
- Settings normalization exists.
- Search and thumbnail services already have service boundaries.
- Structured errors include warning/error severity.

Current gaps:

- Returning to the last working session is not a first-class flow.
- Long-running operations have limited user-visible progress.
- Thumbnail and metadata work can be made more intentionally scheduled.
- Recoverable failures can expose better next-step actions.
- Error, warning, and status surfaces need UI integration with backend state.

## Selection rule

Tasks 16, 17, and 18 each require one concrete implementation slice:

- One continuity feature.
- One responsiveness feature.
- One error-recovery feature.

Each slice must have:

- Requirements mapping.
- Tests.
- Evidence.
- No feature removal.
- No hidden failure.
- No cloud/account/telemetry dependency.

## Recommended continuity slice

### Feature

Local last-session restore with safe missing-file fallback.

### User value

The app can reopen the last useful local context without forcing the user to re-navigate the filesystem every time.

### Proposed model

Add an additive settings model:

```text
ViewerSettings
  LastSessionFilePath
  LastSessionFolderPath
  LastSessionLayoutMode
  LastSessionUpdatedUtc
```

or a separate model if that better matches the existing settings structure:

```text
UserExperienceSettings
  LastSession
```

### Behavior

- Save last successfully opened image path.
- Save parent folder path when folder-as-playlist succeeds.
- On startup, optionally restore last session if the file still exists.
- If the file is missing but the folder exists, offer or select a safe folder/fallback behavior defined by implementation tests.
- If both are missing, do not show a scary error; keep app usable and log a warning if appropriate.
- Do not delete recent files silently.
- Do not weaken normal open-file behavior.

### Tests

- Loads default settings when no last session exists.
- Saves last successful session.
- Restores existing last file.
- Handles missing last file without data loss.
- Handles invalid settings JSON through existing normalization path.
- Does not remove recent files.

### Evidence

```text
TestResults/phase5-evidence/qol/session-restore-before-after.md
```

## Recommended responsiveness slice

### Feature

Bounded neighbor preload around the current playlist index.

### User value

Next/previous navigation in normal folders feels faster without pretending large-folder work is free.

### Proposed service

```text
src/ImageViewer.Core/Services/IImagePreloadService.cs
src/ImageViewer.Core/Services/ImagePreloadService.cs
```

### Behavior

- Maintain a bounded window around the current playlist index.
- Use cancellation when the current index changes quickly.
- Bound memory and concurrent decode work.
- Avoid preloading transformed or unsaved edited variants unless explicitly safe.
- Prioritize current-nearby items.
- Surface progress/status only when operation duration affects user interaction.
- Do not reduce supported formats, playlist size, metadata, or thumbnail behavior.

### Tests

- Preload window respects configured bounds.
- Quick navigation cancels stale work.
- Current-nearby items are prioritized.
- Cache does not exceed configured budget.
- Decode failures are logged or returned without breaking navigation.

### Evidence

```text
TestResults/phase5-evidence/qol/preload-before-after.md
TestResults/phase5-evidence/qol/preload-measurements.json
```

## Recommended error-recovery slice

### Feature

Recoverable navigation failure keeps playlist active and offers next valid image behavior.

### User value

A corrupt or missing image should not make the session feel broken when the surrounding folder still contains valid images.

### Behavior

- Preserve the error message and structured log.
- Keep the playlist loaded when one item fails.
- Provide a visible next-step action if the app can move to the next valid image.
- Do not silently skip errors.
- Do not remove failed files from playlist unless the user takes an explicit action.
- Keep support bundle redaction intact.

### Tests

- Decode failure records structured error.
- Playlist remains intact after recoverable failure.
- Next valid image action selects a valid item.
- Repeated failures stop safely instead of infinite looping.
- Missing file and corrupt file paths are covered separately if behavior differs.

### Evidence

```text
TestResults/phase5-evidence/qol/recoverable-error-before-after.md
```

## Alternate QOL candidates

These are valid only if selected explicitly during implementation and documented with tests and evidence:

- Layout preference per local folder.
- Smarter thumbnail scheduling based on visible viewport.
- Metadata batching with cancellation.
- Safer recent-file recovery for moved or deleted files.
- User-visible progress state for long playlist scans.
- Local pinned folder shortcut.

## Prohibited QOL shortcuts

The following do not count as QOL:

- Reducing playlist size limits to improve speed.
- Disabling thumbnails.
- Hiding metadata extraction failures.
- Silently dropping failed files from the playlist.
- Removing search, comparison, GIF, slideshow, transform, or layout behavior.
- Adding telemetry to infer QOL.
- Adding cloud sync or accounts.
- Treating a UI-only status label as backend QOL without service behavior.

## Evidence requirements

Each QOL task must produce before/after evidence:

- Scenario.
- Initial app state.
- Files/folder used.
- User action.
- Expected result.
- Actual result.
- Test command.
- Test output location.
- Any known limitation.

## Completion rule

Backend QOL is complete only when:

- The selected feature exists in source.
- Tests cover success and failure paths.
- Evidence demonstrates before/after behavior.
- Existing behavior is preserved by tests or explicit verification.
- No prohibited scope was introduced.
