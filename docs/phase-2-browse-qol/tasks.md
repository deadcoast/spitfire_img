# Implementation Plan: Phase 2 Browse And Quality Of Life

## Overview

This implementation plan breaks down the Phase 2 Browse and Quality of Life features into discrete coding tasks organized by development phases. The plan follows the architecture defined in the design document, building upon the Phase 1 MVP Viewer foundation to add search, thumbnails, recent files, drag-and-drop, export, transforms, enhanced metadata, and GIF/TIFF format support.

**Technology Stack**: WinUI 3, Windows App SDK, C#/.NET 8.0, WIC for image decoding and thumbnails, JSON for settings persistence

**Implementation Approach**: Bottom-up development starting with models and services, then ViewModels, then Views, followed by polish and integration. All new features integrate with Phase 1 services without breaking changes.

## Tasks

- [x] 1. Implement Phase 2 data models
  - [x] 1.1 Create Thumbnail model
    - Implement properties: Item (ImageItem), Image (BitmapImage), SizeBytes, LastAccessed
    - _Requirements: 2.3, 2.4, 13.1_

  - [x] 1.2 Create RecentFile model
    - Implement properties: FilePath, LastOpened, Filename (computed), FolderPath (computed)
    - Implement computed properties using Path methods
    - _Requirements: 3.1, 3.2, 3.10, 12.1, 12.2, 12.3_

  - [x] 1.3 Create ExifMetadata model
    - Implement camera properties: CameraMake, CameraModel, Software
    - Implement capture properties: CaptureDate, Iso, Aperture, ShutterSpeed, FocalLength, FlashFired
    - Implement GPS properties: Latitude, Longitude, Altitude
    - Implement image properties: ColorSpace
    - Implement computed properties: HasCameraInfo, HasCaptureSettings, HasGpsInfo, FormattedAperture, FormattedShutterSpeed, FormattedFocalLength, FormattedGpsCoordinates
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.9, 18.2, 18.3_

  - [x] 1.4 Create TransformOperation enum
    - Define operations: RotateLeft, RotateRight, FlipHorizontal, FlipVertical
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [x] 1.5 Create ExportOptions model
    - Implement properties: TargetFormat (ImageFormat enum), JpegQuality, PreserveExif, OverwriteExisting
    - Define ImageFormat enum: Png, Jpeg, Bmp, Webp
    - Implement default values: JpegQuality=90, PreserveExif=true, OverwriteExisting=false
    - _Requirements: 5.3, 14.1, 14.2, 14.3, 14.5_

  - [x] 1.6 Extend ViewerSettings model
    - Add Phase 2 properties: ThumbnailStripVisible, ThumbnailStripPosition (enum), DefaultJpegQuality, PreserveExifOnExport
    - Define ThumbnailPosition enum: Top, Bottom
    - Implement default values
    - _Requirements: 11.1, 11.5, 11.6, 14.3_

  - [x] 1.7 Extend ViewerError model
    - Add Phase 2 error categories: ThumbnailGenerationError, ExportError, TransformError, TransformSaveError, MetadataExtractionError
    - Implement factory methods for each new category with user-friendly messages
    - _Requirements: 5.6, 5.7, 6.9, 6.10, 7.10, 13.5_

  - [x]* 1.8 Write unit tests for all new models
    - Test Thumbnail property initialization
    - Test RecentFile computed properties
    - Test ExifMetadata formatted properties and has-data checks
    - Test ExportOptions defaults and validation
    - Test ViewerSettings Phase 2 properties
    - Test ViewerError Phase 2 factory methods
    - _Requirements: All model requirements_

- [x] 2. Checkpoint - Ensure all model tests pass
  - Ensure all tests pass, ask the user if questions arise.


- [x] 3. Implement SearchService
  - [x] 3.1 Create ISearchService interface and implementation
    - Implement FilterPlaylist() method with case-insensitive substring matching
    - Return full playlist when query is empty or whitespace
    - Return Result<List<ImageItem>> with filtered list
    - Handle errors gracefully (fallback to full playlist)
    - _Requirements: 1.2, 1.3, 1.9, 16.1, 16.2, 16.3_

  - [x]* 3.2 Write unit tests for SearchService
    - Test filtering with various search queries
    - Test empty query returns full playlist
    - Test case-insensitive matching
    - Test special characters in filenames
    - Test Unicode filenames
    - _Requirements: 1.2, 1.3, 1.9, 16.1, 16.2_

  - [x]* 3.3 Write property test for search filtering
    - **Property 1: Search Filtering Correctness**
    - **Validates: Requirements 1.1, 1.3, 1.9**
    - Generate random playlists (10-1000 items) with various filename patterns
    - Generate random search queries
    - Verify all results contain query substring (case-insensitive)
    - Verify empty query returns full playlist (100+ iterations)

  - [x]* 3.4 Write property test for search clear round-trip
    - **Property 2: Search Clear Round-Trip**
    - **Validates: Requirements 1.9**
    - Generate random playlists and search queries
    - Verify Filter → Clear → Result equals original playlist (100+ iterations)

- [x] 4. Implement ThumbnailService
  - [x] 4.1 Create IThumbnailService interface and implementation
    - Implement GetThumbnailAsync() with cache check and generation
    - Implement ClearCache() method
    - Implement GetCacheSize() method
    - Implement LRU cache with 100MB limit
    - Generate 120px thumbnails preserving aspect ratio using WIC
    - Use background thread for generation
    - _Requirements: 2.2, 2.3, 2.4, 2.8, 13.1, 13.2, 13.3, 13.4_

  - [x] 4.2 Implement thumbnail generation algorithm
    - Load source image using WIC BitmapDecoder
    - Calculate scale factor for 120px longest dimension
    - Create scaled bitmap using BitmapTransform
    - Encode to PNG in memory
    - Create BitmapImage from memory stream
    - Return Thumbnail with size tracking
    - _Requirements: 2.3, 13.4_

  - [x] 4.3 Implement LRU cache
    - Create ThumbnailCache class with Dictionary and LinkedList
    - Implement Get() with LRU list update
    - Implement Add() with eviction when exceeding 100MB
    - Track cache size in bytes
    - _Requirements: 2.4, 13.1, 13.3_

  - [x]* 4.4 Write unit tests for ThumbnailService
    - Test cache hit/miss behavior
    - Test cache eviction logic
    - Test cache size tracking
    - _Requirements: 2.4, 13.1, 13.3_

  - [x]* 4.5 Write integration tests for thumbnail generation
    - Generate thumbnails from real images (PNG, JPEG, BMP, WEBP, GIF, TIFF)
    - Verify dimensions (120px longest)
    - Verify aspect ratio preservation
    - Test with various image sizes
    - _Requirements: 2.3, 13.4_

  - [x]* 4.6 Write property test for thumbnail scaling
    - **Property 3: Thumbnail Scaling Preserves Aspect Ratio**
    - **Validates: Requirements 2.3**
    - Generate random image dimensions (1×1 to 10000×10000)
    - Verify longest dimension = 120px
    - Verify aspect ratio preserved within 1px tolerance (100+ iterations)

  - [x]* 4.7 Write property test for thumbnail caching
    - **Property 4: Thumbnail Caching Avoids Regeneration**
    - **Validates: Requirements 2.4**
    - Generate random image items
    - Verify second request is cache hit (100+ iterations)

  - [x]* 4.8 Write property test for thumbnail cache LRU
    - **Property 10: Thumbnail Cache LRU Eviction**
    - **Validates: Requirements 13.3**
    - Generate thumbnails totaling > 100MB
    - Verify LRU item evicted, cache size ≤ 100MB (100+ iterations)

- [x] 5. Implement RecentFilesService
  - [x] 5.1 Create IRecentFilesService interface and implementation
    - Implement LoadRecentFilesAsync() with JSON deserialization
    - Implement AddRecentFileAsync() with duplicate handling and 20-item limit
    - Implement RemoveRecentFileAsync()
    - Implement ClearRecentFilesAsync()
    - Store in ApplicationData.Current.LocalFolder/recent-files.json
    - _Requirements: 3.1, 3.2, 3.3, 3.7, 3.10, 12.1, 12.2, 12.3, 12.4, 12.5_

  - [x]* 5.2 Write unit tests for RecentFilesService
    - Test add with duplicate handling
    - Test 20-item capacity management
    - Test remove operation
    - Test clear operation
    - _Requirements: 3.1, 3.2, 3.3, 12.1, 12.2_

  - [x]* 5.3 Write integration tests for recent files persistence
    - Test saving and loading with real JSON files
    - Test handling of corrupted JSON
    - Test handling of invalid paths
    - _Requirements: 3.7, 3.10, 12.1, 12.2, 12.3, 12.4_

  - [x]* 5.4 Write property test for recent files duplicates
    - **Property 5: Recent Files Duplicate Handling**
    - **Validates: Requirements 3.2**
    - Generate random recent files lists (0-20 items)
    - Verify adding duplicate moves to index 0, appears exactly once (100+ iterations)

  - [x]* 5.5 Write property test for recent files capacity
    - **Property 6: Recent Files Capacity Management**
    - **Validates: Requirements 3.3**
    - Generate lists with exactly 20 items
    - Verify result has 20 items, oldest removed, new at index 0 (100+ iterations)

