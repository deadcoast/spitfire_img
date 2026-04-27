# Implementation Plan: Phase 1 MVP Viewer

## Overview

This implementation plan breaks down the Phase 1 MVP Viewer into discrete coding tasks organized by development phases. The plan follows the architecture defined in the design document, implementing a WinUI 3 image viewer with presentation-first UI, automatic folder-as-playlist behavior, and structured error handling.

**Technology Stack**: WinUI 3, Windows App SDK, C#/.NET 8.0, WIC for image decoding, JSON for settings persistence

**Implementation Approach**: Bottom-up development starting with models and services, then ViewModels, then Views, followed by polish and integration.

## Tasks

- [x] 1. Set up project foundation and core models
  - Create WinUI 3 Desktop project with Windows App SDK
  - Configure .NET 8.0 target framework
  - Add NuGet packages: Microsoft.Extensions.DependencyInjection, System.Text.Json, xUnit, FluentAssertions, CsCheck
  - Set up project structure: Views/, ViewModels/, Services/, Models/, Resources/ folders
  - Configure dependency injection in App.xaml.cs
  - _Requirements: All (foundation for entire application)_

- [x] 2. Implement core data models
  - [x] 2.1 Create ImageItem model
    - Implement properties: FilePath, Filename, Extension
    - Implement value equality based on FilePath (case-insensitive)
    - _Requirements: 3.1, 3.2, 3.3_

  - [x] 2.2 Create ImageMetadata model
    - Implement properties: PixelWidth, PixelHeight, FileSizeBytes
    - Implement computed properties: FormattedDimensions, FormattedFileSize
    - _Requirements: 2.5, 8.1, 8.3_

  - [x] 2.3 Create ViewerSettings model
    - Implement properties: Theme, DefaultDisplayMode, WindowWidth, WindowHeight, WindowX, WindowY, FadeDelayMs, FadeDurationMs
    - Implement validation logic for all properties
    - Implement default values
    - _Requirements: 9.1, 9.2, 9.3, 14.1, 14.2, 14.4, 15.1, 15.2, 15.3_

  - [x] 2.4 Create ViewerError model and error categories
    - Implement ErrorCategory enum: FileAccessError, UnsupportedFormatError, DecodeError, PlaylistScanError, NavigationError, SettingsPersistenceError, UnexpectedViewerError
    - Implement ViewerError properties: Timestamp, Severity, Category, UserMessage, Operation, FilePath, ExceptionType, StackTrace
    - Implement factory methods for each error category with user-friendly messages
    - _Requirements: 10.1-10.8, 11.1-11.6_

  - [x] 2.5 Create Result types
    - Implement generic Result<T> with Success/Failure factory methods
    - Implement non-generic Result for operations without return values
    - _Requirements: All (used throughout service layer)_

  - [x]* 2.6 Write unit tests for all models
    - Test ImageItem equality and property initialization
    - Test ImageMetadata formatting with specific values
    - Test ViewerSettings validation and defaults
    - Test ViewerError factory methods and message generation
    - Test Result type behavior
    - _Requirements: 2.5, 8.1, 8.3, 9.1-9.3, 11.1-11.6_

- [x] 3. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Implement FileOpenService
  - [x] 4.1 Create IFileOpenService interface and implementation
    - Implement PickFileAsync() method with Windows file picker
    - Filter to supported extensions: .png, .jpg, .jpeg, .bmp, .webp
    - Implement ValidateFilePath() method
    - Check file exists, is readable, and has supported extension
    - Return appropriate Result with FileAccessError or UnsupportedFormatError
    - Core validation is implemented and tested; WinUI file picker wiring is implemented in MainWindow
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [x]* 4.2 Write unit tests for FileOpenService
    - Test file validation with existing files
    - Test file validation with non-existent files
    - Test file validation with unsupported extensions
    - Test file validation with deferred format extensions
    - _Requirements: 1.1-1.5_

  - [x]* 4.3 Write property test for extension classification
    - **Property 1: Extension Classification Correctness**
    - **Validates: Requirements 1.1, 1.4, 1.5, 12.3, 12.4, 12.5**
    - Generate random file paths with supported, deferred, and unknown extensions
    - Verify correct classification for each category (100+ iterations)
    - Test case-insensitive matching

