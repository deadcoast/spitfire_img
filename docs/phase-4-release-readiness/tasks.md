# Implementation Plan: Phase 4 Release Readiness

## Overview

This implementation plan converts the Phase 4 Release Readiness requirements and design into required development tasks. Phase 4 prepares the completed Phase 1 through Phase 3 viewer for reproducible local release without adding new viewing workflows.

All tasks preserve the existing architecture: platform-neutral logic remains in `ImageViewer.Core`, WinUI and Windows-specific release behavior remains in `ImageViewer.App`, generated artifacts stay under ignored output folders, and validation evidence goes under `TestResults/phase4-evidence/`.

## Tasks

- [x] 1. Add release artifact hygiene
  - Add or verify `.gitignore` entries for `artifacts/` and `TestResults/`
  - Create the `artifacts/release/` convention in documentation only; do not commit generated artifacts
  - Confirm generated release binaries, screenshots, support bundles, and local logs are ignored
  - _Requirements: 9.1, 9.2, 9.3, 9.4_

- [ ] 2. Define Phase 4 release version and app metadata
  - Add source-controlled release metadata for product name `spitfire.img`
  - Set or document the Phase 4 release version as `0.4.0`
  - Ensure executable or artifact metadata exposes product name and version
  - Preserve the existing `spitfire.img` window title
  - Preserve Per-Monitor V2 DPI awareness in `app.manifest`
  - _Requirements: 3.1, 3.3, 3.4, 3.5_

- [ ] 3. Add app icon and release assets
  - Create `src/ImageViewer.App/Assets/`
  - Add source-controlled icon asset files for executable/window/package contexts
  - Wire assets into `ImageViewer.App.csproj` or manifest metadata as required by the selected distribution strategy
  - Document any icon generation command if derived assets are generated
  - _Requirements: 3.2, 3.6_

- [ ] 4. Implement release build script
  - Create `scripts/build-release.ps1`
  - Stop `ImageViewer.App` before building
  - Clean only repository-owned release artifact output
  - Run a Release x64 publish command
  - Write output under `artifacts/release/spitfire.img-<version>-win-x64/`
  - Exit non-zero on build or publish failure
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 2.1_

- [ ] 5. Generate release manifest
  - Add release manifest generation to `scripts/build-release.ps1`
  - Include product name, version, configuration, platform, commit, build time, artifact kind, and verification placeholders
  - Write `release-manifest.json` into the release artifact folder
  - Add a test or script validation that required manifest fields exist
  - _Requirements: 2.5, 8.3_

- [ ] 6. Validate release artifact launch
  - Launch `ImageViewer.App.exe` from the release artifact folder
  - Verify the process creates a `spitfire.img` window
  - Verify direct launch without a file path does not crash
  - Record evidence under `TestResults/phase4-evidence/`
  - _Requirements: 2.2, 2.4, 8.4_

- [ ] 7. Harden startup file path activation
  - Add or update tests for startup arguments containing spaces
  - Add or update tests for startup arguments containing non-ASCII characters
  - Validate release artifact launch with a file path containing spaces
  - Validate codec-dependent startup behavior still routes through the existing codec unavailable flow
  - _Requirements: 4.1, 4.3, 4.4, 4.5, 4.6, 8.5_

- [ ] 8. Define file association or Open With strategy
  - Document supported extensions and codec-dependent extensions separately
  - For portable distribution, document Open With registration steps or create an optional registration script
  - For installer/package distribution, document the package metadata needed for associations
  - Preserve HEIC and AVIF codec-dependent behavior
  - _Requirements: 4.2, 4.3, 4.6, 7.2, 7.3_

- [ ] 9. Validate first-run and upgrade safety
  - Verify missing settings load defaults
  - Verify older or partial settings normalize safely
  - Verify invalid settings fall back to defaults and log a structured warning
  - Manually validate upgrade behavior using an existing `%LOCALAPPDATA%\spitfire.img` settings folder
  - Document settings and logs location in release docs
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [ ] 10. Implement support bundle script
  - Create `scripts/collect-support.ps1`
  - Collect app version, release manifest when available, Windows version, architecture, and available logs
  - Exclude images, thumbnails, recent file paths, and user folder listings by default
  - Handle missing logs without failing
  - Write validation output under `TestResults/phase4-evidence/support-bundle/`
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

- [ ] 11. Add support bundle redaction tests or validation
  - Verify support bundle output does not include image files
  - Verify support bundle output does not include recent file paths by default
  - Verify missing log folders do not fail the script
  - Verify expected diagnostic metadata is present
  - _Requirements: 6.2, 6.3, 6.4, 6.6_

- [ ] 12. Create release documentation
  - Create `docs/release/README.md`
  - Document system requirements, install/run, uninstall, supported formats, HEIC/AVIF codec dependencies, keyboard shortcuts, troubleshooting, support bundle usage, and known limitations
  - Create `docs/release/release-template.md`
  - Ensure docs do not claim unsupported cataloging, cloud, telemetry, or new editing features
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 10.1, 10.2_

- [ ] 13. Add release evidence template
  - Create `docs/release/phase4-evidence-template.md` or equivalent
  - Include sections for build, test, publish, launch smoke, startup path with spaces, large playlist, 4K image, 100-frame GIF, support bundle, and known issues
  - Use `TestResults/phase4-evidence/` as the evidence output folder
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7_

- [ ] 14. Run release gate checkpoint
  - Run `Stop-Process -Name ImageViewer.App -Force -ErrorAction SilentlyContinue`
  - Run `dotnet build SpitfireImg.sln -p:Platform=x64`
  - Run `dotnet test SpitfireImg.sln -p:Platform=x64`
  - Run `powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1`
  - Verify release artifact launch from `artifacts/release/`
  - Verify startup path with spaces from release artifact
  - Verify 1000-image playlist, 4K image, and 100-frame GIF smoke scenarios from release artifact
  - Verify support bundle creation
  - Save evidence under `TestResults/phase4-evidence/`
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7_

- [ ] 15. Final Phase 4 scope checkpoint
  - Confirm Phase 4 did not add cloud sync, accounts, telemetry, cataloging, albums, tagging, or new editing workflows
  - Confirm folder-as-playlist behavior remains intact
  - Confirm Phase 1 through Phase 3 shortcuts remain documented and unchanged unless requirements were updated
  - Confirm release docs, artifacts, evidence, and task list are reconciled
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

## Notes

- Phase 4 required tasks are release-readiness tasks, not new viewer workflow tasks.
- Generated release artifacts belong under `artifacts/` and must not be committed.
- Manual evidence belongs under `TestResults/phase4-evidence/` and must not be committed.
- The current project uses `WindowsPackageType=None`; the first release implementation should respect that baseline unless the distribution strategy task explicitly changes it.
- HEIC and AVIF remain codec-dependent formats.
