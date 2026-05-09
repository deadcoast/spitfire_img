namespace ImageViewer.App.Tests;

public sealed class ReleaseBuildScriptTests
{
    [Fact]
    public void BuildReleaseScript_ImplementsRequiredReleasePublishContract()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "scripts", "build-release.ps1");
        var designPath = Path.Combine(repositoryRoot, "docs", "phase-4-release-readiness", "design.md");

        Assert.True(File.Exists(scriptPath));

        var script = File.ReadAllText(scriptPath);
        Assert.Contains("$ErrorActionPreference = 'Stop'", script);
        Assert.Contains("Stop-Process -Name ImageViewer.App", script);
        Assert.Contains("dotnet", script);
        Assert.Contains("publish", script);
        Assert.Contains("-c", script);
        Assert.Contains("Release", script);
        Assert.Contains("-p:Platform=x64", script);
        Assert.Contains("spitfire.img-$version-win-x64", script);
        Assert.Contains("artifacts\\release", script);
        Assert.Contains("Remove-Item -LiteralPath $releaseDirectory -Recurse -Force", script);
        Assert.Contains("StartsWith($resolvedArtifactsRoot", script);
        Assert.Contains("exit $exitCode", script);

        var design = File.ReadAllText(designPath);
        Assert.Contains("powershell -ExecutionPolicy Bypass -File scripts\\build-release.ps1", design);
        Assert.Contains("unpackaged Release publish", design);
        Assert.Contains("WindowsPackageType=None", design);
    }

    [Fact]
    public void BuildReleaseScript_GeneratesAndValidatesRequiredReleaseManifestFields()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "scripts", "build-release.ps1");

        Assert.True(File.Exists(scriptPath));

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("release-manifest.json", script);
        Assert.Contains("git -C $repoRoot rev-parse HEAD", script);
        Assert.Contains("Get-Date", script);
        Assert.Contains("ConvertTo-Json", script);
        Assert.Contains("Assert-ManifestField", script);
        Assert.Contains("Assert-ManifestVerificationField", script);
        Assert.Contains("productName = 'spitfire.img'", script);
        Assert.Contains("version = $version", script);
        Assert.Contains("configuration = 'Release'", script);
        Assert.Contains("platform = 'x64'", script);
        Assert.Contains("commit = $commit", script);
        Assert.Contains("buildTimeUtc = $buildTimeUtc", script);
        Assert.Contains("artifactKind = 'unpackaged-publish'", script);
        Assert.Contains("artifactName = [System.IO.Path]::GetFileName($releaseDirectory)", script);
        Assert.Contains("build = 'pending'", script);
        Assert.Contains("tests = 'pending'", script);
        Assert.Contains("launchSmoke = 'pending'", script);
    }

    [Fact]
    public void BuildReleaseScript_CopiesWinUiXamlResourcesIntoPublishedArtifact()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "scripts", "build-release.ps1");

        Assert.True(File.Exists(scriptPath));

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("$targetFramework = 'net8.0-windows10.0.19041.0'", script);
        Assert.Contains("$buildOutputDirectory = Join-Path $repoRoot \"src\\ImageViewer.App\\bin\\x64\\Release\\$targetFramework\"", script);
        Assert.Contains("Copy-ReleaseFile -SourcePath (Join-Path $buildOutputDirectory 'ImageViewer.App.pri')", script);
        Assert.Contains("Get-ChildItem -LiteralPath $buildOutputDirectory -Filter '*.xbf' -Recurse", script);
        Assert.Contains("Copy-ReleaseFile -SourcePath $xbf.FullName", script);
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
}
