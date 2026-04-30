# Implementation Plan: Phase 3 Advanced Viewing Modes

## Overview

This implementation plan converts the Phase 3 Advanced Viewing Modes design into discrete coding tasks for spitfire.img. Phase 3 adds slideshow mode with transitions, image comparison capabilities, command palette with fuzzy search, animated GIF playback, HEIC/AVIF format support with codec detection, and custom layout modes (grid and filmstrip).

All tasks build incrementally on Phase 1 MVP Viewer and Phase 2 Browse And Quality Of Life foundations, maintaining architectural consistency with service separation, Result types, and async operations.

## Tasks

- [x] 1. Implement slideshow mode foundation
  - Create SlideshowService with timing and state management
  - Create SlideshowState ViewModel for playback state
  - Implement slideshow activation, pause, resume, and stop
  - Add slideshow commands to MainViewModel
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

- [x]* 1.1 Write property test for slideshow timing accuracy
  - **Property 1: Slideshow Timing Accuracy**
  - **Validates: Requirements 1.2, 2.1, 2.4**

- [x]* 1.2 Write property test for slideshow looping behavior
  - **Property 2: Slideshow Looping Behavior**
  - **Validates: Requirements 1.7**

- [x]* 1.3 Write property test for slideshow timer reset on manual navigation
  - **Property 3: Slideshow Timer Reset on Manual Navigation**
  - **Validates: Requirements 1.8**

- [x] 2. Implement slideshow timing configuration
  - Add slideshow interval configuration (1s to 60s)
  - Implement interval stepping with Plus/Minus keys
  - Add slideshow settings to ViewerSettings model
  - Persist slideshow preferences via SettingsService
  - Display interval changes in Chrome UI
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_

- [x]* 2.1 Write property test for slideshow interval stepping
  - **Property 4: Slideshow Interval Stepping**
  - **Validates: Requirements 2.6, 2.7**

- [x] 3. Implement slideshow transition effects
  - Create TransitionEffect enum (None, Fade, Slide, Zoom)
  - Implement None transition (instant swap)
  - Implement Fade transition (300ms fade out/in)
  - Implement Slide transition (400ms slide from right)
  - Implement Zoom transition (350ms zoom out with fade)
  - Add transition selection to slideshow settings
  - Render transitions at 60 FPS with hardware acceleration
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9_

- [x]* 3.1 Write property test for transition navigation blocking
  - **Property 5: Transition Navigation Blocking**
  - **Validates: Requirements 3.8**

- [x]* 3.2 Write integration tests for slideshow transitions
  - Test all transition effects with real images
  - Verify transition timing accuracy
  - Test transition cancellation on manual navigation
  - Test memory usage during transitions

- [x] 4. Checkpoint - Ensure slideshow mode tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement comparison mode foundation
  - Create ComparisonService with layout calculations
  - Create ComparisonState ViewModel for comparison state
  - Create ComparisonView XAML component
  - Implement comparison mode activation with Ctrl+D
  - Implement comparison mode exit with Escape
  - Add comparison commands to MainViewModel
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9_

- [x]* 5.1 Write property test for comparison synchronized navigation
  - **Property 6: Comparison Synchronized Navigation**
  - **Validates: Requirements 4.4, 4.5**

- [x] 6. Implement side-by-side comparison layout
  - Create ComparisonLayout enum (SideBySide, Overlay)
  - Implement 2-image side-by-side layout (50/50 split)
  - Implement 3-image side-by-side layout (33/33/33 split)
  - Implement 4-image side-by-side layout (2×2 grid)
  - Add Ctrl+2, Ctrl+3, Ctrl+4 shortcuts for image count
  - Implement image selection with click and Tab key
  - Preserve aspect ratios for all displayed images
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9_

- [x]* 6.1 Write property test for aspect ratio preservation in layouts
  - **Property 7: Aspect Ratio Preservation in Layouts**
  - **Validates: Requirements 5.6, 14.6**

- [x] 7. Implement overlay comparison layout
  - Implement overlay layout with image layering
  - Add opacity slider control (0-100%)
  - Implement opacity adjustment with Up/Down arrows
  - Implement layer swapping with S key
  - Align images by center point for different dimensions
  - Add Ctrl+Shift+D shortcut for overlay mode
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9_

- [x]* 7.1 Write property test for overlay opacity range
  - **Property 8: Overlay Opacity Range**
  - **Validates: Requirements 6.3, 6.4, 6.5**

