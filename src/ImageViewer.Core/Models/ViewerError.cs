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
    ComparisonError,
    CommandPaletteError
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

    public static ViewerError ComparisonError(
        string operation,
        string userMessage,
        string? filePath = null,
        Exception? exception = null)
    {
        return new ViewerError(
            DateTimeOffset.UtcNow,
            ErrorSeverity.Error,
            ErrorCategory.ComparisonError,
            userMessage,
            operation,
            filePath,
            exception?.GetType().Name,
            exception?.StackTrace);
    }

    public static ViewerError CommandPaletteError(
        string operation,
        string userMessage,
        Exception? exception = null)
    {
        return new ViewerError(
            DateTimeOffset.UtcNow,
            ErrorSeverity.Error,
            ErrorCategory.CommandPaletteError,
            userMessage,
            operation,
            FilePath: null,
            exception?.GetType().Name,
            exception?.StackTrace);
    }

    private static ErrorSeverity GetSeverity(ErrorCategory category)
    {
        return category is ErrorCategory.PlaylistScanError or ErrorCategory.SettingsPersistenceError or ErrorCategory.ThumbnailGenerationError
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
            ErrorCategory.ComparisonError => "Unable to enter comparison mode.",
            ErrorCategory.CommandPaletteError => "Unable to load command palette.",
            _ => "An unexpected error occurred. The application will attempt to continue."
        };
    }
}