- [x] 6. Checkpoint - Ensure all service tests pass
  - Ensure all tests pass, ask the user if questions arise.


- [x] 7. Implement ExportService
  - [x] 7.1 Create IExportService interface and implementation
    - Implement ExportImageAsync() with format conversion support
    - Support PNG, JPEG, BMP, WEBP target formats
    - Apply JPEG quality settings (default 90%)
    - Preserve EXIF data when supported by target format
    - Handle overwrite confirmation
    - Return Result with ExportError on failure
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.8, 14.1, 14.2, 14.3, 14.4, 14.5_

  - [x]* 7.2 Write unit tests for ExportService
    - Test format conversion logic
    - Test quality settings
    - Test overwrite handling
    - _Requirements: 5.2, 5.3, 5.4, 14.1, 14.2, 14.3_

  - [x]* 7.3 Write integration tests for export operations
    - Export real images to different formats
    - Verify output file format and quality
    - Verify EXIF preservation
    - Test overwrite behavior
    - _Requirements: 5.1, 5.2, 5.3, 14.1, 14.2, 14.5_

  - [x]* 7.4 Write property test for export format conversion
    - **Property 11: Export Format Conversion Correctness**
    - **Validates: Requirements 14.2**
    - Generate random source/target format pairs
    - Verify output file has correct format (100+ iterations)

  - [x]* 7.5 Write property test for export EXIF preservation
    - **Property 12: Export EXIF Preservation**
    - **Validates: Requirements 14.5**
    - Generate images with various EXIF data
    - Verify EXIF present in output for JPEG/PNG (100+ iterations)

- [x] 8. Implement TransformService
  - [x] 8.1 Create ITransformService interface and implementation
    - Implement ApplyTransformAsync() using WIC transformations
    - Implement SaveTransformsAsync() with file overwrite and EXIF update
    - Support RotateLeft, RotateRight, FlipHorizontal, FlipVertical operations
    - Apply transforms in-memory without modifying original file
    - Return Result with TransformError or TransformSaveError on failure
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.8, 6.9, 15.1, 15.2_

  - [x]* 8.2 Write unit tests for TransformService
    - Test each transform operation
    - Test transform combinations
    - Test save behavior
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.8_

  - [x]* 8.3 Write integration tests for transform operations
    - Apply transforms to real images
    - Save transforms to disk
    - Verify file modifications
    - Test EXIF orientation updates
    - _Requirements: 6.5, 6.6, 6.8, 6.11_

  - [x]* 8.4 Write property test for transform non-destructive
    - **Property 7: Transform Non-Destructive Until Save**
    - **Validates: Requirements 6.6**
    - Generate random images and transform sequences
    - Verify file hash unchanged after applying transforms (before save) (100+ iterations)

  - [x]* 8.5 Write property test for transform dimensions
    - **Property 8: Transform Dimension Updates**
    - **Validates: Requirements 6.11**
    - Generate random dimensions and transform operations
    - Verify rotations swap dimensions, flips preserve dimensions (100+ iterations)

  - [x]* 8.6 Write property test for transform undo/redo
    - **Property 13: Transform Undo/Redo Round-Trip**
    - **Validates: Requirements 15.3, 15.4**
    - Generate random transform sequences and undo/redo counts
    - Verify Undo → Redo restores state (100+ iterations)

- [x] 9. Implement MetadataService
  - [x] 9.1 Create IMetadataService interface and implementation
    - Implement ExtractExifAsync() using Windows.Graphics.Imaging
    - Extract camera info: Make, Model, Software
    - Extract capture settings: DateTime, ISO, Aperture, ShutterSpeed, FocalLength, Flash
    - Extract GPS info: Latitude, Longitude, Altitude
    - Extract image properties: ColorSpace
    - Handle missing/malformed EXIF gracefully (return empty metadata, not error)
    - Return MetadataExtractionError only for file read failures
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.9, 7.10_

  - [x]* 9.2 Write unit tests for MetadataService
    - Test EXIF extraction with mock data
    - Test graceful handling of missing EXIF
    - Test graceful handling of malformed EXIF
    - _Requirements: 7.9, 7.10_

  - [x]* 9.3 Write integration tests for EXIF extraction
    - Extract EXIF from real photos with various cameras
    - Verify specific tag values
    - Test with images lacking EXIF
    - Test with malformed EXIF
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.10_

  - [x]* 9.4 Write property test for EXIF graceful degradation
    - **Property 9: EXIF Extraction Graceful Degradation**
    - **Validates: Requirements 7.10**
    - Generate images with no EXIF, partial EXIF, malformed EXIF
    - Verify all return success with ExifMetadata (possibly empty) (100+ iterations)

