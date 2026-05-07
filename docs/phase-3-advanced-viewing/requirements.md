# Requirements Document: Phase 3 Advanced Viewing Modes

## Introduction

This document defines the requirements for Phase 3 of spitfire.img, building upon the Phase 1 MVP Viewer and Phase 2 Browse And Quality Of Life foundations. Phase 3 adds power-user presentation and comparison workflows designed for advanced viewing scenarios while maintaining the core presentation-first philosophy and low-distraction fading UI principle.

Phase 3 introduces slideshow mode with configurable timing and transitions, image comparison capabilities for side-by-side and overlay viewing, a command palette for quick access to all features, animated GIF playback with frame control, HEIC and AVIF format support with Windows codec dependencies, and custom layout modes for different viewing scenarios.

All Phase 3 features integrate with existing Phase 1 and Phase 2 services and maintain the folder-as-playlist model, service separation architecture, and Result-type error handling patterns established in earlier phases.

## Glossary

- **Viewer**: The spitfire.img application
- **Slideshow_Service**: Service responsible for automated sequential image presentation with timing and transitions
- **Comparison_Service**: Service responsible for managing multi-image comparison layouts and synchronization
- **Command_Palette_Service**: Service responsible for fuzzy search and command execution
- **Animation_Service**: Service responsible for GIF frame extraction and playback control
- **Codec_Detection_Service**: Service responsible for detecting Windows codec availability for HEIC and AVIF
- **Layout_Service**: Service responsible for managing custom viewing layouts
- **Playlist**: The ordered collection of image files from the current folder (from Phase 1)
- **Slideshow_Mode**: Automated presentation mode that advances through the Playlist at configurable intervals
- **Comparison_Mode**: Viewing mode that displays two or more images simultaneously
- **Side_By_Side_Layout**: Comparison layout where images are displayed adjacent to each other
- **Overlay_Layout**: Comparison layout where images are layered with adjustable opacity
- **Command_Palette**: Fuzzy-searchable overlay for quick access to all Viewer commands
- **Animation_Frame**: Individual frame extracted from an animated GIF image
- **Frame_Control**: UI for controlling GIF animation playback (play, pause, frame navigation)
- **HEIC_Format**: High Efficiency Image Container format requiring Windows codec support
- **AVIF_Format**: AV1 Image File Format requiring Windows codec support
- **Codec_Unavailable_Error**: Error returned when required Windows codec is not installed
- **Grid_Layout**: Custom layout displaying multiple images in a grid arrangement
- **Filmstrip_Layout**: Custom layout displaying images in a horizontal strip with focus area
- **Transition_Effect**: Visual effect applied when changing images in Slideshow_Mode
- **Sync_Zoom**: Comparison mode feature that synchronizes zoom and pan across displayed images
- **Chrome**: The top menu bar and bottom info bar UI elements (from Phase 1)
- **User_Activity**: Mouse movement, keyboard input, menu interaction, or image change event (from Phase 1)

## Requirements

### Requirement 1: Slideshow Mode Activation

**User Story:** As a user, I want to start and stop slideshow mode, so that I can view images automatically without manual navigation.

#### Acceptance Criteria

1. WHEN the user presses F5, THE Slideshow_Service SHALL start Slideshow_Mode
2. WHEN Slideshow_Mode starts, THE Slideshow_Service SHALL begin advancing through the Playlist at the configured interval
3. WHEN the user presses F5 during Slideshow_Mode, THE Slideshow_Service SHALL stop Slideshow_Mode
4. WHEN the user presses Escape during Slideshow_Mode, THE Slideshow_Service SHALL stop Slideshow_Mode
5. WHEN the user presses Space during Slideshow_Mode, THE Slideshow_Service SHALL pause Slideshow_Mode
6. WHEN Slideshow_Mode is paused and the user presses Space, THE Slideshow_Service SHALL resume Slideshow_Mode
7. WHEN Slideshow_Mode reaches the last image in the Playlist, THE Slideshow_Service SHALL loop back to the first image
8. WHEN the user manually navigates during Slideshow_Mode, THE Slideshow_Service SHALL reset the advance timer
9. THE Slideshow_Service SHALL use the same Playlist model as Phase 1 navigation
10. WHEN Slideshow_Mode is active, THE Chrome SHALL display a slideshow indicator

### Requirement 2: Slideshow Timing Configuration

**User Story:** As a user, I want to configure slideshow timing, so that I can control how long each image is displayed.

