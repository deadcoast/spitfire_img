# Design Document: Phase 3 Advanced Viewing Modes

## Overview

### Purpose

This design document specifies the technical implementation for Phase 3 of spitfire.img, building upon the Phase 1 MVP Viewer and Phase 2 Browse And Quality Of Life foundations. Phase 3 adds power-user presentation and comparison workflows designed for advanced viewing scenarios: slideshow mode with configurable timing and transitions, image comparison capabilities for side-by-side and overlay viewing, a command palette for quick access to all features, animated GIF playback with frame control, HEIC and AVIF format support with Windows codec dependencies, and custom layout modes for different viewing scenarios.

### Design Goals

1. **Architectural Consistency**: All Phase 3 features integrate seamlessly with Phase 1 and Phase 2 services without breaking changes
2. **Service Separation**: New services follow established patterns (interfaces, Result types, async operations)
3. **Performance**: Advanced modes remain responsive (slideshow transitions < 400ms, comparison rendering < 600ms, command palette search < 50ms)
4. **User Experience**: Features enhance the presentation-first philosophy without adding complexity
5. **Extensibility**: Design supports future enhancements (video playback, advanced editing, cloud integration)

### Technology Stack

- **UI Framework**: WinUI 3 with Windows App SDK (from Phase 1)
- **Language**: C# with .NET 8.0 (from Phase 1)
- **Image Decoding**: Windows Imaging Component (WIC) via WinUI APIs (from Phase 1)
- **Animation**: WIC GIF decoder with frame extraction
- **Codec Detection**: Windows Registry queries for HEIC/AVIF codec availability
- **Fuzzy Search**: Custom implementation or FuzzySharp library
- **Storage**: JSON files in ApplicationData.Current.LocalFolder (from Phase 1)

### Key Design Decisions

1. **Slideshow Uses Existing Playlist**: SlideshowService operates on PlaylistState, not a separate data structure
2. **Comparison Mode Extends ViewerState**: Multi-image display is a display mode, not a separate view
3. **Command Palette Is Universal**: All commands from all phases accessible through one interface
4. **GIF Animation Is Opt-In**: Static display by default, animation on user request
5. **Codec Detection Is Startup-Time**: Check once at launch, cache results for session
6. **Custom Layouts Share ImageCanvas**: Grid and Filmstrip reuse existing rendering infrastructure
7. **Transitions Are Non-Blocking**: User can cancel transitions with navigation input
8. **Sync Zoom Uses Relative Coordinates**: Handles images with different dimensions gracefully


## Architecture

### High-Level Architecture

```
┌────────────────────────────────────────────────────────────┐
│                     Presentation Layer                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ MainWindow   │  │CommandPalette│  │FrameControl  │      │
│  │  (Extended)  │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ComparisonView│  │  GridLayout  │  │FilmstripView │      │
│  │              │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────────┐
│                      ViewModel Layer                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ MainViewModel│  │SlideshowState│  │ComparisonState│     │
│  │  (Extended)  │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │CommandPalette│  │AnimationState│  │ LayoutState  │      │
│  │    State     │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────────┐
│                       Service Layer                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │SlideshowSvc  │  │ComparisonSvc │  │CommandPalette│      │
│  │              │  │              │  │    Service   │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │AnimationSvc  │  │CodecDetection│  │  LayoutSvc   │      │
│  │              │  │    Service   │  │              │      │
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
│  │  ImageItem   │  │SlideshowConfig│ │ComparisonLayout│    │
│  │  (Phase 1)   │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ AnimatedGif  │  │ CommandItem  │  │LayoutConfig  │      │
│  │              │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐                        │
│  │CodecAvailability│ TransitionEffect│                      │
│  │              │  │              │                        │
│  └──────────────┘  └──────────────┘                        │
└────────────────────────────────────────────────────────────┘
```

### Integration with Phase 1 and Phase 2

**Phase 1 Services (Unchanged)**:
- FileOpenService: Validates files (used by all phases)
- FolderPlaylistService: Builds playlists (used by slideshow, comparison, layouts)
- NavigationService: Handles navigation (extended for comparison mode)
- SettingsService: Persists settings (extended with Phase 3 preferences)
- ErrorLogService: Logs errors (extended with new error categories)

**Phase 2 Services (Reused)**:
- ThumbnailService: Generates thumbnails (used by filmstrip layout)
- SearchService: Filters playlists (pattern reused for command palette)
- MetadataService: Extracts EXIF (used in all viewing modes)

**Phase 1 Models (Unchanged)**:
- ImageItem: Represents playlist items (used by all Phase 3 features)
- ImageMetadata: Basic metadata (displayed in all modes)
- ViewerSettings: User preferences (extended with Phase 3 settings)
- ViewerError: Error information (extended with new categories)

**Phase 1 ViewModels (Extended)**:
- MainViewModel: Coordinates operations (adds Phase 3 commands and state)
- ViewerState: Display state (adds comparison and layout modes)
- PlaylistState: Playlist management (used by slideshow and comparison)

### Module Structure

```
src/ImageViewer.App/
├── Views/
│   ├── MainWindow.xaml                  # Extended with Phase 3 modes
│   ├── CommandPalette.xaml              # NEW: Command palette overlay
│   ├── ComparisonView.xaml              # NEW: Side-by-side/overlay comparison
│   ├── GridLayout.xaml                  # NEW: Grid viewing mode
│   ├── FilmstripView.xaml               # NEW: Filmstrip with focus area
│   ├── FrameControl.xaml                # NEW: GIF animation controls
│   └── [Phase 1/2 views unchanged]
│
├── ViewModels/
│   ├── MainViewModel.cs                 # Extended with Phase 3 commands
│   ├── SlideshowState.cs                # NEW: Slideshow state management
│   ├── ComparisonState.cs               # NEW: Comparison mode state
│   ├── CommandPaletteState.cs           # NEW: Command palette state
│   ├── AnimationState.cs                # NEW: GIF animation state
│   ├── LayoutState.cs                   # NEW: Custom layout state
│   └── [Phase 1/2 ViewModels unchanged]
│
├── Services/
│   ├── SlideshowService.cs              # NEW: Slideshow timing and transitions
│   ├── ComparisonService.cs             # NEW: Multi-image comparison
│   ├── CommandPaletteService.cs         # NEW: Command search and execution
│   ├── AnimationService.cs              # NEW: GIF frame extraction and playback
│   ├── CodecDetectionService.cs         # NEW: HEIC/AVIF codec detection
│   ├── LayoutService.cs                 # NEW: Grid and filmstrip layouts
│   ├── ImageDecodeService.cs            # Extended with codec-dependent formats
│   └── [Phase 1/2 services unchanged]
│
├── Models/
│   ├── SlideshowConfig.cs               # NEW: Slideshow settings
│   ├── ComparisonLayout.cs              # NEW: Comparison layout type
│   ├── AnimatedGif.cs                   # NEW: GIF frame data
│   ├── CommandItem.cs                   # NEW: Command palette entry
│   ├── LayoutConfig.cs                  # NEW: Grid/filmstrip settings
│   ├── CodecAvailability.cs             # NEW: Codec detection results
│   ├── TransitionEffect.cs              # NEW: Slideshow transition type
│   └── [Phase 1/2 models unchanged]
│
└── Resources/
    └── [Phase 1/2 resources unchanged]
```


## Components and Interfaces

### New Presentation Layer Components

#### CommandPalette

**Responsibility**: Display searchable command list with fuzzy matching and keyboard navigation.

**Key Properties**:
- `SearchText`: String (current search query)
- `FilteredCommands`: ObservableCollection<CommandItem> (matching commands)
- `SelectedIndex`: int (currently selected command)
- `IsVisible`: bool (palette visibility)
- `RecentCommands`: List<CommandItem> (recently executed commands)

**Key Methods**:
- `Show()`: Display palette and focus search input
- `Hide()`: Close palette and clear search
- `OnSearchTextChanged(string)`: Update filtered results with fuzzy matching
- `OnCommandSelected(CommandItem)`: Execute selected command
- `OnKeyDown(KeyRoutedEventArgs)`: Handle Up/Down/Enter/Escape

**XAML Structure**:
```xml
<Grid x:Name="CommandPalette" Visibility="Collapsed">
  <Border Background="{ThemeResource CommandPaletteBackground}">
    <StackPanel>
      <TextBox x:Name="SearchInput" PlaceholderText="Type a command..." />
      <TextBlock Text="{x:Bind FilteredCommandCount}" />
      <ListView x:Name="CommandList" ItemsSource="{x:Bind FilteredCommands}">
        <ListView.ItemTemplate>
          <DataTemplate x:DataType="models:CommandItem">
            <Grid>
              <TextBlock Text="{x:Bind Title}" />
              <TextBlock Text="{x:Bind KeyboardShortcut}" />
            </Grid>
          </DataTemplate>
        </ListView.ItemTemplate>
      </ListView>
    </StackPanel>
  </Border>
</Grid>
```

#### ComparisonView

**Responsibility**: Display multiple images simultaneously in side-by-side or overlay layouts.

**Key Properties**:
- `Layout`: ComparisonLayout enum (SideBySide, Overlay)
- `Images`: ObservableCollection<BitmapImage> (displayed images)
- `ImageCount`: int (2, 3, or 4 images)
- `OpacitySliderValue`: double (for overlay mode, 0-100)
- `SyncZoomEnabled`: bool (synchronized zoom/pan)
- `ActiveImageIndex`: int (selected image for focus)

**Key Methods**:
- `SetLayout(ComparisonLayout, int imageCount)`: Configure comparison layout
- `UpdateImages(List<BitmapImage>)`: Refresh displayed images
- `SetOpacity(double)`: Adjust overlay opacity
- `SwapLayers()`: Swap image order in overlay mode
- `ToggleSyncZoom()`: Enable/disable synchronized zoom

