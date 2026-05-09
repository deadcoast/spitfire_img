# Phase 4 release evidence template

Use this template to record Phase 4 release gate evidence. Store filled evidence and generated validation output under:

```text
TestResults/phase4-evidence/
```

Do not commit filled evidence files, generated screenshots, support bundles, local logs, or release artifacts.

## Build evidence

Command:

```powershell
dotnet build SpitfireImg.sln -p:Platform=x64
```

Required record:

- Date/time:
- Result:
- Warning count:
- Error count:
- Output summary:

## Test evidence

Command:

```powershell
dotnet test SpitfireImg.sln -p:Platform=x64
```

Required record:

- Date/time:
- Result:
- Core test count:
- App test count:
- Failure count:
- Output summary:

## Publish evidence

Command:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1
```

Required record:

- Date/time:
- Result:
- Artifact folder:
- Output summary:

## Release artifact manifest

Manifest path:

```text
artifacts/release/spitfire.img-0.4.0-win-x64/release-manifest.json
```

Required record:

- Product name:
- Version:
- Configuration:
- Platform:
- Commit:
- Artifact kind:
- Manifest validation result:

## Launch smoke evidence

Manual validation must launch the release artifact, not Debug output.

Required record:

- Executable path:
- Window title observed:
- Process stayed running:
- Evidence file:
- Result:

## Startup path with spaces evidence

Required record:

- Executable path:
- Startup image path containing spaces:
- Window title observed:
- Recent file or equivalent open confirmation:
- Evidence file:
- Result:

## Large playlist smoke evidence

Required record:

- Scenario: 1000-image folder playlist smoke.
- Fixture folder:
- Opened image:
- Playlist count observed:
- Navigation checks performed:
- Evidence file:
- Result:

## 4K image smoke evidence

Required record:

- Fixture image path:
- Pixel dimensions:
- Open result:
- Zoom or fit behavior checked:
- Evidence file:
- Result:

## 100-frame GIF smoke evidence

Required record:

- Fixture GIF path:
- Frame count:
- Playback state checked:
- Pause/resume checked:
- Evidence file:
- Result:

## Support bundle evidence

Command:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\collect-support.ps1 -Validation
```

Required record:

- Support bundle folder:
- Support bundle zip:
- Metadata files present:
- Redaction validation result:
- Missing logs behavior result:
- Result:

## Known issues

Record known release issues without claiming unsupported fixes or new product scope.

- Issue:
- Severity:
- Workaround, if any:
- Release decision:

