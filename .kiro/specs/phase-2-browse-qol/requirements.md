# Requirements Document: Phase 2 Browse And Quality Of Life

## Introduction

This document defines the requirements for Phase 2 of spitfire.img, building upon the Phase 1 MVP Viewer foundation. Phase 2 adds quality-of-life features that make everyday folder browsing faster and more convenient without changing the Phase 1 viewer core. The focus is on search, thumbnails, recent files, drag-and-drop, export, rotate/flip, enhanced metadata, and additional format support.

Phase 2 maintains the architectural boundaries established in Phase 1: presentation-first UI with fading chrome, service separation, and the folder-as-playlist model. All new features integrate with existing services rather than replacing them.

## Glossary

- **Viewer**: The spitfire.img application
- **Search_Service**: Service responsible for filtering the current playlist by filename
- **Thumbnail_Service**: Service responsible for generating and caching thumbnail images
- **Recent_Files_Service**: Service responsible for tracking and persisting recently opened files
- **Export_Service**: Service responsible for saving copies of images to user-selected locations
- **Transform_Service**: Service responsible for rotating and flipping images
- **Metadata_Service**: Service responsible for extracting EXIF and extended image metadata
- **Image_Decode_Service**: Service responsible for decoding image files (from Phase 1)
- **Playlist**: The ordered collection of image files from the current folder (from Phase 1)
- **Filtered_Playlist**: A subset of the Playlist matching search criteria
- **Thumbnail_Strip**: UI component displaying small preview images of playlist items
- **Recent_Files_List**: Ordered collection of recently opened image file paths
- **EXIF_Data**: Exchangeable Image File Format metadata embedded in image files
- **Transform_Operation**: Rotate left, rotate right, flip horizontal, or flip vertical operation
- **Supported_Format**: PNG, JPG, JPEG, BMP, WEBP, GIF, or TIFF file format
- **Chrome**: The top menu bar and bottom info bar UI elements (from Phase 1)
- **User_Activity**: Mouse movement, keyboard input, menu interaction, or image change event (from Phase 1)

## Requirements

### Requirement 1: Filename Search

**User Story:** As a user, I want to search the current folder by filename, so that I can quickly find specific images without manual navigation.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+F, THE Viewer SHALL display a search overlay
2. WHEN the user types in the search overlay, THE Search_Service SHALL filter the Playlist to items whose filenames contain the search text
3. WHEN the Search_Service filters the Playlist, THE Search_Service SHALL perform case-insensitive matching
4. WHEN search results are displayed, THE Viewer SHALL show the count of matching items
5. WHEN the user selects a search result, THE Viewer SHALL navigate to that image and close the search overlay
6. WHEN the user presses Escape in the search overlay, THE Viewer SHALL close the overlay without changing the current image
7. WHEN the search overlay is open, THE Chrome SHALL remain visible and the UI fade timer SHALL pause
8. WHEN the Filtered_Playlist is active, THE Viewer SHALL restrict navigation to only matching images
9. WHEN the user clears the search text, THE Search_Service SHALL restore the full Playlist
10. THE Search_Service SHALL use the same Playlist model as Phase 1 navigation

### Requirement 2: Thumbnail Strip

**User Story:** As a user, I want to see thumbnail previews of images in the current folder, so that I can visually browse and jump to specific images.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+T, THE Viewer SHALL toggle the Thumbnail_Strip visibility
2. WHEN the Thumbnail_Strip is visible, THE Thumbnail_Service SHALL generate thumbnails for all Playlist items
3. WHEN the Thumbnail_Service generates a thumbnail, THE Thumbnail_Service SHALL scale the image to 120 pixels on the longest dimension while preserving aspect ratio
4. WHEN a thumbnail is generated, THE Thumbnail_Service SHALL cache it to avoid regeneration
5. WHEN the user clicks a thumbnail, THE Viewer SHALL navigate to that image
6. WHEN the current image changes, THE Thumbnail_Strip SHALL highlight the corresponding thumbnail
7. WHEN the Thumbnail_Strip is visible, THE Thumbnail_Strip SHALL scroll to keep the current thumbnail visible
8. WHEN the Playlist contains more than 50 images, THE Thumbnail_Service SHALL generate thumbnails on-demand as the user scrolls
9. THE Thumbnail_Strip SHALL use the same Playlist model as Phase 1 navigation
10. WHEN the Thumbnail_Strip is visible, THE Chrome fade behavior SHALL continue to function normally

