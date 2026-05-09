# Requirements Document: Phase 4 Release Readiness

## Introduction

This document defines Phase 4 for `spitfire.img` after completion of the Phase 1 MVP Viewer, Phase 2 Browse And Quality Of Life, and Phase 3 Advanced Viewing Modes scopes.

Phase 4 does not add new viewing workflows. Its purpose is to make the existing Windows-native image viewer releasable, installable, supportable, and reproducible without weakening the presentation-first product direction.

Phase 4 covers release packaging, application identity, icons and assets, file association behavior, first-run and upgrade safety, release documentation, diagnostics, release verification, and repository hygiene.

## Glossary

- **Viewer**: The `spitfire.img` application.
- **Release_Build**: A reproducible build artifact intended for local installation or distribution.
- **Distribution_Package**: The installable or portable artifact produced by the release pipeline.
- **Application_Identity**: The product name, executable metadata, app icon, version, publisher, and manifest identity shown by Windows.
- **File_Association**: Windows behavior that allows supported image files to open with the Viewer from Explorer or Open With.
- **Release_Notes**: Human-readable summary of changes, requirements, known limitations, and verification results for a release.
- **Support_Bundle**: A local diagnostic bundle containing logs and non-private environment metadata useful for troubleshooting.
- **Release_Gate**: The set of automated and manual checks required before a build can be considered releasable.
- **Upgrade_Path**: Behavior when a user installs or runs a newer version over an existing local settings/logs directory.

## Requirements

### Requirement 1: Release build reproducibility

**User Story:** As a maintainer, I want a reproducible release build command, so that release artifacts can be recreated and verified from the repository.

#### 1: Acceptance Criteria

1. THE Repository SHALL document the exact PowerShell command used to create a Release_Build.
2. THE Release_Build command SHALL specify `Platform=x64`.
3. THE Release_Build command SHALL use a `Release` configuration.
4. THE Release_Build command SHALL produce artifacts under a deterministic `artifacts/` folder.
5. THE Release_Build command SHALL fail when build or publish errors occur.
6. THE Release_Build process SHALL NOT depend on files outside the repository except installed SDKs and Windows App SDK prerequisites.

### Requirement 2: Distribution package strategy

**User Story:** As a user, I want a clear install or run option, so that I can launch the Viewer without building it myself.

#### 2: Acceptance Criteria

1. THE Repository SHALL document the selected distribution strategy before implementation.
2. IF the selected strategy is portable distribution, THEN THE Distribution_Package SHALL include the executable and all required runtime files.
3. IF the selected strategy is installer distribution, THEN THE Distribution_Package SHALL include installer metadata and uninstall behavior.
4. THE Distribution_Package SHALL preserve the existing Windows App SDK runtime expectations.
5. THE Distribution_Package SHALL include a release manifest listing artifact name, version, commit, build date, and verification status.
6. THE Distribution_Package SHALL NOT require cloud services, catalog accounts, or telemetry enrollment.

### Requirement 3: Application identity and assets

**User Story:** As a user, I want the app to appear as `spitfire.img` in Windows, so that the installed app is recognizable and professional.

#### 3: Acceptance Criteria

1. THE Application_Identity SHALL use the product name `spitfire.img`.
2. THE Viewer SHALL have application icon assets for executable, window, and package contexts.
3. THE Viewer SHALL expose version metadata in the executable or package artifact.
4. THE Window title SHALL remain `spitfire.img`.
5. THE app manifest SHALL retain Per-Monitor V2 DPI awareness.
6. THE release assets SHALL be stored under repository-controlled paths.

### Requirement 4: File association and Open With behavior

**User Story:** As a Windows user, I want supported image files to open with the Viewer from Explorer, so that the app works like a native image viewer.

#### 4: Acceptance Criteria

1. THE Viewer SHALL support startup file path activation for all supported Phase 1, Phase 2, and Phase 3 image formats.
2. THE File_Association plan SHALL list supported extensions and codec-dependent extensions separately.
3. THE File_Association plan SHALL preserve codec-dependent behavior for HEIC and AVIF.
4. WHEN Windows passes a file path containing spaces, THE Viewer SHALL open that file.
5. WHEN Windows passes a file path with non-ASCII characters, THE Viewer SHALL open that file.
6. WHEN a codec-dependent file is associated but the codec is unavailable, THE Viewer SHALL show the existing codec unavailable flow.

