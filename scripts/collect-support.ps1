param(
    [switch] $Validation,
    [string] $LocalDataFolder,
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$defaultLocalDataFolder = Join-Path $env:LOCALAPPDATA 'spitfire.img'
$localDataFolder = if ([string]::IsNullOrWhiteSpace($LocalDataFolder)) {
    $defaultLocalDataFolder
}
else {
    [System.IO.Path]::GetFullPath($LocalDataFolder)
}
$logsDirectory = Join-Path $localDataFolder 'logs'
$releaseArtifactsRoot = Join-Path $repoRoot 'artifacts\release'
$validationOutputDirectory = Join-Path $repoRoot 'TestResults\phase4-evidence\support-bundle'
$userOutputDirectory = Join-Path $localDataFolder 'support'
$userOutputDescription = '%LOCALAPPDATA%\spitfire.img\support'
$timestamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
$bundleName = "spitfire-support-$timestamp"

$excludedFiles = @(
    'recent-files.json',
    'settings.json'
)

$excludedImagePatterns = @(
    '*.png',
    '*.jpg',
    '*.jpeg',
    '*.bmp',
    '*.webp',
    '*.gif',
    '*.tif',
    '*.tiff',
    '*.heic',
    '*.avif'
)

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = if ($Validation) {
        $validationOutputDirectory
    }
    else {
        $userOutputDirectory
    }
}

$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$stagingDirectory = Join-Path $resolvedOutputDirectory $bundleName
$bundlePath = Join-Path $resolvedOutputDirectory "$bundleName.zip"

function Write-TextFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $Content
    )

    $directory = Split-Path -Parent $Path
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    [System.IO.File]::WriteAllText($Path, $Content + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [object] $Value,

        [int] $Depth = 6
    )

    Write-TextFile -Path $Path -Content ($Value | ConvertTo-Json -Depth $Depth)
}

function Invoke-OptionalTextCommand {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock] $Command
    )

    try {
        return (& $Command | Out-String).TrimEnd()
    }
    catch {
        return "Unavailable: $($_.Exception.GetType().Name)"
    }
}

function Redact-LogContent {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $Content
    )

    $redacted = $Content -replace '(?im)^(File:\s+).+$', '$1[redacted path]'
    $redacted = $redacted -replace '(?i)([A-Z]:\\Users\\)[^\\\r\n]+', '$1[redacted user]'
    return $redacted
}

function Get-LatestReleaseManifestPath {
    if (-not (Test-Path -LiteralPath $releaseArtifactsRoot)) {
        return $null
    }

    return Get-ChildItem -LiteralPath $releaseArtifactsRoot -Filter 'release-manifest.json' -Recurse -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

function Get-PackageState {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PackageName
    )

    try {
        $package = Get-AppxPackage -Name $PackageName -ErrorAction Stop | Select-Object -First 1
        if ($null -eq $package) {
            return [pscustomobject]@{
                packageName = $PackageName
                installed = $false
                version = $null
            }
        }

        return [pscustomobject]@{
            packageName = $PackageName
            installed = $true
            version = [string] $package.Version
        }
    }
    catch {
        return [pscustomobject]@{
            packageName = $PackageName
            installed = $false
            version = $null
            error = $_.Exception.GetType().Name
        }
    }
}