### Requirement 3: Recent Files

**User Story:** As a user, I want to access recently opened images, so that I can quickly return to files I was viewing previously.

#### Acceptance Criteria

1. WHEN the user opens an image, THE Recent_Files_Service SHALL add the file path to the Recent_Files_List
2. WHEN the Recent_Files_Service adds a file path, THE Recent_Files_Service SHALL move existing duplicates to the top of the list
3. WHEN the Recent_Files_List exceeds 20 items, THE Recent_Files_Service SHALL remove the oldest entry
4. WHEN the user opens the File menu, THE Viewer SHALL display the Recent_Files_List
5. WHEN the user selects a recent file, THE Viewer SHALL open that file using the standard file opening flow
6. IF a recent file no longer exists, THEN THE Viewer SHALL display a FileAccessError and remove it from the Recent_Files_List
7. WHEN the Recent_Files_Service persists the list, THE Recent_Files_Service SHALL store it in the application's local data folder
8. WHEN a recent file is opened, THE Viewer SHALL still build the folder-as-playlist from the file's parent folder
9. THE Recent_Files_Service SHALL NOT replace the folder-as-playlist behavior
10. WHEN the Viewer starts, THE Recent_Files_Service SHALL load the Recent_Files_List from persistent storage

### Requirement 4: Drag-and-Drop Image Opening

**User Story:** As a user, I want to drag image files onto the Viewer window, so that I can open images without using the file picker.

#### Acceptance Criteria

1. WHEN the user drags a file over the Viewer window, THE Viewer SHALL display a visual drop indicator
2. WHEN the user drops a file with a Supported_Format extension, THE Viewer SHALL open that file using the standard file opening flow
3. WHEN the user drops a file with an unsupported extension, THE Viewer SHALL display an UnsupportedFormatError
4. WHEN the user drops multiple files, THE Viewer SHALL open the first file with a Supported_Format extension
5. WHEN the user drags a file outside the Viewer window before dropping, THE Viewer SHALL hide the drop indicator
6. THE Viewer SHALL validate dropped files using the File_Open_Service from Phase 1
7. WHEN a dropped file is opened, THE Viewer SHALL build the folder-as-playlist from the file's parent folder

### Requirement 5: Export and Save Copy

**User Story:** As a user, I want to save a copy of the current image to a different location, so that I can organize or share images without leaving the Viewer.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+Shift+S, THE Viewer SHALL open a save file dialog
2. WHEN the user selects a destination path, THE Export_Service SHALL copy the current image file to that location
3. WHEN the Export_Service copies a file, THE Export_Service SHALL preserve the original file format
4. IF the destination file already exists, THEN THE Viewer SHALL prompt the user to confirm overwrite
5. IF the copy operation fails, THEN THE Export_Service SHALL return an ExportError
6. WHEN an ExportError occurs, THE Viewer SHALL display the message "Unable to save a copy of this image to the selected location."
7. WHEN an ExportError occurs, THE Error_Log_Service SHALL write a log entry with category "ExportError"
8. THE Export_Service SHALL NOT modify the original image file
9. WHEN the export completes successfully, THE Viewer SHALL display a brief confirmation message

### Requirement 6: Rotate and Flip Commands

