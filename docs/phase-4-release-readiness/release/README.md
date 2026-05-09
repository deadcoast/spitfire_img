# spitfire.img release notes

## Product summary

`spitfire.img` is a Windows-native image viewer focused on fast local viewing, folder-as-playlist navigation, fading presentation chrome, thumbnail and search workflows, slideshow playback, comparison views, grid and filmstrip layouts, metadata viewing, animated GIF playback, and the existing Phase 2 transform/export tools.

The Phase 4 release artifact is an unpackaged x64 publish output for Windows.

## System requirements

- Windows 10 version 19041 or newer.
- x64 Windows.
- Windows App SDK runtime compatible with the app build.
- Local file-system access to the image folder being viewed.
- Optional Microsoft Store codecs for HEIC and AVIF files.

## Install and run

Build the portable release artifact from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1
```

The release output is written under:

```text
artifacts/release/spitfire.img-0.4.0-win-x64/
```

Run the app directly:

```powershell
artifacts\release\spitfire.img-0.4.0-win-x64\ImageViewer.App.exe
```

Open a file at startup:

```powershell
artifacts\release\spitfire.img-0.4.0-win-x64\ImageViewer.App.exe "C:\Images\sample image.png"
```

For Explorer Open With setup, see:

```text
docs/phase-4-release-readiness/file-association-open-with.md
```

## Uninstall

For the portable Phase 4 artifact, close `spitfire.img` and delete the release artifact folder:

```text
artifacts/release/spitfire.img-0.4.0-win-x64/
```

Removing the release artifact folder does not remove local settings, recent files, or logs. Delete `%LOCALAPPDATA%\spitfire.img` only when intentionally removing local app data.

## Supported formats

Built-in supported formats:

```text
.png, .jpg, .jpeg, .bmp, .webp, .gif, .tif, .tiff
```

Codec-dependent formats:

```text
.heic, .avif
```

The app keeps folder-as-playlist behavior: opening a file builds a playlist from supported files in the same folder.

## HEIC and AVIF codec dependencies

HEIC and AVIF startup and Open With activation are supported through the existing codec-dependent flow. If the required Windows codec is unavailable, `spitfire.img` shows the existing codec unavailable prompt with the Microsoft Store destination for the missing codec.

HEIC uses the HEIF Image Extensions package. AVIF uses the AV1 Video Extension package.

## Keyboard shortcuts

| Workflow | Shortcut | Action |
| --- | --- | --- |
| File | `Ctrl+O` | Open file |
| Navigation | `Right` | Next image |
| Navigation | `Left` | Previous image |
| Navigation | `Home` | First image |
| Navigation | `End` | Last image |
| View | `F11` | Toggle fullscreen |
| View | `Ctrl+0` | Fit to window |
| View | `Ctrl+Shift+1` | Actual size |
| View | `Ctrl++` | Zoom in |
| View | `Ctrl+-` | Zoom out |
| View | `Ctrl+Shift+P` | Command palette |
| Browse | `Ctrl+F` | Search images |
| Browse | `Ctrl+T` | Toggle thumbnail strip |
| Metadata | `Ctrl+I` | Toggle metadata panel |
| Transform | `Ctrl+L` | Rotate left |
| Transform | `Ctrl+R` | Rotate right |
| Transform | `Ctrl+H` | Flip horizontal |
| Transform | `Ctrl+V` | Flip vertical |
| Transform | `Ctrl+Z` | Undo transform |
| Transform | `Ctrl+Shift+Z` | Redo transform |
| Transform | `Ctrl+S` | Save transforms |
| Transform | `Ctrl+Shift+S` | Export current image |
| Slideshow | `F5` | Toggle slideshow |
| Slideshow | `Space` | Pause or resume slideshow |
| Slideshow | `+` | Increase slideshow interval |
| Slideshow | `-` | Decrease slideshow interval |
| Comparison | `Ctrl+D` | Side-by-side comparison |
| Comparison | `Ctrl+Shift+D` | Overlay comparison |
| Comparison | `Ctrl+L` | Toggle comparison sync zoom |
| Comparison | `S` | Swap overlay layers |
| Comparison | `Tab` | Cycle active comparison image |
| Comparison | `Enter` | Open active comparison image |
| Comparison | `Up` | Increase overlay opacity |
| Comparison | `Down` | Decrease overlay opacity |
| Comparison | `Ctrl+2` | Show two comparison images |
| Comparison | `Ctrl+3` | Show three comparison images |
| Comparison | `Ctrl+4` | Show four comparison images |
| Layout | `Ctrl+1` | Single image view |
| Layout | `Ctrl+G` | Grid layout |
| Layout | `Ctrl+Shift+F` | Filmstrip layout |
| Layout | `Ctrl++` | Increase grid density |
| Layout | `Ctrl+-` | Decrease grid density |
| Layout | `Enter` | Open active grid image |
| Animation | `P` | Play or pause animation |
| Animation | `.` | Next animation frame |
| Animation | `,` | Previous animation frame |
| Exit | `Escape` | Close active overlay, layout, slideshow, or fullscreen state |

## Troubleshooting

- If the app does not start, rebuild the artifact with `scripts\build-release.ps1` and verify the release folder contains `ImageViewer.App.exe`, `ImageViewer.App.pri`, and the XAML resource files.
- If a HEIC or AVIF file does not open, install the codec requested by the codec unavailable prompt and try again.
- If settings behave unexpectedly after an upgrade, close the app and inspect `%LOCALAPPDATA%\spitfire.img\settings.json`. Invalid settings are replaced with default runtime settings and a structured warning is written to `logs\`.
- If a folder appears incomplete, verify that files use one of the supported extensions listed above.
- If the UI appears in the wrong size or position, remove only `settings.json` from `%LOCALAPPDATA%\spitfire.img` to reset preferences while preserving logs separately.

## Local settings and logs

`spitfire.img` stores user-local app data under:

```text
%LOCALAPPDATA%\spitfire.img
```

Current local files and folders:

- `settings.json`: viewer preferences, window state, layout preferences, slideshow preferences, codec-dependent workflow preferences, and other user settings.
- `recent-files.json`: recent file entries used by the File menu.
- `logs\`: structured warning and error logs written by the app.

First-run behavior:

- If `settings.json` is missing, the app loads default settings.
- If an older or partial `settings.json` is present, the app normalizes missing or out-of-range values before use.
- If `settings.json` is invalid JSON or cannot be read, the app restores default settings for the session and writes a structured warning to `logs\`.

## Support bundle

Create a local diagnostic bundle with:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\collect-support.ps1
```

The default user bundle path is:

```text
%LOCALAPPDATA%\spitfire.img\support
```

Validation bundles are written under:

```text
TestResults/phase4-evidence/support-bundle/
```

Support bundles include app version metadata, system metadata, codec availability, optional release manifest data, `dotnet --info` when available, Windows App SDK runtime package data when available, and redacted structured logs. They exclude image files, thumbnails, recent file data, settings files, and user folder listings by default.

## Known limitations

- Phase 4 is a release-readiness pass for the existing viewer feature set.
- The portable artifact does not install a system-wide default app entry by itself; use Windows Open With registration for each extension.
- HEIC and AVIF support depends on Windows codec availability.
- The app views local files and folders; it does not maintain a separate media database or online service connection.
- Transform tools remain limited to the existing Phase 2 rotate, flip, undo, redo, save, and export behavior.
- Support bundles redact local paths and exclude images by default, so maintainers may need follow-up details that users choose to provide separately.

