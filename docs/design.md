# Design Document: Phase 1 MVP Viewer

## Overview

### Purpose

This design document specifies the technical implementation for Phase 1 of spitfire.img, a modern Windows-native image viewer application. The application prioritizes presentation-first viewing with a low-distraction fading UI, automatic folder-as-playlist behavior, and responsive keyboard-driven navigation.

### Design Goals

1. **Presentation-First Architecture**: UI chrome fades automatically to let images become the focus, even in windowed mode
2. **Service Separation**: Clear boundaries between presentation, workflow state, and business logic to enable Phase 2/3 features without rewrites
3. **Responsive Performance**: Image loading and navigation feel instant for typical use cases (files under 10 MB)
4. **Extensible Format Support**: Centralized format detection that makes adding new formats straightforward
5. **Structured Error Handling**: User-facing messages are calm and actionable; detailed logs support debugging

### Technology Stack

- **UI Framework**: WinUI 3 with Windows App SDK
- **Language**: C# with .NET 8.0
- **Image Decoding**: Windows Imaging Component (WIC) via WinUI APIs
- **Settings Storage**: JSON files in ApplicationData.Current.LocalFolder
- **Error Logging**: Structured text logs in ApplicationData.Current.LocalFolder/logs

### Key Design Decisions

1. **WinUI 3 over WPF**: Modern Fluent Design support, better Windows 11 integration, future-proof platform
2. **WIC for Decoding**: Native Windows support for PNG, JPEG, BMP, WEBP without external dependencies
3. **MVVM Pattern**: Clear separation between Views (XAML), ViewModels (state/commands), and Services (logic)
4. **Fading UI as Core System**: Not a visual effect added later; built into the architecture from the start
5. **Folder-as-Playlist Default**: No separate "open folder" feature; opening any image automatically creates the playlist

## Architecture

### High-Level Architecture

```
┌────────────────────────────────────────────────────────────┐
│                     Presentation Layer                     │
│    ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│    │ MainWindow   │  │ ImageCanvas  │  │ FadingChrome │    │
│    │              │  │              │  │              │    │
│    └──────────────┘  └──────────────┘  └──────────────┘    │
└────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────────┐
│                      ViewModel Layer                       │
│    ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│    │ MainViewModel│  │ ViewerState  │  │PlaylistState │    │
│    │              │  │              │  │              │    │
│    └──────────────┘  └──────────────┘  └──────────────┘    │
└────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────────┐
│                       Service Layer                        │
│    ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│    │FileOpenSvc   │  │ImageDecodeSvc│  │PlaylistSvc   │    │
│    └──────────────┘  └──────────────┘  └──────────────┘    │
│    ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│    │NavigationSvc │  │SettingsSvc   │  │ErrorLogSvc   │    │
│    └──────────────┘  └──────────────┘  └──────────────┘    │
└────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────────┐
│                        Model Layer                         │
│    ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│    │  ImageItem   │  │ImageMetadata │  │ViewerSettings│    │
│    └──────────────┘  └──────────────┘  └──────────────┘    │
│    ┌──────────────┐                                        │
│    │ ViewerError  │                                        │
│    └──────────────┘                                        │
└────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

**Presentation Layer**
- Renders UI elements (window, canvas, chrome, info bar)
- Handles user input (keyboard, mouse, touch)
- Manages window state (size, position, fullscreen)
- Triggers UI animations (fade in/out)
- Binds to ViewModel properties and commands

**ViewModel Layer**
- Exposes observable properties for data binding
- Implements commands for user actions
- Maintains UI state (current image, zoom level, UI visibility)
- Coordinates service calls
- Transforms service results into presentation-ready data

**Service Layer**
- Implements business logic (file validation, image decoding, playlist scanning)
- Performs I/O operations (file system, settings, logs)
- Returns structured results (success/error)
- Operates independently of UI concerns
- Designed for testability

**Model Layer**
- Defines data structures
- Implements value equality where appropriate
- Contains no business logic
- Serializable for settings persistence

### Module Structure

```
src/ImageViewer.App/
├── Views/
│   ├── MainWindow.xaml              # Main application window
│   ├── MainWindow.xaml.cs
│   ├── ImageCanvas.xaml             # Image display control
│   ├── ImageCanvas.xaml.cs
│   ├── FadingChrome.xaml            # Top menu + bottom info with fade behavior
│   ├── FadingChrome.xaml.cs
│   └── BottomInfoBar.xaml           # Metadata display bar
│       BottomInfoBar.xaml.cs
│
├── ViewModels/
│   ├── MainViewModel.cs             # Primary ViewModel, coordinates all operations
│   ├── ViewerState.cs               # Current image, zoom, display mode state
│   └── PlaylistState.cs             # Playlist items, current index, navigation state
│
├── Services/
│   ├── FileOpenService.cs           # File selection and validation
│   ├── ImageDecodeService.cs        # Image decoding and format detection
│   ├── FolderPlaylistService.cs     # Folder scanning and playlist building
│   ├── NavigationService.cs         # Playlist navigation logic
│   ├── SettingsService.cs           # Settings persistence
│   └── ErrorLogService.cs           # Structured error logging
│
├── Models/
│   ├── ImageItem.cs                 # Represents a single image in the playlist
│   ├── ImageMetadata.cs             # Image dimensions, file size, etc.
│   ├── ViewerSettings.cs            # User preferences
│   └── ViewerError.cs               # Structured error information
│
├── Resources/
│   ├── Styles/
│   │   ├── ButtonStyles.xaml        # Rounded button styles
│   │   ├── PanelStyles.xaml         # Rounded panel styles
│   │   └── ThemeResources.xaml      # Light/dark theme colors
│   └── ThemeTokens/
│       ├── DarkTheme.xaml           # Dark theme color definitions
│       └── LightTheme.xaml          # Light theme color definitions
│
├── App.xaml                         # Application entry point
├── App.xaml.cs
└── Package.appxmanifest             # Windows app manifest

tests/ImageViewer.Tests/
├── Services/
│   ├── FileOpenServiceTests.cs
│   ├── ImageDecodeServiceTests.cs
│   ├── FolderPlaylistServiceTests.cs
│   ├── NavigationServiceTests.cs
│   ├── SettingsServiceTests.cs
│   └── ErrorLogServiceTests.cs
│
└── Models/
    ├── ImageItemTests.cs
    └── ViewerSettingsTests.cs
