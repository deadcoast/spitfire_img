using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace ImageViewer.App.Tests;

public sealed class SupportBundleScriptTests
{
    [Fact]
    public void CollectSupportScript_ImplementsRequiredSupportBundleContract()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "scripts", "collect-support.ps1");

        Assert.True(File.Exists(scriptPath));

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("$ErrorActionPreference = 'Stop'", script);
        Assert.Contains("TestResults\\phase4-evidence\\support-bundle", script);
        Assert.Contains("%LOCALAPPDATA%\\spitfire.img\\support", script);
        Assert.Contains("release-manifest.json", script);
        Assert.Contains("Get-CimInstance Win32_OperatingSystem", script);
        Assert.Contains("[System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture", script);
        Assert.Contains("dotnet --info", script);
        Assert.Contains("Get-AppxPackage", script);
        Assert.Contains("HEIC", script);
        Assert.Contains("AVIF", script);
        Assert.Contains("logs", script);
        Assert.Contains("Compress-Archive", script);
    }

    [Fact]
    public void CollectSupportScript_ExcludesPrivateAndImageDataByDefault()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "scripts", "collect-support.ps1");

        Assert.True(File.Exists(scriptPath));

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("recent-files.json", script);
        Assert.Contains("settings.json", script);
        Assert.Contains("*.png", script);
        Assert.Contains("*.jpg", script);
        Assert.Contains("*.jpeg", script);
        Assert.Contains("*.bmp", script);
        Assert.Contains("*.webp", script);
        Assert.Contains("*.gif", script);
        Assert.Contains("*.tif", script);
        Assert.Contains("*.tiff", script);
        Assert.Contains("*.heic", script);
        Assert.Contains("*.avif", script);
        Assert.Contains("[redacted path]", script);
    }

    [Fact]
    public void CollectSupportScript_WithSeededPrivateData_RedactsAndExcludesPrivateContent()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "scripts", "collect-support.ps1");
        var tempRoot = CreateTempDirectory();
        var localData = Path.Combine(tempRoot, "local-data", "spitfire.img");
        var outputDirectory = Path.Combine(tempRoot, "support-output");
        var privateImagePath = @"C:\Users\SensitiveUser\Pictures\family trip\private image.png";
        var privateRecentPath = @"C:\Users\SensitiveUser\Pictures\recent private.avif";
        var privateSettingsPath = @"C:\Users\SensitiveUser\Pictures\exports";

        try
        {
            Directory.CreateDirectory(localData);
            Directory.CreateDirectory(Path.Combine(localData, "logs"));
            Directory.CreateDirectory(Path.Combine(localData, "thumbnails"));
            File.WriteAllText(
                Path.Combine(localData, "logs", "viewer-2026-05-08.log"),
                $"""
                [Warning] [DecodeError]
                Operation: DecodeImage
                File: {privateImagePath}
                Message: Unable to decode this image.
                """);
            File.WriteAllText(
                Path.Combine(localData, "recent-files.json"),
                $$"""
                [{"filePath":"{{privateRecentPath.Replace("\\", "\\\\")}}"}]
                """);
            File.WriteAllText(
                Path.Combine(localData, "settings.json"),
                $$"""
                {"lastExportFolder":"{{privateSettingsPath.Replace("\\", "\\\\")}}"}
                """);
            File.WriteAllText(Path.Combine(localData, "private image.png"), "image bytes");
            File.WriteAllText(Path.Combine(localData, "thumbnails", "private-thumb.jpg"), "thumbnail bytes");

            var result = RunCollectSupportScript(scriptPath, localData, outputDirectory);

            Assert.Equal(0, result.ExitCode);
            var supportBundle = GetJsonString(result.Output, "supportBundle");
            var stagingDirectory = GetJsonString(result.Output, "stagingDirectory");
            Assert.True(File.Exists(supportBundle));
            Assert.True(Directory.Exists(stagingDirectory));

            using var archive = ZipFile.OpenRead(supportBundle);
            var entryNames = archive.Entries.Select(entry => entry.FullName.Replace('\\', '/')).ToArray();
            Assert.Contains("app-version.json", entryNames);
            Assert.Contains("system-info.json", entryNames);
            Assert.Contains("codec-availability.json", entryNames);
            Assert.Contains("support-bundle-manifest.json", entryNames);
            Assert.Contains("logs/viewer-2026-05-08.log", entryNames);
            Assert.DoesNotContain(entryNames, name => name.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(entryNames, name => name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(entryNames, name => name.EndsWith("recent-files.json", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(entryNames, name => name.EndsWith("settings.json", StringComparison.OrdinalIgnoreCase));

            var bundleText = string.Join(
                Environment.NewLine,
                Directory.GetFiles(stagingDirectory, "*", SearchOption.AllDirectories)
                    .Where(path => !path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    .Select(File.ReadAllText));
            Assert.Contains("[redacted path]", bundleText);
            Assert.DoesNotContain(privateImagePath, bundleText);
            Assert.DoesNotContain(privateRecentPath, bundleText);
            Assert.DoesNotContain(privateSettingsPath, bundleText);
            Assert.DoesNotContain("SensitiveUser", bundleText);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void CollectSupportScript_WhenLogsFolderIsMissing_WritesNoLogsMarkerAndMetadata()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "scripts", "collect-support.ps1");
        var tempRoot = CreateTempDirectory();
        var localData = Path.Combine(tempRoot, "local-data", "spitfire.img");
        var outputDirectory = Path.Combine(tempRoot, "support-output");

        try
        {
            Directory.CreateDirectory(localData);

            var result = RunCollectSupportScript(scriptPath, localData, outputDirectory);

            Assert.Equal(0, result.ExitCode);
            var stagingDirectory = GetJsonString(result.Output, "stagingDirectory");
            Assert.True(File.Exists(Path.Combine(stagingDirectory, "logs", "no-logs.txt")));
            Assert.True(File.Exists(Path.Combine(stagingDirectory, "app-version.json")));
            Assert.True(File.Exists(Path.Combine(stagingDirectory, "system-info.json")));
            Assert.True(File.Exists(Path.Combine(stagingDirectory, "codec-availability.json")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "spitfire-support-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static (int ExitCode, string Output) RunCollectSupportScript(
        string scriptPath,
        string localDataFolder,
        string outputDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-Validation");
        startInfo.ArgumentList.Add("-LocalDataFolder");
        startInfo.ArgumentList.Add(localDataFolder);
        startInfo.ArgumentList.Add("-OutputDirectory");
        startInfo.ArgumentList.Add(outputDirectory);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start PowerShell.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output + error);
    }

    private static string GetJsonString(string jsonOutput, string propertyName)
    {
        var jsonStart = jsonOutput.IndexOf('{', StringComparison.Ordinal);
        var jsonEnd = jsonOutput.LastIndexOf('}');
        Assert.True(jsonStart >= 0 && jsonEnd > jsonStart, jsonOutput);

        using var document = JsonDocument.Parse(jsonOutput[jsonStart..(jsonEnd + 1)]);
        return document.RootElement.GetProperty(propertyName).GetString()
            ?? throw new InvalidOperationException($"Missing JSON property: {propertyName}");
    }
}