#### Acceptance Criteria

1. THE Slideshow_Service SHALL support configurable intervals from 1 second to 60 seconds
2. WHEN the user opens slideshow settings, THE Viewer SHALL display timing options: 1s, 2s, 3s, 5s, 10s, 15s, 30s, 60s
3. WHEN the user changes the slideshow interval, THE Settings_Service SHALL save the preference
4. WHEN Slideshow_Mode starts, THE Slideshow_Service SHALL use the saved interval preference
5. IF no interval preference is saved, THEN THE Slideshow_Service SHALL use 3 seconds as the default
6. WHEN the user presses Plus during Slideshow_Mode, THE Slideshow_Service SHALL increase the interval by one step
7. WHEN the user presses Minus during Slideshow_Mode, THE Slideshow_Service SHALL decrease the interval by one step
8. WHEN the interval changes during Slideshow_Mode, THE Chrome SHALL briefly display the new interval value

### Requirement 3: Slideshow Transition Effects

**User Story:** As a user, I want smooth transitions between images in slideshow mode, so that the presentation feels polished and professional.

#### Acceptance Criteria

1. THE Slideshow_Service SHALL support the following Transition_Effect options: None, Fade, Slide, Zoom
2. WHEN the user selects None transition, THE Slideshow_Service SHALL change images instantly
3. WHEN the user selects Fade transition, THE Slideshow_Service SHALL fade out the current image and fade in the next image over 300 milliseconds
4. WHEN the user selects Slide transition, THE Slideshow_Service SHALL slide the next image in from the right over 400 milliseconds
5. WHEN the user selects Zoom transition, THE Slideshow_Service SHALL zoom out the current image while fading in the next image over 350 milliseconds
6. WHEN the user changes the transition effect, THE Settings_Service SHALL save the preference
7. IF no transition preference is saved, THEN THE Slideshow_Service SHALL use Fade as the default
8. WHEN a Transition_Effect is playing, THE Slideshow_Service SHALL NOT allow manual navigation until the transition completes
9. THE Slideshow_Service SHALL render transitions at 60 frames per second for smooth animation

### Requirement 4: Comparison Mode Activation

**User Story:** As a user, I want to compare two or more images side-by-side, so that I can evaluate differences and similarities.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+D, THE Comparison_Service SHALL enter Comparison_Mode with Side_By_Side_Layout
2. WHEN Comparison_Mode is activated, THE Comparison_Service SHALL display the current image and the next image in the Playlist
3. WHEN the user presses Escape in Comparison_Mode, THE Comparison_Service SHALL exit Comparison_Mode and return to single-image view
4. WHEN the user presses Left Arrow in Comparison_Mode, THE Comparison_Service SHALL shift both images backward in the Playlist
5. WHEN the user presses Right Arrow in Comparison_Mode, THE Comparison_Service SHALL shift both images forward in the Playlist
6. WHEN the user presses Ctrl+Shift+D, THE Comparison_Service SHALL enter Comparison_Mode with Overlay_Layout
7. THE Comparison_Service SHALL use the same Playlist model as Phase 1 navigation
8. WHEN Comparison_Mode is active, THE Chrome SHALL display indices for all visible images
9. WHEN the Playlist contains only one image, THE Comparison_Service SHALL display an error message "Comparison mode requires at least two images"

### Requirement 5: Side-By-Side Comparison Layout

**User Story:** As a user, I want to view images side-by-side, so that I can compare them directly without switching views.

#### Acceptance Criteria

1. WHEN Side_By_Side_Layout is active, THE Comparison_Service SHALL divide the Image_Canvas into equal vertical sections
2. WHEN two images are compared, THE Comparison_Service SHALL display them in a 50/50 split
3. WHEN the user presses Ctrl+2 in Comparison_Mode, THE Comparison_Service SHALL display two images
4. WHEN the user presses Ctrl+3 in Comparison_Mode, THE Comparison_Service SHALL display three images
5. WHEN the user presses Ctrl+4 in Comparison_Mode, THE Comparison_Service SHALL display four images
6. WHEN Side_By_Side_Layout displays multiple images, THE Comparison_Service SHALL fit each image within its section while preserving aspect ratio
7. WHEN the user clicks an image in Side_By_Side_Layout, THE Comparison_Service SHALL highlight that image as the active selection
8. WHEN the user presses Tab in Side_By_Side_Layout, THE Comparison_Service SHALL cycle the active selection through visible images
9. WHEN the user presses Enter on an active selection, THE Comparison_Service SHALL exit Comparison_Mode and display that image in single-image view