```

## Components and Interfaces

### Presentation Layer Components

#### MainWindow

**Responsibility**: Host the primary application UI and manage window-level state.

**Key Properties**:
- `ViewModel`: MainViewModel instance
- `IsFullscreen`: Boolean indicating fullscreen state
- `WindowState`: Saved window dimensions and position

**Key Methods**:
- `ToggleFullscreen()`: Switch between windowed and fullscreen modes
- `OnKeyDown(KeyRoutedEventArgs)`: Route keyboard shortcuts to ViewModel
- `OnActivated()`: Restore UI visibility when window gains focus

**XAML Structure**:
```xml
<Window>
  <Grid>
    <ImageCanvas />
    <FadingChrome />
  </Grid>
</Window>
```

#### ImageCanvas

**Responsibility**: Display the current image with zoom, pan, and fit-to-window behavior.

**Key Properties**:
- `ImageSource`: BitmapImage to display
- `ZoomLevel`: Current zoom percentage (100 = actual size)
- `DisplayMode`: Enum (FitToWindow, ActualSize, Custom)
- `PanOffset`: Point representing current pan position

**Key Methods**:
- `SetImage(BitmapImage)`: Update displayed image
- `ZoomIn()`: Increase zoom by 25%
- `ZoomOut()`: Decrease zoom by 25%
- `ResetToFit()`: Return to fit-to-window mode
- `ResetToActualSize()`: Display at 100% scale
- `OnPointerMoved(PointerRoutedEventArgs)`: Handle pan dragging

**Rendering Algorithm**:
1. If DisplayMode is FitToWindow: Calculate scale to fit image within canvas bounds while preserving aspect ratio
2. If DisplayMode is ActualSize: Use 1:1 pixel mapping
3. If DisplayMode is Custom: Apply ZoomLevel percentage
4. Center image horizontally and vertically within available space
5. If zoomed beyond fit, enable pan and apply PanOffset

#### FadingChrome

**Responsibility**: Manage visibility and fade behavior for top menu bar and bottom info bar.

**Key Properties**:
- `IsVisible`: Boolean controlling opacity (1.0 visible, 0.1 faded)
- `FadeDelay`: TimeSpan before fade begins (default 2 seconds)
- `FadeDuration`: TimeSpan for fade animation (default 300ms)
- `IsPaused`: Boolean preventing fade (true when menu open)

**Key Methods**:
- `Show()`: Set opacity to 1.0 and reset fade timer
- `StartFadeTimer()`: Begin countdown to fade
- `StopFadeTimer()`: Cancel pending fade
- `FadeOut()`: Animate opacity from 1.0 to 0.1

**Fade Logic**:
```
User Activity Detected
  ↓
Show() called
  ↓
Opacity = 1.0
  ↓
Start 2-second timer
  ↓
Timer expires (if not reset)
  ↓
FadeOut() animates opacity to 0.1 over 300ms
```

**Activity Detection**:
- Mouse movement anywhere in window
- Keyboard input
- Image navigation
- Menu opened (also sets IsPaused = true)
- Fullscreen toggle

#### BottomInfoBar

**Responsibility**: Display current image metadata.

**Key Properties**:
- `Filename`: String (e.g., "IMG_0042.jpg")
- `PlaylistPosition`: String (e.g., "42 / 180")
- `Dimensions`: String (e.g., "3840 × 2160")
- `FolderName`: String (e.g., "Vacation Photos")

**Layout**:
```
┌────────────────────────────────────────────────────────────┐
│ IMG_0042.jpg          42 / 180          3840 × 2160        │
│                                         Vacation Photos    │
└────────────────────────────────────────────────────────────┘
```

### ViewModel Layer Components

#### MainViewModel

**Responsibility**: Coordinate all application operations and expose UI-bindable state.

**Dependencies**:
- FileOpenService
- ImageDecodeService
- FolderPlaylistService
- NavigationService
- SettingsService
- ErrorLogService

**Key Properties**:
- `ViewerState`: Current image and display state
- `PlaylistState`: Playlist and navigation state
- `Settings`: User preferences
- `IsChromeVisible`: Boolean for UI fade state
- `IsLoading`: Boolean for loading indicator
- `ErrorMessage`: String for user-facing error display

**Key Commands**:
- `OpenFileCommand`: Trigger file picker and open image
- `NavigateNextCommand`: Move to next image
- `NavigatePreviousCommand`: Move to previous image
- `NavigateFirstCommand`: Jump to first image
- `NavigateLastCommand`: Jump to last image
- `ZoomInCommand`: Increase zoom level
- `ZoomOutCommand`: Decrease zoom level
- `FitToWindowCommand`: Reset to fit mode
- `ActualSizeCommand`: Reset to 100% scale
- `ToggleFullscreenCommand`: Switch fullscreen state
- `ToggleThemeCommand`: Switch between light/dark themes

**Initialization Flow**:
```
App.OnLaunched()
  ↓
Create MainViewModel
  ↓
Load settings via SettingsService
  ↓
Apply theme preference
  ↓
If command-line file argument exists:
  ↓
  OpenFileCommand.Execute(filePath)
Else:
  ↓
  Show empty state
```

**Open Image Flow**:
```
OpenFileCommand.Execute(filePath)
  ↓
Set IsLoading = true
  ↓
FileOpenService.ValidateFile(filePath)
  ↓
If validation fails:
  ↓
  ErrorLogService.LogError(error)
  ↓
  Set ErrorMessage
  ↓
  Set IsLoading = false
  ↓
  Return
  ↓
ImageDecodeService.DecodeImage(filePath)
  ↓
If decode fails:
  ↓
  ErrorLogService.LogError(error)
  ↓
  Set ErrorMessage
  ↓
  Set IsLoading = false
  ↓
  Return
  ↓
FolderPlaylistService.BuildPlaylist(filePath)
  ↓
If playlist scan fails:
  ↓
  ErrorLogService.LogError(error)
  ↓
  Show warning but continue with single image
  ↓
Update ViewerState with decoded image
  ↓
Update PlaylistState with playlist
  ↓