**User Story:** As a user, I want to rotate and flip images, so that I can correct orientation without using external tools.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+L, THE Transform_Service SHALL rotate the current image 90 degrees counterclockwise
2. WHEN the user presses Ctrl+R, THE Transform_Service SHALL rotate the current image 90 degrees clockwise
3. WHEN the user presses Ctrl+H, THE Transform_Service SHALL flip the current image horizontally
4. WHEN the user presses Ctrl+V, THE Transform_Service SHALL flip the current image vertically
5. WHEN a Transform_Operation is applied, THE Viewer SHALL update the displayed image immediately
6. WHEN a Transform_Operation is applied, THE Transform_Service SHALL NOT modify the original file
7. WHEN the user navigates to a different image, THE Viewer SHALL discard unsaved transforms
8. WHEN the user presses Ctrl+S after applying transforms, THE Transform_Service SHALL save the transformed image to the original file
9. IF saving transforms fails, THEN THE Transform_Service SHALL return a TransformSaveError
10. WHEN a TransformSaveError occurs, THE Viewer SHALL display the message "Unable to save changes to this image."
11. WHEN transforms are applied, THE Metadata_Service SHALL update displayed dimensions to reflect the new orientation

### Requirement 7: Enhanced Image Metadata

**User Story:** As a user, I want to see detailed image metadata including EXIF data, so that I can understand camera settings and image properties.

#### Acceptance Criteria

1. WHEN an image is displayed, THE Metadata_Service SHALL extract EXIF_Data if present
2. WHEN EXIF_Data is available, THE Metadata_Service SHALL extract camera make and model
3. WHEN EXIF_Data is available, THE Metadata_Service SHALL extract capture date and time
4. WHEN EXIF_Data is available, THE Metadata_Service SHALL extract exposure settings (ISO, aperture, shutter speed)
5. WHEN EXIF_Data is available, THE Metadata_Service SHALL extract focal length
6. WHEN the user presses Ctrl+I, THE Viewer SHALL display a metadata panel with all available information
7. WHEN the metadata panel is open, THE Chrome SHALL remain visible and the UI fade timer SHALL pause
8. WHEN the user presses Escape or Ctrl+I again, THE Viewer SHALL close the metadata panel
9. WHEN EXIF_Data is not available, THE metadata panel SHALL display only basic file information
10. THE Metadata_Service SHALL handle missing or malformed EXIF_Data gracefully without errors

### Requirement 8: GIF Format Support

**User Story:** As a user, I want to view GIF images, so that I can see all images in folders containing mixed formats.

#### Acceptance Criteria

1. WHEN the user opens a file with a .gif extension, THE Image_Decode_Service SHALL decode the first frame
2. WHEN a GIF image is displayed, THE Viewer SHALL show it as a static image in Phase 2
3. WHEN the Image_Decode_Service decodes a GIF, THE Image_Decode_Service SHALL extract dimensions and file size
4. THE Image_Decode_Service SHALL add ".gif" to the list of Supported_Format extensions
5. WHEN the Folder_Playlist_Service scans a folder, THE Folder_Playlist_Service SHALL include GIF files in the Playlist
6. IF GIF decoding fails, THEN THE Image_Decode_Service SHALL return a DecodeError
7. THE Image_Decode_Service SHALL route GIF decoding through the same service interface as Phase 1 formats

### Requirement 9: TIFF Format Support

**User Story:** As a user, I want to view TIFF images, so that I can work with professional photography and scanned documents.

#### Acceptance Criteria

1. WHEN the user opens a file with a .tiff or .tif extension, THE Image_Decode_Service SHALL decode the image
2. WHEN a TIFF image is displayed, THE Viewer SHALL render it using the same display modes as other formats
3. WHEN the Image_Decode_Service decodes a TIFF, THE Image_Decode_Service SHALL extract dimensions and file size
4. THE Image_Decode_Service SHALL add ".tiff" and ".tif" to the list of Supported_Format extensions
5. WHEN the Folder_Playlist_Service scans a folder, THE Folder_Playlist_Service SHALL include TIFF files in the Playlist
6. IF TIFF decoding fails, THEN THE Image_Decode_Service SHALL return a DecodeError
7. THE Image_Decode_Service SHALL route TIFF decoding through the same service interface as Phase 1 formats
8. WHEN a multi-page TIFF is opened, THE Image_Decode_Service SHALL decode only the first page in Phase 2