- [x] 10. Extend ImageDecodeService for GIF and TIFF
  - [x] 10.1 Update format detection lists
    - Add ".gif" to SupportedExtensions
    - Add ".tiff" and ".tif" to SupportedExtensions
    - Update IsSupportedFormat() to recognize new formats
    - _Requirements: 8.1, 8.4, 8.5, 9.1, 9.4, 9.5, 20.1, 20.2, 20.4_

  - [x] 10.2 Implement GIF decoding
    - Decode first frame only using WIC GIF decoder
    - Extract dimensions and file size
    - Route through existing DecodeImageAsync() method
    - Return DecodeError on failure
    - _Requirements: 8.1, 8.2, 8.3, 8.6, 8.7_

  - [x] 10.3 Implement TIFF decoding
    - Decode first page only using WIC TIFF decoder
    - Support common TIFF variants (uncompressed, LZW, JPEG)
    - Extract dimensions and file size
    - Route through existing DecodeImageAsync() method
    - Return DecodeError on failure
    - _Requirements: 9.1, 9.2, 9.3, 9.6, 9.7, 9.8_

  - [x] 10.4 Add GetFrameCountAsync() method (for future animation support)
    - Define method signature in interface
    - Implement basic frame count extraction for GIF
    - Return 1 for non-animated formats
    - _Requirements: Phase 3 preparation_

  - [x]* 10.5 Write unit tests for format detection
    - Test GIF format recognition
    - Test TIFF format recognition (.tiff and .tif)
    - Test case-insensitive matching
    - _Requirements: 8.4, 9.4, 20.4_

  - [x]* 10.6 Write integration tests for GIF/TIFF decoding
    - Decode real GIF files (animated and static)
    - Decode real TIFF files (single and multi-page)
    - Verify first frame/page extraction
    - Test with various TIFF compressions
    - _Requirements: 8.1, 8.2, 8.3, 9.1, 9.2, 9.3_

  - [x]* 10.7 Write property test for extended format classification
    - **Property 14: Extended Format Classification**
    - **Validates: Requirements 8.4, 9.4, 20.4**
    - Generate file paths with .gif, .tiff, .tif in various cases
    - Verify all recognized as supported, case-insensitive (100+ iterations)

- [x] 11. Checkpoint - Ensure all extended service tests pass
  - Ensure all tests pass, ask the user if questions arise.


- [x] 12. Implement ViewModel state classes
  - [x] 12.1 Create SearchState class
    - Implement properties: SearchQuery, FilteredPlaylist, IsSearchActive, ResultCount, SelectedResultIndex
    - Implement methods: SetSearchQuery(), ClearSearch(), SelectResult(), GetFilteredIndex()
    - Implement INotifyPropertyChanged for data binding
    - Integrate with SearchService for filtering
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.8, 1.9, 10.1, 10.2, 10.5_

  - [x] 12.2 Create ThumbnailState class
    - Implement properties: Thumbnails, IsStripVisible, StripPosition, CacheSize, GenerationProgress
    - Implement methods: RequestThumbnail(), ClearCache(), EvictLeastRecentlyUsed(), SetStripPosition()
    - Implement INotifyPropertyChanged for data binding
    - Integrate with ThumbnailService
    - _Requirements: 2.1, 2.2, 2.5, 2.6, 2.7, 11.1, 11.3, 11.4, 11.5, 11.6_

  - [x] 12.3 Create TransformState class
    - Implement properties: PendingTransforms, HasUnsavedChanges, CanUndo, CanRedo, TransformedImage
    - Implement methods: ApplyTransform(), Undo(), Redo(), SaveTransforms(), DiscardTransforms()
    - Implement INotifyPropertyChanged for data binding
    - Maintain transform stack for undo/redo
    - Integrate with TransformService
    - _Requirements: 6.5, 6.6, 6.7, 6.8, 15.1, 15.2, 15.3, 15.4, 15.5, 19.5_

  - [x] 12.4 Create MetadataState class
    - Implement properties: IsVisible, CurrentExifMetadata, HasExifData
    - Implement methods: ShowPanel(), HidePanel(), UpdateMetadata()
    - Implement INotifyPropertyChanged for data binding
    - Integrate with MetadataService
    - _Requirements: 7.6, 7.7, 7.8, 7.9_

  - [x]* 12.5 Write unit tests for all ViewModel state classes
    - Test SearchState filter updates and clear
    - Test ThumbnailState cache management and strip visibility
    - Test TransformState transform stack and undo/redo
    - Test MetadataState panel visibility and data updates
    - _Requirements: All ViewModel state requirements_

