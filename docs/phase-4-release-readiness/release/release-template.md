# spitfire.img release record

## Version

`0.4.0`

## Date

`YYYY-MM-DD`

## Commit

`<commit sha>`

## Artifact names

- `spitfire.img-0.4.0-win-x64`
- `release-manifest.json`
- Support bundle: `<support bundle zip>`

## Verification commands and results

Record the command output summary and pass/fail result for each command.

```powershell
Stop-Process -Name ImageViewer.App -Force -ErrorAction SilentlyContinue
dotnet build SpitfireImg.sln -p:Platform=x64
dotnet test SpitfireImg.sln -p:Platform=x64
powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1
powershell -ExecutionPolicy Bypass -File scripts\collect-support.ps1 -Validation
git diff --check
```

| Command | Result | Notes |
| --- | --- | --- |
| `dotnet build SpitfireImg.sln -p:Platform=x64` | Pending |  |
| `dotnet test SpitfireImg.sln -p:Platform=x64` | Pending |  |
| `powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1` | Pending |  |
| `powershell -ExecutionPolicy Bypass -File scripts\collect-support.ps1 -Validation` | Pending |  |
| `git diff --check` | Pending |  |

## Manual evidence links

Link evidence files under `TestResults/phase4-evidence/`.

- Release manifest:
- Direct launch smoke:
- Startup path with spaces:
- Upgrade safety:
- Support bundle:
- 1000-image playlist smoke:
- 4K image smoke:
- 100-frame GIF smoke:

## Known issues

- None recorded.