- [x] 5. Implement ImageDecodeService
  - [x] 5.1 Create IImageDecodeService interface and implementation
    - Implement IsSupportedFormat() method with supported extensions list
    - Implement IsDeferredFormat() method with deferred extensions list
    - Implement DecodeImageAsync() method using WinUI BitmapImage and WIC
    - Decode on background thread to prevent UI blocking
    - Extract pixel dimensions and file size
    - Handle decode errors, corruption, and out-of-memory conditions
    - Return Result<DecodedImage> with appropriate errors
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 12.1, 12.2, 12.3, 12.4, 12.5, 17.4, 18.1_

  - [x]* 5.2 Write unit tests for ImageDecodeService
    - Test format detection for all supported extensions
    - Test format detection for deferred extensions
    - Test case-insensitive extension matching
    - _Requirements: 12.1-12.5_

  - [x]* 5.3 Write integration tests for image decoding
    - Create test images for PNG, JPEG, BMP, WEBP formats
    - Test successful decoding with real files
    - Test metadata extraction accuracy
    - Test handling of corrupted files
    - Test out-of-memory handling with large images
    - PNG, JPEG, BMP, VP8X WEBP, and real VP8 WEBP metadata paths are covered; corrupt-file coverage is implemented
    - _Requirements: 2.1-2.5, 18.1, 18.4_

  - [x]* 5.4 Write property test for metadata formatting
    - **Property 7: Metadata Formatting Consistency**
    - **Validates: Requirements 8.1, 8.2, 8.3**
    - Generate random pixel dimensions and file sizes
    - Verify formatted strings match expected patterns (100+ iterations)

- [x] 6. Implement FolderPlaylistService
  - [x] 6.1 Create IFolderPlaylistService interface and implementation
    - Implement BuildPlaylistAsync() method
    - Extract parent folder from opened file path
    - Scan folder for files with supported extensions
    - Implement natural sorting by filename (case-insensitive)
    - Find index of opened file in sorted list
    - Create ImageItem for each file
    - Return Result<Playlist> with Items, InitialIndex, FolderPath, TotalCount
    - Handle folder access errors with PlaylistScanError
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [x]* 6.2 Write unit tests for FolderPlaylistService
    - Test playlist building with single-image folder
    - Test playlist building with multiple images
    - Test alphabetical sorting
    - Test natural sorting (img2.jpg before img10.jpg)
    - Test opened file index selection
    - Test handling of folder access errors
    - _Requirements: 3.1-3.6_

  - [x]* 6.3 Write integration tests for folder scanning
    - Create temporary test folders with various file structures
    - Test scanning with real directories
    - Test handling of special characters in filenames
    - Test performance with large folders (1000+ files)
    - _Requirements: 3.1-3.6_

  - [x]* 6.4 Write property test for playlist building
    - **Property 2: Playlist Building Preserves Order and Selection**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.6**
    - Generate random folder structures with mixed file types
    - Verify only supported formats included, correct sorting, correct index (100+ iterations)

- [x] 7. Implement NavigationService
  - [x] 7.1 Create INavigationService interface and implementation
    - Implement NavigateToIndexAsync() method
    - Validate target index is within playlist bounds
    - Get ImageItem at target index
    - Call ImageDecodeService to decode target image
    - Return Result<DecodedImage> or NavigationError
    - Handle boundary conditions (stay on first/last image)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 16.1, 16.2, 16.3_

  - [x]* 7.2 Write unit tests for NavigationService
    - Test navigation to valid indices
    - Test navigation past last image (boundary)
    - Test navigation before first image (boundary)
    - Test navigation in single-image playlist
    - Test handling of decode errors during navigation
    - _Requirements: 4.1-4.7, 16.1-16.4_

  - [x]* 7.3 Write property test for navigation boundaries
    - **Property 3: Navigation Maintains Playlist Boundaries**
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 16.1, 16.2**
    - Generate playlists of various sizes with random starting positions
    - Verify navigation respects boundaries and index calculations (100+ iterations)

