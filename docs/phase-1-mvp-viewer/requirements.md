# Requirements Document: Phase 1 MVP Viewer

## Introduction

This document defines the requirements for Phase 1 of spitfire.img, a modern Windows-native image viewer application. Phase 1 delivers a complete local image viewing experience with presentation-first UI design, automatic folder-as-playlist behavior, and low-distraction fading chrome. The application is built using WinUI 3 with Windows App SDK and C#/.NET.

Phase 1 focuses on core viewing capabilities while explicitly deferring advanced features (search, thumbnails, drag-and-drop, export, rotate/flip, slideshow, comparison mode, command palette) to Phase 2 and Phase 3.

## Glossary

- **Viewer**: The spitfire.img application
- **Image_Canvas**: The central display area where images are rendered
- **Playlist**: The ordered collection of supported image files from the opened image's parent folder
- **Chrome**: The top menu bar and bottom info bar UI elements
- **File_Open_Service**: Service responsible for file selection and validation
- **Image_Decode_Service**: Service responsible for decoding image files into displayable format
- **Folder_Playlist_Service**: Service responsible for scanning folders and building playlists
- **Navigation_Service**: Service responsible for playlist navigation logic
- **Settings_Service**: Service responsible for persisting and loading user preferences
- **Error_Log_Service**: Service responsible for writing structured error logs to disk
- **Supported_Format**: PNG, JPG, JPEG, BMP, or WEBP file format
- **Deferred_Format**: GIF, TIFF, HEIC, or AVIF file format (planned for future phases)
- **Fit_Mode**: Display mode where image is scaled to fit within window bounds while preserving aspect ratio
- **Actual_Size_Mode**: Display mode where image is displayed at 100% scale (one image pixel per screen pixel)
- **Fullscreen_Mode**: Display mode where the Viewer occupies the entire screen
- **UI_Fade_Timer**: Timer that triggers Chrome fade-out after inactivity period
- **User_Activity**: Mouse movement, keyboard input, menu interaction, or image change event

## Requirements

### Requirement 1: File Opening

**User Story:** As a user, I want to open local image files, so that I can view them in the Viewer.

#### Acceptance Criteria

1. WHEN the user selects a file with a Supported_Format extension, THE File_Open_Service SHALL validate the file path
2. WHEN the File_Open_Service validates a file path, THE File_Open_Service SHALL verify the file exists and is accessible
3. IF a file does not exist or is not accessible, THEN THE File_Open_Service SHALL return a FileAccessError
4. WHEN the user selects a file with a Deferred_Format extension, THE File_Open_Service SHALL return an UnsupportedFormatError
5. WHEN the user selects a file with an unrecognized extension, THE File_Open_Service SHALL return an UnsupportedFormatError

### Requirement 2: Image Decoding

**User Story:** As a user, I want the Viewer to decode supported image formats, so that I can see the image content.

#### Acceptance Criteria

1. WHEN a validated file path is provided, THE Image_Decode_Service SHALL attempt to decode the image data
2. WHEN decoding succeeds, THE Image_Decode_Service SHALL return the decoded image and metadata
3. IF decoding fails due to file corruption, THEN THE Image_Decode_Service SHALL return a DecodeError
4. IF decoding fails due to insufficient memory, THEN THE Image_Decode_Service SHALL return a DecodeError
5. WHEN metadata is extracted, THE Image_Decode_Service SHALL include pixel width, pixel height, and file size

### Requirement 3: Automatic Folder Playlist

**User Story:** As a user, I want the Viewer to automatically treat the opened image's parent folder as a playlist, so that I can browse sibling images without manual setup.

#### Acceptance Criteria

1. WHEN an image is successfully opened, THE Folder_Playlist_Service SHALL scan the parent folder for files with Supported_Format extensions
2. WHEN the Folder_Playlist_Service scans a folder, THE Folder_Playlist_Service SHALL sort files alphabetically by filename
3. WHEN the Folder_Playlist_Service builds a playlist, THE Folder_Playlist_Service SHALL set the opened image as the current selection
4. IF folder scanning fails due to access permissions, THEN THE Folder_Playlist_Service SHALL return a PlaylistScanError
5. WHEN a folder contains only the opened image, THE Playlist SHALL contain exactly one item
6. WHEN the Folder_Playlist_Service completes scanning, THE Playlist SHALL include the current image index and total image count