- [x] 13. Extend MainViewModel with Phase 2 commands
  - [x] 13.1 Add Phase 2 service dependencies
    - Inject ISearchService, IThumbnailService, IRecentFilesService, IExportService, ITransformService, IMetadataService
    - Create instances of SearchState, ThumbnailState, TransformState, MetadataState
    - _Requirements: All Phase 2 features_

  - [x] 13.2 Implement search commands
    - Create ShowSearchCommand (Ctrl+F)
    - Create HideSearchCommand (Escape)
    - Implement search query handling with SearchState
    - Implement result selection and navigation
    - _Requirements: 1.1, 1.5, 1.6, 1.7, 10.1, 10.2, 10.3, 10.4_

  - [x] 13.3 Implement thumbnail commands
    - Create ToggleThumbnailStripCommand (Ctrl+T)
    - Create SetThumbnailPositionCommand
    - Implement thumbnail generation coordination
    - _Requirements: 2.1, 2.5, 11.1, 11.2, 11.3, 11.4_

  - [x] 13.4 Implement recent files commands
    - Create OpenRecentFileCommand
    - Update file opening workflow to add to recent files
    - Handle invalid recent file paths
    - _Requirements: 3.1, 3.4, 3.5, 3.6, 3.8_

  - [x] 13.5 Implement export commands
    - Create ExportImageCommand (Ctrl+Shift+S)
    - Implement save file dialog
    - Implement format selection
    - Implement overwrite confirmation
    - _Requirements: 5.1, 5.2, 5.4, 5.9, 14.1_

  - [x] 13.6 Implement transform commands
    - Create RotateLeftCommand (Ctrl+L)
    - Create RotateRightCommand (Ctrl+R)
    - Create FlipHorizontalCommand (Ctrl+H)
    - Create FlipVerticalCommand (Ctrl+V)
    - Create UndoTransformCommand (Ctrl+Z)
    - Create RedoTransformCommand (Ctrl+Shift+Z)
    - Create SaveTransformsCommand (Ctrl+S)
    - Implement transform preview updates
    - Implement save confirmation dialog
    - Handle navigation with unsaved transforms
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.7, 6.8, 15.1, 15.3, 15.4, 15.5, 19.1, 19.2, 19.3, 19.4, 19.5_

  - [x] 13.7 Implement metadata commands
    - Create ShowMetadataCommand (Ctrl+I)
    - Create HideMetadataCommand (Escape or Ctrl+I)
    - Implement metadata extraction coordination
    - _Requirements: 7.6, 7.7, 7.8_

  - [x]* 13.8 Write unit tests for MainViewModel Phase 2 commands
    - Test all new command execution
    - Test state updates
    - Test error handling
    - Mock all service dependencies
    - _Requirements: All Phase 2 command requirements_

- [x] 14. Checkpoint - Ensure all ViewModel tests pass
  - Ensure all tests pass, ask the user if questions arise.


- [x] 15. Implement SearchOverlay UI
  - [x] 15.1 Create SearchOverlay.xaml with search input and results list
    - Create Grid with semi-transparent background
    - Add TextBox for search input with placeholder
    - Add TextBlock for result count
    - Add ListView for filtered results
    - Apply rounded corner styles (8px)
    - _Requirements: 1.1, 1.4, 10.5_

  - [x] 15.2 Implement SearchOverlay code-behind
    - Implement Show() and Hide() methods
    - Handle OnSearchTextChanged event
    - Handle OnResultSelected event
    - Handle OnKeyDown for Up/Down/Enter/Escape navigation
    - _Requirements: 1.1, 1.5, 1.6, 10.1, 10.2, 10.3, 10.4_

  - [x] 15.3 Wire up data binding to SearchState
    - Bind SearchText to SearchState.SearchQuery
    - Bind ResultCount to SearchState.ResultCount
    - Bind ResultsList to SearchState.FilteredPlaylist
    - Bind SelectedIndex to SearchState.SelectedResultIndex
    - _Requirements: 1.2, 1.4, 1.5, 1.8_

  - [x] 15.4 Implement keyboard navigation
    - Handle Up/Down arrow keys for result selection
    - Handle Enter key for navigation to selected result
    - Handle Escape key to close overlay
    - Implement wrapping (Down on last → first, Up on first → last)
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