### Requirement 6: Overlay Comparison Layout

**User Story:** As a user, I want to overlay images with adjustable opacity, so that I can detect subtle differences in alignment and content.

#### Acceptance Criteria

1. WHEN Overlay_Layout is active, THE Comparison_Service SHALL layer images on top of each other
2. WHEN Overlay_Layout displays two images, THE Comparison_Service SHALL set the top image to 50 percent opacity by default
3. WHEN the user drags the opacity slider in Overlay_Layout, THE Comparison_Service SHALL adjust the top image opacity from 0 to 100 percent
4. WHEN the user presses Up Arrow in Overlay_Layout, THE Comparison_Service SHALL increase top image opacity by 10 percent
5. WHEN the user presses Down Arrow in Overlay_Layout, THE Comparison_Service SHALL decrease top image opacity by 10 percent
6. WHEN the user presses S in Overlay_Layout, THE Comparison_Service SHALL swap the layer order of the two images
7. WHEN images have different dimensions in Overlay_Layout, THE Comparison_Service SHALL align them by center point
8. WHEN the user presses Left Arrow in Overlay_Layout, THE Comparison_Service SHALL replace both images with the previous two images in the Playlist
9. WHEN the user presses Right Arrow in Overlay_Layout, THE Comparison_Service SHALL replace both images with the next two images in the Playlist

### Requirement 7: Synchronized Zoom and Pan in Comparison Mode

**User Story:** As a user, I want zoom and pan to synchronize across compared images, so that I can examine the same region in multiple images.

#### Acceptance Criteria

1. WHEN the user zooms in Comparison_Mode, THE Comparison_Service SHALL apply the same zoom level to all visible images
2. WHEN the user pans in Comparison_Mode, THE Comparison_Service SHALL apply the same pan offset to all visible images
3. WHEN the user presses Ctrl+L in Comparison_Mode, THE Comparison_Service SHALL toggle Sync_Zoom on or off
4. WHEN Sync_Zoom is disabled, THE Comparison_Service SHALL allow independent zoom and pan for each image
5. WHEN Sync_Zoom is enabled, THE Chrome SHALL display a sync indicator
6. WHEN images have different dimensions and Sync_Zoom is enabled, THE Comparison_Service SHALL synchronize relative positions rather than absolute pixel coordinates
7. WHEN the user resets zoom in Comparison_Mode with Sync_Zoom enabled, THE Comparison_Service SHALL reset all images to fit mode

### Requirement 8: Command Palette Activation

**User Story:** As a user, I want quick access to all Viewer commands, so that I can execute actions without navigating menus.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+Shift+P, THE Command_Palette_Service SHALL display the Command_Palette
2. WHEN the Command_Palette is displayed, THE Command_Palette SHALL show a search input and command list
3. WHEN the user presses Escape in the Command_Palette, THE Command_Palette_Service SHALL close the Command_Palette
4. WHEN the Command_Palette is open, THE Chrome SHALL remain visible and the UI fade timer SHALL pause
5. WHEN the user clicks outside the Command_Palette, THE Command_Palette_Service SHALL close the Command_Palette
6. THE Command_Palette SHALL display commands from all phases: Phase 1, Phase 2, and Phase 3
7. WHEN the Command_Palette is displayed, THE Command_Palette SHALL show keyboard shortcuts next to each command
8. WHEN the user presses Enter on a selected command, THE Command_Palette_Service SHALL execute that command and close the Command_Palette

### Requirement 9: Command Palette Fuzzy Search

**User Story:** As a user, I want to search commands by typing partial matches, so that I can find commands quickly without exact names.

#### Acceptance Criteria

1. WHEN the user types in the Command_Palette search input, THE Command_Palette_Service SHALL filter commands using fuzzy matching
2. WHEN the Command_Palette_Service performs fuzzy matching, THE Command_Palette_Service SHALL match characters in order but not necessarily consecutively
3. WHEN the user types "rot", THE Command_Palette SHALL show "Rotate Left", "Rotate Right", and other rotation commands
4. WHEN the user types "fs", THE Command_Palette SHALL show "Toggle Fullscreen" and "Filmstrip Layout"
5. WHEN the Command_Palette_Service filters commands, THE Command_Palette_Service SHALL rank results by match quality
6. WHEN the Command_Palette displays filtered results, THE Command_Palette SHALL highlight matched characters
7. WHEN no commands match the search text, THE Command_Palette SHALL display "No matching commands found"
8. WHEN the user clears the search text, THE Command_Palette SHALL display all available commands
9. WHEN the user presses Up or Down arrows in the Command_Palette, THE Command_Palette_Service SHALL navigate through filtered results
10. THE Command_Palette_Service SHALL update filtered results within 50 milliseconds of user input

