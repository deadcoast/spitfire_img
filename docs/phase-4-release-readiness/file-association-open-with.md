# File association and Open With strategy

This document defines the Phase 4 file association strategy for `spitfire.img`.

The current Phase 4 release artifact is an unpackaged publish output with `artifactKind` set to `unpackaged-publish` in `release-manifest.json`. For that artifact, Phase 4 supports Explorer launch through Windows Open With registration. If a later installer or package is added, it must encode the same extension set and route activation through the same startup file path flow.

## Supported extensions

Associate these extensions with `spitfire.img`:

```text
.png
.jpg
.jpeg
.bmp
.webp
.gif
.tif
.tiff
```

These formats are handled by the existing file validation, folder playlist, decode, and startup activation paths.

## Codec-dependent extensions

Associate these extensions only with the explicit codec-dependent behavior preserved:

```text
.heic
.avif
```

HEIC and AVIF activation must continue to route through the existing codec detection and codec unavailable flow. Association metadata or Open With registration must not hide these extensions from startup activation, convert them to unsupported formats, or bypass the existing Store prompt behavior when the required Windows codec is unavailable.

## Portable Open With registration

For the unpackaged Phase 4 artifact:

1. Build the release artifact with `scripts/build-release.ps1`.
2. In File Explorer, right-click an image with one supported extension.
3. Select **Open with** > **Choose another app**.
4. Select **Choose an app on your PC**.
5. Select `artifacts\release\spitfire.img-0.4.0-win-x64\ImageViewer.App.exe`.
6. Enable the Windows option to always use this app for the selected extension when making it the default.
7. Repeat for each extension that should open with `spitfire.img`.

For `.heic` and `.avif`, this registration is valid only with the codec-dependent behavior above: if the required Windows codec is unavailable, startup activation must show the existing codec unavailable flow.

To remove a portable Open With association, use Windows Settings > Apps > Default apps, search for the extension, and choose a different default app.

## Installer or package metadata

If Phase 4 or a later release adds an installer or package, the installer/package must declare file associations for the same extension groups above.

Required metadata:

- Product display name: `spitfire.img`.
- Executable target: the installed `ImageViewer.App.exe`.
- Open command shape for installer-based registration: `"<install path>\ImageViewer.App.exe" "%1"`.
- Package association shape for MSIX-style packaging: `uap:FileTypeAssociation` entries with `uap:SupportedFileTypes` containing the listed extensions.
- Association display icon: the release app icon asset from `src/ImageViewer.App/Assets/`.

Association activation must pass the selected file path as the startup argument. The app startup parser must remain the single path that handles quoted paths, paths with spaces, non-ASCII paths, and codec-dependent paths.

## Validation

Phase 4 validates association readiness through:

- Startup parser tests for quoted paths, paths with spaces, and non-ASCII paths.
- View-model tests proving valid startup paths open and HEIC/AVIF codec-dependent paths still request the codec unavailable flow.
- Release artifact launch evidence with a quoted file path containing spaces under `TestResults/phase4-evidence/`.
