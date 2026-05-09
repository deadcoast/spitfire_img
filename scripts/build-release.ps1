$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$solutionPath = Join-Path $repoRoot 'SpitfireImg.sln'
$appProjectPath = Join-Path $repoRoot 'src\ImageViewer.App\ImageViewer.App.csproj'
$artifactsRoot = Join-Path $repoRoot 'artifacts\release'
$version = '0.4.0'
$targetFramework = 'net8.0-windows10.0.19041.0'
$buildOutputDirectory = Join-Path $repoRoot "src\ImageViewer.App\bin\x64\Release\$targetFramework"
$releaseDirectory = Join-Path $artifactsRoot "spitfire.img-$version-win-x64"
$manifestPath = Join-Path $releaseDirectory 'release-manifest.json'

$resolvedRepoRoot = $repoRoot.TrimEnd('\') + '\'
$resolvedArtifactsRoot = [System.IO.Path]::GetFullPath($artifactsRoot).TrimEnd('\') + '\'
$resolvedReleaseDirectory = [System.IO.Path]::GetFullPath($releaseDirectory)

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Missing solution file: $solutionPath"
}

if (-not (Test-Path -LiteralPath $appProjectPath)) {
    throw "Missing app project file: $appProjectPath"
}

if (-not $resolvedArtifactsRoot.StartsWith($resolvedRepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Resolved artifacts root is outside the repository: $resolvedArtifactsRoot"
}

if (-not $resolvedReleaseDirectory.StartsWith($resolvedArtifactsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Resolved release directory is outside the release artifacts root: $resolvedReleaseDirectory"
}

function Assert-ManifestField {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject] $Manifest,

        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    $property = $Manifest.PSObject.Properties[$Name]

    if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value)) {
        throw "Release manifest is missing required field: $Name"
    }
}

function Assert-ManifestVerificationField {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject] $Manifest,

        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    if ($null -eq $Manifest.verification) {
        throw 'Release manifest is missing required verification object.'
    }

    $property = $Manifest.verification.PSObject.Properties[$Name]

    if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value)) {
        throw "Release manifest verification object is missing required field: $Name"
    }
}

function Assert-ReleaseManifest {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject] $Manifest
    )

    foreach ($field in @('artifactName', 'productName', 'version', 'configuration', 'platform', 'commit', 'buildTimeUtc', 'artifactKind')) {
        Assert-ManifestField -Manifest $Manifest -Name $field
    }

    foreach ($field in @('build', 'tests', 'launchSmoke')) {
        Assert-ManifestVerificationField -Manifest $Manifest -Name $field
    }
}

function Copy-ReleaseFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $SourcePath,

        [Parameter(Mandatory = $true)]
        [string] $SourceRoot,

        [Parameter(Mandatory = $true)]
        [string] $DestinationRoot
    )

    if (-not (Test-Path -LiteralPath $SourcePath)) {
        throw "Missing release source file: $SourcePath"
    }

    $resolvedSourceRoot = [System.IO.Path]::GetFullPath($SourceRoot).TrimEnd('\') + '\'
    $resolvedSourcePath = [System.IO.Path]::GetFullPath($SourcePath)

    if (-not $resolvedSourcePath.StartsWith($resolvedSourceRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Release source file is outside the expected source root: $resolvedSourcePath"
    }

    $relativePath = $resolvedSourcePath.Substring($resolvedSourceRoot.Length)
    $destinationPath = Join-Path $DestinationRoot $relativePath
    $destinationDirectory = Split-Path -Parent $destinationPath

    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
    Copy-Item -LiteralPath $resolvedSourcePath -Destination $destinationPath -Force
}

Stop-Process -Name ImageViewer.App -Force -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null

if (Test-Path -LiteralPath $releaseDirectory) {
    Remove-Item -LiteralPath $releaseDirectory -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null

$publishArgs = @(
    'publish',
    $appProjectPath,
    '-c',
    'Release',
    '-p:Platform=x64',
    "-o:$releaseDirectory"
)

& dotnet @publishArgs
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    exit $exitCode
}

if (-not (Test-Path -LiteralPath $buildOutputDirectory)) {
    throw "Missing Release build output directory: $buildOutputDirectory"
}

Copy-ReleaseFile -SourcePath (Join-Path $buildOutputDirectory 'ImageViewer.App.pri') -SourceRoot $buildOutputDirectory -DestinationRoot $releaseDirectory

foreach ($xbf in Get-ChildItem -LiteralPath $buildOutputDirectory -Filter '*.xbf' -Recurse) {
    Copy-ReleaseFile -SourcePath $xbf.FullName -SourceRoot $buildOutputDirectory -DestinationRoot $releaseDirectory
}

$commit = (& git -C $repoRoot rev-parse HEAD).Trim()
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0 -or [string]::IsNullOrWhiteSpace($commit)) {
    throw 'Unable to resolve git commit for release manifest.'
}

$buildTimeUtc = (Get-Date).ToUniversalTime().ToString('o')
$manifest = [pscustomobject]@{
    artifactName = [System.IO.Path]::GetFileName($releaseDirectory)
    productName = 'spitfire.img'
    version = $version
    configuration = 'Release'
    platform = 'x64'
    commit = $commit
    buildTimeUtc = $buildTimeUtc
    artifactKind = 'unpackaged-publish'
    verification = [pscustomobject]@{
        build = 'pending'
        tests = 'pending'
        launchSmoke = 'pending'
    }
}

Assert-ReleaseManifest -Manifest $manifest

$manifestJson = $manifest | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText($manifestPath, $manifestJson + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))

Write-Host "Release artifact written to $releaseDirectory"
Write-Host "Release manifest written to $manifestPath"