Set IsLoading = false
  ↓
Show chrome briefly
```

#### ViewerState

**Responsibility**: Maintain current image display state.

**Properties**:
- `CurrentImage`: BitmapImage (the decoded image)
- `Metadata`: ImageMetadata (dimensions, file size)
- `ZoomLevel`: double (percentage, 100.0 = actual size)
- `DisplayMode`: DisplayMode enum (FitToWindow, ActualSize, Custom)
- `PanOffset`: Point (x, y offset for panning)
- `IsFullscreen`: bool

**Methods**:
- `SetImage(BitmapImage, ImageMetadata)`: Update current image
- `SetZoom(double)`: Update zoom level
- `SetDisplayMode(DisplayMode)`: Change display mode
- `ResetPan()`: Center image

#### PlaylistState

**Responsibility**: Maintain playlist and navigation state.

**Properties**:
- `Items`: ObservableCollection<ImageItem>
- `CurrentIndex`: int (0-based index)
- `TotalCount`: int (Items.Count)
- `CurrentItem`: ImageItem (Items[CurrentIndex])
- `CanNavigateNext`: bool (CurrentIndex < TotalCount - 1)
- `CanNavigatePrevious`: bool (CurrentIndex > 0)
- `FolderPath`: string (parent folder)

**Methods**:
- `SetPlaylist(List<ImageItem>, int initialIndex)`: Replace playlist
- `MoveNext()`: Increment CurrentIndex if possible
- `MovePrevious()`: Decrement CurrentIndex if possible
- `MoveFirst()`: Set CurrentIndex = 0
- `MoveLast()`: Set CurrentIndex = TotalCount - 1

### Service Layer Components

#### FileOpenService

**Responsibility**: Handle file selection and validation.

**Interface**:
```csharp
public interface IFileOpenService
{
    Task<Result<string>> PickFileAsync();
    Result<string> ValidateFilePath(string filePath);
}
```

**Methods**:

`PickFileAsync()`:
- Open Windows file picker
- Filter to supported extensions: .png, .jpg, .jpeg, .bmp, .webp
- Return selected file path or cancellation

`ValidateFilePath(string filePath)`:
- Check file exists
- Check file is readable
- Check extension is supported
- Return success with path or FileAccessError/UnsupportedFormatError

**Error Cases**:
- File does not exist → FileAccessError
- File is locked/inaccessible → FileAccessError
- Extension is deferred format (.gif, .tiff, .heic, .avif) → UnsupportedFormatError
- Extension is unrecognized → UnsupportedFormatError

#### ImageDecodeService

**Responsibility**: Decode image files and extract metadata.

**Interface**:
```csharp
public interface IImageDecodeService
{
    Task<Result<DecodedImage>> DecodeImageAsync(string filePath);
    bool IsSupportedFormat(string extension);
    bool IsDeferredFormat(string extension);
}
```

**Methods**:

`DecodeImageAsync(string filePath)`:
- Use WinUI BitmapImage or WIC decoder
- Decode on background thread
- Extract pixel dimensions and file size
- Return BitmapImage + ImageMetadata or DecodeError

`IsSupportedFormat(string extension)`:
- Case-insensitive check against: .png, .jpg, .jpeg, .bmp, .webp
- Return true if supported

`IsDeferredFormat(string extension)`:
- Case-insensitive check against: .gif, .tiff, .tif, .heic, .avif
- Return true if deferred

**Decoding Algorithm**:
```
1. Open file stream
2. Create BitmapImage
3. Set DecodePixelWidth/Height based on screen DPI (optional optimization)
4. Call SetSourceAsync(stream)
5. Await decode completion
6. Extract PixelWidth, PixelHeight from BitmapImage
7. Get file size from FileInfo
8. Return DecodedImage { Image, Metadata }
```

**Error Cases**:
- File is corrupted → DecodeError
- File format is unsupported variant → DecodeError
- Insufficient memory → DecodeError with specific message
- I/O error during read → DecodeError

**Memory Management**:
- Decode on background thread to avoid UI blocking
- Release previous image when navigating (handled by ViewModel)
- No preloading in Phase 1

#### FolderPlaylistService

**Responsibility**: Scan folders and build playlists.

**Interface**:
```csharp
public interface IFolderPlaylistService
{
    Task<Result<Playlist>> BuildPlaylistAsync(string openedFilePath);
}
```

**Methods**:

`BuildPlaylistAsync(string openedFilePath)`:
- Extract parent folder from openedFilePath
- Scan folder for files with supported extensions
- Sort files alphabetically by filename
- Find index of opened file
- Return Playlist { Items, InitialIndex, FolderPath } or PlaylistScanError

**Scanning Algorithm**:
```
1. Get parent directory from openedFilePath
2. Call Directory.GetFiles(folderPath)
3. Filter to supported extensions using ImageDecodeService.IsSupportedFormat()
4. Sort by filename (case-insensitive, natural sort if possible)
5. Create ImageItem for each file:
   - FilePath
   - Filename (without path)
   - Extension
