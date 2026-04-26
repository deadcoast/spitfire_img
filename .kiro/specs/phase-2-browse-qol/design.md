# Design Document: Phase 2 Browse And Quality Of Life

## Overview

### Purpose

This design document specifies the technical implementation for Phase 2 of spitfire.img, building upon the Phase 1 MVP Viewer foundation. Phase 2 adds quality-of-life features that make everyday folder browsing faster and more convenient: filename search, thumbnail previews, recent files tracking, drag-and-drop support, image export with format conversion, rotate/flip transforms, enhanced EXIF metadata extraction, and GIF/TIFF format support.

### Design Goals

1. **Architectural Consistency**: All new features integrate with Phase 1 services without breaking changes
2. **Service Separation**: New services follow Phase 1 patterns (interfaces, Result types, async operations)
3. **Performance**: Search and thumbnails remain responsive even with large folders (1000+ images)
4. **User Experience**: Features feel natural and don't disrupt the presentation-first viewing experience
5. **Extensibility**: Design supports Phase 3 features (slideshow, comparison mode, command palette)

### Technology Stack

- **UI Framework**: WinUI 3 with Windows App SDK (from Phase 1)
- **Language**: C# with .NET 8.0 (from Phase 1)
- **Image Decoding**: Windows Imaging Component (WIC) via WinUI APIs (from Phase 1)
- **EXIF Extraction**: WIC metadata queries or Windows.Graphics.Imaging
- **Thumbnail Generation**: WIC with scaling transformations
- **Storage**: JSON files in ApplicationData.Current.LocalFolder (from Phase 1)

### Key Design Decisions

1. **Search Uses Playlist Model**: SearchService filters the existing Playlist rather than creating a separate data structure
2. **Thumbnail Caching Strategy**: LRU cache with 100MB limit, 120px thumbnails, on-demand generation
3. **Recent Files Don't Replace Playlist**: Opening a recent file still builds folder-as-playlist from parent folder
4. **Transforms Are Non-Destructive**: Rotate/flip operations update display immediately but require explicit save
5. **GIF/TIFF Route Through ImageDecodeService**: New formats extend existing service, not window logic
6. **Export Supports Format Conversion**: Users can save PNG as JPEG, etc., with quality options
7. **EXIF Extraction Is Optional**: Missing or malformed EXIF data doesn't block image viewing
8. **Drag-and-Drop Reuses File Opening**: Dropped files go through FileOpenService validation


## Architecture

### High-Level Architecture

```
┌────────────────────────────────────────────────────────────┐
│                     Presentation Layer                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ MainWindow   │  │SearchOverlay │  │ThumbnailStrip│      │
│  │              │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐                        │
│  │MetadataPanel │  │DropIndicator │                        │
│  │              │  │              │                        │
│  └──────────────┘  └──────────────┘                        │
└────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────────┐
│                      ViewModel Layer                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ MainViewModel│  │ SearchState  │  │ThumbnailState│      │
│  │  (Phase 1)   │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐                        │
│  │TransformState│  │ MetadataState│                        │
│  │              │  │              │                        │
│  └──────────────┘  └──────────────┘                        │
└────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────────┐
│                       Service Layer                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ SearchSvc    │  │ThumbnailSvc  │  │RecentFilesSvc│      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ ExportSvc    │  │TransformSvc  │  │ MetadataSvc  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ImageDecodeSvc│  │ PlaylistSvc  │  │SettingsSvc   │      │
│  │  (Extended)  │  │  (Phase 1)   │  │  (Extended)  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────────┐
│                        Model Layer                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  ImageItem   │  │  Thumbnail   │  │ RecentFile   │      │
│  │  (Phase 1)   │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ExifMetadata  │  │TransformOp   │  │ExportOptions │      │
│  │              │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└────────────────────────────────────────────────────────────┘
```

### Integration with Phase 1

**Phase 1 Services (Unchanged)**:
- FileOpenService: Validates files (now also validates dropped files)
- FolderPlaylistService: Builds playlists (used by search and recent files)
- NavigationService: Handles navigation (works with filtered playlists)
- SettingsService: Persists settings (extended with new preferences)
- ErrorLogService: Logs errors (extended with new error categories)

**Phase 1 Models (Unchanged)**:
- ImageItem: Represents playlist items (used by search and thumbnails)
- ImageMetadata: Basic metadata (extended by ExifMetadata)
- ViewerSettings: User preferences (extended with new settings)
- ViewerError: Error information (extended with new categories)

**Phase 1 ViewModels (Extended)**:
- MainViewModel: Coordinates operations (adds new commands and state)
- ViewerState: Display state (adds transform state)
- PlaylistState: Playlist management (works with filtered playlists)

### Module Structure

```
src/ImageViewer.App/
├── Views/
│   ├── MainWindow.xaml                  # Extended with drop handling
│   ├── SearchOverlay.xaml               # NEW: Search UI
│   ├── ThumbnailStrip.xaml              # NEW: Thumbnail strip
│   ├── MetadataPanel.xaml               # NEW: EXIF display panel
│   └── DropIndicator.xaml               # NEW: Drag-drop visual feedback
│
├── ViewModels/
│   ├── MainViewModel.cs                 # Extended with new commands
│   ├── SearchState.cs                   # NEW: Search state management
│   ├── ThumbnailState.cs                # NEW: Thumbnail state
│   ├── TransformState.cs                # NEW: Transform operations state
│   └── MetadataState.cs                 # NEW: Metadata display state
│
├── Services/
│   ├── SearchService.cs                 # NEW: Filename filtering
│   ├── ThumbnailService.cs              # NEW: Thumbnail generation/caching
│   ├── RecentFilesService.cs            # NEW: Recent files tracking
│   ├── ExportService.cs                 # NEW: Image export/conversion
│   ├── TransformService.cs              # NEW: Rotate/flip operations
│   ├── MetadataService.cs               # NEW: EXIF extraction
│   ├── ImageDecodeService.cs            # Extended with GIF/TIFF support
│   └── [Phase 1 services unchanged]
│
├── Models/
│   ├── Thumbnail.cs                     # NEW: Thumbnail data
│   ├── RecentFile.cs                    # NEW: Recent file entry
│   ├── ExifMetadata.cs                  # NEW: EXIF data
│   ├── TransformOperation.cs            # NEW: Transform operation
│   ├── ExportOptions.cs                 # NEW: Export settings
│   └── [Phase 1 models unchanged]
│
└── Resources/
    └── [Phase 1 resources unchanged]
```


## Components and Interfaces

### New Presentation Layer Components

#### SearchOverlay

**Responsibility**: Display search UI and handle search input.

**Key Properties**:
- `SearchText`: String (current search query)
- `ResultCount`: int (number of matching images)
- `SelectedIndex`: int (currently selected result)
- `IsVisible`: bool (overlay visibility)

**Key Methods**:
- `Show()`: Display overlay and focus search input
- `Hide()`: Close overlay and clear search
- `OnSearchTextChanged(string)`: Update filtered results
- `OnResultSelected(int)`: Navigate to selected image
- `OnKeyDown(KeyRoutedEventArgs)`: Handle Up/Down/Enter/Escape

**XAML Structure**:
```xml
<Grid x:Name="SearchOverlay" Visibility="Collapsed">
  <Border Background="{ThemeResource SearchOverlayBackground}">
    <StackPanel>
      <TextBox x:Name="SearchInput" PlaceholderText="Search by filename..." />
      <TextBlock Text="{x:Bind ResultCount}" />
      <ListView x:Name="ResultsList" ItemsSource="{x:Bind FilteredResults}" />
    </StackPanel>
  </Border>
</Grid>
```

#### ThumbnailStrip

**Responsibility**: Display thumbnail previews with scrolling and selection.

**Key Properties**:
- `Thumbnails`: ObservableCollection<Thumbnail>
- `CurrentIndex`: int (highlighted thumbnail)
- `Position`: ThumbnailPosition enum (Top, Bottom)
- `IsVisible`: bool

**Key Methods**:
- `Show()`: Display thumbnail strip
- `Hide()`: Hide thumbnail strip
- `ScrollToIndex(int)`: Scroll to make thumbnail visible
- `OnThumbnailClicked(int)`: Navigate to clicked image
- `GenerateThumbnails()`: Request thumbnail generation

**Layout**:
```
┌────────────────────────────────────────────────────────────┐
│ [thumb] [thumb] [CURRENT] [thumb] [thumb] [thumb] [thumb]  │
└────────────────────────────────────────────────────────────┘
```

#### MetadataPanel

**Responsibility**: Display detailed image metadata including EXIF data.