### Requirement 4: Keyboard Navigation

**User Story:** As a user, I want to navigate between images using keyboard shortcuts, so that I can browse quickly without using the mouse.

#### Acceptance Criteria

1. WHEN the user presses the Right Arrow key, THE Navigation_Service SHALL advance to the next image in the Playlist
2. WHEN the user presses the Left Arrow key, THE Navigation_Service SHALL move to the previous image in the Playlist
3. WHEN the user presses the Home key, THE Navigation_Service SHALL jump to the first image in the Playlist
4. WHEN the user presses the End key, THE Navigation_Service SHALL jump to the last image in the Playlist
5. WHEN the current image is the last in the Playlist and the user presses Right Arrow, THE Navigation_Service SHALL remain on the last image
6. WHEN the current image is the first in the Playlist and the user presses Left Arrow, THE Navigation_Service SHALL remain on the first image
7. IF navigation encounters an error loading the target image, THEN THE Navigation_Service SHALL return a NavigationError

### Requirement 5: Image Display Modes

**User Story:** As a user, I want multiple image display modes, so that I can view images at the most appropriate scale.

#### Acceptance Criteria

1. WHEN an image is first loaded, THE Image_Canvas SHALL display the image in Fit_Mode
2. WHEN the user presses Ctrl+0, THE Image_Canvas SHALL switch to Fit_Mode
3. WHEN the user presses Ctrl+1, THE Image_Canvas SHALL switch to Actual_Size_Mode
4. WHEN the user presses Ctrl and the Plus key, THE Image_Canvas SHALL increase zoom level by 25 percent
5. WHEN the user presses Ctrl and the Minus key, THE Image_Canvas SHALL decrease zoom level by 25 percent
6. WHEN zoom level exceeds 100 percent, THE Image_Canvas SHALL enable pan behavior
7. THE Image_Canvas SHALL center images horizontally and vertically within available space

### Requirement 6: Fullscreen Mode

**User Story:** As a user, I want to toggle fullscreen mode, so that I can view images without desktop distractions.

#### Acceptance Criteria

1. WHEN the user presses F11, THE Viewer SHALL toggle between windowed and Fullscreen_Mode
2. WHEN the user double-clicks the Image_Canvas, THE Viewer SHALL toggle between windowed and Fullscreen_Mode
3. WHEN the user presses Escape in Fullscreen_Mode, THE Viewer SHALL exit Fullscreen_Mode
4. WHILE in Fullscreen_Mode, THE Viewer SHALL hide the window title bar and borders
5. WHILE in Fullscreen_Mode, THE Chrome SHALL fade according to the same rules as windowed mode

### Requirement 7: Fading UI Chrome

**User Story:** As a user, I want the UI chrome to fade during viewing, so that I can focus on the image without permanent distractions.

#### Acceptance Criteria

1. WHEN User_Activity occurs, THE Chrome SHALL become fully visible
2. WHEN User_Activity occurs, THE UI_Fade_Timer SHALL reset to 2 seconds
3. WHEN the UI_Fade_Timer expires, THE Chrome SHALL fade out over 300 milliseconds
4. WHILE the Chrome is faded, THE Chrome SHALL remain interactive if the user moves the mouse to its location
5. WHEN the user opens a menu, THE UI_Fade_Timer SHALL pause until the menu closes
6. WHEN the image changes, THE Chrome SHALL become fully visible and the UI_Fade_Timer SHALL reset

### Requirement 8: Metadata Display

**User Story:** As a user, I want to see basic image metadata, so that I understand what I am viewing and where I am in the playlist.

#### Acceptance Criteria

1. WHEN an image is displayed, THE Chrome SHALL show the filename without path
2. WHEN an image is displayed, THE Chrome SHALL show the current playlist index and total count in the format "N / Total"
3. WHEN an image is displayed, THE Chrome SHALL show pixel dimensions in the format "Width × Height"
4. WHEN an image is displayed, THE Chrome SHALL show the parent folder name without full path
5. WHEN the Playlist contains only one image, THE Chrome SHALL show "1 / 1"

### Requirement 9: Settings Persistence

**User Story:** As a user, I want my preferences to persist between sessions, so that I don't have to reconfigure the Viewer each time.

