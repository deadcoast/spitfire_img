# spitfire.img Roadmap

## Phase 1: MVP Viewer

Goal: ship a complete local image viewing loop with presentation-first UI.

Acceptance criteria:

- Open `PNG`, `JPG/JPEG`, `BMP`, and `WEBP` files.
- Automatically use the opened image's parent folder as the playlist.
- Support previous, next, first, and last keyboard navigation.
- Provide fit-to-window, actual size, zoom, pan, and fullscreen behavior.
- Fade top and bottom UI chrome in windowed and fullscreen modes.
- Show filename, image index, dimensions, and folder metadata.
- Persist basic theme and viewer settings.
- Write structured error logs for file, decode, playlist, settings, navigation, and unexpected errors.

## Phase 2: Browse And Quality Of Life

Goal: make everyday folder browsing faster and more convenient without changing the Phase 1 viewer core.

Candidate features:

- Search the current folder by filename.
- Add an optional thumbnail strip.
- Add recent files.
- Support drag-and-drop image opening.
- Add export or save-copy behavior.
- Add rotate and flip commands.
- Add better image metadata.
- Add `GIF` and `TIFF` support after decoding behavior is defined.

Acceptance criteria draft:

- Search and thumbnails use the same playlist model as Phase 1.
- Recent files do not replace folder-as-playlist behavior.
- New format support routes through the image decode service, not the window.

## Phase 3: Advanced Viewing Modes

Goal: add power-user presentation and comparison workflows after the core viewer is stable.

Candidate features:

- Slideshow mode.
- Custom layout modes.
- Image comparison mode.
- Command palette.
- Animated image support where relevant.
- `HEIC` and `AVIF` support after dependency and Windows codec strategy is selected.

Acceptance criteria draft:

- Comparison and slideshow modes reuse existing image item and playlist models.
- Advanced formats produce structured unsupported/decode errors when unavailable.
- New layouts preserve the low-distraction fading UI principle.

## Phase 4: Release Readiness

Goal: make the completed local image viewer releasable, installable, supportable, and reproducible without adding new viewing workflows.

Candidate features:

- Reproducible release build and artifact manifest.
- Application identity, icons, and release assets.
- Distribution package or portable release strategy.
- File association or Open With behavior for supported formats.
- First-run and upgrade safety.
- Local diagnostic support bundle.
- Release documentation, release notes template, and release evidence gate.

Acceptance criteria draft:

- Release artifacts are produced from a documented x64 Release command.
- Generated artifacts stay out of source control.
- The app launches from the release artifact and preserves folder-as-playlist behavior.
- Startup file path activation works for paths with spaces and non-ASCII characters.
- HEIC and AVIF remain codec-dependent formats with structured user-facing fallback.
- Release evidence is stored under `TestResults/phase4-evidence/`.

## Deferred Means Planned

Features listed in later phases are deferred to protect earlier delivery. They are not rejected. Earlier-phase code should preserve service boundaries that allow these features to be added without rewriting the viewer shell.
