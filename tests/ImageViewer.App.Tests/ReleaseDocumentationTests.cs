namespace ImageViewer.App.Tests;

public sealed class ReleaseDocumentationTests
{
    [Fact]
    public void ReleaseReadme_CoversRequiredReleaseSections()
    {
        var repositoryRoot = FindRepositoryRoot();
        var readmePath = Path.Combine(repositoryRoot, "docs", "phase-4-release-readiness", "release", "README.md");

        Assert.True(File.Exists(readmePath));

        var readme = File.ReadAllText(readmePath);

        Assert.Contains("# spitfire.img release notes", readme);
        Assert.Contains("## Product summary", readme);
        Assert.Contains("## System requirements", readme);
        Assert.Contains("## Install and run", readme);
        Assert.Contains("## Uninstall", readme);
        Assert.Contains("## Supported formats", readme);
        Assert.Contains("## HEIC and AVIF codec dependencies", readme);
        Assert.Contains("## Keyboard shortcuts", readme);
        Assert.Contains("## Troubleshooting", readme);
        Assert.Contains("## Support bundle", readme);
        Assert.Contains("## Known limitations", readme);
        Assert.Contains(".png, .jpg, .jpeg, .bmp, .webp, .gif, .tif, .tiff", readme);
        Assert.Contains(".heic, .avif", readme);
        Assert.Contains("Ctrl+O", readme);
        Assert.Contains("Ctrl+Shift+P", readme);
        Assert.Contains("F5", readme);
        Assert.Contains("Ctrl+G", readme);
        Assert.Contains("Ctrl+Shift+F", readme);
        Assert.Contains("TestResults/phase4-evidence/support-bundle/", readme);
    }

    [Fact]
    public void ReleaseReadme_DoesNotClaimOutOfScopeFeatures()
    {
        var repositoryRoot = FindRepositoryRoot();
        var readmePath = Path.Combine(repositoryRoot, "docs", "phase-4-release-readiness", "release", "README.md");
        var readme = File.ReadAllText(readmePath);

        Assert.DoesNotContain("cataloging", readme, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("albums", readme, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tagging", readme, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cloud sync", readme, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accounts", readme, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("telemetry", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReleaseTemplate_CoversRequiredReleaseRecordFields()
    {
        var repositoryRoot = FindRepositoryRoot();
        var templatePath = Path.Combine(repositoryRoot, "docs", "phase-4-release-readiness", "release", "release-template.md");

        Assert.True(File.Exists(templatePath));

        var template = File.ReadAllText(templatePath);

        Assert.Contains("## Version", template);
        Assert.Contains("## Date", template);
        Assert.Contains("## Commit", template);
        Assert.Contains("## Artifact names", template);
        Assert.Contains("## Verification commands and results", template);
        Assert.Contains("dotnet build SpitfireImg.sln -p:Platform=x64", template);
        Assert.Contains("dotnet test SpitfireImg.sln -p:Platform=x64", template);
        Assert.Contains("scripts\\build-release.ps1", template);
        Assert.Contains("## Manual evidence links", template);
        Assert.Contains("## Known issues", template);
    }

    [Fact]
    public void Phase4EvidenceTemplate_CoversRequiredReleaseGateEvidenceSections()
    {
        var repositoryRoot = FindRepositoryRoot();
        var templatePath = Path.Combine(repositoryRoot, "docs", "phase-4-release-readiness", "release", "phase4-evidence-template.md");

        Assert.True(File.Exists(templatePath));

        var template = File.ReadAllText(templatePath);

        Assert.Contains("TestResults/phase4-evidence/", template);
        Assert.Contains("## Build evidence", template);
        Assert.Contains("dotnet build SpitfireImg.sln -p:Platform=x64", template);
        Assert.Contains("## Test evidence", template);
        Assert.Contains("dotnet test SpitfireImg.sln -p:Platform=x64", template);
        Assert.Contains("## Publish evidence", template);
        Assert.Contains("scripts\\build-release.ps1", template);
        Assert.Contains("## Release artifact manifest", template);
        Assert.Contains("release-manifest.json", template);
        Assert.Contains("## Launch smoke evidence", template);
        Assert.Contains("## Startup path with spaces evidence", template);
        Assert.Contains("## Large playlist smoke evidence", template);
        Assert.Contains("1000-image", template);
        Assert.Contains("## 4K image smoke evidence", template);
        Assert.Contains("## 100-frame GIF smoke evidence", template);
        Assert.Contains("## Support bundle evidence", template);
        Assert.Contains("scripts\\collect-support.ps1 -Validation", template);
        Assert.Contains("## Known issues", template);
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
