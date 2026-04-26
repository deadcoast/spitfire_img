using ImageViewer.Core.Models;

namespace ImageViewer.Tests.Models;

public sealed class ImageMetadataTests
{
    [Fact]
    public void FormattedDimensions_UsesPixelWidthAndHeight()
    {
        var metadata = new ImageMetadata(3840, 2160, 4_200_000);

        Assert.Equal("3840 × 2160", metadata.FormattedDimensions);
    }

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(4_200_000, "4.0 MB")]
    public void FormattedFileSize_UsesReadableUnits(long bytes, string expected)
    {
        var metadata = new ImageMetadata(100, 100, bytes);

        Assert.Equal(expected, metadata.FormattedFileSize);
    }
}