- [x] 8. Implement synchronized zoom and pan in comparison mode
  - Implement zoom synchronization across all displayed images
  - Implement pan synchronization with relative coordinates
  - Add Ctrl+L shortcut to toggle sync zoom
  - Convert absolute positions to relative (0-1 range)
  - Handle different image dimensions gracefully
  - Display sync indicator in Chrome when enabled
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_

- [x]* 8.1 Write property test for synchronized zoom with relative positioning
  - **Property 9: Synchronized Zoom with Relative Positioning**
  - **Validates: Requirements 7.1, 7.2, 7.6**

- [x]* 8.2 Write integration tests for comparison layouts
  - Test side-by-side with 2, 3, 4 images
  - Test overlay with different image dimensions
  - Test synchronized zoom with real images
  - Test memory usage with multiple images

- [x] 9. Checkpoint - Ensure comparison mode tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Implement command palette foundation
  - Create CommandPaletteService with command registry
  - Create CommandPaletteState ViewModel
  - Create CommandPalette XAML overlay component
  - Implement palette activation with Ctrl+Shift+P
  - Implement palette close with Escape and click outside
  - Build command registry with Phase 1, 2, and 3 commands
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8_

- [x] 11. Implement command palette fuzzy search
  - Create CommandItem model with Id, Title, Category, Shortcut
  - Implement fuzzy matching algorithm with character order
  - Implement match score calculation and ranking
  - Update filtered results within 50ms of input
  - Highlight matched characters in results
  - Display "No matching commands found" for empty results
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8, 9.9, 9.10_

- [x]* 11.1 Write property test for fuzzy matching character order
  - **Property 10: Fuzzy Matching Character Order**
  - **Validates: Requirements 9.2**

- [x]* 11.2 Write property test for fuzzy match ranking consistency
  - **Property 11: Fuzzy Match Ranking Consistency**
  - **Validates: Requirements 9.5**

- [x] 12. Implement command palette execution and recent commands
  - Implement command execution on Enter key
  - Track recent commands (last 10)
  - Display recent commands at top of palette
  - Implement keyboard navigation with Up/Down arrows
  - Group commands by category with headers
  - Display keyboard shortcuts next to commands
  - _Requirements: 18.1, 18.2, 18.3, 18.4, 18.5_

- [x]* 12.1 Write integration tests for command palette
  - Test fuzzy search with real command list
  - Test command execution
  - Test recent commands persistence
  - Test performance with 200+ commands

- [x] 13. Checkpoint - Ensure command palette tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 14. Implement animated GIF detection and loading
  - Create AnimationService with frame extraction
  - Create AnimatedGif model with frames and delays
  - Create AnimationState ViewModel for playback state
  - Implement GIF frame extraction using WIC decoder
  - Extract frame delays from GIF metadata
  - Detect animated vs static GIFs (frame count > 1)
  - Cache all frames in memory at load time
  - _Requirements: 10.1, 10.2, 10.3, 10.8, 10.10_

- [x] 15. Implement GIF animation playback
  - Implement automatic animation playback on load
  - Implement play/pause toggle with P key
  - Respect frame delays embedded in GIF
  - Implement animation looping (last frame → first frame)
  - Implement frame advance with Period key (when paused)
  - Implement frame rewind with Comma key (when paused)
  - Display current frame number and total in Chrome
  - _Requirements: 10.4, 10.5, 10.6, 10.7, 10.9_

- [x]* 15.1 Write property test for GIF frame timing accuracy
  - **Property 12: GIF Frame Timing Accuracy**
  - **Validates: Requirements 10.3**

- [x]* 15.2 Write property test for GIF animation looping
  - **Property 13: GIF Animation Looping**
  - **Validates: Requirements 10.8**

- [x] 16. Implement GIF frame control UI
  - Create FrameControl XAML component
  - Add play/pause button with icon toggle
  - Add previous frame button
  - Add next frame button
  - Add frame slider for seeking
  - Display frame controls in Chrome when GIF is active
  - Hide frame controls for non-animated images
  - Apply Chrome fade rules to frame controls
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 11.9_

- [x]* 16.1 Write integration tests for GIF animation
  - Load real animated GIFs with various frame counts
  - Verify frame extraction and delay parsing
  - Test playback timing with real GIFs
  - Test with single-frame GIFs