**Layout Modes**:
```
Side-by-Side (2 images):
┌─────────────┬─────────────┐
│   Image 1   │   Image 2   │
│             │             │
└─────────────┴─────────────┘

Side-by-Side (3 images):
┌────────┬────────┬────────┐
│ Image 1│ Image 2│ Image 3│
│        │        │        │
└────────┴────────┴────────┘

Overlay:
┌─────────────────────────┐
│   Image 1 (bottom)      │
│   Image 2 (top, 50%)    │
│                         │
│   [Opacity Slider]      │
└─────────────────────────┘
```

#### FrameControl

**Responsibility**: Provide playback controls for animated GIFs.

**Key Properties**:
- `IsPlaying`: bool (animation play state)
- `CurrentFrame`: int (current frame number)
- `TotalFrames`: int (total frame count)
- `FrameRate`: double (frames per second)

**Key Methods**:
- `Play()`: Start animation playback
- `Pause()`: Pause animation
- `NextFrame()`: Advance to next frame
- `PreviousFrame()`: Move to previous frame
- `SeekToFrame(int)`: Jump to specific frame

**Control Layout**:
```
┌────────────────────────────────────────────────────┐
│ [◀] [▶] [▶▶] Frame 12 / 45 [━━━━━━━━━━━━━━━━━━━━] │
└────────────────────────────────────────────────────┘
```

#### GridLayout

**Responsibility**: Display multiple images in a grid arrangement.

**Key Properties**:
- `GridRows`: int (number of rows)
- `GridColumns`: int (number of columns)
- `Images`: ObservableCollection<BitmapImage> (visible images)
- `SelectedIndex`: int (highlighted image)
- `StartIndex`: int (first visible image in playlist)

**Key Methods**:
- `SetGridSize(int rows, int columns)`: Configure grid dimensions
- `LoadImages(int startIndex)`: Load images from playlist
- `SelectImage(int index)`: Highlight image
- `ScrollGrid(int offset)`: Load next/previous page of images

**Grid Example (3×3)**:
```
┌────────┬────────┬────────┐
│ Img 1  │ Img 2  │ Img 3  │
├────────┼────────┼────────┤
│ Img 4  │ Img 5  │ Img 6  │
├────────┼────────┼────────┤
│ Img 7  │ Img 8  │ Img 9  │
└────────┴────────┴────────┘
```

#### FilmstripView

**Responsibility**: Display large focus image with horizontal thumbnail strip.

**Key Properties**:
- `FocusImage`: BitmapImage (large display area)
- `ThumbnailStrip`: ObservableCollection<Thumbnail> (horizontal strip)
- `CurrentIndex`: int (focused image index)
- `VisibleThumbnailCount`: int (7-11 based on window width)

**Key Methods**:
- `SetFocusImage(BitmapImage)`: Update focus area
- `ScrollToThumbnail(int index)`: Center thumbnail in strip
- `OnThumbnailClicked(int index)`: Change focus image

**Layout**:
```
┌────────────────────────────────────────────────┐
│                                                │
│           Focus Image (Large)                  │
│                                                │
├────────────────────────────────────────────────┤
│ [▪] [▪] [▪] [■] [▪] [▪] [▪]  Thumbnail Strip   │
└────────────────────────────────────────────────┘
```

### New ViewModel Layer Components

#### SlideshowState

**Responsibility**: Manage slideshow timing, transitions, and playback state.

**Properties**:
- `IsActive`: bool (slideshow running)
- `IsPaused`: bool (slideshow paused)
- `Interval`: TimeSpan (time between images)
- `TransitionEffect`: TransitionEffect enum (None, Fade, Slide, Zoom)
- `CurrentImageIndex`: int (current position in playlist)
- `IsLooping`: bool (always true in Phase 3)

**Methods**:
- `Start()`: Begin slideshow from current image
- `Stop()`: End slideshow and return to manual mode
- `Pause()`: Pause slideshow, maintain position
- `Resume()`: Resume from paused state
- `SetInterval(TimeSpan)`: Change advance timing
- `SetTransition(TransitionEffect)`: Change transition effect
- `ResetTimer()`: Reset advance timer (on manual navigation)

**State Transitions**:
```
Stopped → Start() → Active
Active → Pause() → Paused
Paused → Resume() → Active
Active → Stop() → Stopped
Active → Manual Navigation → Active (timer reset)
```

#### ComparisonState

**Responsibility**: Manage multi-image comparison display and synchronization.

**Properties**:
- `IsActive`: bool (comparison mode active)
- `Layout`: ComparisonLayout (SideBySide, Overlay)
- `ImageCount`: int (2, 3, or 4)
- `DisplayedImages`: List<BitmapImage> (current images)
- `DisplayedIndices`: List<int> (playlist indices)
- `OpacityValue`: double (overlay opacity, 0-100)
- `SyncZoomEnabled`: bool (synchronized zoom/pan)
- `ActiveImageIndex`: int (focused image)

**Methods**:
- `EnterComparison(ComparisonLayout, int imageCount)`: Activate comparison mode
- `ExitComparison()`: Return to single-image view
- `ShiftImages(int offset)`: Move all images forward/backward in playlist
- `SetOpacity(double)`: Adjust overlay opacity
- `SwapLayers()`: Swap overlay image order
- `ToggleSyncZoom()`: Enable/disable zoom synchronization
- `SetActiveImage(int index)`: Focus specific image

#### CommandPaletteState

**Responsibility**: Manage command search, filtering, and execution.

**Properties**:
- `IsVisible`: bool (palette open)
- `SearchQuery`: string (current search text)
- `AllCommands`: List<CommandItem> (all available commands)
- `FilteredCommands`: ObservableCollection<CommandItem> (matching commands)
- `SelectedIndex`: int (keyboard selection)
- `RecentCommands`: List<CommandItem> (last 10 executed)

**Methods**:
- `Show()`: Open palette
- `Hide()`: Close palette
- `SetSearchQuery(string)`: Update query and filter
- `SelectCommand(int index)`: Set keyboard selection
- `ExecuteCommand(CommandItem)`: Run command and add to recent
- `ClearSearch()`: Reset to all commands

**Fuzzy Matching Algorithm**:
```
Query: "rot"
Matches: "Rotate Left", "Rotate Right", "Protect File"
Non-matches: "Flip Horizontal", "Zoom In"

Algorithm: Characters must appear in order, not necessarily consecutive
```

#### AnimationState

**Responsibility**: Manage GIF animation playback and frame control.

**Properties**:
- `IsAnimated`: bool (current image is animated GIF)
- `IsPlaying`: bool (animation playing)
- `CurrentFrame`: int (current frame index, 0-based)
- `TotalFrames`: int (frame count)
- `FrameDelays`: List<int> (delay per frame in milliseconds)
- `Frames`: List<BitmapImage> (extracted frames)

**Methods**:
- `LoadAnimation(AnimatedGif)`: Load GIF frames
- `Play()`: Start playback
- `Pause()`: Pause playback
- `NextFrame()`: Advance one frame
- `PreviousFrame()`: Go back one frame
- `SeekToFrame(int)`: Jump to frame
- `GetFrameDelay(int)`: Get delay for frame

#### LayoutState

**Responsibility**: Manage custom layout modes (Grid, Filmstrip).

**Properties**:
- `CurrentLayout`: LayoutMode enum (Single, Grid, Filmstrip)
- `GridRows`: int (grid row count)
- `GridColumns`: int (grid column count)
- `GridStartIndex`: int (first visible image in grid)
- `FilmstripFocusIndex`: int (focus image in filmstrip)

**Methods**:
- `SetLayout(LayoutMode)`: Change layout mode
- `SetGridSize(int rows, int columns)`: Configure grid
- `IncreaseGridDensity()`: Add row/column
- `DecreaseGridDensity()`: Remove row/column
- `ScrollGrid(int offset)`: Page through images
- `SetFilmstripFocus(int index)`: Change focus image

### New Service Layer Components

#### SlideshowService

**Responsibility**: Manage slideshow timing, transitions, and automatic advancement.

**Interface**:
```csharp
public interface ISlideshowService
{
    Task<Result> StartSlideshowAsync(
        PlaylistState playlist, 
        SlideshowConfig config);
    
    Result StopSlideshow();
    Result PauseSlideshow();
    Result ResumeSlideshow();
    Result ResetTimer();
    
    Task<Result<BitmapImage>> ApplyTransitionAsync(
        BitmapImage currentImage,
        BitmapImage nextImage,
        TransitionEffect effect);
}
```

**Methods**:

`StartSlideshowAsync(PlaylistState playlist, SlideshowConfig config)`:
- Initialize slideshow with playlist and configuration
- Start advance timer with configured interval
- Return success or SlideshowError

`StopSlideshow()`:
- Cancel advance timer
- Clear slideshow state
- Return success

`PauseSlideshow()`:
- Pause advance timer
- Maintain current position
- Return success

`ResumeSlideshow()`:
- Resume advance timer
- Continue from current position
- Return success

`ResetTimer()`:
- Cancel current timer
- Start new timer with same interval
- Return success

`ApplyTransitionAsync(BitmapImage currentImage, BitmapImage nextImage, TransitionEffect effect)`:
- Apply visual transition between images
- Return transitioned image or TransitionError

**Slideshow Algorithm**:
```
1. Start timer with configured interval
2. On timer tick:
   a. Get next image index (loop if at end)
   b. Load next image via NavigationService
   c. Apply transition effect
   d. Update ViewerState with new image
   e. Restart timer
3. On manual navigation:
   a. Reset timer
   b. Continue slideshow from new position
4. On pause:
   a. Stop timer
   b. Maintain position
5. On resume:
   a. Restart timer from current position
```

**Transition Effects**:
- **None**: Instant image swap
- **Fade**: Fade out current (300ms) → Fade in next (300ms)
- **Slide**: Slide next from right while current slides left (400ms)
- **Zoom**: Zoom out current while fading in next (350ms)

**Performance**:
- Transitions render at 60 FPS
- Preload next image during current image display
- Cancel transition if user navigates manually

#### ComparisonService

**Responsibility**: Manage multi-image comparison layouts and synchronization.

