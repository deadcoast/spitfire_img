namespace ImageViewer.Core.Models;

public enum ErrorCategory
{
    FileAccessError,
    UnsupportedFormatError,
    DecodeError,
    PlaylistScanError,
    NavigationError,
    SettingsPersistenceError,
    UnexpectedViewerError,
    ThumbnailGenerationError,
    ExportError,
    TransformError,
    TransformSaveError,
    MetadataExtractionError,
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

public enum ErrorSeverity
{
    Warning,
    Error
}

public sealed record ViewerError(
    DateTimeOffset Timestamp,
    ErrorSeverity Severity,
    ErrorCategory Category,
    string UserMessage,
    string Operation,
    string? FilePath,
    string? ExceptionType,
    string? StackTrace)
{
    public static ViewerError Create(
        ErrorCategory category,
        string operation,
        string? filePath = null,
        Exception? exception = null)
    {
        return new ViewerError(
            DateTimeOffset.UtcNow,
            GetSeverity(category),
            category,
            GetUserMessage(category),
            operation,
            filePath,
            exception?.GetType().Name,
            exception?.StackTrace);
    }

    public static ViewerError FileAccessError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.FileAccessError, operation, filePath, exception);
    }

    public static ViewerError UnsupportedFormatError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.UnsupportedFormatError, operation, filePath, exception);
    }

    public static ViewerError DecodeError(
        string operation,
        string? filePath,
        Exception? exception = null,
        bool isMemoryError = false)
    {
        if (!isMemoryError)
        {
            return Create(ErrorCategory.DecodeError, operation, filePath, exception);
        }

        return new ViewerError(
            DateTimeOffset.UtcNow,
            ErrorSeverity.Error,
            ErrorCategory.DecodeError,
            "This image is too large to display.",
            operation,
            filePath,
            exception?.GetType().Name,
            exception?.StackTrace);
    }

    public static ViewerError PlaylistScanError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.PlaylistScanError, operation, filePath, exception);
    }

    public static ViewerError NavigationError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.NavigationError, operation, filePath, exception);
    }

    public static ViewerError SettingsPersistenceError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.SettingsPersistenceError, operation, filePath, exception);
    }

    public static ViewerError UnexpectedViewerError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.UnexpectedViewerError, operation, filePath, exception);
    }

    public static ViewerError ThumbnailGenerationError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.ThumbnailGenerationError, operation, filePath, exception);
    }

    public static ViewerError ExportError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.ExportError, operation, filePath, exception);
    }

    public static ViewerError TransformError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.TransformError, operation, filePath, exception);
    }

    public static ViewerError TransformSaveError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.TransformSaveError, operation, filePath, exception);
    }

    public static ViewerError MetadataExtractionError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.MetadataExtractionError, operation, filePath, exception);
    }

    public static ViewerError SlideshowError(string operation, Exception? exception = null)
    {
        return Create(ErrorCategory.SlideshowError, operation, filePath: null, exception);
    }

    public static ViewerError SlideshowError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.SlideshowError, operation, filePath, exception);
    }

    public static ViewerError TransitionError(TransitionEffect effect, Exception? exception = null)
    {
        return Create(ErrorCategory.TransitionError, $"Transition:{effect}", filePath: null, exception);
    }

    public static ViewerError ComparisonError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.ComparisonError, operation, filePath, exception);
    }

    public static ViewerError ComparisonError(string operation, Exception? exception = null)
    {
        return Create(ErrorCategory.ComparisonError, operation, filePath: null, exception);
    }

    public static ViewerError SyncError(string operation, Exception? exception = null)
    {
        return Create(ErrorCategory.SyncError, operation, filePath: null, exception);
    }

    public static ViewerError CommandPaletteError(string operation, Exception? exception = null)
    {
        return Create(ErrorCategory.CommandPaletteError, operation, filePath: null, exception);
    }

    public static ViewerError ExecutionError(string commandId, Exception? exception = null)
    {
        return Create(ErrorCategory.ExecutionError, $"Command:{commandId}", filePath: null, exception);
    }

    public static ViewerError AnimationError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.AnimationError, operation, filePath, exception);
    }

    public static ViewerError AnimationError(string filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.AnimationError, "LoadAnimation", filePath, exception);
    }

    public static ViewerError CodecDetectionError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.CodecDetectionError, operation, filePath, exception);
    }

    public static ViewerError CodecDetectionError(Exception? exception = null)
    {
        return Create(ErrorCategory.CodecDetectionError, "DetectCodecs", filePath: null, exception);
    }

    public static ViewerError CodecUnavailableError(string operation, string? filePath, Exception? exception = null)
    {
        if (IsFormatCodecPair(operation, filePath))
        {
            return new ViewerError(
                DateTimeOffset.UtcNow,
                ErrorSeverity.Error,
                ErrorCategory.CodecUnavailableError,
                $"{operation} format requires the {filePath} from the Microsoft Store. Would you like to open the Store page?",
                $"CodecUnavailable:{operation}",
                null,
                exception?.GetType().Name,
                exception?.StackTrace);
        }

        return new ViewerError(
            DateTimeOffset.UtcNow,
            ErrorSeverity.Error,
            ErrorCategory.CodecUnavailableError,
            GetCodecUnavailableMessage(filePath),
            operation,
            filePath,
            exception?.GetType().Name,
            exception?.StackTrace);
    }

    public static ViewerError LayoutError(string operation, Exception? exception = null)
    {
        return Create(ErrorCategory.LayoutError, operation, filePath: null, exception);
    }

    public static ViewerError LayoutError(string operation, string? filePath, Exception? exception = null)
    {
        return Create(ErrorCategory.LayoutError, operation, filePath, exception);
    }

    private static ErrorSeverity GetSeverity(ErrorCategory category)
    {
        return category is ErrorCategory.PlaylistScanError
            or ErrorCategory.SettingsPersistenceError
            or ErrorCategory.ThumbnailGenerationError
            or ErrorCategory.SyncError
            or ErrorCategory.AnimationError
            or ErrorCategory.CodecDetectionError
            ? ErrorSeverity.Warning
            : ErrorSeverity.Error;
    }

    private static string GetUserMessage(ErrorCategory category)
    {
        return category switch
        {
            ErrorCategory.FileAccessError => "Unable to access this file. It may have been moved, deleted, or you may not have permission to open it.",
            ErrorCategory.UnsupportedFormatError => "This file format is not supported in this version.",
            ErrorCategory.DecodeError => "Unable to decode this image. The file may be corrupted or in an unsupported variant.",
            ErrorCategory.PlaylistScanError => "Unable to scan the folder for additional images. You can still view this image.",
            ErrorCategory.NavigationError => "Unable to load the requested image.",
            ErrorCategory.SettingsPersistenceError => "Unable to save settings. Your preferences may not persist.",
            ErrorCategory.UnexpectedViewerError => "An unexpected error occurred. The application will attempt to continue.",
            ErrorCategory.ThumbnailGenerationError => "Unable to generate a thumbnail for this image.",
            ErrorCategory.ExportError => "Unable to save a copy of this image to the selected location.",
            ErrorCategory.TransformError => "Unable to apply the requested image change.",
            ErrorCategory.TransformSaveError => "Unable to save changes to this image.",
            ErrorCategory.MetadataExtractionError => "Unable to read detailed metadata for this image.",
            ErrorCategory.SlideshowError => "Unable to start slideshow mode.",
            ErrorCategory.TransitionError => "Unable to apply transition effect.",
            ErrorCategory.ComparisonError => "Unable to load images for comparison.",
            ErrorCategory.SyncError => "Unable to synchronize zoom and pan.",
            ErrorCategory.CommandPaletteError => "Unable to load command palette.",
            ErrorCategory.ExecutionError => "Unable to execute command.",
            ErrorCategory.AnimationError => "Unable to load animated GIF.",
            ErrorCategory.CodecDetectionError => "Unable to detect codec availability.",
            ErrorCategory.CodecUnavailableError => "This format requires an additional Windows codec from the Microsoft Store.",
            ErrorCategory.LayoutError => "Unable to load custom layout.",
            _ => "An unexpected error occurred. The application will attempt to continue."
        };
    }

    private static string GetCodecUnavailableMessage(string? filePath)
    {
        return (Path.GetExtension(filePath) ?? string.Empty).ToLowerInvariant() switch
        {
            ".heic" or ".heif" => "HEIC format requires the HEIF Image Extensions from the Microsoft Store. Would you like to open the Store page?",
            ".avif" => "AVIF format requires the AV1 Video Extension from the Microsoft Store. Would you like to open the Store page?",
            _ => "This format requires an additional Windows codec from the Microsoft Store."
        };
    }

    private static bool IsFormatCodecPair(string format, string? codecName)
    {
        return !string.IsNullOrWhiteSpace(codecName)
            && !Path.IsPathFullyQualified(codecName)
            && format is "HEIC" or "HEIF" or "AVIF";
    }
}