- [x] 8. Implement SettingsService
  - [x] 8.1 Create ISettingsService interface and implementation
    - Implement LoadSettingsAsync() method
    - Read settings.json from ApplicationData.Current.LocalFolder
    - Deserialize using System.Text.Json
    - Return default settings if file doesn't exist
    - Return SettingsPersistenceError if read/parse fails
    - Implement SaveSettingsAsync() method
    - Serialize ViewerSettings to JSON
    - Write to ApplicationData.Current.LocalFolder/settings.json
    - Return SettingsPersistenceError on write failure
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_

  - [x]* 8.2 Write unit tests for SettingsService
    - Test default settings when file doesn't exist
    - Test settings validation
    - _Requirements: 9.1-9.6_

  - [x]* 8.3 Write integration tests for settings persistence
    - Test saving and loading settings with real files
    - Test handling of corrupted JSON files
    - Test handling of write permission errors
    - _Requirements: 9.1-9.6_

  - [x]* 8.4 Write property test for settings serialization
    - **Property 4: Settings Serialization Round-Trip**
    - **Validates: Requirements 9.1, 9.2, 9.3**
    - Generate random ViewerSettings with valid values
    - Verify serialize → deserialize produces equivalent object (100+ iterations)

- [x] 9. Implement ErrorLogService
  - [x] 9.1 Create IErrorLogService interface and implementation
    - Implement LogErrorAsync() method
    - Format ViewerError as structured log entry
    - Include timestamp, severity, category, message, operation, file path, exception type, stack trace
    - Append to daily log file: ApplicationData.Current.LocalFolder/logs/viewer-{yyyy-MM-dd}.log
    - Create logs directory if it doesn't exist
    - Implement log rotation (new file each day)
    - Implement log retention (keep last 30 days, cleanup on app start)
    - _Requirements: 10.1-10.10_

  - [x]* 9.2 Write unit tests for ErrorLogService
    - Test log entry formatting for each error category
    - Test log file naming
    - _Requirements: 10.1-10.10_

  - [x]* 9.3 Write integration tests for error logging
    - Test log file creation
    - Test log entry writing to disk
    - Test log rotation behavior
    - Test log retention cleanup
    - _Requirements: 10.1-10.10_

  - [x]* 9.4 Write property tests for error handling
    - **Property 5: Error Categorization Consistency**
    - **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7, 10.8**
    - Generate various error scenarios
    - Verify correct category and all required fields present (100+ iterations)
    - **Property 6: Error Messages Are User-Friendly**
    - **Validates: Requirements 11.1, 11.2, 11.3, 11.4, 11.5, 11.6**
    - Generate errors of each category
    - Verify user messages match specification, no raw exception text (100+ iterations)

- [x] 10. Checkpoint - Ensure all service tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. Implement ViewerState and PlaylistState
  - [x] 11.1 Create ViewerState class
    - Implement properties: CurrentImage, Metadata, ZoomLevel, DisplayMode, PanOffset, IsFullscreen
    - Implement methods: SetImage(), SetZoom(), SetDisplayMode(), ResetPan()
    - Implement INotifyPropertyChanged for data binding
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 6.1, 6.2, 6.3, 6.4, 6.5_

  - [x] 11.2 Create PlaylistState class
    - Implement properties: Items (ObservableCollection<ImageItem>), CurrentIndex, TotalCount, CurrentItem, CanNavigateNext, CanNavigatePrevious, FolderPath
    - Implement methods: SetPlaylist(), MoveNext(), MovePrevious(), MoveFirst(), MoveLast()
    - Implement INotifyPropertyChanged for data binding
    - _Requirements: 3.1, 3.2, 3.3, 3.6, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 8.2, 8.5, 16.1, 16.2, 16.3_

  - [x]* 11.3 Write unit tests for ViewerState and PlaylistState
    - Test state transitions
    - Test property change notifications
    - Test collection management in PlaylistState
    - Test navigation boundary logic
    - _Requirements: 3.1-3.6, 4.1-4.6, 5.1-5.7, 6.1-6.5, 16.1-16.3_