**Interface**:
```csharp
public interface IComparisonService
{
    Task<Result<List<BitmapImage>>> LoadComparisonImagesAsync(
        PlaylistState playlist,
        int startIndex,
        int imageCount);
    
    Result<List<int>> CalculateComparisonIndices(
        int currentIndex,
        int imageCount,
        int playlistSize);
    
    Result SynchronizeZoom(
        List<ImageCanvas> canvases,
        double zoomLevel,
        Point panOffset);
    
    Result<Point> ConvertToRelativePosition(
        Point absolutePosition,
        Size imageSize);
}
```

**Methods**:

`LoadComparisonImagesAsync(PlaylistState playlist, int startIndex, int imageCount)`:
- Load specified number of images starting from index
- Decode images in parallel for performance
- Return list of BitmapImages or ComparisonError

`CalculateComparisonIndices(int currentIndex, int imageCount, int playlistSize)`:
- Calculate playlist indices for comparison images
- Handle boundary cases (near start/end of playlist)
- Return list of indices

`SynchronizeZoom(List<ImageCanvas> canvases, double zoomLevel, Point panOffset)`:
- Apply same zoom level to all canvases
- Convert pan offset to relative coordinates
- Apply relative pan to each canvas
- Return success or SyncError

`ConvertToRelativePosition(Point absolutePosition, Size imageSize)`:
- Convert absolute pixel coordinates to relative (0-1 range)
- Used for synchronized panning with different image sizes
- Return relative position

**Comparison Layout Algorithm**:
```
Side-by-Side (2 images):
1. Divide canvas width by 2
2. Fit each image within its section
3. Preserve aspect ratio
4. Center images vertically

Side-by-Side (3 images):
1. Divide canvas width by 3
2. Fit each image within its section
3. Preserve aspect ratio
4. Center images vertically

Side-by-Side (4 images):
1. Create 2×2 grid
2. Fit each image within its cell
3. Preserve aspect ratio
4. Center images in cells

Overlay:
1. Layer images on top of each other
2. Align by center point
3. Apply opacity to top image
4. Handle different dimensions gracefully
```

**Synchronized Zoom Algorithm**:
```
1. User zooms in Image 1 to 200%
2. Calculate relative pan position (e.g., 0.5, 0.3 = center-left)
3. Apply 200% zoom to all images
4. Convert relative position to absolute for each image:
   - Image 1 (1920×1080): 0.5 × 1920 = 960px, 0.3 × 1080 = 324px
   - Image 2 (3840×2160): 0.5 × 3840 = 1920px, 0.3 × 2160 = 648px
5. Pan each image to calculated absolute position
```

#### CommandPaletteService

**Responsibility**: Provide fuzzy search and command execution for all viewer commands.

**Interface**:
```csharp
public interface ICommandPaletteService
{
    Result<List<CommandItem>> GetAllCommands();
    Result<List<CommandItem>> FilterCommands(string query);
    Task<Result> ExecuteCommandAsync(CommandItem command);
    Result<List<CommandItem>> GetRecentCommands();
    Result AddToRecentCommands(CommandItem command);
}
```

**Methods**:

`GetAllCommands()`:
- Return complete list of all available commands
- Include commands from Phase 1, 2, and 3
- Group by category
- Return list or CommandPaletteError

`FilterCommands(string query)`:
- Apply fuzzy matching to command list
- Rank results by match quality
- Return filtered and ranked list

`ExecuteCommandAsync(CommandItem command)`:
- Execute command action
- Add to recent commands
- Return success or ExecutionError

`GetRecentCommands()`:
- Return last 10 executed commands
- Most recent first
- Return list

`AddToRecentCommands(CommandItem command)`:
- Add command to recent list
- Remove duplicates (move to top)
- Trim to 10 items
- Return success

**Fuzzy Matching Algorithm**:
```csharp
public int CalculateMatchScore(string query, string target)
{
    query = query.ToLowerInvariant();
    target = target.ToLowerInvariant();
    
    int score = 0;
    int queryIndex = 0;
    int lastMatchIndex = -1;
    
    for (int i = 0; i < target.Length && queryIndex < query.Length; i++)
    {
        if (target[i] == query[queryIndex])
        {
            // Consecutive matches score higher
            if (i == lastMatchIndex + 1)
                score += 10;
            else
                score += 5;
            
            // Matches at word boundaries score higher
            if (i == 0 || target[i - 1] == ' ')
                score += 3;
            
            lastMatchIndex = i;
            queryIndex++;
        }
    }
    
    // Full match required
    if (queryIndex < query.Length)
        return 0;
    
    // Shorter targets score higher (more specific match)
    score -= target.Length;
    
    return score;
}
```

**Command Registry**:
```csharp
public class CommandItem
{
    public string Id { get; init; }              // "file.open"
    public string Title { get; init; }           // "Open File"
    public string Category { get; init; }        // "File"
    public string KeyboardShortcut { get; init; } // "Ctrl+O"
    public Action Action { get; init; }          // Command action
    public bool IsAvailable { get; init; }       // Context-dependent availability
}
```

**Command Categories**:
- File: Open, Close, Export, Recent Files
- View: Zoom, Fit, Fullscreen, Theme
- Image: Rotate, Flip, Metadata
- Navigation: Next, Previous, First, Last
- Layout: Single, Grid, Filmstrip, Comparison
- Slideshow: Start, Stop, Pause, Settings
- Comparison: Side-by-Side, Overlay, Sync Zoom
- Animation: Play, Pause, Next Frame, Previous Frame

#### AnimationService

**Responsibility**: Extract GIF frames and manage animation playback.

**Interface**:
```csharp
public interface IAnimationService
{
    Task<Result<AnimatedGif>> LoadAnimatedGifAsync(string filePath);
    Result<int> GetFrameCount(AnimatedGif gif);
    Result<BitmapImage> GetFrame(AnimatedGif gif, int frameIndex);
    Result<int> GetFrameDelay(AnimatedGif gif, int frameIndex);
}
```

**Methods**:

`LoadAnimatedGifAsync(string filePath)`:
- Open GIF file with WIC decoder
- Extract all frames
- Extract frame delays from metadata
- Return AnimatedGif or AnimationError

`GetFrameCount(AnimatedGif gif)`:
- Return total number of frames
- Return count or error

`GetFrame(AnimatedGif gif, int frameIndex)`:
- Return BitmapImage for specified frame
- Return frame or error if index out of bounds

`GetFrameDelay(AnimatedGif gif, int frameIndex)`:
- Return delay in milliseconds for frame
- Return delay or error if index out of bounds

**GIF Frame Extraction Algorithm**:
```csharp
public async Task<Result<AnimatedGif>> LoadAnimatedGifAsync(string filePath)
{
    try
    {
        using var stream = File.OpenRead(filePath);
        var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
        
        var frameCount = decoder.FrameCount;
        var frames = new List<BitmapImage>();
        var delays = new List<int>();
        
        for (uint i = 0; i < frameCount; i++)
        {
            var frame = await decoder.GetFrameAsync(i);
            
            // Extract frame delay from metadata
            var properties = await frame.BitmapProperties.GetPropertiesAsync(
                new[] { "/grctlext/Delay" });
            
            int delay = 100; // Default 100ms
            if (properties.TryGetValue("/grctlext/Delay", out var delayValue))
            {
                delay = (ushort)delayValue.Value * 10; // GIF delay is in 1/100s
            }
            
            // Convert frame to BitmapImage
            var bitmapImage = new BitmapImage();
            using var memoryStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(
                BitmapEncoder.PngEncoderId, 
                memoryStream);
            
            var pixelData = await frame.GetPixelDataAsync();
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                frame.PixelWidth,
                frame.PixelHeight,
                96, 96,
                pixelData.DetachPixelData());
            await encoder.FlushAsync();
            
            memoryStream.Seek(0);
            await bitmapImage.SetSourceAsync(memoryStream);
            
            frames.Add(bitmapImage);
            delays.Add(delay);
        }
        
        var animatedGif = new AnimatedGif
        {
            Frames = frames,
            FrameDelays = delays,
            TotalFrames = frameCount
        };
        
        return Result<AnimatedGif>.Success(animatedGif);
    }
    catch (Exception ex)
    {
        var error = ViewerError.AnimationError(filePath, ex);
        return Result<AnimatedGif>.Failure(error);
    }
}
```

**Animation Playback Algorithm**:
```
1. Load all frames at GIF open
2. Display first frame initially
3. On Play:
   a. Start timer with first frame's delay
   b. On timer tick:
      - Advance to next frame
      - Display frame
      - Restart timer with new frame's delay
   c. Loop to first frame after last frame
4. On Pause:
   a. Stop timer
   b. Maintain current frame
5. On Next/Previous Frame:
   a. Stop timer if playing
   b. Move to target frame
   c. Display frame
```

#### CodecDetectionService

**Responsibility**: Detect Windows codec availability for HEIC and AVIF formats.

**Interface**:
```csharp
public interface ICodecDetectionService
{
    Task<Result<CodecAvailability>> DetectCodecsAsync();
    Result<bool> IsHeicSupported();
    Result<bool> IsAvifSupported();
    Result<string> GetHeicStoreUrl();
    Result<string> GetAvifStoreUrl();
}
```

**Methods**:

`DetectCodecsAsync()`:
- Check Windows Registry for HEIC codec
- Check Windows Registry for AVIF codec
- Cache results for session
- Return CodecAvailability or DetectionError

`IsHeicSupported()`:
- Return cached HEIC codec availability
- Return bool or error

`IsAvifSupported()`:
- Return cached AVIF codec availability
- Return bool or error

`GetHeicStoreUrl()`:
- Return Microsoft Store URL for HEIF Image Extensions
- Return URL string

`GetAvifStoreUrl()`:
- Return Microsoft Store URL for AV1 Video Extension
- Return URL string