### Requirement 10: Search Keyboard Navigation

**User Story:** As a user, I want to navigate search results with the keyboard, so that I can quickly browse matching images without using the mouse.

#### Acceptance Criteria

1. WHEN the search overlay is open and results are displayed, THE Viewer SHALL allow Up and Down arrow keys to select results
2. WHEN the user presses Enter in the search overlay, THE Viewer SHALL navigate to the selected result
3. WHEN the user presses Down arrow on the last search result, THE Viewer SHALL wrap to the first result
4. WHEN the user presses Up arrow on the first search result, THE Viewer SHALL wrap to the last result
5. WHEN no search results match, THE Viewer SHALL display "No matching images found"

### Requirement 11: Thumbnail Strip Positioning

**User Story:** As a user, I want to control where the thumbnail strip appears, so that it doesn't obscure important parts of images.

#### Acceptance Criteria

1. THE Thumbnail_Strip SHALL appear at the bottom of the window by default
2. WHEN the user right-clicks the Thumbnail_Strip, THE Viewer SHALL display a context menu with positioning options
3. WHEN the user selects "Top" from the context menu, THE Thumbnail_Strip SHALL move to the top of the window
4. WHEN the user selects "Bottom" from the context menu, THE Thumbnail_Strip SHALL move to the bottom of the window
5. WHEN the Thumbnail_Strip position changes, THE Settings_Service SHALL save the preference
6. WHEN the Viewer starts, THE Thumbnail_Strip SHALL appear in the saved position

### Requirement 12: Recent Files Persistence

**User Story:** As a developer, I want recent files to persist across application restarts, so that users can access their history after closing the Viewer.

#### Acceptance Criteria

1. WHEN the Viewer closes, THE Recent_Files_Service SHALL save the Recent_Files_List to persistent storage
2. WHEN the Recent_Files_Service saves the list, THE Recent_Files_Service SHALL store it in JSON format
3. WHEN the Recent_Files_Service loads the list, THE Recent_Files_Service SHALL validate that each file path is well-formed
4. IF loading the Recent_Files_List fails, THEN THE Recent_Files_Service SHALL return an empty list and log a SettingsPersistenceError
5. THE Recent_Files_Service SHALL store the list in the application's local data folder

### Requirement 13: Thumbnail Cache Management

**User Story:** As a user, I want thumbnail generation to be fast, so that browsing large folders remains responsive.

#### Acceptance Criteria

1. WHEN a thumbnail is generated, THE Thumbnail_Service SHALL cache it in memory
2. WHEN the Playlist changes, THE Thumbnail_Service SHALL clear thumbnails for files no longer in the Playlist
3. WHEN memory usage exceeds 100 MB for thumbnails, THE Thumbnail_Service SHALL evict the least recently used thumbnails
4. THE Thumbnail_Service SHALL generate thumbnails on a background thread to avoid UI blocking
5. WHEN thumbnail generation fails, THE Thumbnail_Strip SHALL display a placeholder icon

### Requirement 14: Export Format Options

**User Story:** As a user, I want to export images in different formats, so that I can convert images for specific use cases.

#### Acceptance Criteria

1. WHEN the save file dialog is displayed, THE Viewer SHALL offer PNG, JPEG, BMP, and WEBP as export format options
2. WHEN the user selects a different format than the original, THE Export_Service SHALL convert the image during export
3. WHEN exporting to JPEG, THE Export_Service SHALL use 90 percent quality by default
4. WHEN format conversion fails, THE Export_Service SHALL return an ExportError
5. THE Export_Service SHALL preserve EXIF_Data when exporting to formats that support it

### Requirement 15: Transform Preview