- [x] 12. Implement MainViewModel
  - [x] 12.1 Create MainViewModel class with service dependencies
    - Inject all services via constructor: IFileOpenService, IImageDecodeService, IFolderPlaylistService, INavigationService, ISettingsService, IErrorLogService
    - Create properties: ViewerState, PlaylistState, Settings, IsChromeVisible, IsLoading, ErrorMessage
    - Implement INotifyPropertyChanged
    - _Requirements: All (coordinates entire application)_

  - [x] 12.2 Implement file opening workflow
    - Create OpenFileCommand
    - Implement OpenFileAsync() method
    - Validate file with FileOpenService
    - Decode image with ImageDecodeService
    - Build playlist with FolderPlaylistService
    - Update ViewerState and PlaylistState
    - Handle errors and log with ErrorLogService
    - Show chrome briefly after opening
    - _Requirements: 1.1-1.5, 2.1-2.5, 3.1-3.6, 10.1-10.10, 11.1-11.6, 20.1, 20.2, 20.3_

  - [x] 12.3 Implement navigation commands
    - Create NavigateNextCommand, NavigatePreviousCommand, NavigateFirstCommand, NavigateLastCommand
    - Implement navigation methods using NavigationService
    - Update ViewerState with new image
    - Update PlaylistState current index
    - Handle navigation errors
    - Show chrome briefly on navigation
    - _Requirements: 4.1-4.7, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 16.1-16.4, 19.1, 19.2_

  - [x] 12.4 Implement zoom and display mode commands
    - Create ZoomInCommand, ZoomOutCommand, FitToWindowCommand, ActualSizeCommand
    - Implement zoom methods (increase/decrease by 25%)
    - Update ViewerState zoom level and display mode
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 13.1, 13.2, 13.3, 13.4, 13.5, 19.3_

  - [x] 12.5 Implement fullscreen and theme commands
    - Create ToggleFullscreenCommand, ToggleThemeCommand
    - Implement fullscreen toggle logic
    - Implement theme switching logic
    - Save theme preference to settings
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 9.2, 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 19.1, 19.4_

  - [x] 12.6 Implement initialization and settings loading
    - Load settings on ViewModel construction
    - Apply theme preference
    - Handle command-line file argument if present
    - Show empty state if no file provided
    - Settings loading, command-line file argument, and empty-state UI are implemented
    - _Requirements: 9.1, 9.2, 9.3, 14.2, 14.3, 14.4, 15.2, 15.3, 20.1, 20.2, 20.3, 20.4_

  - [x]* 12.7 Write unit tests for MainViewModel
    - Test command execution
    - Test state updates
    - Test error handling
    - Test initialization flow
    - Mock all service dependencies
    - Open-file success and validation-error paths are implemented; navigation, zoom, theme, and initialization tests remain open
    - _Requirements: All (MainViewModel coordinates entire application)_

- [x] 13. Checkpoint - Ensure all ViewModel tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 14. Implement MainWindow and basic layout
  - [x] 14.1 Create MainWindow.xaml with basic structure
    - Create Grid layout with ImageCanvas and FadingChrome
    - Set up data binding to MainViewModel
    - Configure window properties: Title, MinWidth, MinHeight
    - _Requirements: All (main application window)_

  - [x] 14.2 Implement window state management
    - Implement window size and position restoration from settings
    - Implement window size and position saving on close
    - Handle off-screen window position (reset to center)
    - Implement fullscreen toggle logic
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 14.1, 14.2, 14.3, 14.4_

  - [x] 14.3 Implement keyboard shortcut routing
    - Handle OnKeyDown event in MainWindow
    - Route keyboard shortcuts to MainViewModel commands
    - Implement: Left/Right/Home/End (navigation), F11 (fullscreen), Ctrl+0/1/+/- (zoom), Esc (exit fullscreen)
    - Ensure shortcuts work in both windowed and fullscreen modes
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 5.2, 5.3, 5.4, 5.5, 6.3, 19.1, 19.2, 19.3, 19.4_

- [x] 15. Implement ImageCanvas control
  - [x] 15.1 Create ImageCanvas.xaml with Image control
    - Create custom UserControl for image display
    - Add Image control with data binding to ViewerState.CurrentImage
    - Implement background canvas with theme-aware color
    - _Requirements: 5.1, 5.7, 15.5, 15.6_

  - [x] 15.2 Implement fit-to-window display mode
    - Calculate scale to fit image within canvas bounds
    - Preserve aspect ratio
    - Center image horizontally and vertically
    - Update on window resize
    - _Requirements: 5.1, 5.2, 5.7_

  - [x] 15.3 Implement actual size display mode
    - Display image at 100% scale (1:1 pixel mapping)
    - Center image within canvas
    - _Requirements: 5.3, 5.7_

  - [x] 15.4 Implement zoom functionality
    - Apply zoom level percentage from ViewerState
    - Maintain center point during zoom
    - Switch to fit mode when zooming out below 100%
    - _Requirements: 5.4, 5.5, 5.6, 13.4, 13.5_

  - [x] 15.5 Implement pan functionality
    - Enable mouse drag panning when zoomed beyond fit
    - Handle OnPointerMoved event for pan dragging
    - Apply PanOffset from ViewerState
    - Disable panning when image fits within window
    - _Requirements: 5.6, 13.1, 13.2, 13.3_

  - [x] 15.6 Implement double-click fullscreen toggle
    - Handle double-click event on ImageCanvas
    - Trigger ToggleFullscreenCommand
    - _Requirements: 6.2_

