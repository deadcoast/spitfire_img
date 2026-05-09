# Implementation Plan: Phase 5 Professional UI And User QOL

## Overview

This implementation plan converts the Phase 5 Professional UI And User QOL requirements and design into required development tasks.

Phase 5 is dedicated only to professional frontend UI development, sourced visual material, visual transitions, animation quality, and backend user quality-of-life improvements. All tasks must preserve existing features. Quality of life must never be achieved by removing, simplifying away, hiding, disabling, or narrowing existing behavior.

Generated visual evidence belongs under `TestResults/phase5-evidence/` and must not be committed unless explicitly promoted to source-controlled design input.

## Tasks

- [x] 1. Create Phase 5 source-material register
  - Create `docs/phase-5-professional-ui-qol/source-material.md`
  - Record Fluent 2, Microsoft Learn motion guidance, WinUI Gallery, Windows App SDK docs, and any additional asset/library sources used
  - Include source URL or local path, license or usage status, reason for use, implementation target, and verification evidence path
  - Verify no runtime hotlinking of remote visual assets is introduced
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [x] 2. Create current UI visual audit
  - Create `docs/phase-5-professional-ui-qol/visual-audit.md`
  - Audit main viewer, empty state, top chrome, bottom info bar, command palette, search overlay, metadata panel, thumbnail strip, grid, filmstrip, comparison, GIF controls, loading, warning, and error states
  - Identify boilerplate styling, hard-coded styling, text-overflow risk, contrast risk, weak state styling, and animation gaps
  - Capture before screenshots under `TestResults/phase5-evidence/visual/before/`
  - _Requirements: 1.2, 1.6, 4.1, 4.6, 7.1_

- [x] 3. Define Phase 5 design token dictionaries
  - Create or update token dictionaries for color, typography, spacing, radius, border, opacity, density, icon sizing, motion duration, and easing
  - Add `TextStyles.xaml`, `MotionTokens.xaml`, and `LayoutTokens.xaml` if the design requires separate dictionaries
  - Keep light and dark values complete and intentional
  - Add token validation tests that required resource keys exist in both themes
  - _Requirements: 3.1, 3.2, 3.4, 3.5_

- [x] 4. Replace Phase 5-touched hard-coded styling with named resources
  - Review all Phase 5-touched XAML for repeated colors, spacing, radius, opacity, and sizing values
  - Move reusable values into named resources instead of local literals
  - Preserve local values only when they are unique to a single layout and documented by context
  - Verify light and dark themes compile and render
  - _Requirements: 3.2, 3.4, 4.4_

- [x] 5. Redesign app chrome shell
  - Redesign top chrome/menu surface, bottom information bar, transient badges, and idle/active chrome states
  - Extract `AppChrome.xaml` or focused subcomponents if `FadingChrome.xaml` becomes too broad
  - Add visual states for idle, active, hover, pressed, focused, disabled, selected, loading, warning, and error where applicable
  - Capture after screenshots for chrome states
  - _Requirements: 1.1, 1.3, 4.1, 4.2, 4.6_

- [x] 6. Redesign empty, loading, warning, and error states
  - Replace default or sparse empty-state presentation with a finished image-viewer surface
  - Add polished loading and cancellation states without blocking image inspection
  - Improve warning and error presentation with actionable copy and visible recovery affordances where applicable
  - Do not use blank gradients, decorative orbs, bokeh, or glow as the design solution
  - _Requirements: 1.5, 4.3, 7.5, 10.1, 10.3_

- [x] 7. Redesign command palette
  - Improve command palette layout, typography, command grouping, shortcut display, focus states, selected state, empty search state, and keyboard readability
  - Preserve existing command registry and Phase 1 through Phase 4 shortcuts
  - Add or update tests for command palette behavior affected by Phase 5
  - Capture before/after visual evidence
  - _Requirements: 4.1, 4.4, 6.1, 6.2, 6.3, 7.4_

- [x] 8. Redesign search overlay and metadata panel
  - Improve search input, results, selected state, empty state, keyboard traversal, and dismissal
  - Improve metadata panel layout, text hierarchy, overflow handling, and readability
  - Add automation names or identifiers for Phase 5-touched interactive controls
  - Capture after screenshots in light and dark themes
  - _Requirements: 4.1, 4.4, 6.2, 6.3, 7.2, 7.3_

- [x] 9. Redesign thumbnail strip, grid, and filmstrip surfaces
  - Improve item spacing, selected/current item state, focus state, loading state, thumbnail failure state, and density
  - Preserve folder-as-playlist behavior and existing layout shortcuts
  - Verify large playlist behavior remains intact
  - Capture visual evidence for normal and large-folder scenarios
  - _Requirements: 4.1, 4.2, 6.1, 6.5, 9.1_

- [x] 10. Redesign comparison and animated GIF controls
  - Improve side-by-side comparison, overlay comparison, active image state, sync zoom indicators, overlay opacity state, and GIF frame controls
  - Preserve comparison and animation keyboard shortcuts
  - Ensure the controls remain dense and image-first
  - Capture visual evidence for comparison and 100-frame GIF scenarios
  - _Requirements: 4.1, 4.6, 5.2, 6.1, 7.2_

- [ ] 11. Implement Phase 5 motion token system
  - Define named motion durations, easing, and transition categories in XAML resources or a focused motion service
  - Document allowed motion categories and prohibited decorative motion in `visual-audit.md` or a dedicated motion section
  - Add reduced-motion handling or setting design before applying broad animation changes
  - Add tests for service-driven motion configuration where possible
  - _Requirements: 5.1, 5.3, 5.5, 5.6_