### Requirement 10: Animated GIF Playback

**User Story:** As a user, I want to view animated GIFs with playback control, so that I can see animations frame-by-frame or in motion.

#### Acceptance Criteria

1. WHEN the user opens a GIF file with multiple frames, THE Animation_Service SHALL detect that the file is animated
2. WHEN an animated GIF is displayed, THE Animation_Service SHALL begin playing the animation automatically
3. WHEN an animated GIF is playing, THE Animation_Service SHALL respect the frame delay timing embedded in the GIF
4. WHEN the user presses P while viewing an animated GIF, THE Animation_Service SHALL toggle between play and pause states
5. WHEN an animated GIF is paused, THE Animation_Service SHALL display the current frame statically
6. WHEN the user presses Period while an animated GIF is paused, THE Animation_Service SHALL advance to the next frame
7. WHEN the user presses Comma while an animated GIF is paused, THE Animation_Service SHALL move to the previous frame
8. WHEN an animated GIF reaches the last frame, THE Animation_Service SHALL loop back to the first frame
9. WHEN an animated GIF is displayed, THE Chrome SHALL show the current frame number and total frame count
10. THE Animation_Service SHALL extract and cache all Animation_Frame objects when the GIF is loaded

### Requirement 11: GIF Frame Control UI

**User Story:** As a user, I want visual controls for GIF animation, so that I can control playback without memorizing keyboard shortcuts.

#### Acceptance Criteria

1. WHEN an animated GIF is displayed, THE Viewer SHALL show Frame_Control UI in the Chrome
2. WHEN the Frame_Control UI is displayed, THE Frame_Control SHALL include play/pause button, previous frame button, next frame button, and frame slider
3. WHEN the user clicks the play/pause button, THE Animation_Service SHALL toggle the animation play state
4. WHEN the user clicks the previous frame button, THE Animation_Service SHALL move to the previous frame and pause
5. WHEN the user clicks the next frame button, THE Animation_Service SHALL move to the next frame and pause
6. WHEN the user drags the frame slider, THE Animation_Service SHALL jump to the corresponding frame and pause
7. WHEN the animation play state changes, THE Frame_Control SHALL update the play/pause button icon
8. WHEN the Frame_Control UI is visible, THE Frame_Control SHALL fade according to the same Chrome fade rules
9. WHEN the user navigates to a non-animated image, THE Viewer SHALL hide the Frame_Control UI

### Requirement 12: HEIC Format Support with Codec Detection

**User Story:** As a user, I want to view HEIC images when the Windows codec is installed, so that I can view iPhone photos without conversion.

#### Acceptance Criteria

1. WHEN the Viewer starts, THE Codec_Detection_Service SHALL check if the HEIC Windows codec is installed
2. WHEN the HEIC codec is installed, THE Image_Decode_Service SHALL add ".heic" to the list of Supported_Format extensions
3. WHEN the HEIC codec is not installed and the user opens a HEIC file, THE Codec_Detection_Service SHALL return a Codec_Unavailable_Error
4. WHEN a Codec_Unavailable_Error occurs for HEIC, THE Viewer SHALL display the message "HEIC format requires the HEIF Image Extensions from the Microsoft Store. Would you like to open the Store page?"
5. WHEN the user confirms the Store page prompt, THE Viewer SHALL open the Microsoft Store page for HEIF Image Extensions
6. WHEN the HEIC codec is installed and the user opens a HEIC file, THE Image_Decode_Service SHALL decode the image using the Windows codec
7. WHEN a Codec_Unavailable_Error occurs, THE Error_Log_Service SHALL write a log entry with category "Codec_Unavailable_Error"
8. THE Codec_Detection_Service SHALL check codec availability without requiring elevated permissions
9. WHEN the Folder_Playlist_Service scans a folder and HEIC codec is installed, THE Folder_Playlist_Service SHALL include HEIC files in the Playlist
10. WHEN the Folder_Playlist_Service scans a folder and HEIC codec is not installed, THE Folder_Playlist_Service SHALL exclude HEIC files from the Playlist