**Key Properties**:
- `BasicMetadata`: ImageMetadata (from Phase 1)
- `ExifMetadata`: ExifMetadata (camera, exposure, GPS)
- `IsVisible`: bool

**Key Methods**:
- `Show()`: Display panel with slide-in animation
- `Hide()`: Hide panel with slide-out animation
- `UpdateMetadata(ExifMetadata)`: Refresh displayed data

**Panel Sections**:
```
┌─────────────────────┐
│ File Information    │
│ - Filename          │
│ - Size              │
│ - Format            │
│                     │
│ Image Properties    │
│ - Dimensions        │
│ - Color Space       │
│                     │
│ Camera Information  │
│ - Make/Model        │
│ - Capture Date      │
│ - ISO/Aperture/SS   │
│ - Focal Length      │
│ - GPS Coordinates   │
└─────────────────────┘
```

#### DropIndicator

**Responsibility**: Provide visual feedback during drag-and-drop operations.

**Key Properties**:
- `IsVisible`: bool
- `Message`: string ("Drop image to open" or "Unsupported file format")
- `IsValid`: bool (determines visual style)

**Key Methods**:
- `Show(bool isValid)`: Display indicator with appropriate message
- `Hide()`: Hide indicator

**Visual Design**:
- Semi-transparent overlay (60% opacity)
- Large centered icon and text
- Green accent for valid files, red for invalid
- Smooth fade in/out animation

### New ViewModel Layer Components

#### SearchState

**Responsibility**: Manage search query and filtered results.

**Properties**:
- `SearchQuery`: string (current search text)
- `FilteredPlaylist`: ObservableCollection<ImageItem> (matching items)
- `IsSearchActive`: bool
- `ResultCount`: int
- `SelectedResultIndex`: int

**Methods**:
- `SetSearchQuery(string)`: Update query and filter playlist
- `ClearSearch()`: Reset to full playlist
- `SelectResult(int)`: Set selected result
- `GetFilteredIndex(int originalIndex)`: Map original to filtered index

**Filtering Algorithm**:
```
1. If SearchQuery is empty, FilteredPlaylist = full Playlist
2. Otherwise, FilteredPlaylist = items where filename contains SearchQuery (case-insensitive)
3. Update ResultCount
4. Notify UI of changes
```

#### ThumbnailState

**Responsibility**: Manage thumbnail generation and caching state.

**Properties**:
- `Thumbnails`: ObservableCollection<Thumbnail>
- `IsStripVisible`: bool
- `StripPosition`: ThumbnailPosition (Top, Bottom)
- `CacheSize`: long (current cache size in bytes)
- `GenerationProgress`: int (thumbnails generated / total)

**Methods**:
- `RequestThumbnail(ImageItem)`: Get or generate thumbnail
- `ClearCache()`: Remove all cached thumbnails
- `EvictLeastRecentlyUsed()`: Free memory when cache exceeds limit
- `SetStripPosition(ThumbnailPosition)`: Change strip location

#### TransformState

**Responsibility**: Track pending transform operations and undo/redo stack.

**Properties**:
- `PendingTransforms`: List<TransformOperation>
- `HasUnsavedChanges`: bool
- `CanUndo`: bool
- `CanRedo`: bool
- `TransformedImage`: BitmapImage (current display with transforms applied)

**Methods**:
- `ApplyTransform(TransformOperation)`: Add transform and update display
- `Undo()`: Remove last transform
- `Redo()`: Reapply undone transform
- `SaveTransforms()`: Persist transforms to file
- `DiscardTransforms()`: Clear all pending transforms

**Transform Stack**:
```
Original Image
  ↓
Apply Rotate Right → TransformedImage (display)
  ↓
Apply Flip Horizontal → TransformedImage (display)
  ↓
Save → Write to file, clear stack
```

#### MetadataState

**Responsibility**: Manage metadata panel visibility and content.

**Properties**:
- `IsVisible`: bool
- `CurrentExifMetadata`: ExifMetadata
- `HasExifData`: bool

**Methods**:
- `ShowPanel()`: Display metadata panel
- `HidePanel()`: Close metadata panel
- `UpdateMetadata(ExifMetadata)`: Refresh displayed data

### New Service Layer Components

#### SearchService

**Responsibility**: Filter playlist by filename.

**Interface**:
```csharp
public interface ISearchService
{
    Result<List<ImageItem>> FilterPlaylist(
        List<ImageItem> playlist, 
        string searchQuery);
}
```

**Methods**:

`FilterPlaylist(List<ImageItem> playlist, string searchQuery)`:
- Perform case-insensitive substring match on filename
- Return filtered list maintaining original order
- Return full playlist if query is empty or whitespace

**Filtering Algorithm**:
```csharp
public Result<List<ImageItem>> FilterPlaylist(
    List<ImageItem> playlist, 
    string searchQuery)
{
    if (string.IsNullOrWhiteSpace(searchQuery))
        return Result<List<ImageItem>>.Success(playlist);
    
    var query = searchQuery.ToLowerInvariant();
    var filtered = playlist
        .Where(item => item.Filename.ToLowerInvariant().Contains(query))
        .ToList();
    
    return Result<List<ImageItem>>.Success(filtered);
}
```

**Performance**:
- Target: < 100ms for 1000 items
- Use efficient string matching (no regex)
- Consider caching lowercase filenames for large playlists

#### ThumbnailService

**Responsibility**: Generate and cache thumbnail images.

**Interface**:
```csharp
public interface IThumbnailService
{
    Task<Result<Thumbnail>> GetThumbnailAsync(ImageItem item);
    void ClearCache();
    long GetCacheSize();
}
```

**Methods**:

`GetThumbnailAsync(ImageItem item)`:
- Check cache for existing thumbnail
- If not cached, generate thumbnail from source image
- Add to cache with LRU tracking
- Return Thumbnail or ThumbnailGenerationError

`ClearCache()`:
- Remove all cached thumbnails
- Reset cache size counter

`GetCacheSize()`:
- Return current cache size in bytes

**Thumbnail Generation Algorithm**:
```
1. Load source image using WIC decoder
2. Calculate scale factor to fit 120px on longest dimension
3. Create scaled bitmap using WIC scaler
4. Encode to PNG in memory
5. Create BitmapImage from memory stream
6. Return Thumbnail { ImageItem, BitmapImage, SizeBytes }
```

**Caching Strategy**:
- **Cache Type**: In-memory LRU cache
- **Cache Limit**: 100 MB
- **Thumbnail Size**: 120px longest dimension, preserve aspect ratio
- **Eviction**: When cache exceeds 100MB, remove least recently used thumbnails
- **Key**: ImageItem.FilePath (case-insensitive)

**LRU Implementation**:
```csharp
private class ThumbnailCache
{
    private Dictionary<string, CacheEntry> _cache;
    private LinkedList<string> _lruList;
    private long _currentSize;
    private const long MaxSize = 100 * 1024 * 1024; // 100 MB
    
    public Thumbnail Get(string filePath)
    {
        if (_cache.TryGetValue(filePath, out var entry))
        {
            // Move to front of LRU list
            _lruList.Remove(entry.Node);
            _lruList.AddFirst(entry.Node);
            return entry.Thumbnail;
        }
        return null;
    }
    
    public void Add(string filePath, Thumbnail thumbnail)
    {
        // Evict if necessary
        while (_currentSize + thumbnail.SizeBytes > MaxSize && _lruList.Count > 0)
        {
            var lruKey = _lruList.Last.Value;
            Remove(lruKey);
        }
        
        // Add to cache
        var node = _lruList.AddFirst(filePath);
        _cache[filePath] = new CacheEntry { Thumbnail = thumbnail, Node = node };
        _currentSize += thumbnail.SizeBytes;
    }
}
```

**On-Demand Generation**:
- Generate thumbnails as user scrolls thumbnail strip
- Use virtualization to avoid generating all thumbnails upfront
- Generate on background thread to avoid UI blocking

#### RecentFilesService

**Responsibility**: Track and persist recently opened files.

**Interface**:
```csharp
public interface IRecentFilesService
{
    Task<Result<List<RecentFile>>> LoadRecentFilesAsync();
    Task<Result> AddRecentFileAsync(string filePath);
    Task<Result> RemoveRecentFileAsync(string filePath);
    Task<Result> ClearRecentFilesAsync();
}
```

**Methods**:

`LoadRecentFilesAsync()`:
- Read recent files list from JSON storage
- Validate file paths are well-formed
- Return list or empty list if file doesn't exist
- Return SettingsPersistenceError if read/parse fails

`AddRecentFileAsync(string filePath)`:
- Remove existing entry if present (to move to top)
- Add file path to top of list
- Trim list to 20 items
- Save to persistent storage
- Return success or SettingsPersistenceError

