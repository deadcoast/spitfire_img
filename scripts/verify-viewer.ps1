param(
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $repoRoot 'src\ImageViewer.App\ImageViewer.App.csproj'
$executable = Join-Path $repoRoot 'src\ImageViewer.App\bin\x64\Release\net8.0-windows10.0.19041.0\ImageViewer.App.exe'
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot ('TestResults\viewer-ui-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$previousOutput = $env:SPITFIRE_UI_TEST_OUTPUT
$verificationProcess = $null
$verificationFailed = $false
try {
    Get-Process -Name ImageViewer.App -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -eq $executable } | Stop-Process
    & dotnet build $project -p:Platform=x64 -c Release -p:ViewerUiTest=true --nologo -v:q
    if ($LASTEXITCODE -ne 0) { throw 'The rendered verification build failed.' }
    $env:SPITFIRE_UI_TEST_OUTPUT = $OutputDirectory
    $verificationProcess = Start-Process -FilePath $executable -WorkingDirectory (Split-Path $executable) -WindowStyle Hidden -PassThru
    if (-not $verificationProcess.WaitForExit(60000)) { throw 'Rendered verification timed out after 60 seconds.' }
    $results = Get-Content -LiteralPath (Join-Path $OutputDirectory 'results.json') -Raw | ConvertFrom-Json
    $results | Select-Object name, passed | Format-Table -AutoSize
    $failed = @($results | Where-Object { $_.passed -ne $true })
    if ($failed.Count -gt 0) { throw "$($failed.Count) rendered checks failed. See $OutputDirectory" }
    Write-Output "Passed $($results.Count) rendered checks. Evidence: $OutputDirectory"
}
catch {
    $verificationFailed = $true
    Write-Error -Message $_ -ErrorAction Continue
}
finally {
    if ($null -ne $verificationProcess -and -not $verificationProcess.HasExited) {
        Stop-Process -Id $verificationProcess.Id
    }
    $env:SPITFIRE_UI_TEST_OUTPUT = $previousOutput
    # Never leave the opt-in test runner as the executable used for normal launches.
    & dotnet build $project -p:Platform=x64 -c Release -p:ViewerUiTest=false --nologo -v:q
    if ($LASTEXITCODE -ne 0) { $verificationFailed = $true }
}
if ($verificationFailed) { exit 1 }