New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory | Out-Null
if (Test-Path -LiteralPath $stagingDirectory) {
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $stagingDirectory | Out-Null

$releaseManifestPath = Get-LatestReleaseManifestPath
$releaseManifest = $null
if ($releaseManifestPath) {
    Copy-Item -LiteralPath $releaseManifestPath -Destination (Join-Path $stagingDirectory 'release-manifest.json') -Force
    $releaseManifest = Get-Content -LiteralPath $releaseManifestPath -Raw | ConvertFrom-Json
}

$appVersion = if ($null -ne $releaseManifest -and $releaseManifest.PSObject.Properties['version']) {
    [string] $releaseManifest.version
}
else {
    'unknown'
}

Write-JsonFile -Path (Join-Path $stagingDirectory 'app-version.json') -Value ([pscustomobject]@{
    productName = 'spitfire.img'
    version = $appVersion
    releaseManifestIncluded = $null -ne $releaseManifestPath
    releaseArtifactName = if ($null -ne $releaseManifest -and $releaseManifest.PSObject.Properties['artifactName']) {
        [string] $releaseManifest.artifactName
    }
    else {
        $null
    }
})

$os = Get-CimInstance Win32_OperatingSystem
Write-JsonFile -Path (Join-Path $stagingDirectory 'system-info.json') -Value ([pscustomobject]@{
    windowsCaption = $os.Caption
    windowsVersion = $os.Version
    windowsBuildNumber = $os.BuildNumber
    osArchitecture = [string] [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    processArchitecture = [string] [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
})

Write-TextFile -Path (Join-Path $stagingDirectory 'dotnet-info.txt') -Content (Invoke-OptionalTextCommand { dotnet --info })
Write-TextFile -Path (Join-Path $stagingDirectory 'windows-app-runtime-packages.txt') -Content (Invoke-OptionalTextCommand {
    Get-AppxPackage -Name 'Microsoft.WindowsAppRuntime*' |
        Select-Object Name, Version, Architecture, PackageFullName |
        Format-Table -AutoSize
})

Write-JsonFile -Path (Join-Path $stagingDirectory 'codec-availability.json') -Value ([pscustomobject]@{
    HEIC = Get-PackageState -PackageName 'Microsoft.HEIFImageExtension'
    AVIF = Get-PackageState -PackageName 'Microsoft.AV1VideoExtension'
})

$bundleManifest = [pscustomobject]@{
    createdAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    validationMode = [bool] $Validation
    outputDirectory = if ($Validation) { 'TestResults\phase4-evidence\support-bundle' } else { $userOutputDescription }
    defaultUserOutputDirectory = $userOutputDescription
    localDataFolder = '%LOCALAPPDATA%\spitfire.img'
    included = @(
        'app-version.json',
        'system-info.json',
        'dotnet-info.txt',
        'windows-app-runtime-packages.txt',
        'codec-availability.json',
        'release-manifest.json when available',
        'redacted structured logs when available'
    )
    excludedFiles = $excludedFiles
    excludedImagePatterns = $excludedImagePatterns
    excludesUserFolderListings = $true
}
Write-JsonFile -Path (Join-Path $stagingDirectory 'support-bundle-manifest.json') -Value $bundleManifest

$redactedLogsDirectory = Join-Path $stagingDirectory 'logs'
if (Test-Path -LiteralPath $logsDirectory) {
    $logFiles = Get-ChildItem -LiteralPath $logsDirectory -Filter '*.log' -File
    if ($logFiles.Count -eq 0) {
        Write-TextFile -Path (Join-Path $redactedLogsDirectory 'no-logs.txt') -Content 'No structured logs were found.'
    }
    else {
        foreach ($logFile in $logFiles) {
            $content = Get-Content -LiteralPath $logFile.FullName -Raw
            Write-TextFile -Path (Join-Path $redactedLogsDirectory $logFile.Name) -Content (Redact-LogContent -Content $content)
        }
    }
}
else {
    Write-TextFile -Path (Join-Path $redactedLogsDirectory 'no-logs.txt') -Content 'The logs directory does not exist.'
}

foreach ($excludedFile in $excludedFiles) {
    $unexpectedPath = Join-Path $stagingDirectory $excludedFile
    if (Test-Path -LiteralPath $unexpectedPath) {
        throw "Support bundle staging unexpectedly contains excluded file: $excludedFile"
    }
}

foreach ($pattern in $excludedImagePatterns) {
    $unexpectedImages = Get-ChildItem -LiteralPath $stagingDirectory -Filter $pattern -Recurse -File
    if ($unexpectedImages.Count -gt 0) {
        throw "Support bundle staging unexpectedly contains image files matching $pattern"
    }
}

if (Test-Path -LiteralPath $bundlePath) {
    Remove-Item -LiteralPath $bundlePath -Force
}

Compress-Archive -Path (Join-Path $stagingDirectory '*') -DestinationPath $bundlePath -Force

[pscustomobject]@{
    supportBundle = $bundlePath
    stagingDirectory = $stagingDirectory
    validationMode = [bool] $Validation
} | ConvertTo-Json -Depth 4
