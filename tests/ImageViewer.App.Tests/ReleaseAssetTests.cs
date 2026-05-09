using System.Xml.Linq;

namespace ImageViewer.App.Tests;

public sealed class ReleaseAssetTests
{
    [Fact]
    public void ReleaseAssets_AreSourceControlledAndWiredForExecutableAndPackageContexts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appProjectPath = Path.Combine(repositoryRoot, "src", "ImageViewer.App", "ImageViewer.App.csproj");
        var assetsDirectory = Path.Combine(repositoryRoot, "src", "ImageViewer.App", "Assets");
        var readmePath = Path.Combine(assetsDirectory, "README.md");

        Assert.True(File.Exists(Path.Combine(assetsDirectory, "spitfire-icon.svg")));
        AssertIcoFile(Path.Combine(assetsDirectory, "spitfire.ico"), expectedImageCount: 4);
        AssertPngDimensions(Path.Combine(assetsDirectory, "Square44x44Logo.png"), 44, 44);
        AssertPngDimensions(Path.Combine(assetsDirectory, "Square150x150Logo.png"), 150, 150);
        AssertPngDimensions(Path.Combine(assetsDirectory, "Wide310x150Logo.png"), 310, 150);

        Assert.True(File.Exists(readmePath));
        Assert.Contains("scripts\\generate-app-assets.ps1", File.ReadAllText(readmePath));

        var project = XDocument.Load(appProjectPath);
        Assert.Equal(@"Assets\spitfire.ico", GetSingleProperty(project, "ApplicationIcon"));

        AssertProjectContentInclude(project, @"Assets\spitfire.ico");
        AssertProjectContentInclude(project, @"Assets\spitfire-icon.svg");
        AssertProjectContentInclude(project, @"Assets\Square44x44Logo.png");
        AssertProjectContentInclude(project, @"Assets\Square150x150Logo.png");
        AssertProjectContentInclude(project, @"Assets\Wide310x150Logo.png");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SpitfireImg.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing SpitfireImg.sln.");
    }

    private static string? GetSingleProperty(XDocument project, string propertyName)
    {
        return project
            .Descendants(propertyName)
            .SingleOrDefault()
            ?.Value;
    }

    private static void AssertProjectContentInclude(XDocument project, string includePath)
    {
        var contentItem = project
            .Descendants("Content")
            .SingleOrDefault(element => string.Equals(
                element.Attribute("Include")?.Value,
                includePath,
                StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(contentItem);
        Assert.Equal("PreserveNewest", contentItem!.Element("CopyToOutputDirectory")?.Value);
    }

    private static void AssertIcoFile(string path, ushort expectedImageCount)
    {
        Assert.True(File.Exists(path));

        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        Assert.Equal(0, reader.ReadUInt16());
        Assert.Equal(1, reader.ReadUInt16());
        Assert.Equal(expectedImageCount, reader.ReadUInt16());
    }

    private static void AssertPngDimensions(string path, int expectedWidth, int expectedHeight)
    {
        Assert.True(File.Exists(path));

        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 24);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes.Take(8).ToArray());

        var width = ReadBigEndianInt32(bytes, 16);
        var height = ReadBigEndianInt32(bytes, 20);

        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset)
    {
        return
            (bytes[offset] << 24) |
            (bytes[offset + 1] << 16) |
            (bytes[offset + 2] << 8) |
            bytes[offset + 3];
    }
}