6. Find index where FilePath matches openedFilePath
7. Return Playlist
```

**Sorting**:
- Alphabetical by filename (case-insensitive)
- Natural sort preferred (e.g., "img2.jpg" before "img10.jpg")
- Use StringComparer.OrdinalIgnoreCase or natural sort library

**Error Cases**:
- Folder access denied → PlaylistScanError
- Folder does not exist → PlaylistScanError
- I/O error during scan → PlaylistScanError

**Edge Cases**:
- Folder contains only opened image → Return playlist with 1 item
- Opened file is deleted during scan → Return playlist without it, log warning
- Folder contains thousands of images → Scan completes but may be slow (acceptable for Phase 1)

#### NavigationService

**Responsibility**: Handle playlist navigation logic.

**Interface**:
```csharp
public interface INavigationService
{
    Task<Result<DecodedImage>> NavigateToIndexAsync(
        PlaylistState playlist, 
        int targetIndex,
        IImageDecodeService decodeService);
}
```

**Methods**:

`NavigateToIndexAsync(PlaylistState playlist, int targetIndex, IImageDecodeService decodeService)`:
- Validate targetIndex is within bounds
- Get ImageItem at targetIndex
- Call decodeService.DecodeImageAsync()
- Return decoded image or NavigationError

**Navigation Logic**:
```
1. Check targetIndex >= 0 && targetIndex < playlist.TotalCount
2. If out of bounds, return error
3. Get filePath from playlist.Items[targetIndex]
4. Call ImageDecodeService.DecodeImageAsync(filePath)
5. If decode fails, return NavigationError
6. Return DecodedImage
```

**Error Cases**:
- Target index out of bounds → NavigationError
- Target file no longer exists → NavigationError
- Target file decode fails → NavigationError

**Boundary Behavior**:
- Attempting to navigate past last image → Stay on last image, no error
- Attempting to navigate before first image → Stay on first image, no error
- Single-image playlist → Disable navigation commands

#### SettingsService

**Responsibility**: Persist and load user preferences.

**Interface**:
```csharp
public interface ISettingsService
{
    Task<Result<ViewerSettings>> LoadSettingsAsync();
    Task<Result> SaveSettingsAsync(ViewerSettings settings);
}
```

**Methods**:

`LoadSettingsAsync()`:
- Read settings.json from ApplicationData.Current.LocalFolder
- Deserialize to ViewerSettings
- Return settings or default settings if file doesn't exist
- Return SettingsPersistenceError if read/parse fails

`SaveSettingsAsync(ViewerSettings settings)`:
- Serialize settings to JSON
- Write to ApplicationData.Current.LocalFolder/settings.json
- Return success or SettingsPersistenceError

**Storage Location**:
- Path: `ApplicationData.Current.LocalFolder/settings.json`
- Example: `C:\Users\{Username}\AppData\Local\Packages\{AppPackage}\LocalState\settings.json`

**Settings Format**:
```json
{
  "theme": "Dark",
  "defaultDisplayMode": "FitToWindow",
  "windowWidth": 1200,
  "windowHeight": 800,
  "windowX": 100,
  "windowY": 100,
  "fadeDelay": 2000,
  "fadeDuration": 300
}
```

**Default Values**:
- Theme: System preference
- DefaultDisplayMode: FitToWindow
- Window dimensions: 1200 × 800, centered on primary monitor
- FadeDelay: 2000ms
- FadeDuration: 300ms

**Error Cases**:
- File read fails → Return default settings, log SettingsPersistenceError
- JSON parse fails → Return default settings, log SettingsPersistenceError
- File write fails → Log SettingsPersistenceError, continue with in-memory settings

#### ErrorLogService

**Responsibility**: Write structured error logs to disk.

**Interface**:
```csharp
public interface IErrorLogService
{
    Task LogErrorAsync(ViewerError error);
}
```

**Methods**:

`LogErrorAsync(ViewerError error)`:
- Format error as structured log entry
- Append to log file in ApplicationData.Current.LocalFolder/logs
- Create new log file daily (e.g., `viewer-2024-01-15.log`)

**Log Entry Format**:
```
[2024-01-15 14:32:18.456] [ERROR] [DecodeError]
Operation: DecodeImage
File: C:\Photos\IMG_0042.jpg
Message: Unable to decode this image. The file may be corrupted or in an unsupported variant.
Exception: System.IO.IOException
Stack Trace:
  at ImageViewer.Services.ImageDecodeService.DecodeImageAsync(String filePath)
  ...
---
```

**Log File Management**:
- Location: `ApplicationData.Current.LocalFolder/logs/`
- Naming: `viewer-{yyyy-MM-dd}.log`
- Rotation: New file each day
- Retention: Keep last 30 days (cleanup on app start)

**Error Categories**:
- FileAccessError
- UnsupportedFormatError
- DecodeError
- PlaylistScanError
- NavigationError
- SettingsPersistenceError
- UnexpectedViewerError

## Data Models

### ImageItem

**Purpose**: Represent a single image file in the playlist.

**Properties**:
```csharp
public class ImageItem
{
    public string FilePath { get; init; }      // Full path: C:\Photos\IMG_0042.jpg
    public string Filename { get; init; }      // Filename only: IMG_0042.jpg
    public string Extension { get; init; }     // Extension: .jpg
}
```

**Equality**: Value equality based on FilePath (case-insensitive)

**Usage**: Stored in PlaylistState.Items collection

### ImageMetadata

**Purpose**: Store image dimensions and file information.

**Properties**:
```csharp
public class ImageMetadata
{
    public int PixelWidth { get; init; }       // Image width in pixels
    public int PixelHeight { get; init; }      // Image height in pixels
    public long FileSizeBytes { get; init; }   // File size in bytes
    public string FormattedDimensions { get; }  // "3840 × 2160"
    public string FormattedFileSize { get; }    // "2.4 MB"
}
```

**Computed Properties**:
- `FormattedDimensions`: Returns "{PixelWidth} × {PixelHeight}"
- `FormattedFileSize`: Returns human-readable size (KB, MB, GB)

**Usage**: Stored in ViewerState, displayed in BottomInfoBar

### ViewerSettings

**Purpose**: Store user preferences for persistence.

**Properties**:
```csharp
public class ViewerSettings
{
    public string Theme { get; set; }                    // "Light", "Dark", "System"
    public string DefaultDisplayMode { get; set; }       // "FitToWindow", "ActualSize"
    public int WindowWidth { get; set; }                 // Window width in pixels
    public int WindowHeight { get; set; }                // Window height in pixels
    public int WindowX { get; set; }                     // Window X position
    public int WindowY { get; set; }                     // Window Y position
    public int FadeDelayMs { get; set; }                 // Fade delay in milliseconds
    public int FadeDurationMs { get; set; }              // Fade duration in milliseconds
}
```

**Serialization**: JSON via System.Text.Json

**Validation**:
- Theme must be "Light", "Dark", or "System"
- DefaultDisplayMode must be "FitToWindow" or "ActualSize"
- Window dimensions must be >= 400 × 300
- FadeDelayMs must be >= 0
- FadeDurationMs must be >= 0

**Usage**: Loaded on app start, saved on settings change and app exit

### ViewerError

**Purpose**: Represent structured error information for logging and display.

**Properties**:
```csharp
public class ViewerError
{
    public DateTime Timestamp { get; init; }
    public string Severity { get; init; }              // "Error", "Warning"
    public string Category { get; init; }              // Error category enum as string
    public string UserMessage { get; init; }           // User-facing message
    public string Operation { get; init; }             // Operation that failed
    public string? FilePath { get; init; }             // Optional file path
    public string? ExceptionType { get; init; }        // Exception type name
    public string? StackTrace { get; init; }           // Full stack trace
}
```

**Factory Methods**:
```csharp
public static ViewerError FileAccessError(string filePath, Exception ex);
public static ViewerError UnsupportedFormatError(string filePath);
public static ViewerError DecodeError(string filePath, Exception ex);
public static ViewerError PlaylistScanError(string folderPath, Exception ex);
public static ViewerError NavigationError(string operation, Exception ex);
public static ViewerError SettingsPersistenceError(string operation, Exception ex);
public static ViewerError UnexpectedError(string operation, Exception ex);
```

**User Messages** (defined in factory methods):
- FileAccessError: "Unable to access this file. It may have been moved, deleted, or you may not have permission to open it."
- UnsupportedFormatError: "This file format is not supported in this version."
- DecodeError: "Unable to decode this image. The file may be corrupted or in an unsupported variant."
- DecodeError (memory): "This image is too large to display."
- PlaylistScanError: "Unable to scan the folder for additional images. You can still view this image."
- NavigationError: "Unable to load the requested image."
- SettingsPersistenceError: "Unable to save settings. Your preferences may not persist."

**Usage**: Created by services, logged by ErrorLogService, displayed by MainViewModel

### Result Types

**Purpose**: Represent operation outcomes without throwing exceptions for expected failures.

**Generic Result**:
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public ViewerError? Error { get; }
    
    public static Result<T> Success(T value);
    public static Result<T> Failure(ViewerError error);
}

public class Result
{
    public bool IsSuccess { get; }
    public ViewerError? Error { get; }
    
    public static Result Success();
    public static Result Failure(ViewerError error);
}
```