- [x] 16. Implement ThumbnailStrip UI
  - [x] 16.1 Create ThumbnailStrip.xaml with scrollable thumbnail list
    - Create horizontal ScrollViewer
    - Add ItemsControl with thumbnail items
    - Implement current thumbnail highlighting
    - Support Top and Bottom positioning
    - Apply rounded corner styles
    - _Requirements: 2.1, 2.5, 2.6, 2.7, 11.1_

  - [x] 16.2 Implement ThumbnailStrip code-behind
    - Implement Show() and Hide() methods
    - Implement ScrollToIndex() for current thumbnail visibility
    - Handle OnThumbnailClicked event
    - Implement on-demand thumbnail generation as user scrolls
    - _Requirements: 2.1, 2.5, 2.6, 2.7, 2.8_

  - [x] 16.3 Wire up data binding to ThumbnailState
    - Bind Thumbnails to ThumbnailState.Thumbnails
    - Bind CurrentIndex to PlaylistState.CurrentIndex
    - Bind IsVisible to ThumbnailState.IsStripVisible
    - Bind Position to ThumbnailState.StripPosition
    - _Requirements: 2.1, 2.5, 2.6, 11.1, 11.3, 11.4_

  - [x] 16.4 Implement thumbnail strip positioning
    - Add context menu with Top/Bottom options
    - Handle position change events
    - Update layout based on position
    - Save position preference to settings
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_

  - [x] 16.5 Implement thumbnail placeholder for generation failures
    - Display placeholder icon when thumbnail generation fails
    - Continue with other thumbnails
    - Don't block image viewing
    - _Requirements: 13.5_

- [x] 17. Implement MetadataPanel UI
  - [x] 17.1 Create MetadataPanel.xaml with grouped metadata sections
    - Create slide-in panel from right side
    - Add File Information section: Filename, Size, Format
    - Add Image Properties section: Dimensions, Color Space
    - Add Camera Information section: Make/Model, Capture Date, ISO, Aperture, Shutter Speed, Focal Length, GPS
    - Make panel scrollable
    - Apply rounded corner styles
    - _Requirements: 7.6, 18.1, 18.2, 18.5, 18.6_

  - [x] 17.2 Implement MetadataPanel code-behind
    - Implement Show() with slide-in animation
    - Implement Hide() with slide-out animation
    - Handle UpdateMetadata() event
    - _Requirements: 7.6, 7.7, 7.8, 18.6_

  - [x] 17.3 Wire up data binding to MetadataState
    - Bind BasicMetadata to ViewerState.Metadata
    - Bind ExifMetadata to MetadataState.CurrentExifMetadata
    - Bind IsVisible to MetadataState.IsVisible
    - _Requirements: 7.6, 7.9_

  - [x] 17.4 Implement metadata formatting
    - Format dates in user's locale format
    - Format file sizes in human-readable units (KB, MB, GB)
    - Format GPS coordinates with N/S/E/W
    - Format camera settings (f-stop, shutter speed, focal length)
    - _Requirements: 18.2, 18.3, 18.4_

- [x] 18. Implement DropIndicator UI
  - [x] 18.1 Create DropIndicator.xaml with visual feedback
    - Create semi-transparent overlay (60% opacity)
    - Add large centered icon and text
    - Support valid (green accent) and invalid (red accent) states
    - Apply smooth fade in/out animation
    - _Requirements: 4.1, 17.1, 17.2, 17.4, 17.5_

  - [x] 18.2 Implement DropIndicator code-behind
    - Implement Show(bool isValid) method
    - Implement Hide() method
    - Update message based on validity
    - Apply appropriate visual style
    - _Requirements: 4.1, 17.1, 17.2, 17.3_

- [x] 19. Extend MainWindow with drag-and-drop support
  - [x] 19.1 Implement drag-and-drop event handlers
    - Handle OnDragOver event
    - Handle OnDragLeave event
    - Handle OnDrop event
    - Validate file format using ImageDecodeService
    - Show DropIndicator with appropriate feedback
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 17.1, 17.2, 17.3_

  - [x] 19.2 Integrate with file opening workflow
    - Route dropped files through FileOpenService validation
    - Build folder-as-playlist from dropped file's parent folder
    - Handle multiple dropped files (open first supported)
    - _Requirements: 4.2, 4.4, 4.6, 4.7_

