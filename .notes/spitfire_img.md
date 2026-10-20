# spitfire.img - smart and fast image viewer
<!--
i need help planning development documents to create a windows native image viewer application.

I want it to be a modern approach, and i want it to be ui design focused.
- it should have the regular capibilities of an image viewing application.
  - open/close files, import/export,
  - a menu system
    - file, layout, etc..
- its main purpose will be to provide images to the user with two main design purposes;

    1. image presentation
    2. quality of life,

1.1 The image presentation: with a ui that fades, even when it is in its window mode and not fullscreen. the ui should fade for presentation of the image.
  - the corners should be rounded only slightly for UI design.
2.1 When an image is opened, that folder should be treated as the playlist, images should be easily searched by using the left and right keyboard keys. the directory for this should not have to be set, it should be automatic by the location of the opened image.

I want this designed beautifully, because it is a presentation focused application in fuction it must meet that standard in UI design.
 -->

For the Windows-native stack, the current best-fit direction is **WinUI 3 with the Windows App SDK**, because Microsoft describes WinUI as the recommended native UI framework for modern Windows apps, using XAML with C# or C++ and Fluent-style controls. ([Microsoft Learn][1]) Windows App SDK also includes WinUI for modern, high-performance Windows desktop interfaces aligned with Fluent Design. ([Microsoft Learn][2])

---

## Recommended Development Documents

## 1. Product Vision Document

**Purpose:** Define what the app is and why it exists.

**Working product statement:**

> A modern Windows-native image viewer designed for beautiful image presentation, fast local-folder browsing, and minimal UI interference. The application should feel elegant, calm, and highly responsive, with an interface that fades away to let images become the focus even in normal windowed mode.

**Core identity:**

* Native Windows desktop app
* Presentation-first image viewer
* Minimal, beautiful, low-distraction UI
* Fast keyboard-driven image browsing
* Automatic folder-as-playlist behavior
* Quality-of-life focused

**Non-goals:**

* Not a full photo editor
* Not a library/cataloging app at first
* Not cloud-first
* Not a heavy media manager

---

## 2. Product Requirements Document

**Purpose:** Define what the app must do.

## Core Features

### File Handling

The app should support:

* Open image file
* Close current image
* Open recent files
* Import image or folder
* Export/save copy
* Reveal image in File Explorer
* Copy file path
* Copy image to clipboard
* Delete or move image to recycle bin, ideally with confirmation
* Drag-and-drop image opening

### Supported Formats

Initial support should include:

* PNG
* JPEG/JPG
* WEBP
* BMP
* GIF, at least static preview or full animation later
* TIFF, optional depending on decoding support
* AVIF/HEIC, optional stretch goal

### Folder-as-Playlist Behavior

When a user opens an image, the app automatically treats that image’s folder as the browsing context.

Example:

> User opens `C:\Photos\Event\IMG_0042.jpg`.
> The app automatically loads all supported images in `C:\Photos\Event\` as the current playlist.

No folder setup should be required.

### Navigation

Required:

* Left arrow: previous image
* Right arrow: next image
* Home: first image
* End: last image
* Space: toggle UI visibility or presentation mode
* Escape: exit fullscreen or close overlay
* Mouse wheel: zoom or previous/next depending on mode
* Double-click: fullscreen toggle
* Enter: fullscreen or focus image

### Search and Browse

Since the folder becomes the playlist, the app should support:

* Quick search by filename
* Optional thumbnail strip
* Optional side panel folder list
* Jump-to-image command palette
* Sort by name, date modified, size, type
* Filter by extension

### Image Presentation

Required:

* Image centered by default
* Fit-to-window default mode
* Pixel-perfect zoom option
* Actual size / 100%
* Fill window
* Rotate left/right
* Mirror/flip, optional
* Background dimming or neutral canvas
* Smooth zoom and pan
* High-quality scaling

---

## 3. UI/UX Design Specification

**Purpose:** Define the visual and interaction design.

This should be one of the most important documents because your app’s value depends on presentation quality.

## Design Principles

### 1. The Image Is the Interface

The app chrome should never compete with the image. Controls appear only when useful and fade away when the user is viewing.

### 2. UI Should Be Present, Then Absent

Controls should appear when the user moves the mouse, presses a key, opens a menu, or changes image. After a short delay, they fade out.

### 3. Windowed Mode Should Still Feel Like Presentation Mode

Most image viewers only become elegant in fullscreen. This app should feel polished even inside a normal resizable window.

### 4. Small Radius, Soft Contrast

UI corners should be slightly rounded, not overly bubbly. Suggested radius:

* Panels: 8–10 px
* Buttons: 6–8 px
* Image frame, if any: 4–6 px
* Menus: 8 px

### 5. Minimal but Discoverable

The interface should not be hidden to the point of confusion. Users should see controls briefly, then the image takes over.

---

## Main Layout Concept

### Window Structure

```text
┌──────────────────────────────────────────────┐
│ Top Menu / Command Bar                       │
│ File  View  Layout  Image  Tools  Help       │
├──────────────────────────────────────────────┤
│                                              │
│                                              │
│              Image Presentation Area         │
│                                              │
│                                              │
├──────────────────────────────────────────────┤
│ Optional fading bottom info / controls       │
│ IMG_0042.jpg       42 / 180        3840×2160 │
└──────────────────────────────────────────────┘
```

But visually, the top and bottom UI should fade to near-invisible during viewing.

---

## UI Fade Behavior

### Visible State

UI appears when:

* App opens an image
* Mouse moves
* User presses keyboard navigation
* Menu is opened
* Image changes
* User hovers near top or bottom edge

### Fade State

After inactivity:

* Top bar fades out
* Bottom metadata bar fades out
* Side panels fade or collapse
* Cursor may hide in presentation mode
* Image remains fully visible

### Recommended Timing

* Show UI immediately on activity
* Wait 1.5–2.5 seconds
* Fade out over 200–350 ms
* Keep a subtle invisible hover zone near window edges

### Windowed Mode Behavior

Even in windowed mode:

* UI chrome should fade
* The image should occupy maximum available space
* The background should remain elegant and neutral
* Menus should reappear when the mouse nears the top

---

## Menu System Document

**Purpose:** Define the menu structure.

## Proposed Menus

### File

* Open File…
* Open Folder…
* Open Recent
* Close Image
* Import…
* Export / Save Copy…
* Reveal in File Explorer
* Properties
* Exit

### View

* Zoom In
* Zoom Out
* Actual Size
* Fit to Window
* Fill Window
* Toggle Fullscreen
* Toggle UI
* Toggle Metadata
* Toggle Thumbnail Strip

### Layout

* Minimal View
* Presentation View
* Browser View
* Show/Hide Top Bar
* Show/Hide Bottom Bar
* Show/Hide Side Panel
* Background Style

### Image

* Rotate Left
* Rotate Right
* Flip Horizontal
* Flip Vertical
* Copy Image
* Copy Path
* Set as Desktop Background, optional

### Playlist

* Previous Image
* Next Image
* First Image
* Last Image
* Sort By Name
* Sort By Date
* Sort By Size
* Shuffle, optional
* Refresh Folder

### Help

* Keyboard Shortcuts
* About
* Release Notes

---

## 4. Interaction Design Document

**Purpose:** Define how users move through the app.

## Primary User Flow

### Opening an Image

1. User opens app.
2. User selects an image.
3. Image appears centered.
4. App scans the folder.
5. Folder becomes automatic playlist.
6. UI briefly displays:

   * filename
   * image index
   * dimensions
   * folder name
7. UI fades out.

### Navigating Images

1. User presses Right Arrow.
2. Next image loads instantly or with subtle transition.
3. Bottom info appears briefly.
4. UI fades again.

### Searching Current Folder

1. User presses Ctrl+F.
2. A small search overlay appears.
3. User types part of a filename.
4. Matching images appear in a lightweight list.
5. Enter opens selected result.
6. Search overlay disappears.

### Presentation Viewing

1. User double-clicks or presses F11.
2. App enters fullscreen.
3. UI fades out more aggressively.
4. Cursor hides after inactivity.
5. Keyboard navigation remains active.

---

## 5. Visual Design System Document

**Purpose:** Keep the app beautiful and consistent.

## Suggested Visual Direction

The app should use a restrained Fluent-inspired design:

* Dark and light mode support
* Acrylic/Mica-style surfaces where appropriate
* Soft shadows
* Subtle borders
* Slightly rounded controls
* Transparent overlays
* Minimal color accents

WinUI is designed around modern Windows UI patterns and Fluent-style controls, which makes it a good match for this design direction. ([Microsoft Learn][3])

## Color System

### Dark Mode

* App background: near-black, not pure black
* Image canvas: charcoal or adaptive based on image
* UI panels: translucent dark gray
* Text: soft white
* Secondary text: muted gray
* Accent: user-selectable or system accent color

### Light Mode

* App background: soft off-white or pale gray
* Image canvas: neutral gray
* UI panels: translucent white
* Text: near-black
* Secondary text: medium gray

## Typography

Use the native Windows typography direction:

* UI font: Segoe UI Variable or system default
* Metadata: small, calm, readable
* Menus: standard Windows sizing
* Large labels should be rare

## Motion

Motion should be subtle:

* Fade controls in/out
* Smooth zoom
* Gentle image transition, optional
* No flashy animations
* Avoid distracting from the image

---

## 6. Technical Architecture Document

**Purpose:** Define how the app should be built.

## Recommended Stack

### Primary Recommendation

* **WinUI 3 + Windows App SDK + C#/.NET**

Best if you want:

* Modern Windows-native UI
* Fluent-style controls
* Good productivity
* Windows 11 feel
* Native packaging options

Microsoft’s current Windows developer overview identifies WinUI as the recommended native UI framework for modern Windows apps. ([Microsoft Learn][1])

### Alternative

* **WPF + modern styling**

Best if you want:

* Mature desktop framework
* Easier custom rendering in some cases
* More existing examples
* Potentially faster development

But for a “modern native Windows” design-focused app, I would start with **WinUI 3** unless there is a specific reason not to.

---

## Proposed App Modules

```text
ImageViewer.App
├── Views
│   ├── MainWindow
│   ├── ImageCanvas
│   ├── MenuBar
│   ├── BottomInfoBar
│   ├── SearchOverlay
│   └── ThumbnailStrip
│
├── ViewModels
│   ├── MainViewModel
│   ├── ImageViewModel
│   ├── PlaylistViewModel
│   └── SearchViewModel
│
├── Services
│   ├── FileOpenService
│   ├── ImageDecodeService
│   ├── FolderPlaylistService
│   ├── NavigationService
│   ├── ExportService
│   ├── SettingsService
│   └── ThemeService
│
├── Models
│   ├── ImageItem
│   ├── Playlist
│   ├── ImageMetadata
│   └── ViewerSettings
│
└── Resources
    ├── Styles
    ├── Icons
    └── ThemeTokens