**Usage**: All service methods return Result or Result<T> instead of throwing exceptions for expected failures

### Enums

**DisplayMode**:
```csharp
public enum DisplayMode
{
    FitToWindow,    // Scale to fit within window bounds
    ActualSize,     // Display at 100% scale (1:1 pixels)
    Custom          // User-defined zoom level
}
```

**ErrorCategory**:
```csharp
public enum ErrorCategory
{
    FileAccessError,
    UnsupportedFormatError,
    DecodeError,
    PlaylistScanError,
    NavigationError,
    SettingsPersistenceError,
    UnexpectedViewerError
}
```


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Extension Classification Correctness

*For any* file path with a supported extension (.png, .jpg, .jpeg, .bmp, .webp), the File_Open_Service SHALL classify it as supported regardless of case, and *for any* file path with a deferred extension (.gif, .tiff, .tif, .heic, .avif), the service SHALL classify it as deferred, and *for any* file path with an unrecognized extension, the service SHALL classify it as unsupported.

**Validates: Requirements 1.1, 1.4, 1.5, 12.3, 12.4, 12.5**

### Property 2: Playlist Building Preserves Order and Selection

*For any* folder containing supported image files, when the Folder_Playlist_Service builds a playlist from an opened file, the resulting playlist SHALL contain only files with supported extensions, SHALL be sorted alphabetically by filename (case-insensitive), SHALL set the current index to the opened file's position, and SHALL report the correct total count.

**Validates: Requirements 3.1, 3.2, 3.3, 3.6**

### Property 3: Navigation Maintains Playlist Boundaries

*For any* playlist with N items and current index I, navigating next SHALL move to index I+1 when I < N-1 and remain at I when I = N-1, navigating previous SHALL move to index I-1 when I > 0 and remain at I when I = 0, navigating first SHALL move to index 0, and navigating last SHALL move to index N-1.

**Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 16.1, 16.2**

### Property 4: Settings Serialization Round-Trip

*For any* valid ViewerSettings object, serializing to JSON and then deserializing SHALL produce an equivalent ViewerSettings object with all properties preserved.

**Validates: Requirements 9.1, 9.2, 9.3**

### Property 5: Error Categorization Consistency

*For any* error that occurs during file access, format detection, image decoding, playlist scanning, navigation, or settings persistence, the Error_Log_Service SHALL assign the correct error category (FileAccessError, UnsupportedFormatError, DecodeError, PlaylistScanError, NavigationError, or SettingsPersistenceError) and SHALL include all required fields (timestamp, severity, category, user message, operation, optional file path, exception type, stack trace).

**Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7, 10.8**

### Property 6: Error Messages Are User-Friendly

*For any* ViewerError of a specific category, the user-facing message SHALL match the specified calm, actionable message for that category and SHALL NOT contain raw exception text or stack traces.

**Validates: Requirements 11.1, 11.2, 11.3, 11.4, 11.5, 11.6**

### Property 7: Metadata Formatting Consistency

*For any* ImageMetadata with valid pixel dimensions and file size, the FormattedDimensions property SHALL return "{width} × {height}" format and the FormattedFileSize property SHALL return a human-readable size string with appropriate units (B, KB, MB, GB).

**Validates: Requirements 8.1, 8.2, 8.3**

## Error Handling

### Error Handling Philosophy

Phase 1 implements a structured error handling system that separates user-facing concerns from diagnostic information. The system follows these principles:

1. **Expected Failures Are Not Exceptions**: Services return Result<T> types instead of throwing exceptions for expected failures (file not found, unsupported format, etc.)
2. **User Messages Are Calm and Actionable**: Error messages explain what happened and what the user can do, without technical jargon
3. **Diagnostic Information Is Logged**: Full exception details, stack traces, and context are written to log files for debugging
4. **Errors Don't Block Progress**: When possible, errors are logged and the app continues (e.g., playlist scan fails but single image still viewable)

### Error Categories and Handling

#### FileAccessError

**Triggers**:
- File does not exist
- File is locked by another process
- Insufficient permissions to read file
- Network path is unavailable

**User Message**: "Unable to access this file. It may have been moved, deleted, or you may not have permission to open it."

**Handling**:
- Log error with full exception details
- Display user message in UI
- Return to previous state or empty state

**Recovery**: User can try another file or check file permissions

#### UnsupportedFormatError

