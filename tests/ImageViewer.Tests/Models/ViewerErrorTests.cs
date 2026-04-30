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
    public void Phase3Factories_SetExpectedCategoriesAndOperations()
    {
        var exception = new InvalidOperationException("raw exception detail");
        var errors = new[]
        {
            ViewerError.SlideshowError("StartSlideshow", exception),
            ViewerError.TransitionError(TransitionEffect.Slide, exception),
            ViewerError.ComparisonError("EnterComparison", @"C:\Photos", exception),
            ViewerError.SyncError("SynchronizeZoom", exception),
            ViewerError.CommandPaletteError("FilterCommands", exception),
            ViewerError.ExecutionError("open-file", exception),
            ViewerError.AnimationError(@"C:\Photos\animated.gif", exception),
            ViewerError.CodecDetectionError(exception),
            ViewerError.CodecUnavailableError("HEIC", "HEIF Image Extensions"),
            ViewerError.LayoutError("EnterGrid", exception)
        };

        Assert.Equal(
            [
                ErrorCategory.SlideshowError,
                ErrorCategory.TransitionError,
                ErrorCategory.ComparisonError,
                ErrorCategory.SyncError,
                ErrorCategory.CommandPaletteError,
                ErrorCategory.ExecutionError,
                ErrorCategory.AnimationError,
                ErrorCategory.CodecDetectionError,
                ErrorCategory.CodecUnavailableError,
                ErrorCategory.LayoutError
            ],
            errors.Select(error => error.Category).ToArray());
        Assert.All(errors, error =>
        {
            Assert.False(string.IsNullOrWhiteSpace(error.Operation));
            Assert.False(string.IsNullOrWhiteSpace(error.UserMessage));
            Assert.DoesNotContain("raw exception detail", error.UserMessage);
        });
        Assert.Equal("Transition:Slide", errors[1].Operation);
        Assert.Equal("Command:open-file", errors[5].Operation);
        Assert.Contains("HEIC", errors[8].UserMessage);
        Assert.Contains("HEIF Image Extensions", errors[8].UserMessage);
    }

    [Fact]
    public void DecodeMemoryError_UsesTooLargeUserMessage()
    {
        var error = ViewerError.DecodeError("DecodeImage", @"C:\Photos\huge.png", new OutOfMemoryException(), isMemoryError: true);

        Assert.Equal(ErrorCategory.DecodeError, error.Category);
        Assert.Equal("This image is too large to display.", error.UserMessage);
    }
}
