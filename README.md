# Hush Little PC

A quiet Windows tray utility that automatically mutes your default playback device after a configurable period of keyboard and mouse inactivity.

Step away from your PC and Hush Little PC quietly mutes the sound. Click the tray icon to turn it on or off.

<a href="https://get.microsoft.com/installer/download/9pdnxtb6mg9l?referrer=appbadge" target="_self">
  <img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200" alt="Get it from Microsoft" />
</a>

![Example notification](docs/notification.png)

## Features

- **Idle muting:** mutes your default playback device after 1 to 1440 minutes (24 hours) without keyboard or mouse input.
- **One-click toggle:** left-click the tray icon to enable or disable. The icon changes to show the current state.
- **Native tray menu:** right-click for Enabled, Mute now, Settings, and Exit.
- **Unmute when I come back (optional):** only unmutes if Hush Little PC muted the device itself.
- **Stay unmuted while audio is playing (optional):** avoids muting while sound is being output.
- **Mute on output change (optional):** mutes the new default device when Windows switches playback devices.
- **Notification with an Unmute button (optional):** a native Windows notification when the app mutes an unmuted device.
- **Start with Windows (optional):** per-user startup registration; no administrator rights required.
- **Native Fluent settings:** WinUI 3 controls, Mica material, adaptive navigation, and system-aware styling.

## Requirements

- Windows 10 version 2004 (build 19041) or later, or Windows 11.
- x64 release builds.

The portable and Microsoft Store distributions include the required .NET and Windows App SDK runtimes. You do not need to install either runtime separately.

## Install

- **Microsoft Store:** install [Hush Little PC from the Microsoft Store](https://apps.microsoft.com/store/detail/9PDNXTB6MG9L?cid=DevShareMCLPCS). Updates are delivered by the Store.
- **Portable:** download `HushLittlePC_<version>_x64-portable.exe` from the [Releases](../../releases) page.

## Build

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download).

Restore and build the complete solution:

```powershell
dotnet restore HushLittlePC.sln
dotnet build HushLittlePC.sln -c Release
dotnet test tests/HushLittlePC.Tests.csproj -c Release
```

Publish the self-contained WinUI portable executable:

```powershell
dotnet publish src/WinUI/HushLittlePC.WinUI.csproj -c Release -r win-x64 --self-contained true `
  -p:WindowsAppSDKSelfContained=true -p:EnableMsixTooling=true `
  -p:IncludeAllContentForSelfExtract=true -p:PublishSingleFile=true
```

The portable executable is created under `src/WinUI/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/`.

The Store MSIX package is assembled by `.github/scripts/package-msix.ps1`. The release workflow runs both packaging paths after CI succeeds on `main`.

## Pre-commit checks