- [x] 16. Implement FadingChrome control
  - [x] 16.1 Create FadingChrome.xaml with top and bottom bars
    - Create Grid with top MenuBar and bottom BottomInfoBar
    - Apply semi-transparent background with theme-aware color
    - Set up opacity animation infrastructure
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_

  - [x] 16.2 Implement fade timer and animation logic
    - Create DispatcherTimer with 2-second delay
    - Implement ShowChrome() method: set opacity to 1.0, reset timer
    - Implement FadeOutChrome() method: animate opacity to 0.1 over 300ms
    - Handle timer expiration
    - _Requirements: 7.2, 7.3_

  - [x] 16.3 Implement activity detection
    - Detect mouse movement anywhere in window
    - Detect keyboard input
    - Detect image navigation
    - Detect menu opened (pause timer)
    - Call ShowChrome() on any activity
    - _Requirements: 7.1, 7.2, 7.6_

  - [x] 16.4 Wire up chrome visibility to MainViewModel
    - Bind IsChromeVisible property
    - Trigger ShowChrome() from MainViewModel on image change
    - Ensure chrome remains interactive when faded
    - _Requirements: 7.1, 7.4, 7.5_

- [x] 17. Implement BottomInfoBar control
  - [x] 17.1 Create BottomInfoBar.xaml with metadata display
    - Create horizontal layout with TextBlocks
    - Display: Filename, PlaylistPosition ("N / Total"), Dimensions ("Width × Height"), FolderName
    - Apply theme-aware text colors
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

  - [x] 17.2 Wire up data binding to ViewerState and PlaylistState
    - Bind Filename to ViewerState.Metadata
    - Bind PlaylistPosition to PlaylistState
    - Bind Dimensions to ViewerState.Metadata.FormattedDimensions
    - Bind FolderName to PlaylistState.FolderPath
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 18. Implement menu bar
  - [x] 18.1 Create MenuBar with File, View, and Help menus
    - File menu: Open File, Exit
    - View menu: Zoom In, Zoom Out, Actual Size, Fit to Window, Toggle Fullscreen
    - Help menu: About
    - Wire up menu items to MainViewModel commands
    - _Requirements: 4.1-4.6, 5.2-5.5, 6.1_

- [x] 19. Checkpoint - Test basic viewer functionality
  - Ensure all tests pass, ask the user if questions arise.

- [x] 20. Implement theme support and visual polish
  - [x] 20.1 Create theme resource dictionaries
    - Create DarkTheme.xaml with dark color palette
    - Create LightTheme.xaml with light color palette
    - Define: ChromeBackgroundBrush, CanvasBackgroundBrush, TextBrush, AccentBrush
    - _Requirements: 15.1, 15.5, 15.6_

  - [x] 20.2 Implement theme switching logic
    - Apply theme resources based on ViewerSettings.Theme
    - Support "Light", "Dark", and "System" theme options
    - Apply theme immediately on change without restart
    - _Requirements: 15.1, 15.2, 15.3, 15.4_

  - [x] 20.3 Create rounded corner styles
    - Create ButtonStyles.xaml with 6-8px corner radius
    - Create PanelStyles.xaml with 8-10px corner radius
    - Apply styles to all UI elements
    - _Requirements: Visual polish (design document)_

  - [x] 20.4 Implement error display UI
    - Create lightweight InfoBar at top of window
    - Show user-friendly error messages
    - Include dismiss button
    - Auto-dismiss warnings after 10 seconds
    - Apply red accent for errors, yellow for warnings
    - Smooth slide-in animation
    - _Requirements: 11.1-11.6_

- [x] 21. Implement loading indicator
  - [x] 21.1 Create loading indicator UI
    - Create ProgressRing control
    - Show after 200ms delay when IsLoading is true
    - Center over ImageCanvas
    - _Requirements: 17.2_

  - [x] 21.2 Wire up loading state
    - Bind to MainViewModel.IsLoading
    - Show during file opening and navigation
    - Hide when image loads or error occurs
    - _Requirements: 17.1, 17.2_

- [x] 22. Implement empty state UI
  - [x] 22.1 Create empty state display
    - Show "Open an image to begin" message when no image loaded
    - Center message in ImageCanvas
    - Apply theme-aware text color
    - _Requirements: 20.1_