#### Acceptance Criteria

1. WHEN the Viewer starts, THE Settings_Service SHALL load saved settings from local storage
2. WHEN the user changes the theme preference, THE Settings_Service SHALL save the new preference to local storage
3. WHEN the user changes the default view mode, THE Settings_Service SHALL save the new preference to local storage
4. IF loading settings fails, THEN THE Settings_Service SHALL return a SettingsPersistenceError and use default values
5. IF saving settings fails, THEN THE Settings_Service SHALL return a SettingsPersistenceError
6. THE Settings_Service SHALL store settings in the application's local data folder

### Requirement 10: Structured Error Logging

**User Story:** As a developer, I want structured error logs written to disk, so that I can diagnose issues reported by users.

#### Acceptance Criteria

1. WHEN a FileAccessError occurs, THE Error_Log_Service SHALL write a log entry with category "FileAccessError"
2. WHEN an UnsupportedFormatError occurs, THE Error_Log_Service SHALL write a log entry with category "UnsupportedFormatError"
3. WHEN a DecodeError occurs, THE Error_Log_Service SHALL write a log entry with category "DecodeError"
4. WHEN a PlaylistScanError occurs, THE Error_Log_Service SHALL write a log entry with category "PlaylistScanError"
5. WHEN a NavigationError occurs, THE Error_Log_Service SHALL write a log entry with category "NavigationError"
6. WHEN a SettingsPersistenceError occurs, THE Error_Log_Service SHALL write a log entry with category "SettingsPersistenceError"
7. WHEN an unexpected error occurs, THE Error_Log_Service SHALL write a log entry with category "UnexpectedViewerError"
8. WHEN the Error_Log_Service writes a log entry, THE Error_Log_Service SHALL include timestamp, severity, category, user-safe message, operation name, optional file path, exception type, and stack trace
9. THE Error_Log_Service SHALL write log files to the application's local data folder
10. THE Error_Log_Service SHALL create log files that survive application restarts

### Requirement 11: User-Facing Error Messages

**User Story:** As a user, I want clear error messages when something goes wrong, so that I understand what happened and what to do next.

#### Acceptance Criteria

1. WHEN a FileAccessError occurs, THE Viewer SHALL display the message "Unable to access this file. It may have been moved, deleted, or you may not have permission to open it."
2. WHEN an UnsupportedFormatError occurs, THE Viewer SHALL display the message "This file format is not supported in this version."
3. WHEN a DecodeError occurs, THE Viewer SHALL display the message "Unable to decode this image. The file may be corrupted or in an unsupported variant."
4. WHEN a PlaylistScanError occurs, THE Viewer SHALL display the message "Unable to scan the folder for additional images. You can still view this image."
5. WHEN a NavigationError occurs, THE Viewer SHALL display the message "Unable to load the requested image."
6. THE Viewer SHALL NOT display raw exception messages or stack traces to users

### Requirement 12: Supported Format Detection

**User Story:** As a developer, I want format detection to be centralized, so that adding new formats in future phases is straightforward.

#### Acceptance Criteria

1. THE Image_Decode_Service SHALL maintain a list of Supported_Format extensions
2. THE Image_Decode_Service SHALL maintain a list of Deferred_Format extensions
3. WHEN checking if a file is supported, THE Image_Decode_Service SHALL perform case-insensitive extension matching
4. THE Image_Decode_Service SHALL recognize ".png", ".jpg", ".jpeg", ".bmp", and ".webp" as Supported_Format extensions
5. THE Image_Decode_Service SHALL recognize ".gif", ".tiff", ".tif", ".heic", and ".avif" as Deferred_Format extensions

### Requirement 13: Zoom and Pan Behavior

**User Story:** As a user, I want to zoom and pan images, so that I can examine details closely.

#### Acceptance Criteria

1. WHEN zoom level is greater than 100 percent, THE Image_Canvas SHALL enable mouse drag panning
2. WHEN the user drags the mouse while panning is enabled, THE Image_Canvas SHALL move the image viewport
3. WHEN zoom level is less than or equal to 100 percent and the image fits within the window, THE Image_Canvas SHALL disable panning
4. WHEN the user zooms in, THE Image_Canvas SHALL maintain the center point of the viewport
5. WHEN the user zooms out below 100 percent, THE Image_Canvas SHALL switch to Fit_Mode