`RemoveRecentFileAsync(string filePath)`:
- Remove file path from list
- Save to persistent storage
- Return success or SettingsPersistenceError

`ClearRecentFilesAsync()`:
- Remove all entries
- Save empty list to persistent storage
- Return success or SettingsPersistenceError

**Storage Format**:
```json
{
  "recentFiles": [
    {
      "filePath": "C:\\Photos\\IMG_0042.jpg",
      "lastOpened": "2024-01-15T14:32:18.456Z"
    },
    {
      "filePath": "C:\\Photos\\IMG_0041.jpg",
      "lastOpened": "2024-01-15T14:30:12.123Z"
    }
  ]
}
```

**Storage Location**: `ApplicationData.Current.LocalFolder/recent-files.json`

**List Management**:
- Maximum 20 entries
- Most recent at top
- Duplicates moved to top when reopened
- Invalid paths removed on load

#### ExportService

**Responsibility**: Save copies of images with optional format conversion.

**Interface**:
```csharp
public interface IExportService
{
    Task<Result> ExportImageAsync(
        string sourcePath, 
        string destinationPath, 
        ExportOptions options);
}
```

**Methods**:

`ExportImageAsync(string sourcePath, string destinationPath, ExportOptions options)`:
- Load source image
- Apply format conversion if requested
- Apply quality settings for lossy formats
- Preserve EXIF data if supported by target format
- Write to destination path
- Return success or ExportError

**Export Algorithm**:
```
1. Validate source file exists and is readable
2. Validate destination path is writable
3. Check if destination exists and handle overwrite
4. Load source image using WIC decoder
5. If format conversion requested:
   a. Create encoder for target format
   b. Apply quality settings (JPEG)
   c. Copy EXIF data if supported
6. Else:
   a. Copy file directly (faster)
7. Write to destination
8. Return success
```

**Format Conversion Support**:
- **Source Formats**: PNG, JPEG, BMP, WEBP, GIF, TIFF
- **Target Formats**: PNG, JPEG, BMP, WEBP
- **EXIF Preservation**: PNG, JPEG (not BMP, WEBP)
- **Quality Settings**: JPEG (default 90%)

**Error Cases**:
- Source file not found → ExportError
- Destination not writable → ExportError
- Format conversion fails → ExportError
- Insufficient disk space → ExportError

#### TransformService

**Responsibility**: Apply rotate and flip transformations to images.

**Interface**:
```csharp
public interface ITransformService
{
    Task<Result<BitmapImage>> ApplyTransformAsync(
        BitmapImage source, 
        TransformOperation operation);
    
    Task<Result> SaveTransformsAsync(
        string filePath, 
        List<TransformOperation> operations);
}
```

**Methods**:

`ApplyTransformAsync(BitmapImage source, TransformOperation operation)`:
- Apply rotation or flip to source image
- Return transformed BitmapImage
- Return TransformError if operation fails

`SaveTransformsAsync(string filePath, List<TransformOperation> operations)`:
- Load original image
- Apply all operations in sequence
- Overwrite original file
- Preserve EXIF data with updated orientation
- Return success or TransformSaveError

**Transform Operations**:
```csharp
public enum TransformOperation
{
    RotateLeft,      // 90° counterclockwise
    RotateRight,     // 90° clockwise
    FlipHorizontal,  // Mirror left-right
    FlipVertical     // Mirror top-bottom
}
```

**Transform Algorithm** (using WIC):
```csharp
public async Task<Result<BitmapImage>> ApplyTransformAsync(
    BitmapImage source, 
    TransformOperation operation)
{
    try
    {
        // Convert BitmapImage to IWICBitmap
        var wicBitmap = await ConvertToWicBitmap(source);
        
        // Apply transform
        var transformer = new WICBitmapFlipRotator();
        var rotation = operation switch
        {
            TransformOperation.RotateLeft => WICBitmapTransformOptions.Rotate270,
            TransformOperation.RotateRight => WICBitmapTransformOptions.Rotate90,
            TransformOperation.FlipHorizontal => WICBitmapTransformOptions.FlipHorizontal,
            TransformOperation.FlipVertical => WICBitmapTransformOptions.FlipVertical
        };
        transformer.Initialize(wicBitmap, rotation);
        
        // Convert back to BitmapImage
        var result = await ConvertToBitmapImage(transformer);
        return Result<BitmapImage>.Success(result);
    }
    catch (Exception ex)
    {
        var error = ViewerError.TransformError(operation, ex);
        return Result<BitmapImage>.Failure(error);
    }
}
```

**Save Algorithm**:
```
1. Load original image file
2. Apply all pending transforms in sequence
3. Create temporary file
4. Write transformed image to temp file
5. Copy EXIF data from original (update orientation tag)
6. Replace original file with temp file
7. Return success
```

**Undo/Redo**:
- Maintain stack of applied operations
- Undo: Remove last operation, reapply remaining
- Redo: Reapply previously undone operation
- Clear stack on save or navigation

#### MetadataService

**Responsibility**: Extract EXIF and extended metadata from images.

**Interface**:
```csharp
public interface IMetadataService
{
    Task<Result<ExifMetadata>> ExtractExifAsync(string filePath);
}
```

**Methods**:

`ExtractExifAsync(string filePath)`:
- Open image file with WIC decoder
- Query metadata reader for EXIF tags
- Extract camera, exposure, and GPS data
- Return ExifMetadata or empty metadata if not present
- Return MetadataExtractionError only for read failures

**EXIF Tags to Extract**:
```
Camera Information:
- Make (0x010F)
- Model (0x0110)
- Software (0x0131)

Capture Settings:
- DateTime (0x0132)
- DateTimeOriginal (0x9003)
- ISO (0x8827)
- FNumber (0x829D) - Aperture
- ExposureTime (0x829A) - Shutter speed
- FocalLength (0x920A)
- Flash (0x9209)

GPS Information:
- GPSLatitude (0x0002)
- GPSLongitude (0x0004)
- GPSAltitude (0x0006)

Image Properties:
- ColorSpace (0xA001)
- PixelXDimension (0xA002)
- PixelYDimension (0xA003)
```

**Extraction Algorithm** (using Windows.Graphics.Imaging):
```csharp
public async Task<Result<ExifMetadata>> ExtractExifAsync(string filePath)
{
    try
    {
        using var stream = File.OpenRead(filePath);
        var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
        
        var metadata = new ExifMetadata();
        
        try
        {
            var properties = await decoder.BitmapProperties.GetPropertiesAsync(
                new[] { "System.Photo.CameraManufacturer", "System.Photo.CameraModel", ... });
            
            metadata.CameraMake = properties["System.Photo.CameraManufacturer"]?.ToString();
            metadata.CameraModel = properties["System.Photo.CameraModel"]?.ToString();
            // ... extract other properties
        }
        catch
        {
            // EXIF data not present or malformed, return empty metadata
        }
        
        return Result<ExifMetadata>.Success(metadata);
    }
    catch (Exception ex)
    {
        var error = ViewerError.MetadataExtractionError(filePath, ex);
        return Result<ExifMetadata>.Failure(error);
    }
}
```

**Graceful Degradation**:
- Missing EXIF data returns empty ExifMetadata (not an error)
- Malformed EXIF data returns partial metadata
- Only file read failures return errors

#### ImageDecodeService (Extended)

**New Responsibilities**: Add GIF and TIFF format support.

**Extended Interface**:
```csharp
public interface IImageDecodeService
{
    Task<Result<DecodedImage>> DecodeImageAsync(string filePath);
    bool IsSupportedFormat(string extension);
    bool IsDeferredFormat(string extension);
    
    // NEW: Multi-frame support (for future animation)
    Task<Result<int>> GetFrameCountAsync(string filePath);
}
```

**Updated Format Lists**:
```csharp
private static readonly string[] SupportedExtensions = 
{
    ".png", ".jpg", ".jpeg", ".bmp", ".webp", 
    ".gif", ".tiff", ".tif"  // NEW in Phase 2
};

private static readonly string[] DeferredExtensions = 
{
    ".heic", ".avif"  // Phase 3
};
```

**GIF Decoding**:
- Decode first frame only in Phase 2
- Use WIC GIF decoder
- Extract dimensions and file size
- Animation support deferred to Phase 3

**TIFF Decoding**:
- Decode first page only for multi-page TIFFs
- Use WIC TIFF decoder
- Support common TIFF variants (uncompressed, LZW, JPEG)
- Extract dimensions and file size