**Codec Detection Algorithm**:
```csharp
public async Task<Result<CodecAvailability>> DetectCodecsAsync()
{
    try
    {
        bool heicSupported = false;
        bool avifSupported = false;
        
        // Check HEIC codec via WIC
        try
        {
            var heicDecoder = BitmapDecoder.HeifDecoderId;
            heicSupported = true;
        }
        catch
        {
            heicSupported = false;
        }
        
        // Check AVIF codec via WIC
        try
        {
            // AVIF uses AV1 decoder
            var testStream = CreateTestAvifStream();
            var decoder = await BitmapDecoder.CreateAsync(testStream);
            avifSupported = true;
        }
        catch
        {
            avifSupported = false;
        }
        
        var availability = new CodecAvailability
        {
            HeicSupported = heicSupported,
            AvifSupported = avifSupported,
            DetectionTime = DateTime.UtcNow
        };
        
        return Result<CodecAvailability>.Success(availability);
    }
    catch (Exception ex)
    {
        var error = ViewerError.CodecDetectionError(ex);
        return Result<CodecAvailability>.Failure(error);
    }
}
```

**Store URLs**:
- HEIC: `ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG` (HEIF Image Extensions)
- AVIF: `ms-windows-store://pdp/?ProductId=9MVZQVXJBQ9V` (AV1 Video Extension)

**Codec Unavailable Error Handling**:
```
1. User opens HEIC file
2. CodecDetectionService returns HeicSupported = false
3. Display dialog:
   "HEIC format requires the HEIF Image Extensions from the Microsoft Store.
    Would you like to open the Store page?"
   [Yes] [No]
4. If Yes:
   a. Open Store URL
   b. Don't show prompt again this session
5. If No:
   a. Don't show prompt again this session
```

#### LayoutService

**Responsibility**: Manage grid and filmstrip custom layouts.

**Interface**:
```csharp
public interface ILayoutService
{
    Task<Result<List<BitmapImage>>> LoadGridImagesAsync(
        PlaylistState playlist,
        int startIndex,
        int rows,
        int columns);
    
    Result<GridLayout> CalculateGridLayout(
        int rows,
        int columns,
        Size canvasSize);
    
    Task<Result<FilmstripData>> LoadFilmstripDataAsync(
        PlaylistState playlist,
        int focusIndex,
        int thumbnailCount);
}
```

**Methods**:

`LoadGridImagesAsync(PlaylistState playlist, int startIndex, int rows, int columns)`:
- Calculate number of images needed (rows × columns)
- Load images from playlist starting at index
- Decode images in parallel
- Return list of BitmapImages or LayoutError

`CalculateGridLayout(int rows, int columns, Size canvasSize)`:
- Calculate cell dimensions
- Calculate spacing between cells
- Return GridLayout with cell sizes and positions

`LoadFilmstripDataAsync(PlaylistState playlist, int focusIndex, int thumbnailCount)`:
- Load focus image at full resolution
- Load thumbnails for strip (reuse ThumbnailService)
- Calculate visible thumbnail range
- Return FilmstripData or LayoutError

**Grid Layout Algorithm**:
```
1. Calculate total cells: rows × columns
2. Calculate cell width: canvasWidth / columns
3. Calculate cell height: canvasHeight / rows
4. For each image:
   a. Calculate cell position (row, column)
   b. Fit image within cell preserving aspect ratio
   c. Center image in cell
5. Add spacing between cells (8px)
```

**Filmstrip Layout Algorithm**:
```
1. Divide canvas vertically: 75% focus area, 25% thumbnail strip
2. Display focus image in top area (fit to area)
3. Calculate visible thumbnail count based on strip width
4. Load thumbnails centered around current index
5. Scroll strip to keep current thumbnail centered
```

**Grid Density Adjustment**:
```
Default: 3×3 (9 images)
Increase: 3×3 → 4×4 → 5×5 → 6×6 (max)
Decrease: 6×6 → 5×5 → 4×4 → 3×3 → 2×2 (min)
```


## Data Models

### SlideshowConfig

**Purpose**: Store slideshow configuration settings.

**Properties**:
```csharp
public class SlideshowConfig
{
    public TimeSpan Interval { get; init; }           // Time between images
    public TransitionEffect Transition { get; init; }  // Transition effect
    public bool IsLooping { get; init; }              // Always true in Phase 3
}
```

**Default Values**:
- Interval: 3 seconds
- Transition: Fade
- IsLooping: true

**Usage**: Passed to SlideshowService, persisted in ViewerSettings

### ComparisonLayout

**Purpose**: Define comparison display mode.

**Definition**:
```csharp
public enum ComparisonLayout
{
    SideBySide,  // Images displayed adjacent
    Overlay      // Images layered with opacity control
}
```

**Usage**: Stored in ComparisonState, determines rendering mode

### AnimatedGif

**Purpose**: Store extracted GIF frame data.

**Properties**:
```csharp
public class AnimatedGif
{
    public List<BitmapImage> Frames { get; init; }    // All frames
    public List<int> FrameDelays { get; init; }       // Delay per frame (ms)
    public int TotalFrames { get; init; }             // Frame count
    public Size Dimensions { get; init; }             // Image dimensions
}
```

**Usage**: Created by AnimationService, used by AnimationState

### CommandItem

**Purpose**: Represent a single command in the command palette.

**Properties**:
```csharp
public class CommandItem
{
    public string Id { get; init; }                   // Unique identifier
    public string Title { get; init; }                // Display name
    public string Category { get; init; }             // Command category
    public string KeyboardShortcut { get; init; }     // Shortcut text
    public string Description { get; init; }          // Optional description
    public Action Action { get; init; }               // Command action
    public bool IsAvailable { get; init; }            // Context availability
    public int MatchScore { get; set; }               // Fuzzy match score
}
```

**Usage**: Stored in CommandPaletteService, displayed in CommandPalette

### LayoutConfig

**Purpose**: Store grid and filmstrip layout settings.

**Properties**:
```csharp
public class LayoutConfig
{
    public LayoutMode Mode { get; init; }             // Current layout
    public int GridRows { get; init; }                // Grid row count
    public int GridColumns { get; init; }             // Grid column count
    public int FilmstripThumbnailCount { get; init; } // Visible thumbnails
}

public enum LayoutMode
{
    Single,      // Standard single-image view
    Grid,        // Grid layout
    Filmstrip    // Filmstrip with focus area
}
```

**Default Values**:
- Mode: Single
- GridRows: 3
- GridColumns: 3
- FilmstripThumbnailCount: 7

**Usage**: Stored in LayoutState, persisted in ViewerSettings

### CodecAvailability

**Purpose**: Store codec detection results.

**Properties**:
```csharp
public class CodecAvailability
{
    public bool HeicSupported { get; init; }          // HEIC codec available
    public bool AvifSupported { get; init; }          // AVIF codec available
    public DateTime DetectionTime { get; init; }      // When detected
}
```

**Usage**: Created by CodecDetectionService, cached for session

### TransitionEffect

**Purpose**: Define slideshow transition types.

**Definition**:
```csharp
public enum TransitionEffect
{
    None,   // Instant swap
    Fade,   // Fade out/in
    Slide,  // Slide from right
    Zoom    // Zoom out/fade in
}
```

**Timing**:
- None: 0ms
- Fade: 300ms
- Slide: 400ms
- Zoom: 350ms

**Usage**: Stored in SlideshowConfig, applied by SlideshowService

### Extended ViewerSettings

**New Properties** (added to Phase 2 ViewerSettings):
```csharp
public class ViewerSettings
{
    // Phase 1 and 2 properties (unchanged)
    // ...
    
    // NEW Phase 3 properties
    public TimeSpan SlideshowInterval { get; set; }         // Default 3s
    public TransitionEffect SlideshowTransition { get; set; } // Default Fade
    public LayoutMode PreferredLayout { get; set; }         // Default Single
    public int GridRows { get; set; }                       // Default 3
    public int GridColumns { get; set; }                    // Default 3
    public bool ComparisonSyncZoom { get; set; }            // Default true
    public bool GifAutoPlay { get; set; }                   // Default true
}
```

**Serialization**: JSON via System.Text.Json (same as Phase 1/2)

**Usage**: Loaded on app start, saved on settings change

### Extended ViewerError

**New Error Categories**:
```csharp
public enum ErrorCategory
{
    // Phase 1 and 2 categories (unchanged)
    // ...
    
    // NEW Phase 3 categories
    SlideshowError,
    TransitionError,
    ComparisonError,
    SyncError,
    CommandPaletteError,
    ExecutionError,
    AnimationError,
    CodecDetectionError,
    CodecUnavailableError,
    LayoutError
}
```

**New Factory Methods**:
```csharp
public static ViewerError SlideshowError(string operation, Exception ex);
public static ViewerError TransitionError(TransitionEffect effect, Exception ex);
public static ViewerError ComparisonError(string operation, Exception ex);
public static ViewerError SyncError(string operation, Exception ex);
public static ViewerError CommandPaletteError(string operation, Exception ex);
public static ViewerError ExecutionError(string commandId, Exception ex);
public static ViewerError AnimationError(string filePath, Exception ex);
public static ViewerError CodecDetectionError(Exception ex);
public static ViewerError CodecUnavailableError(string format, string codecName);
public static ViewerError LayoutError(string operation, Exception ex);
```

**New User Messages**:
- SlideshowError: "Unable to start slideshow mode."
- TransitionError: "Unable to apply transition effect."
- ComparisonError: "Unable to load images for comparison."
- SyncError: "Unable to synchronize zoom and pan."
- CommandPaletteError: "Unable to load command palette."
- ExecutionError: "Unable to execute command."
- AnimationError: "Unable to load animated GIF."
- CodecDetectionError: "Unable to detect codec availability."
- CodecUnavailableError: "{Format} format requires the {CodecName} from the Microsoft Store. Would you like to open the Store page?"
- LayoutError: "Unable to load custom layout."


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Slideshow Timing Accuracy

*For any* configured slideshow interval between 1 and 60 seconds, the Slideshow_Service SHALL advance to the next image at the configured interval within a tolerance of ±100 milliseconds.

