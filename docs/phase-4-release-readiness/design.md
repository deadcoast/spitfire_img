# Design Document: Phase 4 Release Readiness

## Overview

Phase 4 turns the completed viewer into a releasable Windows application. It focuses on distribution, app identity, install/run documentation, diagnostics, and a repeatable release gate.

This phase intentionally avoids new viewing workflows. The design preserves the existing architecture:

- `ImageViewer.Core` remains platform-neutral.
- `ImageViewer.App` owns WinUI, Windows App SDK integration, app startup, and Windows-specific services.
- Generated artifacts and validation evidence stay outside source control.

## Current baseline

The app currently uses:

- `src/ImageViewer.App/ImageViewer.App.csproj`
- `TargetFramework=net8.0-windows10.0.19041.0`
- `TargetPlatformMinVersion=10.0.17763.0`
- `UseWinUI=true`
- `WindowsPackageType=None`
- `ApplicationManifest=app.manifest`
- `Platforms=x64`

The existing app manifest provides DPI awareness but does not yet define release identity, installer metadata, file associations, package assets, or distribution artifacts.

## Design goals

1. Produce a deterministic Release artifact from source.
2. Make the app recognizable as `spitfire.img` in Windows.
3. Preserve current startup-file behavior and folder-as-playlist behavior.
4. Provide a support bundle that helps diagnose failures without copying user images.
5. Keep generated release output out of source control.
6. Create release evidence that can be reviewed after the build.

## Non-goals

- No cloud sync.
- No catalog, albums, database, or tagging system.
- No telemetry.
- No new image editing workflows.
- No change to the folder-as-playlist model.
- No Phase 1 through Phase 3 shortcut changes unless the requirements and task list are updated first.

## Architecture

```text
docs/phase-4-release-readiness/
  requirements.md        Phase 4 user-visible and maintainer requirements
  design.md              Release architecture and implementation details
  tasks.md               Required implementation checklist

docs/release/
  README.md              User-facing install, run, uninstall, troubleshooting docs
  release-template.md    Release notes template

scripts/
  build-release.ps1      Deterministic release build/publish/package entrypoint
  collect-support.ps1    Support bundle export script

artifacts/
  release/               Generated release output, ignored by git

TestResults/
  phase4-evidence/       Manual and automated release evidence, ignored by git

src/ImageViewer.App/
  ImageViewer.App.csproj Project metadata, publish settings, asset includes
  app.manifest           Unpackaged app manifest settings
  Assets/                Source-controlled icon and visual assets
```

## Distribution strategy

Phase 4 starts by documenting and implementing one distribution path. The repository currently uses `WindowsPackageType=None`, so the first implementation slice should support an unpackaged Release publish unless a later task explicitly changes the package model.

The release build script should:

1. Stop any running `ImageViewer.App` process.
2. Clean only the repository-owned artifact output folder.
3. Run `dotnet publish` with `Configuration=Release` and `Platform=x64`.
4. Write output to `artifacts/release/spitfire.img-<version>-win-x64/`.
5. Generate `release-manifest.json` in the artifact folder.
6. Exit non-zero on any failure.

The release manifest should contain:

```json
{
  "productName": "spitfire.img",
  "version": "0.4.0",
  "configuration": "Release",
  "platform": "x64",
  "commit": "<git commit>",
  "buildTimeUtc": "<ISO-8601 timestamp>",
  "artifactKind": "unpackaged-publish",
  "verification": {
    "build": "pending",
    "tests": "pending",
    "launchSmoke": "pending"
  }
}
```

## Application identity and assets

App identity work should stay in `ImageViewer.App`.

Required metadata:

- Product name: `spitfire.img`
- Executable assembly name: existing `ImageViewer.App` unless changing it is explicitly planned.
- Version: start with `0.4.0` for Phase 4 release readiness.
- Window title: `spitfire.img`
- DPI awareness: keep Per-Monitor V2.

Required assets:

- App icon source asset.
- Generated icon outputs suitable for executable/window/package use.
- Any conversion script must be deterministic and source controlled.

If icon generation requires external tools, document the exact dependency and include a fallback source asset in the repository.

## File activation and associations

The app already parses startup file paths through `App.ParseInitialFilePath`. Phase 4 should harden this behavior for release scenarios.