**Triggers**:
- File extension is in deferred format list (.gif, .tiff, .heic, .avif)
- File extension is not recognized

**User Message**: "This file format is not supported in this version."

**Handling**:
- Log error with file path and extension
- Display user message in UI
- Return to previous state or empty state

**Recovery**: User can try a supported format or wait for future phase

#### DecodeError

**Triggers**:
- File is corrupted or invalid
- File format is unsupported variant
- Insufficient memory to decode image
- WIC decoder fails

**User Message**: 
- Standard: "Unable to decode this image. The file may be corrupted or in an unsupported variant."
- Memory: "This image is too large to display."

**Handling**:
- Log error with full exception details
- Display appropriate user message
- Return to previous state or empty state

**Recovery**: User can try another file or close other applications to free memory

#### PlaylistScanError

**Triggers**:
- Folder access denied
- Folder does not exist (deleted during operation)
- I/O error during folder scan

**User Message**: "Unable to scan the folder for additional images. You can still view this image."

**Handling**:
- Log error with folder path and exception
- Display user message as warning (not blocking)
- Create single-item playlist with opened image
- Continue with limited functionality

**Recovery**: User can still view the opened image; navigation is disabled

#### NavigationError

**Triggers**:
- Target file deleted during session
- Target file becomes inaccessible
- Decode fails for target image

**User Message**: "Unable to load the requested image."

**Handling**:
- Log error with target file path and exception
- Display user message
- Remain on current image
- Optionally remove failed item from playlist

**Recovery**: User can try navigating to a different image

#### SettingsPersistenceError

**Triggers**:
- Settings file is corrupted
- Settings file location is not writable
- JSON deserialization fails
- I/O error during read/write

**User Message**: "Unable to save settings. Your preferences may not persist."

**Handling**:
- Log error with operation and exception
- Display user message as warning (not blocking)
- Use default settings if load fails
- Continue with in-memory settings if save fails

**Recovery**: App continues with defaults; settings may not persist between sessions

#### UnexpectedViewerError

**Triggers**:
- Any unhandled exception
- Unexpected null reference
- Programming errors

**User Message**: "An unexpected error occurred. The application will attempt to continue."

**Handling**:
- Log error with full context and stack trace
- Display generic user message
- Attempt to recover to stable state
- If recovery fails, offer to restart

**Recovery**: Varies by context; may require app restart

### Error Logging Implementation

**Log File Format**:
```
[2024-01-15 14:32:18.456] [ERROR] [DecodeError]
Operation: DecodeImage
File: C:\Photos\IMG_0042.jpg
Message: Unable to decode this image. The file may be corrupted or in an unsupported variant.
Exception: System.IO.IOException: The file is corrupted
Stack Trace:
  at ImageViewer.Services.ImageDecodeService.DecodeImageAsync(String filePath) in C:\...\ImageDecodeService.cs:line 42
  at ImageViewer.ViewModels.MainViewModel.OpenFileAsync(String filePath) in C:\...\MainViewModel.cs:line 87
---
```

**Log File Management**:
- Location: `ApplicationData.Current.LocalFolder/logs/`
- Naming: `viewer-{yyyy-MM-dd}.log`
- Rotation: New file created each day
- Retention: Keep last 30 days, delete older logs on app start
- Size: No size limit per file (daily rotation prevents excessive growth)

**Logging Levels**:
- ERROR: All error categories
- WARNING: Non-blocking errors (PlaylistScanError, SettingsPersistenceError)
- INFO: Not used in Phase 1 (reserved for future diagnostic logging)

### Error Display in UI

**Error Display Component**: Lightweight info bar at top of window

**Display Behavior**:
- Appears when error occurs
- Shows user-friendly message
- Includes dismiss button
- Auto-dismisses after 10 seconds for warnings
- Remains visible for errors until dismissed
- Does not block interaction with rest of UI

**Visual Design**:
- Error: Red accent, warning icon
- Warning: Yellow accent, info icon
- Rounded corners (8px)
- Subtle shadow
- Smooth slide-in animation

## Testing Strategy

### Testing Approach

Phase 1 uses a dual testing approach:

1. **Property-Based Tests**: Verify universal properties across randomized inputs for pure logic components
2. **Example-Based Unit Tests**: Verify specific scenarios, edge cases, and integration points
3. **Integration Tests**: Verify file I/O, WIC decoding, and settings persistence with real files
4. **Manual Testing**: Verify UI behavior, visual polish, and user experience

### Property-Based Testing

