# Requirements Document: Phase 5 Professional UI And User QOL

## Introduction

This document defines Phase 5 for `spitfire.img` after completion of Phase 4 Release Readiness.

Phase 5 is dedicated only to:

1. Professional frontend UI development, replacing common boilerplate with better sourced material, and further developing visual transitions and animations.
2. Backend user quality-of-life enhancements that improve daily use without removing, simplifying, hiding, weakening, or bypassing existing features.

Phase 5 is not a release-packaging phase, not a cataloging phase, not a cloud phase, and not an editing-workflow expansion phase. The phase must preserve the existing image-viewer product direction: presentation-first viewing, fading UI, automatic folder-as-playlist behavior, WinUI 3, Windows App SDK, and the Phase 1 through Phase 4 behavior already validated.

## Source material

Phase 5 implementation must use professional source material instead of improvised boilerplate styling. The minimum source set is:

- Microsoft Fluent 2 design guidance for motion, typography, spacing, color, density, and interaction tone: <https://fluent2.microsoft.design/>
- Microsoft Learn Windows app motion guidance, including connected animations and XAML animation guidance: <https://learn.microsoft.com/en-us/windows/apps/design/motion/>
- WinUI Gallery source and samples for WinUI control usage and interaction patterns: <https://github.com/microsoft/WinUI-Gallery>
- Windows App SDK and WinUI documentation for platform-appropriate composition, XAML, and system backdrop behavior: <https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/>

Additional libraries, assets, or design references may be used only when they are named, licensed or license-reviewed, source-controlled when required, and validated against the application instead of accepted as generic inspiration.

## Glossary

- **Viewer**: The `spitfire.img` application.
- **Professional_UI_System**: The source-controlled set of design tokens, component styles, layout rules, animation rules, visual assets, and validation evidence used to deliver a mature app interface.
- **Design_Token**: A named color, typography, spacing, elevation, opacity, radius, duration, easing, or density value used consistently across views.
- **Sourced_Material**: A design pattern, asset, animation pattern, icon, layout reference, or library that has a documented source, license status, and reason for use.
- **Motion_System**: The viewer's transitions, animations, easing, durations, reduced-motion behavior, and transition orchestration.
- **Chrome**: Top menus, bottom information bar, overlays, panels, HUDs, command palette, metadata panel, thumbnail strip, frame controls, and transient viewer controls.
- **QOL_Backend**: Platform-neutral or app-service work that improves user convenience, continuity, responsiveness, error recovery, personalization, and operational clarity without deleting or reducing existing features.
- **Phase5_Evidence**: Verification output under `TestResults/phase5-evidence/`, including screenshots, interaction notes, perf measurements, accessibility notes, and command output.

## Requirements

### Requirement 1: Professional visual direction

**User Story:** As a user, I want the viewer to feel like a mature Windows image application, so that it appears intentionally designed instead of assembled from default boilerplate.

#### 1: Acceptance Criteria

1. THE Viewer SHALL replace default-looking boilerplate chrome with a source-controlled visual system covering color, typography, spacing, density, radius, border, opacity, and elevation.
2. THE Viewer SHALL document the intended visual direction before implementation in a Phase 5 design audit.
3. THE Viewer SHALL preserve an image-first canvas where the loaded image remains the primary visual focus.
4. THE Viewer SHALL use a restrained, production-quality palette that is not dominated by one hue family.
5. THE Viewer SHALL NOT rely on blank gradients, overused glow effects, decorative orbs, bokeh blobs, or other rudimentary filler treatments as primary design work.
6. THE Viewer SHALL include before-and-after visual evidence for the main viewer, empty state, command palette, metadata panel, thumbnail strip, grid, filmstrip, comparison, GIF controls, error states, and search overlay.

### Requirement 2: Sourced material and asset discipline

**User Story:** As a maintainer, I want UI decisions backed by source material and traceable assets, so that Phase 5 improves quality without arbitrary styling.

#### 2: Acceptance Criteria

1. THE Repository SHALL include a Phase 5 source-material register listing each external design source, library, asset, or sample used.
2. THE source-material register SHALL record the reason for use, license status, source URL or local source, and implementation location.
3. THE Viewer SHALL prefer WinUI-native controls, Fluent-compatible patterns, and source-controlled assets before adding new dependencies.
4. IF a third-party UI, icon, animation, or asset package is added, THEN the Repository SHALL document why a WinUI-native or source-controlled alternative was insufficient.
5. THE Viewer SHALL NOT hotlink remote visual assets at runtime.
6. THE Viewer SHALL keep generated or captured visual evidence under `TestResults/phase5-evidence/` unless explicitly source-controlled as design input.