**Validates: Requirements 1.2, 2.1, 2.4**

### Property 2: Slideshow Looping Behavior

*For any* playlist with N images, when Slideshow_Mode advances through all N images, the Slideshow_Service SHALL loop back to the first image and continue advancing.

**Validates: Requirements 1.7**

### Property 3: Slideshow Timer Reset on Manual Navigation

*For any* slideshow in progress, when the user manually navigates to a different image, the Slideshow_Service SHALL reset the advance timer and continue slideshow from the new position.

**Validates: Requirements 1.8**

### Property 4: Slideshow Interval Stepping

*For any* current slideshow interval in the valid set {1s, 2s, 3s, 5s, 10s, 15s, 30s, 60s}, pressing Plus SHALL move to the next interval in the sequence, and pressing Minus SHALL move to the previous interval, with wraparound at boundaries.

**Validates: Requirements 2.6, 2.7**

### Property 5: Transition Navigation Blocking

*For any* active transition effect (Fade, Slide, Zoom), the Slideshow_Service SHALL block manual navigation input until the transition completes.

**Validates: Requirements 3.8**

### Property 6: Comparison Synchronized Navigation

*For any* pair of images displayed in Comparison_Mode at indices I and I+1, navigating forward SHALL display images at indices I+1 and I+2, and navigating backward SHALL display images at indices I-1 and I.

**Validates: Requirements 4.4, 4.5**

### Property 7: Aspect Ratio Preservation in Layouts

*For any* image with dimensions W×H displayed in Side_By_Side_Layout, Grid_Layout, or Filmstrip_Layout, the displayed image SHALL preserve the aspect ratio W:H within a tolerance of 1 pixel.

**Validates: Requirements 5.6, 14.6**

### Property 8: Overlay Opacity Range

*For any* opacity slider position P (0-100), the Comparison_Service SHALL set the top image opacity to P percent, where 0 is fully transparent and 100 is fully opaque.

**Validates: Requirements 6.3, 6.4, 6.5**

### Property 9: Synchronized Zoom with Relative Positioning

*For any* set of images with different dimensions displayed in Comparison_Mode with Sync_Zoom enabled, applying zoom level Z and pan offset (Rx, Ry) in relative coordinates SHALL result in all images having zoom level Z and pan offsets calculated as (Rx × Wi, Ry × Hi) where Wi and Hi are the dimensions of image i.

**Validates: Requirements 7.1, 7.2, 7.6**

### Property 10: Fuzzy Matching Character Order

*For any* search query Q and command title T, if the Command_Palette_Service returns T as a match for Q, then all characters in Q SHALL appear in T in the same order (not necessarily consecutively).

**Validates: Requirements 9.2**

### Property 11: Fuzzy Match Ranking Consistency

*For any* search query Q and two command titles T1 and T2, if T1 has more consecutive character matches with Q than T2, then T1 SHALL rank higher than T2 in the filtered results.

**Validates: Requirements 9.5**

### Property 12: GIF Frame Timing Accuracy

*For any* animated GIF with frame delays D1, D2, ..., DN (in milliseconds), the Animation_Service SHALL display each frame i for Di milliseconds within a tolerance of ±50 milliseconds.

**Validates: Requirements 10.3**

### Property 13: GIF Animation Looping

*For any* animated GIF with N frames, when animation playback reaches frame N, the Animation_Service SHALL loop back to frame 1 and continue playback.

**Validates: Requirements 10.8**

### Property 14: Codec-Dependent Playlist Inclusion

*For any* folder containing HEIC files, when the HEIC codec is installed, the Folder_Playlist_Service SHALL include HEIC files in the playlist, and when the HEIC codec is not installed, the Folder_Playlist_Service SHALL exclude HEIC files from the playlist.

**Validates: Requirements 12.9, 12.10, 13.9, 13.10**

### Property 15: Grid Layout Cell Distribution

*For any* grid configuration with R rows and C columns, the Layout_Service SHALL display exactly R × C images (or fewer if playlist is smaller), with each image occupying one cell and cells distributed evenly across the canvas.

**Validates: Requirements 14.2, 14.3**

### Property 16: Filmstrip Thumbnail Centering

*For any* focus image change in Filmstrip_Layout, the Layout_Service SHALL scroll the thumbnail strip such that the thumbnail corresponding to the focus image is centered in the visible strip area within ±1 thumbnail width.

**Validates: Requirements 15.5**

### Property 17: Slideshow in Comparison Mode

*For any* Comparison_Mode displaying images at indices I and I+1, when Slideshow_Mode is active, advancing SHALL display images at indices I+1 and I+2, maintaining the comparison relationship.

**Validates: Requirements 17.1, 17.2**

### Property 18: Codec Detection Caching

*For any* codec availability check, the second check for the same codec within the same session SHALL return the cached result without re-detecting, as verified by detection timestamp remaining unchanged.

**Validates: Requirements 20.5**


## Error Handling

### Error Handling Philosophy

Phase 3 extends Phase 1 and Phase 2's structured error handling system with new error categories for slideshow, comparison, animation, codec detection, and custom layouts. The same principles apply:

1. **Expected Failures Are Not Exceptions**: Services return Result<T> types
2. **User Messages Are Calm and Actionable**: Clear explanations without technical jargon
3. **Diagnostic Information Is Logged**: Full details written to log files
4. **Errors Don't Block Progress**: Graceful degradation where possible

### New Error Categories and Handling

#### SlideshowError

**Triggers**:
- Timer initialization fails
- Transition rendering fails
- Playlist navigation fails during slideshow

**User Message**: "Unable to start slideshow mode."

**Handling**:
- Log error with operation and exception
- Display user message
- Return to manual viewing mode
- Don't block image viewing

**Recovery**: User can view images manually; slideshow remains available to retry

#### TransitionError

**Triggers**:
- Transition animation fails
- Insufficient memory for transition rendering
- Image decoding fails during transition

**User Message**: "Unable to apply transition effect."

**Handling**:
- Log error with transition type and exception
- Fall back to instant transition (None)
- Continue slideshow with fallback
- Don't stop slideshow

**Recovery**: Slideshow continues with instant transitions

#### ComparisonError

**Triggers**:
- Unable to load multiple images
- Insufficient memory for comparison
- Playlist too small for requested image count

**User Message**: "Unable to load images for comparison."

**Handling**:
- Log error with operation and exception
- Display user message
- Return to single-image view
- Don't block image viewing

**Recovery**: User can view images individually; comparison remains available to retry

#### SyncError

**Triggers**:
- Zoom synchronization calculation fails
- Pan offset conversion fails
- Canvas update fails

**User Message**: "Unable to synchronize zoom and pan."

**Handling**:
- Log error with operation and exception
- Disable sync zoom temporarily
- Display user message as warning
- Continue comparison without sync

**Recovery**: User can disable sync zoom or exit comparison mode

#### CommandPaletteError

**Triggers**:
- Command registry loading fails
- Fuzzy search algorithm fails
- Command list is empty

**User Message**: "Unable to load command palette."

**Handling**:
- Log error with operation and exception
- Display user message
- Close palette
- Don't block other functionality

**Recovery**: User can access commands via menus and keyboard shortcuts

#### ExecutionError

**Triggers**:
- Command action throws exception
- Command is unavailable in current context
- Command parameters are invalid

**User Message**: "Unable to execute command."

**Handling**:
- Log error with command ID and exception
- Display user message
- Close palette
- Don't block other functionality

**Recovery**: User can retry command or use alternative method

#### AnimationError

**Triggers**:
- GIF frame extraction fails
- Frame delay metadata is missing or invalid
- Insufficient memory for frame cache

**User Message**: "Unable to load animated GIF."

**Handling**:
- Log error with file path and exception
- Display first frame as static image
- Hide frame controls
- Continue with static display

**Recovery**: User can view GIF as static image; other GIFs may work

#### CodecDetectionError

**Triggers**:
- Registry access fails
- WIC codec enumeration fails
- Permissions insufficient

**User Message**: "Unable to detect codec availability."

**Handling**:
- Log error with exception
- Assume codecs unavailable (safe default)
- Display user message as warning
- Continue with supported formats

**Recovery**: User can manually install codecs; detection will retry on next launch

#### CodecUnavailableError

**Triggers**:
- User opens HEIC file without HEIC codec
- User opens AVIF file without AVIF codec

**User Message**: "{Format} format requires the {CodecName} from the Microsoft Store. Would you like to open the Store page?"

**Handling**:
- Display dialog with Store link
- If user accepts, open Store URL
- If user declines, don't show again this session
- Log codec unavailable event
- Don't block other functionality

**Recovery**: User can install codec from Store; file will open after codec installation

#### LayoutError

**Triggers**:
- Grid layout calculation fails
- Filmstrip thumbnail loading fails
- Insufficient memory for layout

**User Message**: "Unable to load custom layout."

**Handling**:
- Log error with operation and exception
- Display user message
- Return to single-image view
- Don't block image viewing

**Recovery**: User can view images individually; layout remains available to retry

### Graceful Degradation Strategies

**Slideshow Failures**:
- Fall back to instant transitions if transition fails
- Continue slideshow with manual navigation if timer fails
- Return to manual mode if critical failure

**Comparison Failures**:
- Reduce image count if memory insufficient (4 → 3 → 2)
- Disable sync zoom if synchronization fails
- Return to single-image view if critical failure

**Animation Failures**:
- Display first frame as static if animation fails
- Hide frame controls if playback unavailable
- Continue with static GIF display

**Codec Unavailable**:
- Exclude unsupported formats from playlist
- Show helpful error with Store link
- Don't repeatedly prompt user

**Layout Failures**:
- Reduce grid density if memory insufficient
- Fall back to single-image view if critical failure
- Maintain thumbnail strip if filmstrip fails

### Error Logging Extensions

**Log Entry Format** (same as Phase 1/2):
```
[2024-01-15 14:32:18.456] [ERROR] [SlideshowError]
Operation: StartSlideshow
Message: Unable to start slideshow mode.
Exception: System.InvalidOperationException
Stack Trace:
  at ImageViewer.Services.SlideshowService.StartSlideshowAsync(...)
  ...
---
```

