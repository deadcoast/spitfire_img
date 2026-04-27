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
}