```

---

## 7. Image Playlist Behavior Specification

**Purpose:** Define the automatic folder behavior clearly.

## Rules

When an image is opened:

1. Determine parent folder.
2. Scan for supported image files.
3. Sort using current sort mode.
4. Set opened image as selected item.
5. Enable previous/next navigation.
6. Watch folder for changes, optional.
7. Refresh playlist when files are added, deleted, or renamed.

## Example

```text
Opened file:
D:\Portfolio\Shoot01\hero-shot.png

Playlist source:
D:\Portfolio\Shoot01\

Detected files:
cover.jpg
hero-shot.png
wide-angle.webp
detail-01.png
detail-02.png

Current index:
2 / 5
```

## Edge Cases

* Folder contains only one image
* Image is deleted while open
* File is renamed externally
* Folder access denied
* Network folder is slow
* Huge folder with thousands of images
* Corrupt image file
* Unsupported extension

---

## 8. Keyboard Shortcuts Document

**Purpose:** Define efficiency and quality-of-life behavior.

| Action                    | Shortcut     |
| ------------------------- | ------------ |
| Open file                 | Ctrl+O       |
| Open folder               | Ctrl+Shift+O |
| Close image               | Ctrl+W       |
| Previous image            | Left Arrow   |
| Next image                | Right Arrow  |
| First image               | Home         |
| Last image                | End          |
| Search current folder     | Ctrl+F       |
| Fullscreen                | F11          |
| Toggle UI                 | Space        |
| Zoom in                   | Ctrl++       |
| Zoom out                  | Ctrl+-       |
| Actual size               | Ctrl+1       |
| Fit to window             | Ctrl+0       |
| Rotate left               | Ctrl+L       |
| Rotate right              | Ctrl+R       |
| Copy image                | Ctrl+C       |
| Copy path                 | Ctrl+Shift+C |
| Reveal in Explorer        | Ctrl+E       |
| Exit fullscreen / overlay | Esc          |

---

## 9. MVP Scope Document

**Purpose:** Prevent the first version from becoming too large.

## MVP Version 0.1

Must include:

* Open image
* Automatic folder playlist
* Left/right keyboard navigation
* Fit-to-window display
* Zoom and pan
* Fullscreen
* Fading top/bottom UI
* File menu
* Basic metadata display
* Dark/light theme
* Basic settings persistence

## Version 0.2

Add:

* Search current folder
* Thumbnail strip
* Export/save copy
* Rotate image
* Recent files
* Better metadata
* Drag-and-drop

## Version 0.3

Add:

* Slideshow mode
* Custom layout modes
* Image comparison mode
* Animated GIF/WebP support
* HEIC/AVIF support
* Command palette

---

## Suggested Document Set to Actually Create

I would create these files in your project repo:

```text
docs/
├── 00-product-vision.md
├── 01-product-requirements.md
├── 02-ui-ux-design-spec.md
├── 03-menu-and-navigation-spec.md
├── 04-folder-playlist-behavior.md
├── 05-technical-architecture.md
├── 06-keyboard-shortcuts.md
├── 07-mvp-roadmap.md
├── 08-design-system.md
└── 09-open-questions.md
```

---

## Important Design Decisions to Lock Early

These are the decisions I would make before coding:

1. **Use WinUI 3 + Windows App SDK** for the native modern Windows approach.
2. **Make fading UI a core system**, not a later visual effect.
3. **Treat folder-as-playlist as default behavior**, not a separate “open folder” feature.
4. **Build the image canvas as the heart of the app**, with the menus and panels orbiting it.
5. **Keep editing features minimal**, because editing can dilute the presentation-first purpose.
6. **Design windowed mode like presentation mode**, which is the unique part of your app.

---

## App Concept Name Ideas

A few names that fit the presentation-first direction:

* **Gallery Frame**
* **LumaView**
* **Stillroom**
* **Frameglass**
* **VistaFrame**
* **QuietView**
* **FolioView**
* **ImageStage**
* **ClearFrame**
* **AuraView**

My favorite for this concept: **ImageStage**. It communicates that the app presents images rather than merely opening them.

[1]: https://learn.microsoft.com/en-us/windows/apps/get-started/?utm_source=chatgpt.com "Windows developer platform overview - Windows apps"
[2]: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/?utm_source=chatgpt.com "Build desktop Windows apps with the Windows App SDK"
[3]: https://learn.microsoft.com/en-us/windows/apps/winui/winui3/?utm_source=chatgpt.com "WinUI 3 - Windows apps"

---

## Development Plan Refinement 2

## spitfire.img Roadmap

## Phase 1: MVP Viewer

Goal: ship a complete local image viewing loop with presentation-first UI.

Acceptance criteria:

* Open `PNG`, `JPG/JPEG`, `BMP`, and `WEBP` files.
* Automatically use the opened image's parent folder as the playlist.
* Support previous, next, first, and last keyboard navigation.
* Provide fit-to-window, actual size, zoom, pan, and fullscreen behavior.
* Fade top and bottom UI chrome in windowed and fullscreen modes.
* Show filename, image index, dimensions, and folder metadata.
* Persist basic theme and viewer settings.
* Write structured error logs for file, decode, playlist, settings, navigation, and unexpected errors.

## Phase 2: Browse And Quality Of Life

Goal: make everyday folder browsing faster and more convenient without changing the Phase 1 viewer core.

Candidate features:

* Search the current folder by filename.
* Add an optional thumbnail strip.
* Add recent files.
* Support drag-and-drop image opening.
* Add export or save-copy behavior.
* Add rotate and flip commands.
* Add better image metadata.
* Add `GIF` and `TIFF` support after decoding behavior is defined.

Acceptance criteria draft:

* Search and thumbnails use the same playlist model as Phase 1.
* Recent files do not replace folder-as-playlist behavior.
* New format support routes through the image decode service, not the window.

## Phase 3: Advanced Viewing Modes

Goal: add power-user presentation and comparison workflows after the core viewer is stable.

Candidate features:

* Slideshow mode.
* Custom layout modes.
* Image comparison mode.
* Command palette.
* Animated image support where relevant.
* `HEIC` and `AVIF` support after dependency and Windows codec strategy is selected.

Acceptance criteria draft:

* Comparison and slideshow modes reuse existing image item and playlist models.
* Advanced formats produce structured unsupported/decode errors when unavailable.
* New layouts preserve the low-distraction fading UI principle.

## Deferred Means Planned

Features listed in Phase 2 or Phase 3 are deferred to protect Phase 1 delivery. They are not rejected. Phase 1 code should preserve service boundaries that allow these features to be added without rewriting the viewer shell.

---

## Phase 1 MVP Viewer Design

## Purpose

Build the first complete version of `spitfire.img`: a modern Windows-native image viewer focused on presentation, fast folder navigation, and low-distraction UI. Phase 1 must feel usable end-to-end while preserving clear seams for Phase 2 and Phase 3.

## Scope

Phase 1 includes:

* Open local `PNG`, `JPG/JPEG`, `BMP`, and `WEBP` files.
* Automatically treat the opened file's parent folder as the playlist.
* Navigate previous, next, first, and last image by keyboard.
* Display images centered with fit-to-window as the default mode.
* Support zoom, pan, actual-size, fit reset, and fullscreen.
* Fade top and bottom UI chrome after inactivity in windowed and fullscreen modes.
* Show basic metadata: filename, playlist index, dimensions, and folder name.
* Persist basic settings such as theme and default view mode.
* Log structured errors to disk.

Phase 1 explicitly defers:

* `GIF`, `TIFF`, `HEIC`, and `AVIF` support.
* Search, thumbnail strip, recent files, drag-and-drop, export/save copy, rotate/flip, slideshow, comparison mode, command palette, and advanced metadata.

Deferred means planned but not built in this phase.

## Architecture

Use WinUI 3 with Windows App SDK and C#/.NET. The app should be split into presentation, workflow state, and services.

Presentation owns `MainWindow`, `ImageCanvas`, fading chrome, menu bar, bottom info bar, fullscreen state, and input routing. Workflow state owns current image, playlist state, zoom state, UI visibility state, and command availability. Services own file opening, image decoding, playlist scanning, navigation, settings persistence, and error logging.

The main window must not contain playlist scanning, image decoding, settings serialization, or error formatting logic. That boundary keeps Phase 2 and Phase 3 additive.

## Proposed Module Structure

```text
src/ImageViewer.App/
├── Views/
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── ImageCanvas.xaml
│   ├── FadingChrome.xaml
│   └── BottomInfoBar.xaml
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── ViewerState.cs
│   └── PlaylistState.cs
├── Services/
│   ├── FileOpenService.cs
│   ├── ImageDecodeService.cs
│   ├── FolderPlaylistService.cs
│   ├── NavigationService.cs
│   ├── SettingsService.cs
│   └── ErrorLogService.cs
├── Models/
│   ├── ImageItem.cs
│   ├── ImageMetadata.cs
│   ├── ViewerSettings.cs
│   └── ViewerError.cs
└── Resources/
    ├── Styles/
    └── ThemeTokens/
