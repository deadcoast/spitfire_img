using ImageViewer.Core.Services;

namespace ImageViewer.Tests.Services;

public sealed class ImageFormatCatalogTests
{
    [Theory]
    [InlineData(".png")]
    [InlineData(".JPG")]
    [InlineData(".jpeg")]
    [InlineData(".BMP")]
    [InlineData(".webp")]
    public void IsSupportedFormat_RecognizesPhaseOneFormatsCaseInsensitively(string extension)
    {
        Assert.True(ImageFormatCatalog.IsSupportedFormat(extension));
        Assert.False(ImageFormatCatalog.IsDeferredFormat(extension));
    }

    [Theory]
    [InlineData(".gif")]
    [InlineData(".TIFF")]
    [InlineData(".tif")]
    [InlineData(".HEIC")]
    [InlineData(".avif")]
    public void IsDeferredFormat_RecognizesFutureFormatsCaseInsensitively(string extension)
    {
        Assert.True(ImageFormatCatalog.IsDeferredFormat(extension));
        Assert.False(ImageFormatCatalog.IsSupportedFormat(extension));
    }

    [Theory]
    [InlineData("")]
    [InlineData(".txt")]
    [InlineData(".png.backup")]
    public void UnknownFormat_IsNeitherSupportedNorDeferred(string extension)
    {
        Assert.False(ImageFormatCatalog.IsSupportedFormat(extension));
        Assert.False(ImageFormatCatalog.IsDeferredFormat(extension));
    }
}