**Framework**: Use **FsCheck** (F# property testing library with C# support) or **CsCheck** (C#-native property testing)

**Configuration**:
- Minimum 100 iterations per property test
- Each test tagged with comment referencing design property
- Tag format: `// Feature: phase-1-mvp-viewer, Property {number}: {property_text}`

**Property Tests to Implement**:

1. **Extension Classification** (Property 1)
   - Generate: Random file paths with various extensions (supported, deferred, unknown) and mixed case
   - Verify: Correct classification for each category
   - Edge cases: Empty extension, multiple dots, no extension, very long extension

2. **Playlist Building** (Property 2)
   - Generate: Random folder structures with mixed file types and various naming patterns
   - Verify: Only supported formats included, alphabetical sorting, correct index, correct count
   - Edge cases: Empty folder, single file, thousands of files, special characters in names

3. **Navigation Boundaries** (Property 3)
   - Generate: Playlists of various sizes (1 to 1000 items) with random starting positions
   - Verify: Navigation respects boundaries, index calculations correct
   - Edge cases: Single-item playlist, navigation at boundaries, jump to first/last

4. **Settings Round-Trip** (Property 4)
   - Generate: Random ViewerSettings with various valid values
   - Verify: Serialize → Deserialize produces equivalent object
   - Edge cases: Default values, extreme window sizes, zero fade delays

5. **Error Categorization** (Property 5)
   - Generate: Various error scenarios (simulated exceptions, file paths, operations)
   - Verify: Correct category assigned, all required fields present
   - Edge cases: Null file paths, empty messages, nested exceptions

6. **Error Messages** (Property 6)
   - Generate: Errors of each category
   - Verify: User message matches specification, no raw exception text
   - Edge cases: Exceptions with special characters, very long stack traces

7. **Metadata Formatting** (Property 7)
   - Generate: Random pixel dimensions (1 to 100000) and file sizes (0 to 10GB)
   - Verify: Formatted strings match expected patterns
   - Edge cases: Zero dimensions, very large dimensions, byte-level file sizes

**Example Property Test** (C# with CsCheck):
```csharp
// Feature: phase-1-mvp-viewer, Property 1: Extension Classification Correctness
[Fact]
public void ExtensionClassification_CorrectlyIdentifiesAllCategories()
{
    Gen.Select(
        Gen.Const(".png", ".jpg", ".jpeg", ".bmp", ".webp"),  // Supported
        Gen.Const(".gif", ".tiff", ".tif", ".heic", ".avif"), // Deferred
        Gen.String[1, 10].Where(s => !IsSupportedOrDeferred(s)) // Unknown
    )
    .Select(ext => Gen.String[1, 50].Select(name => $"{name}{ext}"))
    .Sample(path =>
    {
        var service = new ImageDecodeService();
        var ext = Path.GetExtension(path).ToLowerInvariant();
        
        if (SupportedExtensions.Contains(ext))
            Assert.True(service.IsSupportedFormat(path));
        else if (DeferredExtensions.Contains(ext))
            Assert.True(service.IsDeferredFormat(path));
        else
            Assert.False(service.IsSupportedFormat(path) || service.IsDeferredFormat(path));
    }, 
    iter: 100);
}
```

### Example-Based Unit Tests

**Framework**: xUnit with FluentAssertions

**Test Categories**:

1. **Service Tests**:
   - FileOpenService: Test specific file paths, validation logic
   - ImageDecodeService: Test format detection with known extensions
   - FolderPlaylistService: Test specific folder structures
   - NavigationService: Test specific navigation scenarios
   - SettingsService: Test default values, validation
   - ErrorLogService: Test log file creation, formatting

2. **Model Tests**:
   - ImageItem: Test equality, property initialization
   - ImageMetadata: Test formatting with specific values
   - ViewerSettings: Test validation, defaults
   - ViewerError: Test factory methods, message generation

3. **ViewModel Tests**:
   - MainViewModel: Test command execution, state updates
   - ViewerState: Test state transitions
   - PlaylistState: Test collection management

**Example Unit Test**:
```csharp
[Fact]
public void FolderPlaylistService_SingleImageFolder_ReturnsOneItemPlaylist()
{
    // Arrange
    var testFolder = CreateTestFolder(new[] { "image.png" });
    var service = new FolderPlaylistService(new ImageDecodeService());
    
    // Act
    var result = await service.BuildPlaylistAsync(Path.Combine(testFolder, "image.png"));
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Items.Should().HaveCount(1);
    result.Value.CurrentIndex.Should().Be(0);
    result.Value.TotalCount.Should().Be(1);
}
```

### Integration Tests

**Test Scenarios**:

1. **File I/O**:
   - Create temporary test files
   - Test actual file reading and validation
   - Test folder scanning with real directories
   - Test settings persistence to disk

2. **Image Decoding**:
   - Use real sample images (PNG, JPEG, BMP, WEBP)
   - Verify WIC decoding succeeds
   - Verify metadata extraction
   - Test with corrupted files

3. **Error Logging**:
   - Verify log files are created
   - Verify log entries are written
   - Verify log rotation works

**Example Integration Test**:
```csharp
[Fact]
public async Task ImageDecodeService_RealPngFile_DecodesSuccessfully()
{
    // Arrange
    var testImagePath = Path.Combine(TestDataFolder, "sample.png");
    var service = new ImageDecodeService();
    
    // Act
    var result = await service.DecodeImageAsync(testImagePath);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Image.Should().NotBeNull();
    result.Value.Metadata.PixelWidth.Should().BeGreaterThan(0);
    result.Value.Metadata.PixelHeight.Should().BeGreaterThan(0);
}
```

### Manual Testing

**Test Scenarios**:

1. **UI Behavior**:
   - Fading chrome timing and animation
   - Fullscreen transitions
   - Window resizing and positioning
   - Theme switching visual appearance

2. **Keyboard Shortcuts**:
   - All navigation shortcuts in windowed mode
   - All navigation shortcuts in fullscreen mode
   - Zoom shortcuts
   - Display mode shortcuts

3. **Image Display**:
   - Fit-to-window scaling
   - Actual size display
   - Zoom and pan feel
   - Image centering

4. **Performance**:
   - Image loading speed for various file sizes
   - Navigation responsiveness
   - Memory usage with large images
   - Startup time

5. **Visual Polish**:
   - Rounded corners on all UI elements
   - Smooth animations
   - Consistent spacing and alignment
   - Dark/light theme appearance

**Manual Test Checklist**: Create a checklist document for each release covering all manual test scenarios

### Test Coverage Goals

- **Services**: 90%+ code coverage with unit and integration tests
- **Models**: 100% code coverage with unit tests
- **ViewModels**: 80%+ code coverage with unit tests
- **Views**: Manual testing for UI behavior, no automated coverage target

### Continuous Testing

- Run unit tests on every build
- Run integration tests before commits
- Run property tests in CI pipeline (may take longer)
- Perform manual testing before releases

## Implementation Guidance

### Development Phases

**Phase 1A: Foundation (Week 1-2)**
1. Set up WinUI 3 project structure
2. Implement Models (ImageItem, ImageMetadata, ViewerSettings, ViewerError)
3. Implement Result types
4. Write unit tests for models
5. Set up dependency injection container

**Phase 1B: Services (Week 3-4)**
1. Implement FileOpenService with validation
2. Implement ImageDecodeService with WIC integration
3. Implement FolderPlaylistService with scanning and sorting
4. Implement NavigationService
5. Implement SettingsService with JSON persistence
6. Implement ErrorLogService
7. Write unit and integration tests for all services
8. Write property-based tests

**Phase 1C: ViewModels (Week 5)**
1. Implement ViewerState
2. Implement PlaylistState
3. Implement MainViewModel with commands
4. Wire up service dependencies
5. Write unit tests for ViewModels

**Phase 1D: Views (Week 6-7)**
1. Implement MainWindow with basic layout
2. Implement ImageCanvas with zoom/pan
3. Implement FadingChrome with animation
4. Implement BottomInfoBar
5. Wire up data binding
6. Implement keyboard shortcuts

**Phase 1E: Polish (Week 8)**
1. Implement theme support
2. Create rounded corner styles
3. Implement fullscreen mode
4. Tune fade timing and animations
5. Manual testing and bug fixes
6. Performance optimization

### Key Implementation Details

#### WinUI 3 Project Setup

**Project Type**: WinUI 3 in Desktop (Windows App SDK)

**Target Framework**: .NET 8.0

**NuGet Packages**:
- Microsoft.WindowsAppSDK (latest stable)
- Microsoft.Extensions.DependencyInjection (for DI)
- System.Text.Json (for settings serialization)
- CsCheck or FsCheck (for property-based testing)
- xUnit (for unit testing)
- FluentAssertions (for test assertions)

**App.xaml.cs Setup**:
```csharp
public partial class App : Application
{
    private IServiceProvider _serviceProvider;
    
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Configure DI container
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        
        // Create main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Activate();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IFileOpenService, FileOpenService>();
        services.AddSingleton<IImageDecodeService, ImageDecodeService>();
        services.AddSingleton<IFolderPlaylistService, FolderPlaylistService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IErrorLogService, ErrorLogService>();
        
        // Register ViewModels
        services.AddTransient<MainViewModel>();
        
        // Register Views
        services.AddTransient<MainWindow>();
    }
}
```

#### Image Decoding with WIC

**Using BitmapImage**:
```csharp
public async Task<Result<DecodedImage>> DecodeImageAsync(string filePath)
{
    try
    {
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
        
        return Result<DecodedImage>.Success(new DecodedImage(bitmapImage, metadata));
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

#### Fading Chrome Implementation

**XAML**:
```xml
<Grid x:Name="ChromeContainer" Opacity="1.0">
    <Grid.Transitions>
        <TransitionCollection>
            <RepositionThemeTransition />
        </TransitionCollection>
    </Grid.Transitions>
    
    <!-- Top menu bar -->
    <Grid VerticalAlignment="Top" Background="{ThemeResource ChromeBackgroundBrush}">
        <MenuBar />
    </Grid>
    
    <!-- Bottom info bar -->
    <Grid VerticalAlignment="Bottom" Background="{ThemeResource ChromeBackgroundBrush}">
        <BottomInfoBar />
    </Grid>
</Grid>
```

**Code-behind**:
```csharp
private DispatcherTimer _fadeTimer;
private const int FadeDelayMs = 2000;
private const int FadeDurationMs = 300;

public void ShowChrome()
{
    // Cancel any pending fade
    _fadeTimer?.Stop();
    
    // Show chrome immediately
    var fadeIn = new DoubleAnimation
    {
        To = 1.0,
        Duration = TimeSpan.FromMilliseconds(100)
    };
    Storyboard.SetTarget(fadeIn, ChromeContainer);
    Storyboard.SetTargetProperty(fadeIn, "Opacity");
    
    var storyboard = new Storyboard();
    storyboard.Children.Add(fadeIn);
    storyboard.Begin();
    
    // Start fade timer
    _fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FadeDelayMs) };
    _fadeTimer.Tick += (s, e) => FadeOutChrome();
    _fadeTimer.Start();
}