### Requirement 13: AVIF Format Support with Codec Detection

**User Story:** As a user, I want to view AVIF images when the Windows codec is installed, so that I can view modern high-efficiency images.

#### Acceptance Criteria

1. WHEN the Viewer starts, THE Codec_Detection_Service SHALL check if the AVIF Windows codec is installed
2. WHEN the AVIF codec is installed, THE Image_Decode_Service SHALL add ".avif" to the list of Supported_Format extensions
3. WHEN the AVIF codec is not installed and the user opens an AVIF file, THE Codec_Detection_Service SHALL return a Codec_Unavailable_Error
4. WHEN a Codec_Unavailable_Error occurs for AVIF, THE Viewer SHALL display the message "AVIF format requires the AV1 Video Extension from the Microsoft Store. Would you like to open the Store page?"
5. WHEN the user confirms the Store page prompt, THE Viewer SHALL open the Microsoft Store page for AV1 Video Extension
6. WHEN the AVIF codec is installed and the user opens an AVIF file, THE Image_Decode_Service SHALL decode the image using the Windows codec
7. THE Codec_Detection_Service SHALL check codec availability without requiring elevated permissions
8. WHEN the Folder_Playlist_Service scans a folder and AVIF codec is installed, THE Folder_Playlist_Service SHALL include AVIF files in the Playlist
9. WHEN the Folder_Playlist_Service scans a folder and AVIF codec is not installed, THE Folder_Playlist_Service SHALL exclude AVIF files from the Playlist
10. WHEN both HEIC and AVIF codecs are unavailable, THE Codec_Detection_Service SHALL check for each codec independently

### Requirement 14: Grid Layout Mode

**User Story:** As a user, I want to view multiple images in a grid, so that I can browse and compare many images simultaneously.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+G, THE Layout_Service SHALL enter Grid_Layout mode
2. WHEN Grid_Layout is active, THE Layout_Service SHALL display images in a grid with configurable rows and columns
3. WHEN Grid_Layout is first activated, THE Layout_Service SHALL use a 3×3 grid by default
4. WHEN the user presses Ctrl+Plus in Grid_Layout, THE Layout_Service SHALL increase the grid density
5. WHEN the user presses Ctrl+Minus in Grid_Layout, THE Layout_Service SHALL decrease the grid density
6. WHEN Grid_Layout displays images, THE Layout_Service SHALL fit each image within its grid cell while preserving aspect ratio
7. WHEN the user clicks an image in Grid_Layout, THE Layout_Service SHALL highlight that image as the active selection
8. WHEN the user presses Enter on an active selection in Grid_Layout, THE Layout_Service SHALL exit Grid_Layout and display that image in single-image view
9. WHEN the user scrolls in Grid_Layout, THE Layout_Service SHALL load additional images from the Playlist
10. THE Layout_Service SHALL use the same Playlist model as Phase 1 navigation

### Requirement 15: Filmstrip Layout Mode

**User Story:** As a user, I want to view images in a filmstrip with a focus area, so that I can see context while examining one image closely.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+F in Filmstrip_Layout context, THE Layout_Service SHALL enter Filmstrip_Layout mode
2. WHEN Filmstrip_Layout is active, THE Layout_Service SHALL display a large focus area showing the current image and a horizontal strip of thumbnails below
3. WHEN the user clicks a thumbnail in Filmstrip_Layout, THE Layout_Service SHALL display that image in the focus area
4. WHEN the user presses Left or Right arrows in Filmstrip_Layout, THE Layout_Service SHALL navigate to the previous or next image and update the focus area
5. WHEN the focus image changes in Filmstrip_Layout, THE Layout_Service SHALL scroll the thumbnail strip to keep the current image centered
6. WHEN Filmstrip_Layout is active, THE Layout_Service SHALL display 7 to 11 thumbnails in the strip depending on window width
7. WHEN the user presses Escape in Filmstrip_Layout, THE Layout_Service SHALL exit Filmstrip_Layout and return to single-image view
8. THE Filmstrip_Layout SHALL reuse thumbnails generated by the Thumbnail_Service from Phase 2
9. WHEN the user zooms in Filmstrip_Layout, THE Layout_Service SHALL apply zoom only to the focus area, not the thumbnail strip
10. WHEN Filmstrip_Layout is active, THE Chrome SHALL display the current image index and total count