### Requirement 3: Design token system

**User Story:** As a developer, I want UI styling expressed through coherent tokens, so that future polish is consistent and testable.

#### 3: Acceptance Criteria

1. THE Viewer SHALL define Phase 5 design tokens for color, typography, spacing, radius, border, opacity, panel materials, icon sizing, animation duration, easing, and density.
2. THE Viewer SHALL support light and dark themes without duplicating unrelated styling logic.
3. THE Viewer SHALL expose a high-contrast/accessibility path that does not depend only on color or transparency.
4. THE Viewer SHALL replace hard-coded repeated styling values in Phase 5-touched views with named resources.
5. THE token system SHALL include tests or static validation that required token keys exist in light and dark dictionaries.
6. THE token system SHALL be documented in the Phase 5 design document.

### Requirement 4: Professional chrome and layout refinement

**User Story:** As a user, I want controls to feel polished, discoverable, and out of the way, so that the viewer supports repeated image browsing without visual clutter.

#### 4: Acceptance Criteria

1. THE Viewer SHALL redesign top chrome, bottom information, transient badges, panels, and overlays as a coherent application shell.
2. THE Viewer SHALL keep controls dense, legible, and aligned for repeated use rather than using marketing-style hero layouts or oversized decorative cards.
3. THE Viewer SHALL make the empty state useful and visually finished without using blank gradients or decorative filler.
4. THE Viewer SHALL ensure text does not clip, overlap, or overflow in supported desktop window sizes.
5. THE Viewer SHALL keep cards and panels purposeful; page sections and tool surfaces SHALL NOT be nested cards inside cards.
6. THE Viewer SHALL provide visual states for idle, hover, pressed, selected, focused, disabled, loading, warning, and error where applicable.

### Requirement 5: Motion and transition system

**User Story:** As a user, I want motion to clarify what changed, so that navigation, overlays, and mode switches feel continuous without distraction.

#### 5: Acceptance Criteria

1. THE Viewer SHALL define a Motion_System with named durations, easing curves, reduced-motion rules, and allowed animation categories.
2. THE Viewer SHALL add or refine transitions for chrome fade, image navigation, command palette open/close, search overlay, metadata panel, thumbnail strip, grid entry/exit, filmstrip entry/exit, comparison mode, GIF frame controls, loading, and errors.
3. THE Viewer SHALL prefer purposeful connected, entrance, reposition, opacity, scale, and translation animations over arbitrary decorative motion.
4. THE Viewer SHALL avoid animations that obscure image inspection, delay critical input, or make keyboard navigation feel slower.
5. THE Viewer SHALL provide a reduced-motion setting or platform-respecting reduced-motion behavior.
6. THE Motion_System SHALL be validated with manual evidence and automated tests where timing/state transitions are service-driven.

### Requirement 6: Interaction quality and input ergonomics

**User Story:** As a user, I want the app to respond predictably to keyboard, mouse, and pointer input, so that expert and casual use both feel smooth.

#### 6: Acceptance Criteria

1. THE Viewer SHALL preserve all Phase 1 through Phase 4 shortcuts unless requirements, design, tasks, and evidence are updated.
2. THE Viewer SHALL improve focus visuals, keyboard traversal, and overlay dismissal behavior for all Phase 5-touched controls.
3. THE Viewer SHALL keep command palette, search, menus, overlays, and panel interactions accessible from keyboard.
4. THE Viewer SHALL provide clear pointer feedback for clickable, draggable, selected, focused, and disabled states.
5. THE Viewer SHALL prevent mode conflicts between slideshow, grid, filmstrip, comparison, search, metadata, command palette, and fullscreen.
6. THE Viewer SHALL include regression tests for input routing changed by Phase 5.

### Requirement 7: Accessibility and readability

**User Story:** As a user, I want text, controls, contrast, and motion to remain readable and usable, so that the viewer works across normal desktop conditions.

#### 7: Acceptance Criteria

1. THE Viewer SHALL meet documented contrast targets for Phase 5 UI text, icons, borders, focus indicators, and critical state indicators.
2. THE Viewer SHALL keep UI text readable at supported window sizes and display scale factors.
3. THE Viewer SHALL define accessible names or automation identifiers for Phase 5-touched interactive controls.
4. THE Viewer SHALL support keyboard-only operation for Phase 5-touched controls.
5. THE Viewer SHALL avoid communicating state only through color, glow, transparency, or animation.
6. THE Viewer SHALL include accessibility evidence under `TestResults/phase5-evidence/`.