- [x] 17. Checkpoint - Ensure GIF animation tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 18. Implement codec detection service
  - Create CodecDetectionService with registry checks
  - Create CodecAvailability model
  - Implement HEIC codec detection at startup
  - Implement AVIF codec detection at startup
  - Cache codec availability results for session
  - Implement Store URL generation for codecs
  - Handle registry access errors gracefully
  - _Requirements: 12.1, 12.7, 12.8, 13.1, 13.7, 13.10_

- [x]* 18.1 Write property test for codec detection caching
  - **Property 18: Codec Detection Caching**
  - **Validates: Requirements 20.5**

- [x] 19. Implement HEIC format support with codec detection
  - Extend ImageDecodeService with HEIC support
  - Add ".heic" to supported formats when codec available
  - Implement CodecUnavailableError for missing HEIC codec
  - Display codec unavailable dialog with Store link
  - Implement Store page opening on user confirmation
  - Update FolderPlaylistService to include/exclude HEIC based on codec
  - Don't repeat codec prompt during same session
  - _Requirements: 12.2, 12.3, 12.4, 12.5, 12.6, 12.9, 12.10_

- [x] 20. Implement AVIF format support with codec detection
  - Extend ImageDecodeService with AVIF support
  - Add ".avif" to supported formats when codec available
  - Implement CodecUnavailableError for missing AVIF codec
  - Display codec unavailable dialog with Store link
  - Update FolderPlaylistService to include/exclude AVIF based on codec
  - Handle independent codec detection for HEIC and AVIF
  - _Requirements: 13.2, 13.3, 13.4, 13.5, 13.8, 13.9_

- [ ]* 20.1 Write property test for codec-dependent playlist inclusion
  - **Property 14: Codec-Dependent Playlist Inclusion**
  - **Validates: Requirements 12.9, 12.10, 13.9, 13.10**

- [ ]* 20.2 Write integration tests for codec detection
  - Test codec detection on systems with/without codecs
  - Verify Store URL generation
  - Test caching behavior
  - Test with mock registry access

- [ ] 21. Checkpoint - Ensure codec detection tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 22. Implement grid layout mode
  - Create LayoutService with grid calculations
  - Create LayoutState ViewModel
  - Create GridLayout XAML component
  - Create LayoutConfig model with grid settings
  - Implement grid layout activation with Ctrl+G
  - Implement default 3×3 grid layout
  - Calculate grid cell dimensions and spacing
  - Load images from playlist into grid cells
  - Preserve aspect ratios for all grid images
  - _Requirements: 14.1, 14.2, 14.3, 14.6_

- [ ] 23. Implement grid layout density adjustment
  - Implement grid density increase with Ctrl+Plus
  - Implement grid density decrease with Ctrl+Minus
  - Support grid sizes from 2×2 to 6×6
  - Implement image selection with click
  - Implement Enter key to view selected image full-screen
  - Implement grid scrolling to load additional images
  - _Requirements: 14.4, 14.5, 14.7, 14.8, 14.9, 14.10_

- [ ]* 23.1 Write property test for grid layout cell distribution
  - **Property 15: Grid Layout Cell Distribution**
  - **Validates: Requirements 14.2, 14.3**

- [ ] 24. Implement filmstrip layout mode
  - Create FilmstripView XAML component
  - Implement filmstrip activation with Ctrl+F (in layout context)
  - Divide canvas into focus area (75%) and thumbnail strip (25%)
  - Display current image in focus area
  - Display 7-11 thumbnails in horizontal strip
  - Reuse ThumbnailService from Phase 2 for thumbnails
  - _Requirements: 15.1, 15.2, 15.6, 15.8_

- [ ] 25. Implement filmstrip navigation and scrolling
  - Implement thumbnail click to change focus image
  - Implement Left/Right arrow navigation in filmstrip
  - Scroll thumbnail strip to keep current image centered
  - Implement Escape to exit filmstrip mode
  - Apply zoom only to focus area, not thumbnail strip
  - Display current image index and total in Chrome
  - _Requirements: 15.3, 15.4, 15.5, 15.7, 15.9, 15.10_

- [ ]* 25.1 Write property test for filmstrip thumbnail centering
  - **Property 16: Filmstrip Thumbnail Centering**
  - **Validates: Requirements 15.5**

- [ ]* 25.2 Write integration tests for custom layouts
  - Test grid layout with various sizes
  - Test filmstrip with real thumbnails
  - Test layout switching
  - Test memory usage with many images