### Requirement 16: Layout Persistence

**User Story:** As a user, I want the Viewer to remember my preferred layout, so that it opens in my preferred viewing mode.

#### Acceptance Criteria

1. WHEN the user changes to a custom layout, THE Settings_Service SHALL save the layout preference
2. WHEN the Viewer starts, THE Layout_Service SHALL restore the saved layout preference
3. IF no layout preference is saved, THEN THE Layout_Service SHALL use single-image view as the default
4. WHEN the user presses Ctrl+1, THE Layout_Service SHALL switch to single-image view and save the preference
5. THE Settings_Service SHALL store layout preferences in the application's local data folder

### Requirement 17: Slideshow Mode in Comparison and Custom Layouts

**User Story:** As a user, I want slideshow mode to work in comparison and custom layouts, so that I can automate advanced viewing workflows.

#### Acceptance Criteria

1. WHEN the user starts Slideshow_Mode in Comparison_Mode, THE Slideshow_Service SHALL advance both compared images through the Playlist
2. WHEN the user starts Slideshow_Mode in Grid_Layout, THE Slideshow_Service SHALL advance all visible grid images through the Playlist
3. WHEN the user starts Slideshow_Mode in Filmstrip_Layout, THE Slideshow_Service SHALL advance the focus image through the Playlist
4. WHEN Slideshow_Mode is active in a custom layout, THE Slideshow_Service SHALL use the same timing and transition settings as single-image view
5. WHEN the user exits a custom layout during Slideshow_Mode, THE Slideshow_Service SHALL continue Slideshow_Mode in the new layout

### Requirement 18: Command Palette Command Categories

**User Story:** As a user, I want commands organized by category in the command palette, so that I can discover related commands easily.

#### Acceptance Criteria

1. WHEN the Command_Palette displays all commands, THE Command_Palette SHALL group commands into categories: File, View, Image, Navigation, Layout, Slideshow, Comparison, Animation
2. WHEN the user types a category name, THE Command_Palette_Service SHALL show all commands in that category
3. WHEN the Command_Palette displays grouped commands, THE Command_Palette SHALL show category headers
4. WHEN the user navigates with arrow keys, THE Command_Palette_Service SHALL skip category headers and select only executable commands
5. THE Command_Palette SHALL display recently used commands at the top of the list

### Requirement 19: Performance Requirements for Advanced Modes

**User Story:** As a user, I want advanced viewing modes to remain responsive, so that the Viewer feels fast even with complex layouts.

#### Acceptance Criteria

1. WHEN Grid_Layout displays 9 or fewer images, THE Layout_Service SHALL render the grid within 500 milliseconds
2. WHEN Comparison_Mode displays 2 images, THE Comparison_Service SHALL render both images within 600 milliseconds
3. WHEN the user changes slideshow timing during Slideshow_Mode, THE Slideshow_Service SHALL apply the new timing within 100 milliseconds
4. WHEN the Animation_Service plays an animated GIF, THE Animation_Service SHALL maintain the frame rate specified in the GIF metadata
5. WHEN the Command_Palette_Service performs fuzzy search, THE Command_Palette_Service SHALL return results within 50 milliseconds for up to 200 commands
6. THE Layout_Service SHALL render custom layouts on a background thread to prevent UI blocking
7. WHEN Sync_Zoom is enabled in Comparison_Mode, THE Comparison_Service SHALL synchronize zoom and pan within 16 milliseconds (60 FPS)

### Requirement 20: Codec Detection Error Handling

**User Story:** As a developer, I want codec detection errors to be logged and handled gracefully, so that users understand format limitations.

#### Acceptance Criteria

1. WHEN a Codec_Unavailable_Error occurs, THE Error_Log_Service SHALL write a log entry with timestamp, format name, and codec name
2. WHEN the Codec_Detection_Service checks for codecs, THE Codec_Detection_Service SHALL handle registry access errors gracefully
3. IF codec detection fails due to permissions, THEN THE Codec_Detection_Service SHALL assume codecs are unavailable and log a warning
4. WHEN the user declines to install a codec, THE Viewer SHALL NOT display the prompt again for that format during the current session
5. THE Codec_Detection_Service SHALL cache codec availability results to avoid repeated checks
6. WHEN codec availability changes (user installs codec), THE Viewer SHALL detect the change on next restart
7. WHEN a Codec_Unavailable_Error occurs, THE Viewer SHALL NOT display raw exception messages to users