**Decoding Algorithm** (unchanged, WIC handles new formats):
```csharp
public async Task<Result<DecodedImage>> DecodeImageAsync(string filePath)
{
    try
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        if (!IsSupportedFormat(extension))
            return Result<DecodedImage>.Failure(
                ViewerError.UnsupportedFormatError(filePath));
        
        var bitmapImage = new BitmapImage();
        using (var stream = File.OpenRead(filePath))
        {
            await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
        }
        
        var metadata = new ImageMetadata
        {
            PixelWidth = bitmapImage.PixelWidth,
            PixelHeight = bitmapImage.PixelHeight,
            FileSizeBytes = new FileInfo(filePath).Length
        };
        
        return Result<DecodedImage>.Success(
            new DecodedImage(bitmapImage, metadata));
    }
    catch (Exception ex) when (ex is OutOfMemoryException)
    {
        var error = ViewerError.DecodeError(filePath, ex, isMemoryError: true);
        return Result<DecodedImage>.Failure(error);
    }
    catch (Exception ex)
    {
        var error = ViewerError.DecodeError(filePath, ex);
        return Result<DecodedImage>.Failure(error);
    }
}
```


## Data Models

### Thumbnail

**Purpose**: Represent a cached thumbnail image.

**Properties**:
```csharp
public class Thumbnail
{
    public ImageItem Item { get; init; }           // Associated playlist item
    public BitmapImage Image { get; init; }        // Thumbnail image (120px)
    public long SizeBytes { get; init; }           // Memory size for cache management
    public DateTime LastAccessed { get; set; }     // For LRU eviction
}
```

**Usage**: Stored in ThumbnailService cache, displayed in ThumbnailStrip

### RecentFile

**Purpose**: Represent a recently opened file entry.

**Properties**:
```csharp
public class RecentFile
{
    public string FilePath { get; init; }          // Full path to file
    public DateTime LastOpened { get; init; }      // When file was last opened
    public string Filename { get; }                // Computed: filename only
    public string FolderPath { get; }              // Computed: parent folder
}
```

**Computed Properties**:
- `Filename`: Returns Path.GetFileName(FilePath)
- `FolderPath`: Returns Path.GetDirectoryName(FilePath)

**Serialization**: JSON via System.Text.Json

**Usage**: Stored in RecentFilesService, displayed in File menu

### ExifMetadata

**Purpose**: Store extracted EXIF data from images.

**Properties**:
```csharp
public class ExifMetadata
{
    // Camera Information
    public string? CameraMake { get; init; }
    public string? CameraModel { get; init; }
    public string? Software { get; init; }
    
    // Capture Settings
    public DateTime? CaptureDate { get; init; }
    public int? Iso { get; init; }
    public double? Aperture { get; init; }          // f-number
    public double? ShutterSpeed { get; init; }      // seconds
    public double? FocalLength { get; init; }       // mm
    public bool? FlashFired { get; init; }
    
    // GPS Information
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? Altitude { get; init; }          // meters
    
    // Image Properties
    public string? ColorSpace { get; init; }
    
    // Computed Properties
    public bool HasCameraInfo { get; }
    public bool HasCaptureSettings { get; }
    public bool HasGpsInfo { get; }
    public string FormattedAperture { get; }        // "f/2.8"
    public string FormattedShutterSpeed { get; }    // "1/250s"
    public string FormattedFocalLength { get; }     // "50mm"
    public string FormattedGpsCoordinates { get; }  // "37.7749° N, 122.4194° W"
}
```

**Computed Properties**:
- `HasCameraInfo`: Returns CameraMake != null || CameraModel != null
- `HasCaptureSettings`: Returns Iso != null || Aperture != null || ShutterSpeed != null
- `HasGpsInfo`: Returns Latitude != null && Longitude != null
- `FormattedAperture`: Returns "f/{Aperture:F1}" or empty
- `FormattedShutterSpeed`: Returns "1/{1/ShutterSpeed}s" or "{ShutterSpeed}s"
- `FormattedFocalLength`: Returns "{FocalLength:F0}mm" or empty
- `FormattedGpsCoordinates`: Returns formatted lat/long with N/S/E/W

**Usage**: Extracted by MetadataService, displayed in MetadataPanel

### TransformOperation

**Purpose**: Represent a single transform operation.

**Definition**:
```csharp
public enum TransformOperation
{
    RotateLeft,      // 90° counterclockwise
    RotateRight,     // 90° clockwise
    FlipHorizontal,  // Mirror left-right
    FlipVertical     // Mirror top-bottom
}
```

**Usage**: Stored in TransformState, applied by TransformService

### ExportOptions

**Purpose**: Configure export/save-copy operations.

**Properties**:
```csharp
public class ExportOptions
{
    public ImageFormat TargetFormat { get; init; }  // PNG, JPEG, BMP, WEBP
    public int JpegQuality { get; init; }           // 1-100, default 90
    public bool PreserveExif { get; init; }         // Default true
    public bool OverwriteExisting { get; init; }    // Default false
}

public enum ImageFormat
{
    Png,
    Jpeg,
    Bmp,
    Webp
}
```

**Default Values**:
- TargetFormat: Same as source
- JpegQuality: 90
- PreserveExif: true
- OverwriteExisting: false

**Usage**: Passed to ExportService.ExportImageAsync()

### Extended ViewerSettings

**New Properties** (added to Phase 1 ViewerSettings):
```csharp
public class ViewerSettings
{
    // Phase 1 properties (unchanged)
    public string Theme { get; set; }
    public string DefaultDisplayMode { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public int WindowX { get; set; }
    public int WindowY { get; set; }
    public int FadeDelayMs { get; set; }
    public int FadeDurationMs { get; set; }
    
    // NEW Phase 2 properties
    public bool ThumbnailStripVisible { get; set; }         // Default false
    public ThumbnailPosition ThumbnailStripPosition { get; set; }  // Default Bottom
    public int DefaultJpegQuality { get; set; }             // Default 90
    public bool PreserveExifOnExport { get; set; }          // Default true
}

public enum ThumbnailPosition
{
    Top,
    Bottom
}
```

**Serialization**: JSON via System.Text.Json (same as Phase 1)

**Usage**: Loaded on app start, saved on settings change

### Extended ViewerError

**New Error Categories**:
```csharp
public enum ErrorCategory
{
    // Phase 1 categories (unchanged)
    FileAccessError,
    UnsupportedFormatError,
    DecodeError,
    PlaylistScanError,
    NavigationError,
    SettingsPersistenceError,
    UnexpectedViewerError,
    
    // NEW Phase 2 categories
    ThumbnailGenerationError,
    ExportError,
    TransformError,
    TransformSaveError,
    MetadataExtractionError
}
```

**New Factory Methods**:
```csharp
public static ViewerError ThumbnailGenerationError(string filePath, Exception ex);
public static ViewerError ExportError(string sourcePath, string destPath, Exception ex);
public static ViewerError TransformError(TransformOperation operation, Exception ex);
public static ViewerError TransformSaveError(string filePath, Exception ex);
public static ViewerError MetadataExtractionError(string filePath, Exception ex);
```

**New User Messages**:
- ThumbnailGenerationError: "Unable to generate thumbnail for this image."
- ExportError: "Unable to save a copy of this image to the selected location."
- TransformError: "Unable to apply the requested transformation."
- TransformSaveError: "Unable to save changes to this image."
- MetadataExtractionError: "Unable to read metadata from this image."


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Search Filtering Correctness

*For any* playlist of image items and any search query string, the SearchService SHALL return only items whose filenames contain the query as a case-insensitive substring, and SHALL return the full playlist when the query is empty or whitespace.

**Validates: Requirements 1.1, 1.3, 1.9**

### Property 2: Search Clear Round-Trip

*For any* playlist, applying a search filter and then clearing the search SHALL restore the original playlist with the same items in the same order.

**Validates: Requirements 1.9**

### Property 3: Thumbnail Scaling Preserves Aspect Ratio

*For any* source image with dimensions W×H, the generated thumbnail SHALL have its longest dimension equal to 120 pixels and SHALL preserve the aspect ratio W:H within a tolerance of 1 pixel.

**Validates: Requirements 2.3**

### Property 4: Thumbnail Caching Avoids Regeneration

*For any* image item, requesting a thumbnail twice SHALL return the cached thumbnail on the second request without regenerating it, as evidenced by cache hit metrics or generation count.

**Validates: Requirements 2.4**

### Property 5: Recent Files Duplicate Handling

*For any* recent files list and any file path, adding a file path that already exists in the list SHALL result in that path appearing at index 0 (most recent) and appearing exactly once in the list.

**Validates: Requirements 3.2**

### Property 6: Recent Files Capacity Management

*For any* recent files list at maximum capacity (20 items), adding a new file path SHALL result in a list of exactly 20 items with the oldest entry removed.

