# Phase 1 MVP Viewer Design

## Purpose

Build the first complete version of `spitfire.img`: a modern Windows-native image viewer focused on presentation, fast folder navigation, and low-distraction UI. Phase 1 must feel usable end-to-end while preserving clear seams for Phase 2 and Phase 3.

## Scope

Phase 1 includes:

- Open local `PNG`, `JPG/JPEG`, `BMP`, and `WEBP` files.
- Automatically treat the opened file's parent folder as the playlist.
- Navigate previous, next, first, and last image by keyboard.
- Display images centered with fit-to-window as the default mode.
- Support zoom, pan, actual-size, fit reset, and fullscreen.
- Fade top and bottom UI chrome after inactivity in windowed and fullscreen modes.
- Show basic metadata: filename, playlist index, dimensions, and folder name.
- Persist basic settings such as theme and default view mode.
- Log structured errors to disk.

Phase 1 explicitly defers:

- `GIF`, `TIFF`, `HEIC`, and `AVIF` support.
- Search, thumbnail strip, recent files, drag-and-drop, export/save copy, rotate/flip, slideshow, comparison mode, command palette, and advanced metadata.

Deferred means planned but not built in this phase.

## Architecture

Use WinUI 3 with Windows App SDK and C#/.NET. The app should be split into presentation, workflow state, and services.

Presentation owns `MainWindow`, `ImageCanvas`, fading chrome, menu bar, bottom info bar, fullscreen state, and input routing. Workflow state owns current image, playlist state, zoom state, UI visibility state, and command availability. Services own file opening, image decoding, playlist scanning, navigation, settings persistence, and error logging.

The main window must not contain playlist scanning, image decoding, settings serialization, or error formatting logic. That boundary keeps Phase 2 and Phase 3 additive.

## Proposed Module Structure

```text
src/ImageViewer.App/
├── Views/
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── ImageCanvas.xaml
│   ├── FadingChrome.xaml
│   └── BottomInfoBar.xaml
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── ViewerState.cs
│   └── PlaylistState.cs
├── Services/
│   ├── FileOpenService.cs
│   ├── ImageDecodeService.cs
│   ├── FolderPlaylistService.cs
│   ├── NavigationService.cs
│   ├── SettingsService.cs
│   └── ErrorLogService.cs
├── Models/
│   ├── ImageItem.cs
│   ├── ImageMetadata.cs
│   ├── ViewerSettings.cs
│   └── ViewerError.cs
└── Resources/
    ├── Styles/
    └── ThemeTokens/
```

Tests should live under `tests/ImageViewer.Tests/` and focus on services and models.

## User Flow

When a user opens an image, the app validates the extension, decodes the image, scans the parent folder for supported images, sorts the playlist, selects the opened image, updates metadata, and renders the viewer. The UI appears briefly, then fades after 1.5 to 2.5 seconds of inactivity. Mouse movement, keyboard navigation, menu activation, fullscreen changes, or image changes should reveal the UI again.

Keyboard behavior:

- `Left Arrow`: previous image.
- `Right Arrow`: next image.
- `Home`: first image.
- `End`: last image.
- `F11` or double-click: toggle fullscreen.
- `Ctrl+0`: fit to window.
- `Ctrl+1`: actual size.
- `Ctrl++` / `Ctrl+-`: zoom in/out.
- `Esc`: exit fullscreen or close transient overlay.

## Error Handling

Phase 1 includes a structured error system with `ViewerError` and `ErrorLogService`.

Defined categories:

- `FileAccessError`
- `UnsupportedFormatError`
- `DecodeError`
- `PlaylistScanError`
- `NavigationError`
- `SettingsPersistenceError`
- `UnexpectedViewerError`

Each log entry should include timestamp, severity, category, user-safe message, operation, optional file path, exception type, and stack trace when available. User-facing messages must be calm and actionable, for example: "This file format is not supported in this version." Raw stack traces should never appear in the UI.

Logs should be written under the app's local data folder so they survive restarts but do not require elevated permissions.

## Continuity For Later Phases

Phase 2 and Phase 3 must remain visible during Phase 1 development through explicit docs and stable interfaces.

Create or maintain:

- `docs/roadmap.md`, listing Phase 2 and Phase 3 features with short acceptance criteria.
- Architecture decisions for WinUI 3, decoding strategy, playlist model, settings persistence, and UI fade behavior.
- Service interfaces that allow search, thumbnails, recent files, drag-and-drop, advanced formats, and comparison mode to plug in later.

Phase 1 should not hardwire future decisions into UI code. For example, supported format detection should live behind an image format or decode service rather than scattered file-extension checks in the window.

## Testing Strategy

Automated tests should cover:

- Supported extension detection for `PNG`, `JPG/JPEG`, `BMP`, and `WEBP`.
- Rejection of deferred formats with `UnsupportedFormatError`.
- Folder playlist scanning and deterministic sorting.
- Opened image index selection.
- Previous, next, first, and last navigation boundaries.
- Settings defaults and persistence.
- Structured error log entry creation.

Manual verification should cover:

- Windowed fading UI.
- Fullscreen behavior.
- Keyboard shortcuts.
- Zoom and pan feel.
- Theme switching.
- Visual polish and low-distraction presentation.

## Acceptance Criteria

- A user can open a supported local image and immediately browse sibling images in the same folder.
- The viewer never requires the user to manually define a playlist for normal folder browsing.
- The UI fades in windowed mode and fullscreen mode without hiding controls permanently.
- Unsupported or corrupt files show a user-safe message and write a structured log entry.
- Phase 2 and Phase 3 features remain documented as deferred, not forgotten.
- Core non-UI behavior is covered by automated tests.