Install [pre-commit](https://pre-commit.com/) and enable the hooks:

```powershell
python -m pip install pre-commit
pre-commit install
```

Before each commit, the hooks check whitespace, final newlines, merge-conflict markers, YAML syntax, and Conventional Commit messages. Run all checks manually with:

```powershell
pre-commit run --all-files
```

## Usage

| Action | Result |
| --- | --- |
| Left-click the tray icon | Enable or disable idle muting |
| Right-click the tray icon | Open the menu (Enabled, Mute now, Settings, Exit) |

### Settings

| Setting | Description |
| --- | --- |
| Mute after *N* minutes | Idle time before muting. The slider and number box cover 1 minute to 24 hours. |
| Enable Hush Little PC | Turns automatic muting on or off. |
| Unmute when I come back | Unmutes on activity, only if Hush Little PC muted the device. |
| Stay unmuted while audio is playing | Skips muting while sound is being output. |
| Mute when the audio output changes | Mutes the new default playback device when Windows switches outputs. |
| Show mute notifications | Shows a Windows notification with an Unmute action. |
| Run at startup | Portable build: uses the per-user Run key. Store build: uses the package StartupTask (manage it under **Settings > Apps > Startup**). |

Settings are saved automatically as you change them to `%AppData%\HushLittlePC\settings.json`. For the Store version, Windows redirects this folder to the package's private storage and removes it when you uninstall.

## How it works

- Idle time comes from the Win32 `GetLastInputInfo` API, checked once per second. Only keyboard and mouse input counts as activity.
- Muting uses the Windows Core Audio API through [NAudio](https://github.com/naudio/NAudio).
- Muting is tracked so the app only unmutes devices it muted itself. The decision logic lives in `HushLittlePC.Core` and is covered by unit tests.
- Output changes are detected through Windows Core Audio device notifications.
- "Audio is playing" is detected from the device's master peak meter.
- Notifications use the [Windows Community Toolkit notifications library](https://learn.microsoft.com/windows/apps/design/shell/tiles-and-notifications/send-local-toast).
- Startup uses the per-user Run registry key for the portable executable and the manifest's StartupTask for the MSIX package.
- The tray icon and menu use H.NotifyIcon.WinUI and a WinUI MenuFlyout; audio and Windows integration remain in the shared Windows services.

## Project layout

| Path | Purpose |
| --- | --- |
| `HushLittlePC.sln` | Solution containing the legacy app, shared libraries, WinUI app, and tests |
| `src/WinUI/` | WinUI 3 application, Fluent Settings/About pages, and window lifecycle |
| `src/Core/` | Shared settings model, persistence, logging, and platform-independent policy |
| `src/Platform/Windows/` | Audio, idle monitoring, device watching, startup, and notifications |
| `src/UI/` | Legacy WinForms UI, tray icon drawing, and native popup menu retained temporarily as a fallback |
| `HushLittlePC.csproj` | Legacy WinForms application project; still buildable but no longer used for releases |
| `tests/` | MSTest coverage for policy, persistence, and Windows integration |
| `packaging/` | MSIX manifest and Store assets |
| `store/`, `docs/`, `resources/` | Store listing art, documentation, test guidance, and notification assets |
| `.github/` | CI, semantic-release, and MSIX packaging automation |

## Diagnostics

Application errors are written to `%LocalAppData%\HushLittlePC\app.log`. The log is capped at approximately 512 KiB; when it reaches the limit, the next write starts a fresh log. Logging is best-effort and will never prevent the app from running.

The About page shows the app/runtime version and can copy basic environment diagnostics to the clipboard.

## Contributing

- Pull request titles must follow [Conventional Commits](https://www.conventionalcommits.org/) because the title becomes the commit message on `main` and drives versioning.
- CI formats and builds the full solution, runs unit tests, smoke-tests both the legacy fallback and WinUI portable app, and creates a test MSIX package.
- When CI succeeds on `main`, [semantic-release](https://semantic-release.gitbook.io/) creates the version tag and GitHub release, attaches the portable executable, and submits the MSIX package to the Microsoft Store.
- Store publishing requires repository variables `MSIX_IDENTITY_NAME`, `MSIX_PUBLISHER`, and `MSIX_PUBLISHER_DISPLAY_NAME`, plus secrets `TENANT_ID`, `SELLER_ID`, `CLIENT_ID`, `SECRET_VALUE`, and `STORE_ID`.
- See [Windows integration testing](docs/windows-integration-testing.md) for automated coverage and the manual hardware/UI acceptance scenarios.

## Uninstall

1. Turn off **Run at startup** in Settings.
2. Right-click the tray icon and choose **Exit**.
3. Delete the portable executable and, if desired, the `%AppData%\HushLittlePC` and `%LocalAppData%\HushLittlePC` folders.

For the Store version, uninstall it from **Settings > Apps > Installed apps**.

## Limitations

- "Mute when the audio output changes" reacts to Windows changing the *default device*. Plugging headphones into a wired 3.5 mm jack usually does not change the default device, so it will not trigger.
- Only the default playback device is muted, not every audio device.
- Idle detection is system-wide input only. Playing media without touching the keyboard or mouse counts as idle unless "Stay unmuted while audio is playing" is enabled.

## License

This project is licensed under the [MIT License](LICENSE).