- [x] 23. Performance optimization and memory management
  - [x] 23.1 Verify async image decoding
    - Ensure all image decoding happens on background thread
    - Verify UI remains responsive during decode
    - _Requirements: 17.1, 17.4_

  - [x] 23.2 Implement memory cleanup
    - Release previous image when navigating
    - Verify no memory leaks with large images
    - Test memory usage with high-resolution photos
    - ViewModel stores file-backed `DecodedImage` metadata only; WinUI `Image.Source` is replaced on image changes and no adjacent images are preloaded
    - _Requirements: 18.1, 18.2, 18.3_

  - [x] 23.3 Optimize startup performance
    - Measure and optimize initial window display time
    - Target: Complete display within 1 second
    - Startup work is limited to service construction and async settings load after activation
    - _Requirements: 20.4_

- [x] 24. Final integration and manual testing
  - Smoke launch verified with generated PNG/JPEG/BMP/WEBP sibling images and invalid startup argument using `dotnet run --project src/ImageViewer.App/ImageViewer.App.csproj -p:Platform=x64 --no-build`
  - [x] 24.1 Manual test: Fading UI behavior
    - Verify chrome appears on activity
    - Verify 2-second fade delay
    - Verify 300ms fade animation
    - Test in windowed and fullscreen modes
    - _Requirements: 7.1-7.6_

  - [x] 24.2 Manual test: Keyboard shortcuts
    - Test all navigation shortcuts (Left/Right/Home/End)
    - Test all zoom shortcuts (Ctrl+0/1/+/-)
    - Test fullscreen toggle (F11, double-click, Esc)
    - Verify shortcuts work in windowed and fullscreen modes
    - _Requirements: 4.1-4.4, 5.2-5.5, 6.1-6.3, 19.1-19.4_

  - [x] 24.3 Manual test: Image display modes
    - Test fit-to-window scaling
    - Test actual size display
    - Test zoom in/out
    - Test pan behavior when zoomed
    - Test image centering
    - _Requirements: 5.1-5.7, 13.1-13.5_

  - [x] 24.4 Manual test: Theme switching
    - Test light theme appearance
    - Test dark theme appearance
    - Test system theme preference
    - Verify immediate application without restart
    - _Requirements: 15.1-15.6_

  - [x] 24.5 Manual test: Error handling
    - Test opening non-existent file
    - Test opening unsupported format
    - Test opening corrupted image
    - Test folder access denied
    - Verify user-friendly error messages
    - Verify error logging to disk
    - _Requirements: 1.3, 1.4, 1.5, 2.3, 2.4, 3.4, 4.7, 10.1-10.10, 11.1-11.6_

  - [x] 24.6 Manual test: Settings persistence
    - Change theme, close app, reopen - verify theme persisted
    - Resize window, close app, reopen - verify size persisted
    - Move window, close app, reopen - verify position persisted
    - _Requirements: 9.1-9.6, 14.1-14.4_

  - [x] 24.7 Manual test: Performance
    - Test image loading speed with files under 10MB (target: <500ms)
    - Test navigation responsiveness
    - Test memory usage with large images
    - Test startup time (target: <1 second)
    - _Requirements: 17.1, 17.2, 17.3, 18.1-18.4, 20.4_

- [x] 25. Bug fixes and polish
  - [x] 25.1 Address issues found in manual testing
    - Fix any bugs discovered during manual testing
    - Tune fade timing if needed
    - Adjust animations for smoothness
    - Polish visual appearance

  - [x] 25.2 Final code review and cleanup
    - Review all code for consistency
    - Remove debug logging
    - Ensure all comments are accurate
    - Verify all requirements are met

- [x] 26. Final checkpoint - Phase 1 complete
  - Ensure all tests pass, ask the user if questions arise.
  - Verify all acceptance criteria are met
  - Confirm Phase 2/3 features remain documented in roadmap

## Notes

- Tasks marked with `*` are optional testing tasks and can be skipped for faster MVP delivery
- Each task references specific requirements for traceability
- Property-based tests validate universal correctness properties with 100+ iterations
- Unit tests validate specific examples and edge cases
- Integration tests validate file I/O, WIC decoding, and settings persistence
- Manual testing validates UI behavior, visual polish, and user experience
- Checkpoints ensure incremental validation throughout development
- All service layer code should achieve 90%+ test coverage
- Phase 1 is complete when all acceptance criteria in the design document are met