### Requirement 14: Window State Preservation

**User Story:** As a user, I want the Viewer to remember my window size and position, so that it appears where I left it.

#### Acceptance Criteria

1. WHEN the Viewer closes, THE Settings_Service SHALL save the window width, height, and position
2. WHEN the Viewer starts, THE Settings_Service SHALL restore the saved window width, height, and position
3. IF saved window position is off-screen, THEN THE Viewer SHALL open at the default centered position
4. IF no saved window state exists, THEN THE Viewer SHALL open with default dimensions of 1200 × 800 pixels

### Requirement 15: Theme Support

**User Story:** As a user, I want to choose between light and dark themes, so that the Viewer matches my preference and environment.

#### Acceptance Criteria

1. THE Viewer SHALL support a light theme and a dark theme
2. WHEN the Viewer starts, THE Viewer SHALL apply the theme saved in settings
3. IF no theme preference is saved, THEN THE Viewer SHALL use the system theme preference
4. WHEN the user changes the theme, THE Viewer SHALL apply the new theme immediately without restart
5. WHILE in dark theme, THE Image_Canvas background SHALL use a dark neutral color
6. WHILE in light theme, THE Image_Canvas background SHALL use a light neutral color

### Requirement 16: Playlist Boundary Behavior

**User Story:** As a user, I want clear feedback when I reach the beginning or end of the playlist, so that I know there are no more images in that direction.

#### Acceptance Criteria

1. WHEN the user attempts to navigate past the last image, THE Viewer SHALL remain on the last image
2. WHEN the user attempts to navigate before the first image, THE Viewer SHALL remain on the first image
3. WHEN the Playlist contains only one image, THE Navigation_Service SHALL disable previous and next navigation
4. WHEN navigation is disabled, THE Viewer SHALL NOT display error messages for boundary navigation attempts

### Requirement 17: Image Loading Performance

**User Story:** As a user, I want images to load quickly, so that navigation feels responsive.

#### Acceptance Criteria

1. WHEN the user navigates to a new image, THE Viewer SHALL display the new image within 500 milliseconds for files under 10 MB
2. WHEN an image is loading, THE Viewer SHALL display a loading indicator after 200 milliseconds
3. WHEN image decoding takes longer than 2 seconds, THE Viewer SHALL allow the user to cancel and return to the previous image
4. THE Image_Decode_Service SHALL decode images on a background thread to prevent UI blocking

### Requirement 18: Memory Management

**User Story:** As a user, I want the Viewer to handle large images without crashing, so that I can view high-resolution photos.

#### Acceptance Criteria

1. WHEN an image exceeds available memory during decode, THE Image_Decode_Service SHALL return a DecodeError
2. WHEN the user navigates away from an image, THE Viewer SHALL release the previous image from memory
3. THE Viewer SHALL NOT preload adjacent images in Phase 1 (deferred to Phase 2)
4. WHEN a DecodeError occurs due to memory, THE Viewer SHALL display the message "This image is too large to display."

### Requirement 19: Keyboard Shortcut Consistency

**User Story:** As a user, I want keyboard shortcuts to work consistently across windowed and fullscreen modes, so that I don't have to relearn controls.

#### Acceptance Criteria

1. WHILE in Fullscreen_Mode, THE Viewer SHALL respond to all navigation keyboard shortcuts
2. WHILE in Fullscreen_Mode, THE Viewer SHALL respond to all zoom keyboard shortcuts
3. WHILE in Fullscreen_Mode, THE Viewer SHALL respond to all display mode keyboard shortcuts
4. THE Viewer SHALL NOT change keyboard shortcut behavior based on window state

### Requirement 20: Initial Window Appearance

**User Story:** As a user, I want the Viewer to open quickly with a clean initial state, so that I can start viewing immediately.

#### Acceptance Criteria

1. WHEN the Viewer starts without a file argument, THE Viewer SHALL display an empty Image_Canvas with a centered "Open an image to begin" message
2. WHEN the Viewer starts with a valid file argument, THE Viewer SHALL open and display that image
3. WHEN the Viewer starts with an invalid file argument, THE Viewer SHALL display an error message and show the empty state
4. THE Viewer SHALL complete initial window display within 1 second on typical hardware