**Validates: Requirements 3.3**

### Property 7: Transform Non-Destructive Until Save

*For any* image file and any sequence of transform operations, applying transforms SHALL NOT modify the original file on disk until SaveTransformsAsync is explicitly called, as verified by file hash comparison.

**Validates: Requirements 6.6**

### Property 8: Transform Dimension Updates

*For any* image with dimensions W×H, applying RotateLeft or RotateRight SHALL result in displayed dimensions H×W, and applying FlipHorizontal or FlipVertical SHALL result in displayed dimensions W×H.

**Validates: Requirements 6.11**

### Property 9: EXIF Extraction Graceful Degradation

*For any* image file (with valid EXIF, partial EXIF, malformed EXIF, or no EXIF), the MetadataService SHALL return success with an ExifMetadata object (possibly empty or partial) and SHALL NOT return an error unless the file itself cannot be read.

**Validates: Requirements 7.10**

### Property 10: Thumbnail Cache LRU Eviction

*For any* thumbnail cache at maximum capacity (100 MB), adding a new thumbnail SHALL evict the least recently used thumbnail and SHALL maintain total cache size at or below 100 MB.

**Validates: Requirements 13.3**

### Property 11: Export Format Conversion Correctness

*For any* source image in a supported format and any target format (PNG, JPEG, BMP, WEBP), the ExportService SHALL produce an output file in the target format as verified by file header or extension.

**Validates: Requirements 14.2**

### Property 12: Export EXIF Preservation

*For any* image with EXIF data, exporting to JPEG or PNG format with PreserveExif=true SHALL result in an output file containing the same EXIF data (within the subset supported by the target format).

**Validates: Requirements 14.5**

### Property 13: Transform Undo/Redo Round-Trip

*For any* sequence of transform operations, applying N transforms, then undoing M transforms (M ≤ N), then redoing M transforms SHALL result in the same visual state as after the original N transforms.

**Validates: Requirements 15.3, 15.4**

### Property 14: Extended Format Classification

*For any* file path with extension .gif, .tiff, or .tif (in any case variation), the ImageDecodeService SHALL classify it as a supported format, and SHALL use case-insensitive matching consistent with Phase 1 formats.

**Validates: Requirements 8.4, 9.4, 20.4**


## Error Handling

### Error Handling Philosophy

Phase 2 extends Phase 1's structured error handling system with new error categories for thumbnails, export, transforms, and metadata extraction. The same principles apply:

1. **Expected Failures Are Not Exceptions**: Services return Result<T> types
2. **User Messages Are Calm and Actionable**: Clear explanations without technical jargon
3. **Diagnostic Information Is Logged**: Full details written to log files
4. **Errors Don't Block Progress**: Graceful degradation where possible

### New Error Categories and Handling

#### ThumbnailGenerationError

**Triggers**:
- Source image cannot be decoded
- Scaling operation fails
- Insufficient memory for thumbnail
- I/O error during generation

**User Message**: "Unable to generate thumbnail for this image."

**Handling**:
- Log error with file path and exception
- Display placeholder icon in thumbnail strip
- Continue with other thumbnails
- Don't block image viewing

**Recovery**: User can still view full image; thumbnail strip shows placeholder

#### ExportError

**Triggers**:
- Source file not found or inaccessible
- Destination path not writable
- Insufficient disk space
- Format conversion fails
- EXIF preservation fails

**User Message**: "Unable to save a copy of this image to the selected location."

**Handling**:
- Log error with source path, destination path, and exception
- Display user message
- Don't modify source file
- Clean up partial destination file if created

**Recovery**: User can try different destination or format

#### TransformError

**Triggers**:
- Transform operation fails (WIC error)
- Insufficient memory for transform
- Invalid transform operation

**User Message**: "Unable to apply the requested transformation."

**Handling**:
- Log error with operation and exception
- Display user message
- Revert to previous transform state
- Don't modify displayed image

**Recovery**: User can try again or use different transform

#### TransformSaveError

**Triggers**:
- Original file not writable
- Insufficient disk space
- File locked by another process
- EXIF update fails

**User Message**: "Unable to save changes to this image."