```

Tests should live under `tests/ImageViewer.Tests/` and focus on services and models.

## User Flow

When a user opens an image, the app validates the extension, decodes the image, scans the parent folder for supported images, sorts the playlist, selects the opened image, updates metadata, and renders the viewer. The UI appears briefly, then fades after 1.5 to 2.5 seconds of inactivity. Mouse movement, keyboard navigation, menu activation, fullscreen changes, or image changes should reveal the UI again.

Keyboard behavior:

* `Left Arrow`: previous image.
* `Right Arrow`: next image.
* `Home`: first image.
* `End`: last image.
* `F11` or double-click: toggle fullscreen.
* `Ctrl+0`: fit to window.
* `Ctrl+1`: actual size.
* `Ctrl++` / `Ctrl+-`: zoom in/out.
* `Esc`: exit fullscreen or close transient overlay.

## Error Handling

Phase 1 includes a structured error system with `ViewerError` and `ErrorLogService`.

Defined categories:

* `FileAccessError`
* `UnsupportedFormatError`
* `DecodeError`
* `PlaylistScanError`
* `NavigationError`
* `SettingsPersistenceError`
* `UnexpectedViewerError`

Each log entry should include timestamp, severity, category, user-safe message, operation, optional file path, exception type, and stack trace when available. User-facing messages must be calm and actionable, for example: "This file format is not supported in this version." Raw stack traces should never appear in the UI.

Logs should be written under the app's local data folder so they survive restarts but do not require elevated permissions.

## Continuity For Later Phases

Phase 2 and Phase 3 must remain visible during Phase 1 development through explicit docs and stable interfaces.

Create or maintain:

* `docs/roadmap.md`, listing Phase 2 and Phase 3 features with short acceptance criteria.
* Architecture decisions for WinUI 3, decoding strategy, playlist model, settings persistence, and UI fade behavior.
* Service interfaces that allow search, thumbnails, recent files, drag-and-drop, advanced formats, and comparison mode to plug in later.

Phase 1 should not hardwire future decisions into UI code. For example, supported format detection should live behind an image format or decode service rather than scattered file-extension checks in the window.

## Testing Strategy

Automated tests should cover:

* Supported extension detection for `PNG`, `JPG/JPEG`, `BMP`, and `WEBP`.
* Rejection of deferred formats with `UnsupportedFormatError`.
* Folder playlist scanning and deterministic sorting.
* Opened image index selection.
* Previous, next, first, and last navigation boundaries.
* Settings defaults and persistence.
* Structured error log entry creation.

Manual verification should cover:

* Windowed fading UI.
* Fullscreen behavior.
* Keyboard shortcuts.
* Zoom and pan feel.
* Theme switching.
* Visual polish and low-distraction presentation.

## Acceptance Criteria

* A user can open a supported local image and immediately browse sibling images in the same folder.
* The viewer never requires the user to manually define a playlist for normal folder browsing.
* The UI fades in windowed mode and fullscreen mode without hiding controls permanently.
* Unsupported or corrupt files show a user-safe message and write a structured log entry.
* Phase 2 and Phase 3 features remain documented as deferred, not forgotten.
* Core non-UI behavior is covered by automated tests.