- [ ] 26. Checkpoint - Ensure custom layout tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 27. Implement layout persistence
  - Add layout preferences to ViewerSettings
  - Save layout mode on layout change
  - Restore saved layout on app start
  - Default to single-image view if no preference saved
  - Add Ctrl+1 shortcut to return to single-image view
  - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5_

- [ ] 28. Implement slideshow in advanced modes
  - Implement slideshow in comparison mode (advance both images)
  - Implement slideshow in grid layout (advance all visible images)
  - Implement slideshow in filmstrip mode (advance focus image)
  - Use same timing and transition settings across all modes
  - Continue slideshow when switching layouts
  - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5_

- [ ]* 28.1 Write property test for slideshow in comparison mode
  - **Property 17: Slideshow in Comparison Mode**
  - **Validates: Requirements 17.1, 17.2**

- [ ] 29. Extend error handling for Phase 3
  - Add new error categories to ViewerError enum
  - Implement SlideshowError factory method
  - Implement TransitionError factory method
  - Implement ComparisonError factory method
  - Implement SyncError factory method
  - Implement CommandPaletteError factory method
  - Implement ExecutionError factory method
  - Implement AnimationError factory method
  - Implement CodecDetectionError factory method
  - Implement CodecUnavailableError factory method
  - Implement LayoutError factory method
  - Add user-friendly error messages for all new categories
  - Extend ErrorLogService with Phase 3 error logging
  - _Requirements: 20.1, 20.2, 20.3, 20.4, 20.5, 20.6, 20.7_

- [ ] 30. Implement graceful degradation strategies
  - Fall back to instant transitions if transition fails
  - Reduce image count in comparison if memory insufficient
  - Display first frame as static if GIF animation fails
  - Exclude unsupported formats from playlist when codec unavailable
  - Reduce grid density if memory insufficient
  - Fall back to single-image view for critical layout failures
  - _Requirements: Error Handling section_

- [ ] 31. Optimize performance for advanced modes
  - Preload next image during slideshow
  - Load comparison images in parallel
  - Cache fuzzy match scores in command palette
  - Debounce command palette search input (50ms)
  - Extract all GIF frames at load time
  - Use virtualization for grid layout
  - Implement progressive loading for layouts
  - _Requirements: 19.1, 19.2, 19.3, 19.4, 19.5, 19.6, 19.7_

- [ ] 32. Integrate Phase 3 features with MainViewModel
  - Add slideshow commands to MainViewModel
  - Add comparison commands to MainViewModel
  - Add command palette commands to MainViewModel
  - Add animation commands to MainViewModel
  - Add layout commands to MainViewModel
  - Wire all keyboard shortcuts to commands
  - Update Chrome UI with Phase 3 indicators
  - _Requirements: Integration section_

- [ ] 33. Update settings persistence with Phase 3 preferences
  - Add SlideshowInterval to ViewerSettings
  - Add SlideshowTransition to ViewerSettings
  - Add PreferredLayout to ViewerSettings
  - Add GridRows and GridColumns to ViewerSettings
  - Add ComparisonSyncZoom to ViewerSettings
  - Add GifAutoPlay to ViewerSettings
  - Serialize and deserialize Phase 3 settings via SettingsService
  - _Requirements: Extended ViewerSettings section_

- [ ] 34. Final checkpoint - Ensure all Phase 3 tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 35. Manual testing and polish
  - Verify slideshow timing accuracy and transitions
  - Verify comparison mode layouts and sync zoom
  - Verify command palette fuzzy search and responsiveness
  - Verify GIF animation playback and frame controls
  - Verify codec detection and Store link functionality
  - Verify grid and filmstrip layouts
  - Verify performance targets (transitions < 400ms, comparison < 600ms, search < 50ms)
  - Verify UI polish and Chrome fade behavior
  - Verify all keyboard shortcuts work correctly
  - Test with large playlists (1000+ images)
  - Test with high-resolution images (4K+)
  - Test with animated GIFs (100+ frames)
  - _Requirements: Manual Testing section_

## Notes

- Tasks marked with `*` are optional property-based and integration tests
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at major milestones
- Property tests validate universal correctness properties from the design
- Integration tests validate real-world scenarios with actual files
- All Phase 3 features build on Phase 1 and Phase 2 foundations
- Service separation and Result types maintained throughout
- Performance targets: slideshow transitions < 400ms, comparison < 600ms, command palette search < 50ms
- All new error categories logged with structured information
- Graceful degradation ensures features fail safely without blocking core functionality