**Handling**:
- Log error with file path and exception
- Display user message
- Keep transforms in memory (don't discard)
- Don't modify original file

**Recovery**: User can try saving again, save to different location, or discard transforms

#### MetadataExtractionError

**Triggers**:
- File cannot be opened
- File is corrupted
- I/O error during read

**User Message**: "Unable to read metadata from this image."

**Handling**:
- Log error with file path and exception
- Display user message as warning (not blocking)
- Show metadata panel with basic info only
- Continue with image viewing

**Recovery**: User can still view image; metadata panel shows limited info

### Graceful Degradation Strategies

**Thumbnail Generation Failures**:
- Show placeholder icon for failed thumbnails
- Continue generating other thumbnails
- Don't disable thumbnail strip entirely
- Log errors for debugging

**EXIF Extraction Failures**:
- Return empty ExifMetadata (not an error)
- Show metadata panel with basic file info only
- Don't block image viewing
- Only log errors for file read failures

**Search Failures**:
- If filtering fails, return full playlist
- Log error but don't show user message
- Search remains functional for next query

**Recent Files Failures**:
- If load fails, start with empty list
- If save fails, continue with in-memory list
- Don't block file opening
- Log errors for debugging

### Error Logging Extensions

**Log Entry Format** (same as Phase 1):
```
[2024-01-15 14:32:18.456] [ERROR] [ThumbnailGenerationError]
Operation: GenerateThumbnail
File: C:\Photos\IMG_0042.jpg
Message: Unable to generate thumbnail for this image.
Exception: System.OutOfMemoryException
Stack Trace:
  at ImageViewer.Services.ThumbnailService.GenerateThumbnailAsync(ImageItem item)
  ...
---
```

**Log File Management** (same as Phase 1):
- Location: `ApplicationData.Current.LocalFolder/logs/`
- Naming: `viewer-{yyyy-MM-dd}.log`
- Rotation: Daily
- Retention: 30 days


## Testing Strategy

### Testing Approach

Phase 2 extends Phase 1's dual testing approach:

1. **Property-Based Tests**: Verify universal properties for search, thumbnails, recent files, transforms, export
2. **Example-Based Unit Tests**: Verify specific scenarios, edge cases, UI interactions
3. **Integration Tests**: Verify file I/O, WIC operations, EXIF extraction, thumbnail generation
4. **Manual Testing**: Verify UI behavior, performance, visual polish

### Property-Based Testing

**Framework**: CsCheck or FsCheck (same as Phase 1)

**Configuration**:
- Minimum 100 iterations per property test
- Each test tagged with comment referencing design property
- Tag format: `// Feature: phase-2-browse-qol, Property {number}: {property_text}`

**Property Tests to Implement**:

1. **Search Filtering** (Property 1)
   - Generate: Random playlists (10-1000 items) with various filename patterns, random search queries
   - Verify: All results contain query substring (case-insensitive), empty query returns full playlist
   - Edge cases: Empty playlist, query with special characters, very long queries, Unicode filenames

2. **Search Clear Round-Trip** (Property 2)
   - Generate: Random playlists, random search queries
   - Verify: Filter → Clear → Result equals original playlist
   - Edge cases: Empty playlist, no matches, all matches

3. **Thumbnail Scaling** (Property 3)
   - Generate: Random image dimensions (1×1 to 10000×10000)
   - Verify: Longest dimension = 120px, aspect ratio preserved within 1px tolerance
   - Edge cases: Square images, very wide images, very tall images, 1px images

4. **Thumbnail Caching** (Property 4)
   - Generate: Random image items
   - Verify: Second request is cache hit (no regeneration)
   - Edge cases: Cache eviction, cache clear, different items with same path

5. **Recent Files Duplicates** (Property 5)
   - Generate: Random recent files lists (0-20 items), random file paths
   - Verify: Adding duplicate moves to index 0, appears exactly once
   - Edge cases: Empty list, full list, path at various positions

6. **Recent Files Capacity** (Property 6)
   - Generate: Lists with exactly 20 items, random new file paths
   - Verify: Result has 20 items, oldest removed, new at index 0
   - Edge cases: Adding duplicate when full, adding multiple items

7. **Transform Non-Destructive** (Property 7)
   - Generate: Random images, random transform sequences
   - Verify: File hash unchanged after applying transforms (before save)
   - Edge cases: Multiple transforms, undo/redo, navigation

8. **Transform Dimensions** (Property 8)
   - Generate: Random dimensions, random transform operations
   - Verify: Rotations swap dimensions, flips preserve dimensions
   - Edge cases: Square images, multiple rotations, combined operations

9. **EXIF Graceful Degradation** (Property 9)
   - Generate: Images with no EXIF, partial EXIF, malformed EXIF
   - Verify: All return success with ExifMetadata (possibly empty)
   - Edge cases: Corrupted EXIF, missing required tags, invalid values

10. **Thumbnail Cache LRU** (Property 10)
    - Generate: Thumbnails totaling > 100MB, access patterns
    - Verify: LRU item evicted, cache size ≤ 100MB
    - Edge cases: Exact capacity, single large thumbnail, many small thumbnails

11. **Export Format Conversion** (Property 11)
    - Generate: Random source/target format pairs
    - Verify: Output file has correct format (check header/extension)
    - Edge cases: Same format, lossy to lossless, lossless to lossy

12. **Export EXIF Preservation** (Property 12)
    - Generate: Images with various EXIF data, target formats
    - Verify: EXIF present in output for JPEG/PNG
    - Edge cases: Partial EXIF, large EXIF, GPS data

13. **Transform Undo/Redo** (Property 13)
    - Generate: Random transform sequences, random undo/redo counts
    - Verify: Undo → Redo restores state
    - Edge cases: Undo all, redo all, undo/redo interleaved

14. **Extended Format Classification** (Property 14)
    - Generate: File paths with .gif, .tiff, .tif in various cases
    - Verify: All recognized as supported, case-insensitive
    - Edge cases: Mixed case, multiple dots, no extension

**Example Property Test**:
```csharp
// Feature: phase-2-browse-qol, Property 1: Search Filtering Correctness
[Fact]
public void SearchFiltering_ReturnsOnlyMatchingItems()
{
    Gen.Select(
        Gen.List(Gen.String[5, 50], 10, 1000),  // Random filenames
        Gen.String[0, 20]                        // Random search query
    )
    .Sample((filenames, query) =>
    {
        var playlist = filenames.Select(f => new ImageItem 
        { 
            FilePath = $"C:\\Photos\\{f}.jpg", 
            Filename = $"{f}.jpg" 
        }).ToList();
        
        var service = new SearchService();
        var result = service.FilterPlaylist(playlist, query);
        
        Assert.True(result.IsSuccess);
        
        if (string.IsNullOrWhiteSpace(query))
        {
            Assert.Equal(playlist.Count, result.Value.Count);
        }
        else
        {
            var lowerQuery = query.ToLowerInvariant();
            Assert.All(result.Value, item => 
                Assert.Contains(lowerQuery, item.Filename.ToLowerInvariant()));
        }
    }, 
    iter: 100);
}
```

### Example-Based Unit Tests

**Framework**: xUnit with FluentAssertions (same as Phase 1)

**Test Categories**:

1. **Service Tests**:
   - SearchService: Specific search queries, empty results, special characters
   - ThumbnailService: Specific dimensions, cache operations, eviction
   - RecentFilesService: Add/remove operations, persistence, validation
   - ExportService: Specific format conversions, quality settings, overwrite
   - TransformService: Specific transform sequences, undo/redo, save
   - MetadataService: Specific EXIF tags, missing data, malformed data
   - ImageDecodeService: GIF/TIFF decoding, multi-frame handling

2. **Model Tests**:
   - Thumbnail: Equality, property initialization
   - RecentFile: Computed properties, serialization
   - ExifMetadata: Formatted properties, has-data checks
   - ExportOptions: Defaults, validation

3. **ViewModel Tests**:
   - SearchState: Filter updates, clear, result selection
   - ThumbnailState: Cache management, strip visibility
   - TransformState: Transform stack, undo/redo, save state
   - MetadataState: Panel visibility, data updates

**Example Unit Test**:
```csharp
[Fact]
public void SearchService_EmptyQuery_ReturnsFullPlaylist()
{
    // Arrange
    var playlist = new List<ImageItem>
    {
        new ImageItem { FilePath = "C:\\Photos\\img1.jpg", Filename = "img1.jpg" },
        new ImageItem { FilePath = "C:\\Photos\\img2.jpg", Filename = "img2.jpg" },
        new ImageItem { FilePath = "C:\\Photos\\img3.jpg", Filename = "img3.jpg" }
    };
    var service = new SearchService();
    
    // Act
    var result = service.FilterPlaylist(playlist, "");
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().HaveCount(3);
    result.Value.Should().BeEquivalentTo(playlist);
}
```

### Integration Tests

**Test Scenarios**:

1. **Thumbnail Generation**:
   - Generate thumbnails from real images (PNG, JPEG, BMP, WEBP, GIF, TIFF)
   - Verify dimensions and aspect ratio
   - Verify cache persistence
   - Test memory limits

2. **EXIF Extraction**:
   - Extract EXIF from real photos with various cameras
   - Verify specific tag values
   - Test with images lacking EXIF
   - Test with malformed EXIF

3. **Export Operations**:
   - Export real images to different formats
   - Verify output file format and quality
   - Verify EXIF preservation
   - Test overwrite behavior

4. **Transform Operations**:
   - Apply transforms to real images
   - Save transforms to disk
   - Verify file modifications
   - Test undo/redo with real files

5. **GIF/TIFF Decoding**:
   - Decode real GIF files (animated and static)
   - Decode real TIFF files (single and multi-page)
   - Verify first frame/page extraction
   - Test with various TIFF compressions

6. **Recent Files Persistence**:
   - Add files and verify JSON storage
   - Load files and verify deserialization
   - Test with invalid paths
   - Test with corrupted JSON

**Example Integration Test**:
```csharp
[Fact]
public async Task ThumbnailService_RealImage_GeneratesCorrectSize()
{
    // Arrange
    var testImagePath = Path.Combine(TestDataFolder, "sample-1920x1080.jpg");
    var item = new ImageItem 
    { 
        FilePath = testImagePath, 
        Filename = "sample-1920x1080.jpg" 
    };
    var service = new ThumbnailService();
    
    // Act
    var result = await service.GetThumbnailAsync(item);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Image.Should().NotBeNull();
    
    var longestDimension = Math.Max(
        result.Value.Image.PixelWidth, 
        result.Value.Image.PixelHeight);
    longestDimension.Should().Be(120);
    
    // Verify aspect ratio preserved (1920:1080 = 16:9)
    var aspectRatio = (double)result.Value.Image.PixelWidth / 
                      result.Value.Image.PixelHeight;
    aspectRatio.Should().BeApproximately(16.0 / 9.0, 0.01);
}
```

### Manual Testing

**Test Scenarios**:

1. **Search UI**:
   - Search overlay appearance and animation
   - Keyboard navigation through results
   - Search performance with large folders
   - Search clear behavior

2. **Thumbnail Strip**:
   - Thumbnail generation progress
   - Scrolling and selection
   - Position switching (top/bottom)
   - Performance with many thumbnails

3. **Drag-and-Drop**:
   - Drop indicator appearance
   - Valid/invalid file feedback
   - Multiple file handling
   - Drop outside window

4. **Export**:
   - Save dialog appearance
   - Format selection
   - Overwrite confirmation
   - Success confirmation

5. **Transforms**:
   - Transform preview responsiveness
   - Undo/redo feel
   - Save confirmation
   - Unsaved changes indicator

6. **Metadata Panel**:
   - Panel slide-in animation
   - EXIF data formatting
   - Missing data display
   - GPS coordinate formatting

7. **Recent Files**:
   - Recent files menu appearance
   - File selection
   - Invalid file handling
   - List ordering

8. **GIF/TIFF Support**:
   - GIF display (first frame)
   - TIFF display (first page)
   - Mixed format folders
   - Format detection

### Test Coverage Goals

- **Services**: 90%+ code coverage with unit and integration tests
- **Models**: 100% code coverage with unit tests
- **ViewModels**: 80%+ code coverage with unit tests
- **Views**: Manual testing for UI behavior

### Performance Testing

**Performance Targets**:
- Search filtering: < 100ms for 1000 items
- Thumbnail generation: < 200ms per thumbnail
- Transform preview: < 200ms
- Export: < 2s for typical images
- EXIF extraction: < 100ms

**Performance Test Scenarios**:
- Search with 1000+ images
- Thumbnail strip with 500+ images
- Transform operations on large images (10000×10000)
- Export with format conversion
- Recent files with 20 entries


## Implementation Guidance

### Development Phases

**Phase 2A: Search and Thumbnails (Week 1-2)**
1. Implement SearchService with filtering logic
2. Implement SearchState and SearchOverlay UI
3. Implement ThumbnailService with generation and caching
4. Implement ThumbnailState and ThumbnailStrip UI
5. Write unit and property tests for search and thumbnails
6. Integration tests with real images

**Phase 2B: Recent Files and Drag-Drop (Week 3)**
1. Implement RecentFilesService with persistence
2. Extend MainViewModel with recent files commands
3. Update File menu with recent files list
4. Implement drag-and-drop handling in MainWindow
5. Implement DropIndicator UI
6. Write unit and integration tests

**Phase 2C: Export and Transforms (Week 4-5)**
1. Implement ExportService with format conversion
2. Implement TransformService with WIC operations
3. Implement TransformState with undo/redo
4. Add export and transform commands to MainViewModel
5. Implement save confirmation dialogs
6. Write unit and integration tests

**Phase 2D: Metadata and Format Support (Week 6)**
1. Implement MetadataService with EXIF extraction
2. Implement MetadataState and MetadataPanel UI
3. Extend ImageDecodeService with GIF/TIFF support
4. Update format detection lists
5. Write unit and integration tests
6. Test with various image formats

**Phase 2E: Polish and Integration (Week 7-8)**
1. Tune search and thumbnail performance
2. Implement all keyboard shortcuts
3. Add visual feedback and animations
4. Update settings persistence
5. Manual testing and bug fixes
6. Performance optimization
7. Documentation updates

### Key Implementation Details

#### Search Implementation

**SearchService**:
```csharp
public class SearchService : ISearchService
{
    public Result<List<ImageItem>> FilterPlaylist(
        List<ImageItem> playlist, 
        string searchQuery)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
                return Result<List<ImageItem>>.Success(playlist);
            
            var query = searchQuery.ToLowerInvariant();
            var filtered = playlist
                .Where(item => item.Filename.ToLowerInvariant().Contains(query))
                .ToList();
            
            return Result<List<ImageItem>>.Success(filtered);
        }
        catch (Exception ex)
        {
            // Fallback to full playlist on error
            _errorLogService.LogErrorAsync(
                ViewerError.UnexpectedError("FilterPlaylist", ex));
            return Result<List<ImageItem>>.Success(playlist);
        }
    }
}
```

**SearchOverlay XAML**:
```xml
<Grid x:Name="SearchOverlay" 
      Visibility="{x:Bind ViewModel.SearchState.IsVisible, Mode=OneWay}"
      Background="{ThemeResource SearchOverlayBackgroundBrush}">
    <Border MaxWidth="600" 
            CornerRadius="8" 
            Background="{ThemeResource SearchPanelBackgroundBrush}"
            Padding="16">
        <StackPanel Spacing="12">
            <TextBox x:Name="SearchInput"
                     PlaceholderText="Search by filename..."
                     Text="{x:Bind ViewModel.SearchState.SearchQuery, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                     KeyDown="OnSearchKeyDown" />
            
            <TextBlock Text="{x:Bind ViewModel.SearchState.ResultCountText, Mode=OneWay}"
                       Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
            
            <ListView ItemsSource="{x:Bind ViewModel.SearchState.FilteredPlaylist, Mode=OneWay}"
                      SelectedIndex="{x:Bind ViewModel.SearchState.SelectedResultIndex, Mode=TwoWay}"
                      MaxHeight="400"
                      SelectionChanged="OnResultSelected">
                <ListView.ItemTemplate>
                    <DataTemplate x:DataType="models:ImageItem">
                        <TextBlock Text="{x:Bind Filename}" />
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </StackPanel>
    </Border>
</Grid>
```

#### Thumbnail Generation with WIC

**ThumbnailService**:
```csharp
public class ThumbnailService : IThumbnailService
{
    private readonly ThumbnailCache _cache = new();
    private const int ThumbnailSize = 120;
    
    public async Task<Result<Thumbnail>> GetThumbnailAsync(ImageItem item)
    {
        // Check cache first
        var cached = _cache.Get(item.FilePath);
        if (cached != null)
            return Result<Thumbnail>.Success(cached);
        
        try
        {
            // Generate thumbnail
            var thumbnail = await GenerateThumbnailAsync(item);
            
            // Add to cache
            _cache.Add(item.FilePath, thumbnail);
            
            return Result<Thumbnail>.Success(thumbnail);
        }
        catch (Exception ex)
        {
            var error = ViewerError.ThumbnailGenerationError(item.FilePath, ex);
            await _errorLogService.LogErrorAsync(error);
            return Result<Thumbnail>.Failure(error);
        }
    }
    
    private async Task<Thumbnail> GenerateThumbnailAsync(ImageItem item)
    {
        using var stream = File.OpenRead(item.FilePath);
        var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
        
        // Calculate scaled dimensions
        var sourceWidth = decoder.PixelWidth;
        var sourceHeight = decoder.PixelHeight;
        var scale = ThumbnailSize / (double)Math.Max(sourceWidth, sourceHeight);
        var targetWidth = (uint)(sourceWidth * scale);
        var targetHeight = (uint)(sourceHeight * scale);
        
        // Get scaled pixels
        var transform = new BitmapTransform
        {
            ScaledWidth = targetWidth,
            ScaledHeight = targetHeight,
            InterpolationMode = BitmapInterpolationMode.Fant
        };
        var pixelData = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            transform,
            ExifOrientationMode.RespectExifOrientation,
            ColorManagementMode.DoNotColorManage);
        
        // Create BitmapImage
        var thumbnail = new BitmapImage();
        using var memoryStream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(
            BitmapEncoder.PngEncoderId, 
            memoryStream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            targetWidth,
            targetHeight,
            96, 96,
            pixelData.DetachPixelData());
        await encoder.FlushAsync();
        
        memoryStream.Seek(0);
        await thumbnail.SetSourceAsync(memoryStream);
        
        var sizeBytes = (long)memoryStream.Size;
        
        return new Thumbnail
        {
            Item = item,
            Image = thumbnail,
            SizeBytes = sizeBytes,
            LastAccessed = DateTime.UtcNow
        };
    }
}
```

#### EXIF Extraction

**MetadataService**:
```csharp
public class MetadataService : IMetadataService
{
    public async Task<Result<ExifMetadata>> ExtractExifAsync(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
            
            var metadata = new ExifMetadata();
            
            try
            {
                // Extract camera info
                var cameraProps = await decoder.BitmapProperties.GetPropertiesAsync(new[]
                {
                    "System.Photo.CameraManufacturer",
                    "System.Photo.CameraModel",
                    "System.ApplicationName"
                });
                
                metadata.CameraMake = GetStringValue(cameraProps, "System.Photo.CameraManufacturer");
                metadata.CameraModel = GetStringValue(cameraProps, "System.Photo.CameraModel");
                metadata.Software = GetStringValue(cameraProps, "System.ApplicationName");
                
                // Extract capture settings
                var captureProps = await decoder.BitmapProperties.GetPropertiesAsync(new[]
                {
                    "System.Photo.DateTaken",
                    "System.Photo.ISOSpeed",
                    "System.Photo.FNumber",
                    "System.Photo.ExposureTime",
                    "System.Photo.FocalLength"
                });
                
                metadata.CaptureDate = GetDateTimeValue(captureProps, "System.Photo.DateTaken");
                metadata.Iso = GetIntValue(captureProps, "System.Photo.ISOSpeed");
                metadata.Aperture = GetDoubleValue(captureProps, "System.Photo.FNumber");
                metadata.ShutterSpeed = GetDoubleValue(captureProps, "System.Photo.ExposureTime");
                metadata.FocalLength = GetDoubleValue(captureProps, "System.Photo.FocalLength");
                
                // Extract GPS info
                var gpsProps = await decoder.BitmapProperties.GetPropertiesAsync(new[]
                {
                    "System.GPS.Latitude",
                    "System.GPS.Longitude",
                    "System.GPS.Altitude"
                });
                
                metadata.Latitude = GetGpsCoordinate(gpsProps, "System.GPS.Latitude");
                metadata.Longitude = GetGpsCoordinate(gpsProps, "System.GPS.Longitude");
                metadata.Altitude = GetDoubleValue(gpsProps, "System.GPS.Altitude");
            }
            catch
            {
                // EXIF data missing or malformed, return partial metadata
            }
            
            return Result<ExifMetadata>.Success(metadata);
        }
        catch (Exception ex)
        {
            var error = ViewerError.MetadataExtractionError(filePath, ex);
            await _errorLogService.LogErrorAsync(error);
            return Result<ExifMetadata>.Failure(error);
        }
    }
    
    private string? GetStringValue(IDictionary<string, BitmapTypedValue> props, string key)
    {
        return props.TryGetValue(key, out var value) ? value.Value?.ToString() : null;
    }
    
    private int? GetIntValue(IDictionary<string, BitmapTypedValue> props, string key)
    {
        if (props.TryGetValue(key, out var value) && value.Value != null)
        {
            if (int.TryParse(value.Value.ToString(), out var result))
                return result;
        }
        return null;
    }
    
    private double? GetDoubleValue(IDictionary<string, BitmapTypedValue> props, string key)
    {
        if (props.TryGetValue(key, out var value) && value.Value != null)
        {
            if (double.TryParse(value.Value.ToString(), out var result))
                return result;
        }
        return null;
    }
    
    private DateTime? GetDateTimeValue(IDictionary<string, BitmapTypedValue> props, string key)
    {
        if (props.TryGetValue(key, out var value) && value.Value is DateTime dt)
            return dt;
        return null;
    }
    
    private double? GetGpsCoordinate(IDictionary<string, BitmapTypedValue> props, string key)
    {
        // GPS coordinates are stored as arrays of rationals
        // Implementation depends on Windows.Graphics.Imaging format
        // Return decimal degrees
        return null; // Simplified for example
    }
}
```

#### Transform Operations

**TransformService**:
```csharp
public class TransformService : ITransformService
{
    public async Task<Result<BitmapImage>> ApplyTransformAsync(
        BitmapImage source, 
        TransformOperation operation)
    {
        try
        {
            // Convert BitmapImage to pixel data
            var writeableBitmap = new WriteableBitmap(
                source.PixelWidth, 
                source.PixelHeight);
            source.CopyTo(writeableBitmap);
            
            // Apply transform using WIC
            var transformed = await ApplyWicTransform(writeableBitmap, operation);
            
            return Result<BitmapImage>.Success(transformed);
        }
        catch (Exception ex)
        {
            var error = ViewerError.TransformError(operation, ex);
            await _errorLogService.LogErrorAsync(error);
            return Result<BitmapImage>.Failure(error);
        }
    }
    
    public async Task<Result> SaveTransformsAsync(
        string filePath, 
        List<TransformOperation> operations)
    {
        try
        {
            // Load original image
            using var stream = File.OpenRead(filePath);
            var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
            
            // Apply all transforms
            var pixelData = await decoder.GetPixelDataAsync();
            var width = decoder.PixelWidth;
            var height = decoder.PixelHeight;
            
            foreach (var op in operations)
            {
                (pixelData, width, height) = ApplyTransformToPixelData(
                    pixelData, width, height, op);
            }
            
            // Create temporary file
            var tempPath = Path.GetTempFileName();
            using (var outputStream = File.Create(tempPath))
            {
                var encoder = await BitmapEncoder.CreateAsync(
                    GetEncoderFromExtension(Path.GetExtension(filePath)),
                    outputStream.AsRandomAccessStream());
                
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    width, height,
                    96, 96,
                    pixelData.DetachPixelData());
                
                // Copy EXIF data (update orientation)
                await CopyExifData(decoder, encoder, operations);
                
                await encoder.FlushAsync();
            }
            
            // Replace original file
            File.Delete(filePath);
            File.Move(tempPath, filePath);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            var error = ViewerError.TransformSaveError(filePath, ex);
            await _errorLogService.LogErrorAsync(error);
            return Result.Failure(error);
        }
    }
}
```

#### Drag-and-Drop Handling

**MainWindow.xaml.cs**:
```csharp
public sealed partial class MainWindow : Window
{
    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var deferral = e.GetDeferral();
            
            _ = Task.Run(async () =>
            {
                var items = await e.DataView.GetStorageItemsAsync();
                var file = items.FirstOrDefault() as StorageFile;
                
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (file != null)
                    {
                        var extension = Path.GetExtension(file.Path);
                        var isValid = _viewModel.ImageDecodeService.IsSupportedFormat(extension);
                        DropIndicator.Show(isValid);
                    }
                    
                    deferral.Complete();
                });
            });
        }
    }
    
    private void OnDragLeave(object sender, DragEventArgs e)
    {
        DropIndicator.Hide();
    }
    
    private async void OnDrop(object sender, DragEventArgs e)
    {
        DropIndicator.Hide();
        
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var file = items.FirstOrDefault() as StorageFile;
            
            if (file != null)
            {
                await _viewModel.OpenFileCommand.ExecuteAsync(file.Path);
            }
        }
    }
}
```

### Performance Optimization

**Search Performance**:
- Use efficient string matching (no regex)
- Consider caching lowercase filenames for large playlists
- Debounce search input (wait 100ms after last keystroke)

**Thumbnail Performance**:
- Generate thumbnails on background thread
- Use virtualization in thumbnail strip
- Implement progressive loading (visible thumbnails first)
- Tune cache size based on available memory

**Transform Performance**:
- Apply transforms in-memory (don't write to disk until save)
- Combine multiple transforms into single operation when possible
- Use hardware acceleration if available

**EXIF Performance**:
- Extract EXIF on background thread
- Cache EXIF data per image
- Only extract when metadata panel is opened

### Extensibility for Phase 3

**Animation Support** (GIF):
- GetFrameCountAsync() already defined in ImageDecodeService
- Add frame timing extraction
- Implement frame playback in ImageCanvas
- Add playback controls

**Multi-Page Support** (TIFF):
- Extend navigation to support pages within a file
- Add page indicator to info bar
- Implement page navigation shortcuts

**Command Palette**:
- SearchService can be reused for command search
- Add command registry
- Implement fuzzy matching

**Comparison Mode**:
- ThumbnailService can generate comparison thumbnails
- Extend ViewerState with multi-image support
- Add split-view layout


## Acceptance Criteria

Phase 2 is complete when:

1. ✅ A user can press Ctrl+F to search the current folder by filename with case-insensitive matching
2. ✅ Search results update within 100ms and support keyboard navigation (Up/Down/Enter/Escape)
3. ✅ Clearing search restores the full playlist
4. ✅ A user can press Ctrl+T to toggle a thumbnail strip showing 120px previews
5. ✅ Thumbnails are generated on-demand and cached with LRU eviction at 100MB
6. ✅ Clicking a thumbnail navigates to that image
7. ✅ The thumbnail strip can be positioned at top or bottom via context menu
8. ✅ Recent files menu shows the last 20 opened files with most recent first
9. ✅ Opening a recent file builds the folder-as-playlist from its parent folder
10. ✅ Recent files list persists to JSON and survives application restarts
11. ✅ A user can drag image files onto the viewer window to open them
12. ✅ Drag-and-drop shows visual feedback (valid/invalid file indicator)
13. ✅ Dropped files are validated through FileOpenService
14. ✅ A user can press Ctrl+Shift+S to export the current image
15. ✅ Export supports format conversion (PNG, JPEG, BMP, WEBP) with quality settings
16. ✅ Export preserves EXIF data when supported by target format
17. ✅ A user can rotate images with Ctrl+L (left) and Ctrl+R (right)
18. ✅ A user can flip images with Ctrl+H (horizontal) and Ctrl+V (vertical)
19. ✅ Transforms update the display within 200ms without modifying the original file
20. ✅ A user can undo transforms with Ctrl+Z and redo with Ctrl+Shift+Z
21. ✅ Pressing Ctrl+S after transforms shows save confirmation dialog
22. ✅ Saving transforms overwrites the original file with EXIF orientation updated
23. ✅ Navigating to a different image discards unsaved transforms
24. ✅ A user can press Ctrl+I to view detailed metadata including EXIF data
25. ✅ Metadata panel displays camera make/model, capture date, ISO, aperture, shutter speed, focal length, and GPS coordinates
26. ✅ Missing or malformed EXIF data is handled gracefully without errors
27. ✅ Metadata panel slides in from the right with smooth animation
28. ✅ GIF files are recognized as supported and display the first frame
29. ✅ TIFF files (.tiff and .tif) are recognized as supported and display the first page
30. ✅ Multi-page TIFFs show only the first page in Phase 2
31. ✅ All new formats route through ImageDecodeService with consistent error handling
32. ✅ Search filtering works correctly for folders with 1000+ images
33. ✅ Thumbnail generation completes within 200ms per thumbnail
34. ✅ Transform preview feels responsive (< 200ms)
35. ✅ All correctness properties pass property-based tests with 100+ iterations
36. ✅ Service layer has 90%+ unit test coverage
37. ✅ Integration tests verify thumbnail generation, EXIF extraction, export, and transforms with real files
38. ✅ Manual testing confirms UI polish, animations, and performance
39. ✅ All new error categories are logged with structured information
40. ✅ User-facing error messages are calm and actionable
41. ✅ Phase 1 functionality remains unchanged and all Phase 1 tests still pass
42. ✅ Settings persistence includes new preferences (thumbnail strip visibility/position, JPEG quality)
43. ✅ Phase 3 features remain documented in roadmap.md
44. ✅ Service interfaces support future extensibility (animation, multi-page, command palette)