### Requirement 8: Backend QOL continuity

**User Story:** As a returning user, I want the app to remember useful context safely, so that common workflows require less repeated setup.

#### 8: Acceptance Criteria

1. THE Viewer SHALL improve session continuity without introducing accounts, cloud sync, cataloging, albums, tagging, or telemetry.
2. THE Viewer SHALL preserve and extend local settings safely under `%LOCALAPPDATA%\spitfire.img`.
3. THE Viewer SHALL support at least one new local QOL continuity feature such as last session restore, last folder recall, layout-per-folder preference, pinned local shortcut, or safer recent-file recovery.
4. THE Viewer SHALL handle missing, partial, invalid, or older settings without data loss.
5. THE Viewer SHALL include migration or normalization tests for any settings changes.
6. THE Viewer SHALL NOT remove existing settings, recent files, playlist, or navigation behavior to make QOL work easier.

### Requirement 9: Backend QOL responsiveness

**User Story:** As a user browsing folders, I want the app to feel faster and more informative, so that large folders, thumbnails, metadata, and animated content do not interrupt viewing.

#### 9: Acceptance Criteria

1. THE Viewer SHALL improve responsiveness for at least one existing workflow: startup, folder playlist scanning, thumbnail loading, metadata loading, image navigation, search, command palette, GIF loading, grid, or filmstrip.
2. THE improvement SHALL be achieved by additive service design such as caching, preloading, cancellation, progress state, batching, priority queues, or better error recovery.
3. THE improvement SHALL NOT be achieved by removing supported formats, reducing playlist size, disabling thumbnails, bypassing metadata, or hiding errors.
4. THE Viewer SHALL expose user-visible progress or status when operations take long enough to affect interaction.
5. THE Viewer SHALL include performance or behavioral evidence before and after the QOL change.
6. THE Viewer SHALL bound memory use and cancellation behavior for any cache, queue, or preload mechanism.

### Requirement 10: Backend QOL error recovery and user trust

**User Story:** As a user, I want failures to be recoverable and understandable, so that corrupt files, missing files, codec gaps, and large folders do not break my session.

#### 10: Acceptance Criteria

1. THE Viewer SHALL improve user-facing error recovery for at least one existing failure path.
2. THE Viewer SHALL preserve structured logging and support bundle redaction behavior.
3. THE Viewer SHALL provide actionable UI copy for recoverable errors without exposing raw stack traces.
4. THE Viewer SHALL keep the current playlist/session usable after a recoverable file error when technically possible.
5. THE Viewer SHALL include tests for any modified error path.
6. THE Viewer SHALL NOT hide errors silently to create the appearance of polish.

### Requirement 11: Validation evidence and review gate

**User Story:** As a maintainer, I want Phase 5 completion to be evidence-backed, so that visual polish and QOL are not declared complete from intent alone.

#### 11: Acceptance Criteria

1. THE Phase5_Gate SHALL run `dotnet build SpitfireImg.sln -p:Platform=x64`.
2. THE Phase5_Gate SHALL run `dotnet test SpitfireImg.sln -p:Platform=x64`.
3. THE Phase5_Gate SHALL capture screenshots or visual evidence for all Phase 5-touched primary surfaces.
4. THE Phase5_Gate SHALL include manual interaction evidence for motion, reduced motion, keyboard navigation, and accessibility.
5. THE Phase5_Gate SHALL include backend QOL evidence showing before/after behavior or measurable improvement.
6. THE Phase5_Gate SHALL write evidence under `TestResults/phase5-evidence/`.

### Requirement 12: Phase 5 scope boundary

**User Story:** As a maintainer, I want Phase 5 to stay focused on professional UI and user QOL, so that scope does not drift into unrelated product areas.

#### 12: Acceptance Criteria

1. THE Phase 5 implementation SHALL NOT add cloud sync, accounts, telemetry, cataloging, albums, tagging, or social features.
2. THE Phase 5 implementation SHALL NOT add new image editing workflows beyond existing Phase 2 transform behavior unless a later phase updates requirements, design, and tasks.
3. THE Phase 5 implementation SHALL NOT remove, simplify away, hide, or disable existing features to make UI or QOL completion easier.
4. THE Phase 5 implementation SHALL preserve folder-as-playlist behavior.
5. THE Phase 5 implementation SHALL preserve WinUI 3 and Windows App SDK as the app platform.
6. THE Phase 5 implementation SHALL complete only when all required task-list items are verified against source, tests, and evidence.