- [ ] 12. Implement chrome and overlay transitions
  - Refine chrome fade/reveal behavior
  - Add consistent entrance/exit transitions for command palette, search overlay, metadata panel, status HUD, loading, and error surfaces
  - Verify keyboard input remains responsive during animations
  - Record motion evidence under `TestResults/phase5-evidence/motion/`
  - _Requirements: 5.2, 5.3, 5.4, 6.3_

- [ ] 13. Implement mode transition polish
  - Add or refine transitions for single image, grid, filmstrip, comparison, and GIF control state changes
  - Use connected, entrance, reposition, opacity, scale, or translation animations only where they clarify continuity
  - Avoid transitions that delay critical navigation or obscure image inspection
  - Record normal-motion and reduced-motion evidence
  - _Requirements: 5.2, 5.3, 5.4, 5.5_

- [ ] 14. Improve focus, keyboard, and pointer ergonomics
  - Review all Phase 5-touched controls for focus visuals, keyboard traversal, pointer hover, pressed state, and disabled state
  - Add regression tests for changed input routing
  - Verify command palette, search, menus, overlays, metadata, grid, filmstrip, comparison, GIF controls, and fullscreen mode do not conflict
  - Record keyboard-only evidence
  - _Requirements: 6.2, 6.3, 6.4, 6.5, 6.6_

- [ ] 15. Complete accessibility and readability pass
  - Verify contrast for Phase 5-touched text, icons, borders, focus indicators, warning, error, and selected states
  - Verify no state is communicated only by color, glow, transparency, or animation
  - Verify text does not clip or overlap at supported desktop window sizes and common display scale factors
  - Save accessibility evidence under `TestResults/phase5-evidence/accessibility/`
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_

- [ ] 16. Design and implement first backend QOL continuity feature
  - Select one concrete local-only continuity feature from the design: last session restore, last folder recall, layout-per-folder preference, pinned local shortcut, or safer recent-file recovery
  - Implement it through additive settings/service code
  - Add migration and invalid-settings tests
  - Verify no existing settings, recent-file, playlist, or navigation behavior is removed or weakened
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [ ] 17. Design and implement backend QOL responsiveness improvement
  - Select one concrete responsiveness improvement: bounded preloading, smarter thumbnail scheduling, metadata batching, progress state, operation cancellation, or cache improvement
  - Implement bounded memory and cancellation behavior
  - Add tests for queue/cache/cancellation/progress behavior where applicable
  - Capture before/after behavioral or performance evidence under `TestResults/phase5-evidence/qol/`
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_

- [ ] 18. Improve backend QOL error recovery
  - Select one recoverable error path such as corrupt image, missing file, codec unavailable, failed thumbnail, failed metadata, or failed navigation target
  - Preserve structured logging and support bundle redaction behavior
  - Keep playlist/session usable after recoverable errors when technically possible
  - Add tests for the modified error path
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6_

- [x] 19. Add Phase 5 visual and QOL evidence template
  - Create `docs/phase-5-professional-ui-qol/phase5-evidence-template.md`
  - Include sections for visual before/after, source-material register, token validation, motion, reduced motion, accessibility, keyboard, backend QOL before/after, build, test, and known issues
  - Use `TestResults/phase5-evidence/` as the evidence output folder
  - _Requirements: 1.6, 2.1, 5.6, 7.6, 9.5, 11.3, 11.4, 11.5, 11.6_

- [ ] 20. Run Phase 5 visual review checkpoint
  - Compare before and after evidence for all primary surfaces
  - Confirm the result does not rely on blank gradients, overused glow effects, decorative orbs, bokeh blobs, or one-note palette filler
  - Confirm text, focus, hover, selected, loading, warning, and error states are represented
  - Save review output under `TestResults/phase5-evidence/visual/visual-review.md`
  - _Requirements: 1.1, 1.4, 1.5, 1.6, 4.6, 7.1, 7.2_

- [ ] 21. Run Phase 5 backend QOL checkpoint
  - Verify selected continuity, responsiveness, and error-recovery QOL features against source and tests
  - Confirm QOL work did not remove, simplify away, hide, disable, or narrow existing behavior
  - Confirm no cloud sync, accounts, telemetry, cataloging, albums, tagging, or social features were introduced
  - Save review output under `TestResults/phase5-evidence/qol/qol-checkpoint.md`
  - _Requirements: 8.6, 9.3, 10.6, 12.1, 12.2, 12.3_

- [ ] 22. Run Phase 5 gate
  - Run `Stop-Process -Name ImageViewer.App -Force -ErrorAction SilentlyContinue`
  - Run `dotnet build SpitfireImg.sln -p:Platform=x64`
  - Run `dotnet test SpitfireImg.sln -p:Platform=x64`
  - Verify token validation, source-material validation, visual evidence, motion evidence, accessibility evidence, and QOL evidence exist
  - Save command output and final checkpoint under `TestResults/phase5-evidence/phase5-gate/`
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_

- [ ] 23. Final Phase 5 scope checkpoint
  - Confirm Phase 5 stayed dedicated only to professional UI development and backend user QOL
  - Confirm no existing feature was removed or simplified away for convenience
  - Confirm folder-as-playlist behavior remains intact
  - Confirm Phase 1 through Phase 4 shortcuts and release behavior remain intact unless requirements were updated
  - Confirm source material, design docs, task list, tests, and evidence are reconciled
  - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6_

## Notes

- The checklist intentionally contains only required tasks. Phase 5 is broad enough without completion ambiguity.
- Professional UI completion requires source, implementation, screenshots, interaction evidence, and verification.
- Backend QOL completion requires a real user-facing improvement, tests, and before/after evidence.
- Generated Phase 5 evidence belongs under `TestResults/phase5-evidence/` and must not be committed.
- Existing features are not bargaining chips for polish; removal or simplification does not count as quality of life.