- [x] 20. Update File menu with recent files
  - [x] 20.1 Add recent files section to File menu
    - Display last 20 opened files
    - Show most recent first
    - Add separator between Open and recent files
    - _Requirements: 3.4, 3.10_

  - [x] 20.2 Implement recent file selection
    - Handle recent file menu item click
    - Open file using standard file opening flow
    - Handle invalid file paths (display error, remove from list)
    - _Requirements: 3.4, 3.5, 3.6_

  - [x] 20.3 Update recent files on file open
    - Add file to recent files list on successful open
    - Save updated list to persistent storage
    - _Requirements: 3.1, 3.7, 3.10_

- [ ] 21. Checkpoint - Test basic Phase 2 UI functionality
  - Ensure all tests pass, ask the user if questions arise.


- [x] 22. Implement keyboard shortcuts for Phase 2 features
  - [x] 22.1 Add search shortcuts
    - Ctrl+F: Show search overlay
    - Escape: Hide search overlay (when search is active)
    - Up/Down: Navigate search results
    - Enter: Select search result
    - _Requirements: 1.1, 1.6, 10.1, 10.2_

  - [x] 22.2 Add thumbnail shortcuts
    - Ctrl+T: Toggle thumbnail strip
    - _Requirements: 2.1_

  - [x] 22.3 Add export shortcuts
    - Ctrl+Shift+S: Export image
    - _Requirements: 5.1_

  - [x] 22.4 Add transform shortcuts
    - Ctrl+L: Rotate left
    - Ctrl+R: Rotate right
    - Ctrl+H: Flip horizontal
    - Ctrl+V: Flip vertical
    - Ctrl+Z: Undo transform
    - Ctrl+Shift+Z: Redo transform
    - Ctrl+S: Save transforms (when transforms pending)
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 15.3, 15.4_

  - [x] 22.5 Add metadata shortcuts
    - Ctrl+I: Toggle metadata panel
    - Escape: Hide metadata panel (when panel is open)
    - _Requirements: 7.6, 7.8_

  - [x] 22.6 Update keyboard shortcut routing in MainWindow
    - Route all Phase 2 shortcuts to MainViewModel commands
    - Handle shortcut conflicts (e.g., Escape for search vs metadata)
    - Ensure shortcuts work in windowed and fullscreen modes
    - _Requirements: All keyboard shortcut requirements_

- [x] 23. Implement visual polish and animations
  - [x] 23.1 Add search overlay animations
    - Smooth fade in/out for overlay
    - Focus search input on show
    - _Requirements: 1.1, 1.6_

  - [x] 23.2 Add thumbnail strip animations
    - Smooth scroll to current thumbnail
    - Highlight animation for current thumbnail
    - _Requirements: 2.6, 2.7_

  - [x] 23.3 Add metadata panel animations
    - Slide-in animation from right (300ms)
    - Slide-out animation to right (300ms)
    - _Requirements: 7.6, 18.6_

  - [x] 23.4 Add drop indicator animations
    - Smooth fade in/out (100ms)
    - _Requirements: 17.3_

  - [x] 23.5 Add transform preview feedback
    - Immediate display update (< 200ms)
    - Unsaved changes indicator in chrome
    - _Requirements: 6.5, 15.1, 15.5_

  - [x] 23.6 Add export and save confirmation dialogs
    - Overwrite confirmation for export
    - Transform save confirmation with filename
    - Success confirmation message
    - _Requirements: 5.4, 5.9, 19.1, 19.2, 19.3, 19.4_

- [x] 24. Update settings persistence for Phase 2
  - [x] 24.1 Extend SettingsService for Phase 2 settings
    - Save/load ThumbnailStripVisible
    - Save/load ThumbnailStripPosition
    - Save/load DefaultJpegQuality
    - Save/load PreserveExifOnExport
    - _Requirements: 11.5, 11.6_

  - [x] 24.2 Apply Phase 2 settings on startup
    - Restore thumbnail strip visibility and position
    - Apply default JPEG quality for exports
    - Apply EXIF preservation preference
    - _Requirements: 11.6_

- [x] 25. Performance optimization
  - [x] 25.1 Optimize search performance
    - Implement search input debouncing (100ms)
    - Cache lowercase filenames for large playlists
    - Target: < 100ms for 1000 items, < 500ms for 1000+ items
    - _Requirements: 16.1, 16.2, 16.3_

  - [x] 25.2 Optimize thumbnail generation
    - Generate thumbnails on background thread
    - Implement progressive loading (visible thumbnails first)
    - Use virtualization in thumbnail strip
    - Target: < 200ms per thumbnail
    - _Requirements: 2.8, 13.4_

  - [x] 25.3 Optimize transform preview
    - Apply transforms in-memory
    - Combine multiple transforms into single operation when possible
    - Target: < 200ms for transform preview
    - _Requirements: 15.1, 15.2_

  - [x] 25.4 Optimize EXIF extraction
    - Extract EXIF on background thread
    - Cache EXIF data per image
    - Only extract when metadata panel is opened
    - _Requirements: 7.1_