**Log File Management** (same as Phase 1/2):
- Location: `ApplicationData.Current.LocalFolder/logs/`
- Naming: `viewer-{yyyy-MM-dd}.log`
- Rotation: Daily
- Retention: 30 days


## Testing Strategy

### Testing Approach

Phase 3 extends Phase 1 and Phase 2's dual testing approach:

1. **Property-Based Tests**: Verify universal properties for slideshow, comparison, command palette, animation, layouts
2. **Example-Based Unit Tests**: Verify specific scenarios, edge cases, UI interactions
3. **Integration Tests**: Verify GIF decoding, codec detection, transition rendering, layout calculations
4. **Manual Testing**: Verify UI behavior, performance, visual polish, timing accuracy

### Property-Based Testing

**Framework**: CsCheck or FsCheck (same as Phase 1/2)

**Configuration**:
- Minimum 100 iterations per property test
- Each test tagged with comment referencing design property
- Tag format: `// Feature: phase-3-advanced-viewing, Property {number}: {property_text}`

**Property Tests to Implement**:

1. **Slideshow Timing Accuracy** (Property 1)
   - Generate: Random intervals (1-60 seconds)
   - Verify: Advance timing within ±100ms tolerance
   - Edge cases: Minimum interval (1s), maximum interval (60s), rapid interval changes

2. **Slideshow Looping** (Property 2)
   - Generate: Random playlists (1-100 images)
   - Verify: After advancing through all images, loops to first
   - Edge cases: Single-image playlist, two-image playlist

3. **Slideshow Timer Reset** (Property 3)
   - Generate: Random navigation actions during slideshow
   - Verify: Timer resets after each manual navigation
   - Edge cases: Rapid navigation, navigation during transition

4. **Slideshow Interval Stepping** (Property 4)
   - Generate: Random starting intervals from valid set
   - Verify: Plus/Minus moves to correct next/previous interval
   - Edge cases: At minimum (1s), at maximum (60s), wraparound

5. **Transition Navigation Blocking** (Property 5)
   - Generate: Random transition types (Fade, Slide, Zoom)
   - Verify: Navigation blocked during transition, allowed after
   - Edge cases: Rapid navigation attempts, transition cancellation

6. **Comparison Synchronized Navigation** (Property 6)
   - Generate: Random image pairs and navigation directions
   - Verify: Both images shift together maintaining relationship
   - Edge cases: At playlist boundaries, single-image playlist

7. **Aspect Ratio Preservation** (Property 7)
   - Generate: Random image dimensions (1×1 to 10000×10000), random layout modes
   - Verify: Aspect ratio preserved within 1px tolerance
   - Edge cases: Square images, very wide images, very tall images, tiny images

8. **Overlay Opacity Range** (Property 8)
   - Generate: Random opacity values (0-100)
   - Verify: Opacity maps correctly to 0-100% range
   - Edge cases: 0%, 100%, fractional values

9. **Synchronized Zoom Relative Positioning** (Property 9)
   - Generate: Random image dimension pairs, random zoom levels, random pan offsets
   - Verify: Relative positions synchronized correctly
   - Edge cases: Same dimensions, very different dimensions, extreme zoom levels

10. **Fuzzy Matching Character Order** (Property 10)
    - Generate: Random queries and command titles
    - Verify: All matches have characters in correct order
    - Edge cases: Single character, all characters match, no matches

11. **Fuzzy Match Ranking** (Property 11)
    - Generate: Queries with known good/bad matches
    - Verify: Better matches rank higher
    - Edge cases: Identical scores, consecutive vs non-consecutive matches

12. **GIF Frame Timing** (Property 12)
    - Generate: GIFs with various frame delays
    - Verify: Frame display timing within ±50ms tolerance
    - Edge cases: Very fast (10ms), very slow (5000ms), variable delays

13. **GIF Animation Looping** (Property 13)
    - Generate: Random GIFs with various frame counts
    - Verify: After last frame, loops to first
    - Edge cases: Single-frame GIF, two-frame GIF

14. **Codec-Dependent Playlist Inclusion** (Property 14)
    - Generate: Folders with HEIC/AVIF files, random codec availability
    - Verify: Files included when codec available, excluded when not
    - Edge cases: Mixed formats, all HEIC, no HEIC

15. **Grid Layout Cell Distribution** (Property 15)
    - Generate: Random grid sizes (2×2 to 6×6), random playlist sizes
    - Verify: Correct number of images displayed, even distribution
    - Edge cases: Playlist smaller than grid, playlist larger than grid

16. **Filmstrip Thumbnail Centering** (Property 16)
    - Generate: Random focus changes, random strip widths
    - Verify: Focus thumbnail centered within ±1 thumbnail width
    - Edge cases: First image, last image, rapid focus changes

17. **Slideshow in Comparison Mode** (Property 17)
    - Generate: Random image pairs, random slideshow intervals
    - Verify: Both images advance together in slideshow
    - Edge cases: At playlist boundaries, single-image playlist

18. **Codec Detection Caching** (Property 18)
    - Generate: Random codec check sequences
    - Verify: Second check uses cache (timestamp unchanged)
    - Edge cases: Multiple checks, different codecs

**Example Property Test**:
```csharp
// Feature: phase-3-advanced-viewing, Property 1: Slideshow Timing Accuracy
[Fact]
public void SlideshowTiming_AdvancesAtConfiguredInterval()
{
    Gen.Int[1, 60].Sample(intervalSeconds =>
    {
        var config = new SlideshowConfig
        {
            Interval = TimeSpan.FromSeconds(intervalSeconds),
            Transition = TransitionEffect.None,
            IsLooping = true
        };
        
        var playlist = CreateTestPlaylist(10);
        var service = new SlideshowService();
        var stopwatch = Stopwatch.StartNew();
        
        service.StartSlideshowAsync(playlist, config).Wait();
        
        // Wait for first advance
        var advanceEvent = new ManualResetEvent(false);
        service.OnImageAdvanced += () => advanceEvent.Set();
        advanceEvent.WaitOne(TimeSpan.FromSeconds(intervalSeconds + 1));
        
        stopwatch.Stop();
        var actualInterval = stopwatch.Elapsed.TotalMilliseconds;
        var expectedInterval = intervalSeconds * 1000;
        
        Assert.InRange(actualInterval, expectedInterval - 100, expectedInterval + 100);
    }, 
    iter: 100);
}
```

### Example-Based Unit Tests

**Framework**: xUnit with FluentAssertions (same as Phase 1/2)

**Test Categories**:

1. **Service Tests**:
   - SlideshowService: Specific intervals, transitions, pause/resume
   - ComparisonService: Specific image counts, layouts, sync operations
   - CommandPaletteService: Specific queries, command execution, recent commands
   - AnimationService: Specific GIFs, frame extraction, playback control
   - CodecDetectionService: Codec availability, Store URLs, caching
   - LayoutService: Specific grid sizes, filmstrip configurations

2. **Model Tests**:
   - SlideshowConfig: Defaults, validation
   - AnimatedGif: Frame data, delays
   - CommandItem: Equality, match scoring
   - LayoutConfig: Grid calculations, filmstrip settings
   - CodecAvailability: Detection results

3. **ViewModel Tests**:
   - SlideshowState: Start/stop/pause, timer reset
   - ComparisonState: Layout changes, image loading, sync toggle
   - CommandPaletteState: Search filtering, command execution
   - AnimationState: Playback control, frame navigation
   - LayoutState: Layout switching, grid density

**Example Unit Test**:
```csharp
[Fact]
public void CommandPaletteService_FuzzyMatch_ReturnsCorrectMatches()
{
    // Arrange
    var commands = new List<CommandItem>
    {
        new CommandItem { Id = "rotate.left", Title = "Rotate Left" },
        new CommandItem { Id = "rotate.right", Title = "Rotate Right" },
        new CommandItem { Id = "flip.horizontal", Title = "Flip Horizontal" }
    };
    var service = new CommandPaletteService(commands);
    
    // Act
    var result = service.FilterCommands("rot");
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().HaveCount(2);
    result.Value.Should().Contain(c => c.Id == "rotate.left");
    result.Value.Should().Contain(c => c.Id == "rotate.right");
    result.Value.Should().NotContain(c => c.Id == "flip.horizontal");
}
```

### Integration Tests

**Test Scenarios**:

1. **GIF Animation**:
   - Load real animated GIFs with various frame counts
   - Verify frame extraction and delay parsing
   - Test playback timing with real GIFs
   - Test with single-frame GIFs

2. **Codec Detection**:
   - Test codec detection on systems with/without codecs
   - Verify Store URL generation
   - Test caching behavior
   - Test with mock registry access

3. **Slideshow Transitions**:
   - Test all transition effects with real images
   - Verify transition timing
   - Test transition cancellation
   - Test memory usage during transitions

4. **Comparison Layouts**:
   - Test side-by-side with 2, 3, 4 images
   - Test overlay with different image dimensions
   - Test synchronized zoom with real images
   - Test memory usage with multiple images

5. **Custom Layouts**:
   - Test grid layout with various sizes
   - Test filmstrip with real thumbnails
   - Test layout switching
   - Test memory usage with many images

6. **Command Palette**:
   - Test fuzzy search with real command list
   - Test command execution
   - Test recent commands persistence
   - Test performance with 200+ commands

**Example Integration Test**:
```csharp
[Fact]
public async Task AnimationService_RealAnimatedGif_ExtractsAllFrames()
{
    // Arrange
    var testGifPath = Path.Combine(TestDataFolder, "animated-10frames.gif");
    var service = new AnimationService();
    
    // Act
    var result = await service.LoadAnimatedGifAsync(testGifPath);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.TotalFrames.Should().Be(10);
    result.Value.Frames.Should().HaveCount(10);
    result.Value.FrameDelays.Should().HaveCount(10);
    result.Value.FrameDelays.Should().AllSatisfy(delay => delay.Should().BeGreaterThan(0));
}
```

