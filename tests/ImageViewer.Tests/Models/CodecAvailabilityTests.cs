using ImageViewer.Core.Models;
using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Models;

public sealed class CodecAvailabilityTests
{
    [Fact]
    public void CodecAvailability_StoresHeicAvifAndDetectionTime()
    {
        var detectionTime = new DateTimeOffset(2026, 5, 7, 10, 45, 0, TimeSpan.Zero);

        var availability = new CodecAvailability(
            HeicSupported: true,
            AvifSupported: false,
            DetectionTime: detectionTime);

        Assert.True(availability.HeicSupported);
        Assert.False(availability.AvifSupported);
        Assert.Equal(detectionTime, availability.DetectionTime);
    }

    [Fact]
    public void CodecDetectionError_ReturnsUserSafeWarning()
    {
        var error = ViewerError.CodecDetectionError(
            "DetectCodecs",
            new UnauthorizedAccessException("raw registry failure"));

        Assert.Equal(ErrorCategory.CodecDetectionError, error.Category);
        Assert.Equal(ErrorSeverity.Warning, error.Severity);
        Assert.Equal("Unable to detect codec availability.", error.UserMessage);
        Assert.Equal(nameof(UnauthorizedAccessException), error.ExceptionType);
        Assert.DoesNotContain("raw registry failure", error.UserMessage);
    }

    [Fact]
    public void CodecUnavailableError_ReturnsFormatSpecificStorePrompt()
    {
        var error = ViewerError.CodecUnavailableError(
            "OpenHeic",
            "HEIC",
            "HEIF Image Extensions",
            @"C:\Photos\image.heic");

        Assert.Equal(ErrorCategory.CodecUnavailableError, error.Category);
        Assert.Equal(ErrorSeverity.Warning, error.Severity);
        Assert.Equal(
            "HEIC format requires the HEIF Image Extensions from the Microsoft Store. Would you like to open the Store page?",
            error.UserMessage);
        Assert.Equal(@"C:\Photos\image.heic", error.FilePath);
    }

    [Fact]
    public void CodecDetectionServiceContract_ExposesDetectionCacheAndStoreUrls()
    {
        var methods = typeof(ICodecDetectionService).GetMethods().Select(method => method.Name);

        Assert.Contains(nameof(ICodecDetectionService.DetectCodecsAsync), methods);
        Assert.Contains(nameof(ICodecDetectionService.IsHeicSupported), methods);
        Assert.Contains(nameof(ICodecDetectionService.IsAvifSupported), methods);
        Assert.Contains(nameof(ICodecDetectionService.GetHeicStoreUrl), methods);
        Assert.Contains(nameof(ICodecDetectionService.GetAvifStoreUrl), methods);
    }
}
