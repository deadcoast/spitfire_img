using ImageViewer.Core.Models;

namespace ImageViewer.Tests.Models;

public sealed class ImageItemTests
{
    [Fact]
    public void Constructor_DerivesFilenameAndLowercaseExtension()
    {
        var item = new ImageItem(@"C:\Photos\Event\IMG_0042.PNG");

        Assert.Equal(@"C:\Photos\Event\IMG_0042.PNG", item.FilePath);
        Assert.Equal("IMG_0042.PNG", item.Filename);
        Assert.Equal(".png", item.Extension);
    }

    [Fact]
    public void Equality_UsesCaseInsensitiveFilePath()
    {
        var first = new ImageItem(@"C:\Photos\Event\IMG_0042.PNG");
        var second = new ImageItem(@"c:\photos\event\img_0042.png");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsFilenameForUiFallbackRendering()
    {
        var item = new ImageItem(@"C:\Photos\Event\IMG_0042.PNG");

        Assert.Equal("IMG_0042.PNG", item.ToString());
    }
}