### Manual Testing

**Test Scenarios**:

1. **Slideshow Mode**:
   - Slideshow timing accuracy (visual verification)
   - Transition smoothness and appearance
   - Pause/resume behavior
   - Interval adjustment during playback
   - Looping behavior

2. **Comparison Mode**:
   - Side-by-side layout with 2, 3, 4 images
   - Overlay opacity adjustment
   - Layer swapping
   - Synchronized zoom and pan feel
   - Navigation in comparison mode

3. **Command Palette**:
   - Fuzzy search responsiveness
   - Keyboard navigation
   - Command execution
   - Recent commands display
   - Visual appearance and animations

4. **GIF Animation**:
   - Animation playback smoothness
   - Frame control responsiveness
   - Frame slider accuracy
   - Looping behavior
   - Static GIF display

5. **Codec Detection**:
   - HEIC file handling with/without codec
   - AVIF file handling with/without codec
   - Store link functionality
   - Error message clarity

6. **Custom Layouts**:
   - Grid layout appearance and responsiveness
   - Grid density adjustment
   - Filmstrip focus area and thumbnail strip
   - Thumbnail scrolling
   - Layout switching

7. **Performance**:
   - Slideshow with large images
   - Comparison with 4 high-resolution images
   - Grid with 36 images (6×6)
   - Command palette search with 200+ commands
   - GIF with 100+ frames

### Test Coverage Goals

- **Services**: 90%+ code coverage with unit and integration tests
- **Models**: 100% code coverage with unit tests
- **ViewModels**: 80%+ code coverage with unit tests
- **Views**: Manual testing for UI behavior

### Performance Testing

**Performance Targets**:
- Slideshow transition: < 400ms
- Comparison rendering (2 images): < 600ms
- Command palette search: < 50ms for 200 commands
- GIF frame extraction: < 2s for 100 frames
- Grid layout rendering (9 images): < 500ms
- Codec detection: < 100ms

**Performance Test Scenarios**:
- Slideshow with 1000+ images
- Comparison with 4K images
- Command palette with 500+ commands
- GIF with 200+ frames
- Grid layout with 6×6 (36 images)
- Filmstrip with 500+ thumbnails


## Implementation Guidance

### Development Phases

**Phase 3A: Slideshow Mode (Week 1-2)**
1. Implement SlideshowService with timing and transitions
2. Implement SlideshowState and slideshow controls
3. Implement transition effects (None, Fade, Slide, Zoom)
4. Add slideshow commands to MainViewModel
5. Write unit and property tests for slideshow
6. Integration tests with real images and timing

**Phase 3B: Comparison Mode (Week 3-4)**
1. Implement ComparisonService with layout calculations
2. Implement ComparisonState and ComparisonView UI
3. Implement side-by-side layouts (2, 3, 4 images)
4. Implement overlay layout with opacity control
5. Implement synchronized zoom and pan
6. Write unit and property tests for comparison
7. Integration tests with real images

**Phase 3C: Command Palette (Week 5)**
1. Implement CommandPaletteService with fuzzy search
2. Implement CommandPaletteState and CommandPalette UI
3. Build command registry with all Phase 1/2/3 commands
4. Implement keyboard navigation and execution
5. Implement recent commands tracking
6. Write unit and property tests for command palette
7. Performance tests with large command lists

**Phase 3D: GIF Animation (Week 6)**
1. Implement AnimationService with frame extraction
2. Implement AnimationState and FrameControl UI
3. Implement playback timing and looping
4. Implement frame navigation controls
5. Write unit and property tests for animation
6. Integration tests with real animated GIFs

**Phase 3E: Codec Detection and Format Support (Week 7)**
1. Implement CodecDetectionService with registry checks
2. Extend ImageDecodeService with HEIC/AVIF support
3. Implement codec unavailable error dialogs
4. Implement Store link functionality
5. Update FolderPlaylistService with codec-dependent filtering
6. Write unit and integration tests for codec detection

**Phase 3F: Custom Layouts (Week 8-9)**
1. Implement LayoutService with grid and filmstrip
2. Implement LayoutState and layout views
3. Implement GridLayout with density adjustment
4. Implement FilmstripView with focus area
5. Integrate with ThumbnailService from Phase 2
6. Write unit and property tests for layouts
7. Integration tests with real images

**Phase 3G: Integration and Polish (Week 10-11)**
1. Integrate all Phase 3 features with MainViewModel
2. Implement slideshow in comparison and custom layouts
3. Update settings persistence with Phase 3 preferences
4. Tune performance for all advanced modes
5. Implement all keyboard shortcuts
6. Manual testing and bug fixes
7. Performance optimization
8. Documentation updates

### Key Implementation Details

#### Slideshow Transition Rendering

**Fade Transition**:
```csharp
public async Task<Result<BitmapImage>> ApplyFadeTransitionAsync(
    BitmapImage currentImage,
    BitmapImage nextImage)
{
    try
    {
        // Create storyboard for fade out/in
        var storyboard = new Storyboard();
        
        // Fade out current image (300ms)
        var fadeOut = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(300)
        };
        Storyboard.SetTarget(fadeOut, CurrentImageCanvas);
        Storyboard.SetTargetProperty(fadeOut, "Opacity");
        storyboard.Children.Add(fadeOut);
        
        // Fade in next image (300ms, starts after fade out)
        var fadeIn = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(300),
            BeginTime = TimeSpan.FromMilliseconds(300)
        };
        Storyboard.SetTarget(fadeIn, NextImageCanvas);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");
        storyboard.Children.Add(fadeIn);
        
        // Set next image
        NextImageCanvas.Source = nextImage;
        NextImageCanvas.Opacity = 0.0;
        
        // Run animation
        await storyboard.BeginAsync();
        
        // Swap canvases
        CurrentImageCanvas.Source = nextImage;
        CurrentImageCanvas.Opacity = 1.0;
        
        return Result<BitmapImage>.Success(nextImage);
    }
    catch (Exception ex)
    {
        var error = ViewerError.TransitionError(TransitionEffect.Fade, ex);
        return Result<BitmapImage>.Failure(error);
    }
}
```

#### Comparison Synchronized Zoom

**Relative Position Calculation**:
```csharp
public Result<Point> ConvertToRelativePosition(
    Point absolutePosition,
    Size imageSize)
{
    try
    {
        var relativeX = absolutePosition.X / imageSize.Width;
        var relativeY = absolutePosition.Y / imageSize.Height;
        
        // Clamp to 0-1 range
        relativeX = Math.Clamp(relativeX, 0.0, 1.0);
        relativeY = Math.Clamp(relativeY, 0.0, 1.0);
        
        return Result<Point>.Success(new Point(relativeX, relativeY));
    }
    catch (Exception ex)
    {
        var error = ViewerError.SyncError("ConvertToRelativePosition", ex);
        return Result<Point>.Failure(error);
    }
}

public Result SynchronizeZoom(
    List<ImageCanvas> canvases,
    double zoomLevel,
    Point panOffset)
{
    try
    {
        // Get relative position from first canvas
        var firstCanvas = canvases[0];
        var relativePosition = ConvertToRelativePosition(
            panOffset, 
            new Size(firstCanvas.ImageWidth, firstCanvas.ImageHeight));
        
        if (!relativePosition.IsSuccess)
            return Result.Failure(relativePosition.Error);
        
        // Apply zoom and relative pan to all canvases
        foreach (var canvas in canvases)
        {
            canvas.ZoomLevel = zoomLevel;
            
            // Convert relative position to absolute for this canvas
            var absoluteX = relativePosition.Value.X * canvas.ImageWidth;
            var absoluteY = relativePosition.Value.Y * canvas.ImageHeight;
            
            canvas.PanOffset = new Point(absoluteX, absoluteY);
        }
        
        return Result.Success();
    }
    catch (Exception ex)
    {
        var error = ViewerError.SyncError("SynchronizeZoom", ex);
        return Result.Failure(error);
    }
}
```

#### Command Palette Fuzzy Search

**Fuzzy Matching Implementation**:
```csharp
public Result<List<CommandItem>> FilterCommands(string query)
{
    try
    {
        if (string.IsNullOrWhiteSpace(query))
            return Result<List<CommandItem>>.Success(_allCommands);
        
        var matches = new List<(CommandItem command, int score)>();
        
        foreach (var command in _allCommands)
        {
            var score = CalculateMatchScore(query, command.Title);
            if (score > 0)
            {
                matches.Add((command, score));
            }
        }
        
        // Sort by score descending
        var sortedMatches = matches
            .OrderByDescending(m => m.score)
            .Select(m => m.command)
            .ToList();
        
        return Result<List<CommandItem>>.Success(sortedMatches);
    }
    catch (Exception ex)
    {
        var error = ViewerError.CommandPaletteError("FilterCommands", ex);
        return Result<List<CommandItem>>.Failure(error);
    }
}
```

#### GIF Frame Extraction