Validation cases:

- Path with spaces.
- Path with non-ASCII characters.
- Codec-dependent HEIC and AVIF paths.
- Direct executable launch with no file path.
- Launch from artifact folder rather than Debug output.

File association implementation depends on the selected distribution strategy:

- Unpackaged portable release: provide documented Open With registration instructions or an optional registration script.
- Installer/package release: encode file associations in installer/package metadata.

Supported extensions must be listed in release docs:

```text
.png, .jpg, .jpeg, .bmp, .webp, .gif, .tif, .tiff, .heic, .avif
```

HEIC and AVIF remain codec-dependent and must keep the existing codec unavailable flow.

## Support bundle design

The support bundle script should collect diagnostic data without collecting user images.

Included:

- App version and artifact manifest when available.
- Windows version and architecture.
- `dotnet --info` summary when available.
- Windows App SDK runtime package list when available.
- Codec detection results if the app exposes them or a standalone check is implemented.
- Structured logs under `%LOCALAPPDATA%\spitfire.img\logs`.

Excluded by default:

- Image files.
- Thumbnails.
- Recent file paths.
- Full directory listings of user folders.
- Settings values that include user-specific paths unless redacted.

Output:

```text
%LOCALAPPDATA%\spitfire.img\support\spitfire-support-<timestamp>.zip
```

Validation output during development:

```text
TestResults/phase4-evidence/support-bundle/
```

## Release documentation

Create `docs/release/README.md` with:

- Product summary.
- System requirements.
- Install or run instructions.
- Uninstall instructions.
- Supported formats.
- Codec dependency notes for HEIC and AVIF.
- Keyboard shortcut table for Phase 1 through Phase 3.
- Troubleshooting.
- Support bundle instructions.
- Known limitations.

Create `docs/release/release-template.md` with:

- Version.
- Date.
- Commit.
- Artifact names.
- Verification commands and results.
- Manual evidence links.
- Known issues.

## Release gate

The release gate is a required verification sequence:

```powershell
Stop-Process -Name ImageViewer.App -Force -ErrorAction SilentlyContinue
dotnet build SpitfireImg.sln -p:Platform=x64
dotnet test SpitfireImg.sln -p:Platform=x64
powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1
```

Manual smoke validation must launch the release artifact, not Debug output.

Required evidence:

- Build/test command output summary.
- Release artifact manifest.
- Launch smoke result.
- Startup path with spaces result.
- 1000-image playlist smoke result.
- 4K image smoke result.
- 100-frame GIF smoke result.
- Support bundle creation result.

Evidence location:

```text
TestResults/phase4-evidence/
```

## Repository hygiene

`.gitignore` should exclude:

```text
artifacts/
TestResults/
```

Source-controlled files should include:

- Release scripts.
- Source icons/assets.
- Manifest/source metadata.
- Release docs.
- Phase 4 specs and task list.

Generated binaries, release packages, screenshots, support bundles, and local logs must not be committed.

## Testing strategy

Use automated tests for:

- Startup argument parsing with spaces and non-ASCII paths.
- Release manifest generation if implemented in testable code.
- Support bundle redaction behavior.
- Settings fallback and normalization behavior.

Use script-level verification for:

- Release build output path.
- Manifest file existence and required fields.
- Artifact launch smoke.

Use manual evidence for:

- Windows file association or Open With behavior.
- Icon appearance in Explorer or install context.
- Upgrade behavior from an existing `%LOCALAPPDATA%\spitfire.img` folder.
- 4K, large playlist, and animated GIF release-artifact smoke tests.

## Acceptance criteria mapping

- Requirements 1 and 2 map to release scripts and artifact manifest.
- Requirement 3 maps to project metadata, manifest, and assets.
- Requirement 4 maps to startup path tests and file association documentation or metadata.
- Requirement 5 maps to settings/log compatibility tests and manual upgrade evidence.
- Requirement 6 maps to support bundle script and redaction tests.
- Requirement 7 maps to release documentation.
- Requirement 8 maps to release gate scripts and `TestResults/phase4-evidence/`.
- Requirement 9 maps to `.gitignore` and artifact folder discipline.
- Requirement 10 maps to review checks before Phase 4 implementation is closed.