- [ ] 26. Manual testing for Phase 2 features
  - [ ] 26.1 Manual test: Search UI
    - Test search overlay appearance and animation
    - Test keyboard navigation through results
    - Test search performance with large folders (1000+ images)
    - Test search clear behavior
    - _Requirements: 1.1-1.9, 10.1-10.5, 16.1-16.4_

  - [ ] 26.2 Manual test: Thumbnail strip
    - Test thumbnail generation progress
    - Test scrolling and selection
    - Test position switching (top/bottom)
    - Test performance with many thumbnails (500+)
    - Test placeholder display for failed thumbnails
    - _Requirements: 2.1-2.10, 11.1-11.6, 13.1-13.5_

  - [ ] 26.3 Manual test: Drag-and-drop
    - Test drop indicator appearance
    - Test valid/invalid file feedback
    - Test multiple file handling
    - Test drop outside window
    - _Requirements: 4.1-4.7, 17.1-17.5_

  - [ ] 26.4 Manual test: Export
    - Test save dialog appearance
    - Test format selection
    - Test overwrite confirmation
    - Test success confirmation
    - Test EXIF preservation
    - _Requirements: 5.1-5.9, 14.1-14.5_

  - [ ] 26.5 Manual test: Transforms
    - Test transform preview responsiveness
    - Test undo/redo feel
    - Test save confirmation
    - Test unsaved changes indicator
    - Test navigation with unsaved transforms
    - _Requirements: 6.1-6.11, 15.1-15.5, 19.1-19.5_

  - [ ] 26.6 Manual test: Metadata panel
    - Test panel slide-in animation
    - Test EXIF data formatting
    - Test missing data display
    - Test GPS coordinate formatting
    - _Requirements: 7.1-7.10, 18.1-18.6_

  - [ ] 26.7 Manual test: Recent files
    - Test recent files menu appearance
    - Test file selection
    - Test invalid file handling
    - Test list ordering (most recent first)
    - Test persistence across restarts
    - _Requirements: 3.1-3.10, 12.1-12.5_

  - [ ] 26.8 Manual test: GIF/TIFF support
    - Test GIF display (first frame)
    - Test TIFF display (first page)
    - Test mixed format folders
    - Test format detection
    - _Requirements: 8.1-8.7, 9.1-9.8, 20.1-20.4_

  - [ ] 26.9 Manual test: Keyboard shortcuts
    - Test all Phase 2 keyboard shortcuts
    - Test shortcut conflicts resolution
    - Test shortcuts in windowed and fullscreen modes
    - _Requirements: All keyboard shortcut requirements_

  - [ ] 26.10 Manual test: Phase 1 regression
    - Verify all Phase 1 functionality still works
    - Verify Phase 1 keyboard shortcuts unchanged
    - Verify Phase 1 UI behavior unchanged
    - Run Phase 1 test suite
    - _Requirements: Phase 1 compatibility_

- [ ] 27. Bug fixes and final polish
  - [ ] 27.1 Address issues found in manual testing
    - Fix any bugs discovered during manual testing
    - Tune animation timing if needed
    - Adjust performance if targets not met
    - Polish visual appearance

  - [ ] 27.2 Final code review and cleanup
    - Review all code for consistency with Phase 1
    - Remove debug logging
    - Ensure all comments are accurate
    - Verify all requirements are met
    - Verify all acceptance criteria are met

- [ ] 28. Final checkpoint - Phase 2 complete
  - Ensure all tests pass, ask the user if questions arise.
  - Verify all acceptance criteria are met
  - Confirm Phase 1 functionality unchanged
  - Confirm Phase 3 features remain documented in roadmap

## Notes

- Tasks marked with `*` are optional testing tasks and can be skipped for faster delivery
- Each task references specific requirements for traceability
- Property-based tests validate universal correctness properties with 100+ iterations
- Unit tests validate specific examples and edge cases
- Integration tests validate file I/O, WIC operations, thumbnail generation, EXIF extraction, export, and transforms
- Manual testing validates UI behavior, visual polish, performance, and Phase 1 compatibility
- Checkpoints ensure incremental validation throughout development
- All service layer code should achieve 90%+ test coverage
- Phase 2 is complete when all acceptance criteria in the design document are met
- Phase 2 must not break any Phase 1 functionality