**User Story:** As a user, I want to see transform changes immediately, so that I can verify the result before saving.

#### Acceptance Criteria

1. WHEN a Transform_Operation is applied, THE Viewer SHALL update the display within 200 milliseconds
2. WHEN multiple transforms are applied, THE Transform_Service SHALL combine them into a single operation
3. WHEN the user presses Ctrl+Z after applying transforms, THE Transform_Service SHALL undo the last transform
4. WHEN the user presses Ctrl+Shift+Z after undoing, THE Transform_Service SHALL redo the last undone transform
5. WHEN transforms are pending, THE Chrome SHALL display an indicator showing unsaved changes

### Requirement 16: Search Performance

**User Story:** As a user, I want search to be fast, so that I can filter large folders without delay.

#### Acceptance Criteria

1. WHEN the user types in the search overlay, THE Search_Service SHALL update results within 100 milliseconds
2. WHEN the Playlist contains more than 1000 items, THE Search_Service SHALL still complete filtering within 500 milliseconds
3. THE Search_Service SHALL use efficient string matching algorithms to minimize CPU usage
4. WHEN search is active, THE Viewer SHALL NOT block navigation or other operations

### Requirement 17: Drag-and-Drop Visual Feedback

**User Story:** As a user, I want clear visual feedback during drag-and-drop, so that I know when and where I can drop files.

#### Acceptance Criteria

1. WHEN the user drags a file over the Viewer window, THE Viewer SHALL display a semi-transparent overlay with the text "Drop image to open"
2. WHEN the dragged file has an unsupported extension, THE overlay SHALL display "Unsupported file format"
3. WHEN the user drags the file outside the window, THE Viewer SHALL hide the overlay within 100 milliseconds
4. THE overlay SHALL NOT obscure the current image completely
5. THE overlay SHALL use the current theme colors for consistency

### Requirement 18: Metadata Panel Layout

**User Story:** As a user, I want the metadata panel to be readable and well-organized, so that I can quickly find specific information.

#### Acceptance Criteria

1. WHEN the metadata panel is displayed, THE Viewer SHALL group information into sections: File, Image, and Camera
2. WHEN EXIF_Data includes GPS coordinates, THE metadata panel SHALL display them in the Camera section
3. WHEN the metadata panel is displayed, THE Viewer SHALL format dates in the user's locale format
4. WHEN the metadata panel is displayed, THE Viewer SHALL format file sizes in human-readable units (KB, MB, GB)
5. THE metadata panel SHALL be scrollable when content exceeds available height
6. THE metadata panel SHALL appear as a slide-in overlay from the right side of the window

### Requirement 19: Transform Save Confirmation

**User Story:** As a user, I want confirmation before saving transforms, so that I don't accidentally overwrite original files.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+S with pending transforms, THE Viewer SHALL display a confirmation dialog
2. WHEN the confirmation dialog is displayed, THE Viewer SHALL show the message "Save changes to [filename]? This will overwrite the original file."
3. WHEN the user confirms, THE Transform_Service SHALL save the transforms to the original file
4. WHEN the user cancels, THE Viewer SHALL keep the transforms in memory without saving
5. WHEN transforms are saved successfully, THE Viewer SHALL clear the unsaved changes indicator

### Requirement 20: Format Support Extensibility

**User Story:** As a developer, I want format detection to remain centralized, so that Phase 3 formats can be added without refactoring.

#### Acceptance Criteria

1. THE Image_Decode_Service SHALL maintain the list of Supported_Format extensions from Phase 1
2. THE Image_Decode_Service SHALL add GIF and TIFF to the Supported_Format list
3. THE Image_Decode_Service SHALL maintain a separate list for Phase 3 deferred formats (HEIC, AVIF)
4. WHEN checking format support, THE Image_Decode_Service SHALL use the same case-insensitive matching as Phase 1
5. THE Image_Decode_Service SHALL route all format decoding through the same service interface

