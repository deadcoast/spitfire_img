using ImageViewer.Core.Models;

namespace ImageViewer.Tests.Models;

public sealed class ViewerErrorTests
{
    [Theory]
    [InlineData(ErrorCategory.FileAccessError, "Unable to access this file. It may have been moved, deleted, or you may not have permission to open it.")]
    [InlineData(ErrorCategory.UnsupportedFormatError, "This file format is not supported in this version.")]
    [InlineData(ErrorCategory.DecodeError, "Unable to decode this image. The file may be corrupted or in an unsupported variant.")]
    [InlineData(ErrorCategory.PlaylistScanError, "Unable to scan the folder for additional images. You can still view this image.")]
    [InlineData(ErrorCategory.NavigationError, "Unable to load the requested image.")]
    [InlineData(ErrorCategory.SettingsPersistenceError, "Unable to save settings. Your preferences may not persist.")]
    [InlineData(ErrorCategory.UnexpectedViewerError, "An unexpected error occurred. The application will attempt to continue.")]
    [InlineData(ErrorCategory.SlideshowError, "Unable to start slideshow mode.")]
    [InlineData(ErrorCategory.TransitionError, "Unable to apply transition effect.")]
    [InlineData(ErrorCategory.ComparisonError, "Unable to load images for comparison.")]
    [InlineData(ErrorCategory.SyncError, "Unable to synchronize zoom and pan.")]
    [InlineData(ErrorCategory.CommandPaletteError, "Unable to load command palette.")]
    [InlineData(ErrorCategory.ExecutionError, "Unable to execute command.")]
    [InlineData(ErrorCategory.AnimationError, "Unable to load animated GIF.")]
    [InlineData(ErrorCategory.CodecDetectionError, "Unable to detect codec availability.")]
    [InlineData(ErrorCategory.LayoutError, "Unable to load custom layout.")]
    public void Create_UsesSpecifiedUserSafeMessages(ErrorCategory category, string expectedMessage)
    {
        var exception = new InvalidOperationException("raw exception detail");

        var error = ViewerError.Create(category, "TestOperation", @"C:\Photos\bad.png", exception);

        Assert.Equal(category, error.Category);
        Assert.Equal(expectedMessage, error.UserMessage);
        Assert.Equal("TestOperation", error.Operation);
        Assert.Equal(@"C:\Photos\bad.png", error.FilePath);
        Assert.Equal(nameof(InvalidOperationException), error.ExceptionType);
        Assert.DoesNotContain("raw exception detail", error.UserMessage);
    }

    [Fact]
    public void DecodeMemoryError_UsesTooLargeUserMessage()
    {
        var error = ViewerError.DecodeError("DecodeImage", @"C:\Photos\huge.png", new OutOfMemoryException(), isMemoryError: true);

        Assert.Equal(ErrorCategory.DecodeError, error.Category);
        Assert.Equal("This image is too large to display.", error.UserMessage);
    }

    [Fact]
    public void SettingsPersistenceError_LoadSettings_UsesLoadFailureWarningMessage()
    {
        var error = ViewerError.SettingsPersistenceError(
            "LoadSettings",
            @"C:\Users\User\AppData\Local\spitfire.img\settings.json",
            new InvalidDataException("bad settings"));

        Assert.Equal(ErrorSeverity.Warning, error.Severity);
        Assert.Equal(ErrorCategory.SettingsPersistenceError, error.Category);
        Assert.Equal("Unable to load settings. Default preferences were restored.", error.UserMessage);
        Assert.Equal("LoadSettings", error.Operation);
    }

    [Fact]
    public void Phase3Factories_CreateExpectedErrorCategories()
    {
        var exception = new InvalidOperationException("raw exception detail");

        var errors = new[]
        {
            ViewerError.SlideshowError("StartSlideshow", exception),
            ViewerError.TransitionError(TransitionEffect.Fade, exception),
            ViewerError.SyncError("SynchronizeZoom", exception),
            ViewerError.ExecutionError("layout.grid", exception)
        };

        Assert.Equal(
            [ErrorCategory.SlideshowError, ErrorCategory.TransitionError, ErrorCategory.SyncError, ErrorCategory.ExecutionError],
            errors.Select(error => error.Category));
        Assert.All(errors, error => Assert.DoesNotContain("raw exception detail", error.UserMessage));
    }
}