### Requirement 5: First-run and upgrade safety

**User Story:** As a returning user, I want upgrades to preserve my settings and logs safely, so that installing a newer build does not damage local state.

#### 5: Acceptance Criteria

1. THE Viewer SHALL continue using `%LOCALAPPDATA%\spitfire.img` for settings, logs, and local app data.
2. THE Viewer SHALL handle missing settings files by loading defaults.
3. THE Viewer SHALL handle older settings files by normalizing values.
4. THE Viewer SHALL handle invalid settings files by falling back to defaults and logging a structured warning.
5. THE Upgrade_Path SHALL be manually validated with an existing settings file from a prior phase.
6. THE release notes SHALL state where local settings and logs are stored.

### Requirement 6: Diagnostics and support bundle

**User Story:** As a user reporting a problem, I want a local diagnostic export, so that I can share useful troubleshooting information without exposing images.

#### 6: Acceptance Criteria

1. THE Viewer SHALL provide a command or documented script to collect a Support_Bundle.
2. THE Support_Bundle SHALL include structured error logs.
3. THE Support_Bundle SHALL include app version, Windows version, architecture, and codec availability.
4. THE Support_Bundle SHALL NOT include image files, thumbnails, recent file paths, or user folder listings by default.
5. THE Support_Bundle SHALL be written under `TestResults/` during validation and under a documented local path for users.
6. THE Support_Bundle flow SHALL handle missing logs without failing.

### Requirement 7: Release documentation

**User Story:** As a user or maintainer, I want release documentation, so that installation, verification, known limits, and troubleshooting are clear.

#### 7: Acceptance Criteria

1. THE Repository SHALL include a release README or equivalent release document.
2. THE release document SHALL list supported image formats.
3. THE release document SHALL explain HEIC and AVIF codec dependencies.
4. THE release document SHALL list keyboard shortcuts for Phase 1 through Phase 3 workflows.
5. THE release document SHALL include install, run, uninstall, and troubleshooting sections.
6. THE release document SHALL include known limitations without claiming unsupported features.

### Requirement 8: Release verification gate

**User Story:** As a maintainer, I want a release gate, so that release artifacts are not shipped from unverified source.

#### 8: Acceptance Criteria

1. THE Release_Gate SHALL run `dotnet build SpitfireImg.sln -p:Platform=x64`.
2. THE Release_Gate SHALL run `dotnet test SpitfireImg.sln -p:Platform=x64`.
3. THE Release_Gate SHALL run the release publish or package command.
4. THE Release_Gate SHALL validate that the app launches from the packaged or published artifact.
5. THE Release_Gate SHALL validate startup file path activation with at least one image path containing spaces.
6. THE Release_Gate SHALL validate large playlist, 4K image, and animated GIF smoke scenarios.
7. THE Release_Gate SHALL write evidence under `TestResults/phase4-evidence/`.

### Requirement 9: Repository hygiene for release work

**User Story:** As a maintainer, I want release artifacts kept out of source control, so that the repository stays clean and reproducible.

#### 9: Acceptance Criteria

1. THE Repository SHALL ignore generated release artifacts under `artifacts/`.
2. THE Repository SHALL keep source assets, manifests, scripts, and docs under source control.
3. THE Repository SHALL NOT commit generated binaries, package outputs, screenshots, or local machine logs.
4. THE release scripts SHALL be deterministic and safe to run repeatedly.
5. THE release scripts SHALL not delete user data or files outside intended artifact folders.

### Requirement 10: Phase 4 scope boundary

**User Story:** As a maintainer, I want Phase 4 to stay focused on release readiness, so that product scope does not drift.

#### 10: Acceptance Criteria

1. THE Phase 4 implementation SHALL NOT add editing workflows beyond existing Phase 2 transform behavior.
2. THE Phase 4 implementation SHALL NOT add cataloging, albums, tagging, cloud sync, accounts, or telemetry.
3. THE Phase 4 implementation SHALL NOT change folder-as-playlist behavior.
4. THE Phase 4 implementation SHALL NOT remove or rename existing Phase 1 through Phase 3 keyboard shortcuts without updating requirements, design, tasks, and manual evidence.
5. THE Phase 4 implementation SHALL preserve WinUI 3 and Windows App SDK as the app platform.
