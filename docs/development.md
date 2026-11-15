# Development guide

This guide is for people who want to build, test, or contribute to Hush Little PC. For installation and everyday usage, see the [README](../README.md).

## How it works

- Idle time comes from the Win32 `GetLastInputInfo` API, checked once per second. Only keyboard and mouse input counts as activity.
- Muting uses the Windows Core Audio API through [NAudio](https://github.com/naudio/NAudio).
- Muting is tracked so the app only unmutes devices it muted itself. The decision logic lives in `HushLittlePC.Core` and is covered by unit tests.
- Output changes are detected through Windows Core Audio device notifications.
- "Audio is playing" is detected from the device's master peak meter.
- Notifications use the [Windows Community Toolkit notifications library](https://learn.microsoft.com/windows/apps/design/shell/tiles-and-notifications/send-local-toast).
- Startup uses the per-user Run registry key for the portable executable and the manifest's StartupTask for the MSIX package.
- `AppDataPaths` picks the data directory for `settings.json` and the logs. Portable builds use a `Data` folder beside the executable when it is writable; otherwise (including the MSIX package) the app uses `%LocalAppData%\HushLittlePC`. On first run, existing settings from `%AppData%\HushLittlePC` and logs from `%LocalAppData%\HushLittlePC` are copied in if missing.
- The settings window uses the Windows Community Toolkit `SettingsCard` controls, with Mica on Windows 11 only.
- The tray icon and menu use H.NotifyIcon.WinUI and a WinUI MenuFlyout; audio and Windows integration remain in the shared Windows services.

## Project layout

| Path | Purpose |
| --- | --- |
| `HushLittlePC.sln` | Solution containing the WinUI app, shared libraries, and tests |
| `src/WinUI/` | WinUI 3 application, Fluent Settings/About pages, and window lifecycle |
| `src/Core/` | Shared settings model, persistence, logging, and platform-independent policy |
| `src/Platform/Windows/` | Audio, idle monitoring, device watching, startup, and notifications |
| `tests/` | MSTest coverage for policy, persistence, and Windows integration, plus the CI smoke test |
| `packaging/` | MSIX manifest, app icon, and Store assets |
| `store/`, `docs/`, `resources/` | Store listing art, documentation, test guidance, and notification assets |
| `.github/` | CI, CodeQL, semantic-release, MSIX packaging, wiki sync, Dependabot, and contributor skills (`skills/`) |

## Build

Building requires Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download). The app targets `net10.0-windows10.0.19041.0`.

Restore and build the complete solution:

```powershell
dotnet restore HushLittlePC.sln
dotnet build HushLittlePC.sln -c Release
dotnet test tests/HushLittlePC.Tests.csproj -c Release
```

CI also fails on formatting changes and on warnings. To check the same things locally:

```powershell
dotnet format HushLittlePC.sln --verify-no-changes
dotnet build HushLittlePC.sln -c Release -warnaserror
```

### Run the WinUI 3 application locally

Run the WinUI 3 application from source:

```powershell
dotnet run --project src/WinUI/HushLittlePC.WinUI.csproj
```

In Visual Studio, set **HushLittlePC.WinUI** as the startup project.

### Publish the portable executable

Publish the self-contained WinUI portable executable:

```powershell
dotnet publish src/WinUI/HushLittlePC.WinUI.csproj -c Release -r win-x64 --self-contained true `
  -p:WindowsAppSDKSelfContained=true -p:EnableMsixTooling=true `
  -p:IncludeAllContentForSelfExtract=true -p:PublishSingleFile=true
```

The portable executable (`HushLittlePC.WinUI.exe`) is created under `src/WinUI/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/`. The release workflow renames it to `HushLittlePC_<version>_x64-portable.exe`.

The project sets `ProjectPriFileName` to `resources.pri` so the compiled XAML still loads after that rename, and `EnableMsixTooling=true` is required for the MSIX package to include it. See [WinUI 3 architecture](architecture.md#compatibility-decisions) before changing either.

### Package for the Microsoft Store

The Store MSIX package is assembled by `.github/scripts/package-msix.ps1`. The release workflow runs both packaging paths after CI succeeds on `main`.

## Pre-commit checks

Install [pre-commit](https://pre-commit.com/) and enable the hooks:

```powershell
python -m pip install pre-commit
pre-commit install
```

Before each commit, the hooks check whitespace, final newlines, merge-conflict markers, YAML syntax, and Conventional Commit messages. When you change `.cs` or `.csproj` files, they also build the solution and run the unit tests in Release mode, so the .NET SDK must be installed. Run all checks manually with:

```powershell
pre-commit run --all-files
```

## Contributing

- Pull request titles must follow [Conventional Commits](https://www.conventionalcommits.org/) because the title becomes the commit message on `main` and drives versioning. The individual commits in a pull request are linted against the same rules.
- CI verifies formatting, builds the full solution with warnings as errors, runs unit tests, smoke-tests the WinUI portable app (including under the release file name), and creates a test MSIX package.
- When CI succeeds on `main`, [semantic-release](https://semantic-release.gitbook.io/) creates the version tag and GitHub release, attaches the portable executable, and submits the MSIX package to the Microsoft Store. If the optional `WINGET_CREATE_GITHUB_TOKEN` secret is set, it also opens a WinGet manifest update for the new portable executable.
- Store publishing requires repository variables `MSIX_IDENTITY_NAME`, `MSIX_PUBLISHER`, and `MSIX_PUBLISHER_DISPLAY_NAME`, plus secrets `TENANT_ID`, `SELLER_ID`, `CLIENT_ID`, `SECRET_VALUE`, and `STORE_ID`.
- CodeQL analyzes the C# code on pull requests, pushes to `main`, and weekly. Dependabot proposes NuGet and GitHub Actions updates weekly.
- See [Windows integration testing](windows-integration-testing.md) for automated coverage and the manual hardware/UI acceptance scenarios, and [WinUI 3 architecture](architecture.md) for launch behavior and compatibility decisions.

### Documentation and the wiki

When `README.md` or `docs/` changes on `main`, the sync-wiki workflow publishes the README as the GitHub wiki's Home page (it needs a `WIKI_TOKEN` secret). `.github/scripts/sync-wiki.py` rewrites only these relative links so they work from the wiki: `../../releases`, `LICENSE`, and paths under `docs/`. If you add a README link of another form, update the script too.

## Diagnostics

The app writes `winui-app.log` and `app.log` to its data directory (see [How it works](#how-it-works) and the [README](../README.md#troubleshooting)). When running from source, the data directory is a `Data` folder beside the built executable. Check `winui-app.log` first for startup problems.
