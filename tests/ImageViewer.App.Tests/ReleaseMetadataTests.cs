using System.Diagnostics;
using System.Reflection;

namespace ImageViewer.App.Tests;

public sealed class ReleaseMetadataTests
{
    [Fact]
    public void AppAssembly_ExposesPhase4ProductNameAndVersionMetadata()
    {
        var assembly = typeof(App).Assembly;
        var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);

        Assert.Equal("spitfire.img", title);
        Assert.Equal("spitfire.img", product);
        Assert.Equal("0.4.0", informationalVersion);
        Assert.Equal("0.4.0.0", fileVersion.FileVersion);
        Assert.Equal("spitfire.img", fileVersion.ProductName);
        Assert.Equal("0.4.0", fileVersion.ProductVersion);
    }
}