**Frame Extraction with WIC**:
```csharp
public async Task<Result<AnimatedGif>> LoadAnimatedGifAsync(string filePath)
{
    try
    {
        using var stream = File.OpenRead(filePath);
        var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
        
        // Check if animated
        if (decoder.FrameCount <= 1)
        {
            // Single frame, treat as static
            var staticFrame = await LoadStaticFrame(decoder);
            return Result<AnimatedGif>.Success(new AnimatedGif
            {
                Frames = new List<BitmapImage> { staticFrame },
                FrameDelays = new List<int> { 0 },
                TotalFrames = 1,
                Dimensions = new Size(staticFrame.PixelWidth, staticFrame.PixelHeight)
            });
        }
        
        // Extract all frames
        var frames = new List<BitmapImage>();
        var delays = new List<int>();
        
        for (uint i = 0; i < decoder.FrameCount; i++)
        {
            var frame = await decoder.GetFrameAsync(i);
            
            // Extract frame delay
            var delay = await ExtractFrameDelay(frame);
            delays.Add(delay);
            
            // Convert to BitmapImage
            var bitmapImage = await ConvertFrameToBitmapImage(frame);
            frames.Add(bitmapImage);
        }
        
        var animatedGif = new AnimatedGif
        {
            Frames = frames,
            FrameDelays = delays,
            TotalFrames = (int)decoder.FrameCount,
            Dimensions = new Size(frames[0].PixelWidth, frames[0].PixelHeight)
        };
        
        return Result<AnimatedGif>.Success(animatedGif);
    }
    catch (Exception ex)
    {
        var error = ViewerError.AnimationError(filePath, ex);
        return Result<AnimatedGif>.Failure(error);
    }
}

private async Task<int> ExtractFrameDelay(BitmapFrame frame)
{
    try
    {
        var properties = await frame.BitmapProperties.GetPropertiesAsync(
            new[] { "/grctlext/Delay" });
        
        if (properties.TryGetValue("/grctlext/Delay", out var delayValue))
        {
            // GIF delay is in 1/100 seconds
            var delay = (ushort)delayValue.Value * 10;
            return Math.Max(delay, 10); // Minimum 10ms
        }
        
        return 100; // Default 100ms
    }
    catch
    {
        return 100; // Default on error
    }
}
```

#### Codec Detection

**Windows Codec Detection**:
```csharp
public async Task<Result<CodecAvailability>> DetectCodecsAsync()
{
    try
    {
        bool heicSupported = await CheckHeicCodec();
        bool avifSupported = await CheckAvifCodec();
        
        var availability = new CodecAvailability
        {
            HeicSupported = heicSupported,
            AvifSupported = avifSupported,
            DetectionTime = DateTime.UtcNow
        };
        
        // Cache for session
        _cachedAvailability = availability;
        
        return Result<CodecAvailability>.Success(availability);
    }
    catch (Exception ex)
    {
        var error = ViewerError.CodecDetectionError(ex);
        return Result<CodecAvailability>.Failure(error);
    }
}

private async Task<bool> CheckHeicCodec()
{
    try
    {
        // Try to create HEIC decoder
        var testStream = CreateTestHeicStream();
        var decoder = await BitmapDecoder.CreateAsync(testStream);
        return decoder.DecoderInformation.CodecId == BitmapDecoder.HeifDecoderId;
    }
    catch
    {
        return false;
    }
}

private async Task<bool> CheckAvifCodec()
{
    try
    {
        // Try to create AVIF decoder
        var testStream = CreateTestAvifStream();
        var decoder = await BitmapDecoder.CreateAsync(testStream);
        // AVIF doesn't have a specific decoder ID, check by attempting decode
        return true;
    }
    catch
    {
        return false;
    }
}
```

#### Grid Layout Calculation

**Grid Cell Positioning**:
```csharp
public Result<GridLayout> CalculateGridLayout(
    int rows,
    int columns,
    Size canvasSize)
{
    try
    {
        const int spacing = 8; // 8px between cells
        
        var totalSpacingWidth = (columns - 1) * spacing;
        var totalSpacingHeight = (rows - 1) * spacing;
        
        var cellWidth = (canvasSize.Width - totalSpacingWidth) / columns;
        var cellHeight = (canvasSize.Height - totalSpacingHeight) / rows;
        
        var cells = new List<GridCell>();
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                var x = col * (cellWidth + spacing);
                var y = row * (cellHeight + spacing);
                
                cells.Add(new GridCell
                {
                    Row = row,
                    Column = col,
                    X = x,
                    Y = y,
                    Width = cellWidth,
                    Height = cellHeight
                });
            }
        }
        
        var layout = new GridLayout
        {
            Rows = rows,
            Columns = columns,
            Cells = cells,
            TotalCells = rows * columns
        };
        
        return Result<GridLayout>.Success(layout);
    }
    catch (Exception ex)
    {
        var error = ViewerError.LayoutError("CalculateGridLayout", ex);
        return Result<GridLayout>.Failure(error);
    }
}
```

### Performance Optimization

**Slideshow Performance**:
- Preload next image during current image display
- Use hardware-accelerated transitions
- Cancel transitions on manual navigation
- Reuse image canvases to avoid allocation

**Comparison Performance**:
- Load images in parallel
- Use shared zoom/pan calculations
- Optimize synchronized updates
- Limit comparison to 4 images maximum

**Command Palette Performance**:
- Cache fuzzy match scores
- Debounce search input (50ms)
- Virtualize command list for large sets
- Precompute lowercase strings

**Animation Performance**:
- Extract all frames at load time
- Cache frames in memory
- Use timer with variable delays
- Optimize frame rendering

**Layout Performance**:
- Use virtualization for grid layout
- Reuse thumbnails from Phase 2 cache
- Load visible images first
- Implement progressive loading

### Extensibility for Future Phases

**Video Playback**:
- AnimationService pattern can extend to video frames
- Frame control UI reusable for video
- Playback timing logic applicable

**Advanced Editing**:
- Comparison mode foundation for before/after editing
- Transform operations from Phase 2 extensible
- Layout modes support editing workflows

**Cloud Integration**:
- Playlist model supports remote sources
- Codec detection pattern for cloud formats
- Command palette extensible with cloud commands


## Acceptance Criteria

Phase 3 is complete when:

1. ✅ A user can press F5 to start slideshow mode with automatic image advancement
2. ✅ Slideshow advances at the configured interval (1s to 60s) within ±100ms tolerance
3. ✅ Slideshow loops back to the first image after reaching the last image
4. ✅ Manual navigation during slideshow resets the advance timer
5. ✅ A user can press Space to pause/resume slideshow
6. ✅ A user can press Plus/Minus to adjust slideshow interval during playback
7. ✅ Slideshow supports transition effects: None, Fade, Slide, Zoom
8. ✅ Transitions render at 60 FPS and complete within specified durations
9. ✅ Transitions block manual navigation until completion
10. ✅ A user can press Ctrl+D to enter side-by-side comparison mode
11. ✅ Comparison mode displays 2, 3, or 4 images simultaneously
12. ✅ Side-by-side layout divides canvas evenly and preserves aspect ratios
13. ✅ A user can press Ctrl+Shift+D to enter overlay comparison mode
14. ✅ Overlay mode layers images with adjustable opacity (0-100%)
15. ✅ A user can press S in overlay mode to swap layer order
16. ✅ Navigation in comparison mode shifts all images together
17. ✅ A user can press Ctrl+L to toggle synchronized zoom/pan
18. ✅ Synchronized zoom applies same zoom level to all images
19. ✅ Synchronized pan uses relative coordinates for different image dimensions
20. ✅ A user can press Ctrl+Shift+P to open the command palette
21. ✅ Command palette displays all commands from Phase 1, 2, and 3
22. ✅ Fuzzy search filters commands with character order matching
23. ✅ Fuzzy search ranks results by match quality
24. ✅ Command palette updates within 50ms of user input
25. ✅ Command palette shows keyboard shortcuts next to commands
26. ✅ Recent commands appear at the top of the palette
27. ✅ Animated GIFs are detected and display first frame by default
28. ✅ A user can press P to play/pause GIF animation
29. ✅ GIF animation respects frame delays embedded in the file
30. ✅ GIF animation loops back to first frame after last frame
31. ✅ A user can press Period/Comma to navigate frames when paused
32. ✅ Frame control UI displays play/pause, previous/next, and frame slider
33. ✅ Frame slider allows jumping to specific frames
34. ✅ HEIC codec detection runs at startup and caches results
35. ✅ HEIC files open when codec is installed
36. ✅ HEIC files show codec unavailable error with Store link when codec missing
37. ✅ AVIF codec detection runs at startup and caches results
38. ✅ AVIF files open when codec is installed
39. ✅ AVIF files show codec unavailable error with Store link when codec missing
40. ✅ Codec unavailable dialogs don't repeat during the same session
41. ✅ Folder playlist includes HEIC/AVIF files only when codecs are available
42. ✅ A user can press Ctrl+G to enter grid layout mode
43. ✅ Grid layout displays images in configurable rows and columns (default 3×3)
44. ✅ A user can press Ctrl+Plus/Minus to adjust grid density
45. ✅ Grid layout preserves aspect ratios for all images
46. ✅ A user can click an image in grid layout to view it full-screen
47. ✅ A user can press Ctrl+F (in layout context) to enter filmstrip mode
48. ✅ Filmstrip displays large focus image with horizontal thumbnail strip
49. ✅ Clicking a thumbnail in filmstrip changes the focus image
50. ✅ Filmstrip scrolls thumbnail strip to keep current image centered
51. ✅ Filmstrip reuses thumbnails from Phase 2 ThumbnailService
52. ✅ Slideshow works in comparison mode, advancing both images together
53. ✅ Slideshow works in grid layout, advancing all visible images
54. ✅ Slideshow works in filmstrip mode, advancing the focus image
55. ✅ Layout preference persists between sessions
56. ✅ Slideshow interval and transition preferences persist between sessions
57. ✅ Comparison sync zoom preference persists between sessions
58. ✅ All correctness properties pass property-based tests with 100+ iterations
59. ✅ Service layer has 90%+ unit test coverage
60. ✅ Integration tests verify GIF decoding, codec detection, transitions, and layouts with real files
61. ✅ Manual testing confirms UI polish, animations, timing accuracy, and performance
62. ✅ Slideshow transitions complete within specified durations (Fade 300ms, Slide 400ms, Zoom 350ms)
63. ✅ Comparison mode renders 2 images within 600ms
64. ✅ Command palette search returns results within 50ms for 200 commands
65. ✅ Grid layout renders 9 images within 500ms
66. ✅ All new error categories are logged with structured information
67. ✅ User-facing error messages are calm and actionable
68. ✅ Phase 1 and Phase 2 functionality remains unchanged and all tests still pass
69. ✅ Settings persistence includes all Phase 3 preferences
70. ✅ Service interfaces support future extensibility (video playback, advanced editing, cloud integration)