private void FadeOutChrome()
{
    _fadeTimer?.Stop();
    
    var fadeOut = new DoubleAnimation
    {
        To = 0.1,
        Duration = TimeSpan.FromMilliseconds(FadeDurationMs)
    };
    Storyboard.SetTarget(fadeOut, ChromeContainer);
    Storyboard.SetTargetProperty(fadeOut, "Opacity");
    
    var storyboard = new Storyboard();
    storyboard.Children.Add(fadeOut);
    storyboard.Begin();
}
```

#### Natural Sorting for Filenames

**Use NaturalSort library or implement**:
```csharp
public class NaturalStringComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        // Implementation of natural sort algorithm
        // "img2.jpg" comes before "img10.jpg"
        // Use regex to split into text and number segments
        // Compare segments appropriately
    }
}
```

### Performance Considerations

1. **Async Image Decoding**: Always decode on background thread to prevent UI blocking
2. **Memory Management**: Release previous image when navigating to avoid memory buildup
3. **Lazy Loading**: Don't preload adjacent images in Phase 1 (deferred to Phase 2)
4. **Efficient Rendering**: Use WinUI's built-in image scaling rather than manual bitmap manipulation
5. **Settings Caching**: Load settings once on startup, cache in memory, save only on change

### Extensibility Points for Phase 2/3

1. **Format Detection**: Centralized in ImageDecodeService, easy to add new formats
2. **Playlist Building**: FolderPlaylistService can be extended with filtering, search
3. **Navigation**: NavigationService can support preloading, history
4. **Settings**: ViewerSettings can be extended with new preferences
5. **Error Handling**: Error categories can be extended for new features

## Acceptance Criteria

Phase 1 is complete when:

1. A user can open a supported local image (PNG, JPG, JPEG, BMP, WEBP) and immediately browse sibling images in the same folder
2. The viewer automatically treats the opened file's parent folder as the playlist without manual configuration
3. Keyboard navigation (Left/Right/Home/End) works correctly with proper boundary handling
4. The UI chrome fades after 2 seconds of inactivity in both windowed and fullscreen modes
5. The UI chrome reappears on mouse movement, keyboard input, or image change
6. Images display correctly in fit-to-window mode by default
7. Zoom in/out and actual size mode work correctly
8. Pan behavior works when zoomed beyond fit
9. Fullscreen mode can be toggled with F11 or double-click
10. Metadata (filename, playlist position, dimensions, folder name) displays correctly
11. Theme switching between light and dark works correctly
12. Window size and position persist between sessions
13. Unsupported or corrupt files show user-friendly error messages
14. All errors are logged to disk with structured information
15. Deferred formats (GIF, TIFF, HEIC, AVIF) show "not supported in this version" message
16. All correctness properties pass property-based tests with 100+ iterations
17. Service layer has 90%+ unit test coverage
18. Manual testing confirms UI polish, smooth animations, and responsive feel
19. Phase 2 and Phase 3 features remain documented in roadmap.md
20. Service interfaces support future extensibility without breaking changes

