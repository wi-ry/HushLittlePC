# HushLittlePC

A tiny Windows tray app that automatically mutes your speakers after a configurable period of keyboard and mouse inactivity.

Step away from your PC and HushLittlePC quietly mutes the sound. Click the tray icon to turn it on or off.

[![Windows Store - Release Channel](https://img.shields.io/badge/Windows_Store-Download-blue?logo=data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiPz4KPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZlcnNpb249IjIuMCIgdmlld0JveD0iMCAwIDI1NiAyNTYiPgogIDxwYXRoIGQ9Ik0yNTIgNDZDMjUyIDQwLjQ3NzIgMjQ3LjUyMyAzNiAyNDIgMzZIMTRDOC40NzcxNSAzNiA0IDQwLjQ3NzIgNCA0NlYyMTJDNCAyMzQuMDkxIDIxLjkwODYgMjUyIDQ0IDI1MkgyMTJDMjM0LjA5MSAyNTIgMjUyIDIzNC4wOTEgMjUyIDIxMlY0NloiIGZpbGw9InVybCgjcGFpbnQwX2xpbmVhcl8zMjBfMikiPjwvcGF0aD4KICA8cGF0aCBkPSJNMTI0IDkySDc2VjE0MEgxMjRWOTJaIiBmaWxsPSIjRjI1MDIyIj48L3BhdGg+CiAgPHBhdGggZD0iTTE4MCA5MkgxMzJWMTQwSDE4MFY5MloiIGZpbGw9IiM3RkJBMDAiPjwvcGF0aD4KICA8cGF0aCBkPSJNMTI0IDE0OEg3NlYxOTZIMTI0VjE0OFoiIGZpbGw9IiMwMEE0RUYiPjwvcGF0aD4KICA8cGF0aCBkPSJNMTgwIDE0OEgxMzJWMTk2SDE4MFYxNDhaIiBmaWxsPSIjRkZCOTAwIj48L3BhdGg+CiAgPHBhdGggb3BhY2l0eT0iMC44NzI2MjkiIGQ9Ik0xMTUuMzM2IDM2SDIwLjY2MzdDMjAuMjI3MiAzOC42MDE3IDIwIDQxLjI3NDMgMjAgNDRDMjAgNzAuNTA5NyA0MS40OTAzIDkyIDY4IDkyQzk0LjUwOTcgOTIgMTE2IDcwLjUwOTcgMTE2IDQ0QzExNiA0MS4yNzQzIDExNS43NzMgMzguNjAxNyAxMTUuMzM2IDM2WiIgZmlsbD0idXJsKCNwYWludDFfcmFkaWFsXzMyMF8yKSI+PC9wYXRoPgogIDxwYXRoIG9wYWNpdHk9IjAuODcyNjI5IiBkPSJNMjM1LjMzNiAzNkgxNDAuNjY0QzE0MC4yMjcgMzguNjAxNyAxNDAgNDEuMjc0MyAxNDAgNDRDMTQwIDcwLjUwOTcgMTYxLjQ5IDkyIDE4OCA5MkMyMTQuNTEgOTIgMjM2IDcwLjUwOTcgMjM2IDQ0QzIzNiA0MS4yNzQzIDIzNS43NzMgMzguNjAxNyAyMzUuMzM2IDM2WiIgZmlsbD0idXJsKCNwYWludDJfcmFkaWFsXzMyMF8yKSI+PC9wYXRoPgogIDxwYXRoIGQ9Ik02MCAyMEM2MCAxMS4xNjM0IDY3LjE2MzQgNCA3NiA0SDE4MEMxODguODM3IDQgMTk2IDExLjE2MzQgMTk2IDIwVjQ0QzE5NiA0OC40MTgzIDE5Mi40MTggNTIgMTg4IDUyQzE4My41ODIgNTIgMTgwIDQ4LjQxODMgMTgwIDQ0VjIwSDc2VjQ0Qzc2IDQ4LjQxODMgNzIuNDE4MyA1MiA2OCA1MkM2My41ODE3IDUyIDYwIDQ4LjQxODMgNjAgNDRWMjBaIiBmaWxsPSJ1cmwoI3BhaW50M19saW5lYXJfMzIwXzIpIj48L3BhdGg+CiAgPHBhdGggZD0iTTE4MCA0SDc2VjIwSDE4MFY0WiIgZmlsbD0idXJsKCNwYWludDRfbGluZWFyXzMyMF8yKSI+PC9wYXRoPgogIDxkZWZzPgogICAgPGxpbmVhckdyYWRpZW50IGlkPSJwYWludDBfbGluZWFyXzMyMF8yIiB4MT0iMTQ4LjI1NiIgeTE9IjY2Ljk1MzciIHgyPSIxOTAuNjgxIiB5Mj0iMjI1LjU3MiIgZ3JhZGllbnRVbml0cz0idXNlclNwYWNlT25Vc2UiPgogICAgICA8c3RvcCBzdG9wLWNvbG9yPSJ3aGl0ZSI+PC9zdG9wPgogICAgICA8c3RvcCBvZmZzZXQ9IjAuNzE0NzAzIiBzdG9wLWNvbG9yPSIjRUJFQkVCIj48L3N0b3A+CiAgICAgIDxzdG9wIG9mZnNldD0iMSIgc3RvcC1jb2xvcj0iI0RGREZERiI+PC9zdG9wPgogICAgPC9saW5lYXJHcmFkaWVudD4KICAgIDxyYWRpYWxHcmFkaWVudCBpZD0icGFpbnQxX3JhZGlhbF8zMjBfMiIgY3g9IjAiIGN5PSIwIiByPSIxIiBncmFkaWVudFVuaXRzPSJ1c2VyU3BhY2VPblVzZSIgZ3JhZGllbnRUcmFuc2Zvcm09InRyYW5zbGF0ZSg2Ny44Mzg0IDQzLjkzNTMpIHJvdGF0ZSgxMzQuODkxKSBzY2FsZSgzMC43MDk4IDMyLjI5MjIpIj4KICAgICAgPHN0b3Agc3RvcC1jb2xvcj0iI0JGQkZCRiI+PC9zdG9wPgogICAgICA8c3RvcCBvZmZzZXQ9IjEiIHN0b3AtY29sb3I9IndoaXRlIiBzdG9wLW9wYWNpdHk9IjAiPjwvc3RvcD4KICAgIDwvcmFkaWFsR3JhZGllbnQ+CiAgICA8cmFkaWFsR3JhZGllbnQgaWQ9InBhaW50Ml9yYWRpYWxfMzIwXzIiIGN4PSIwIiBjeT0iMCIgcj0iMSIgZ3JhZGllbnRVbml0cz0idXNlclNwYWNlT25Vc2UiIGdyYWRpZW50VHJhbnNmb3JtPSJ0cmFuc2xhdGUoMTg3LjgzOCA0My45MzUzKSByb3RhdGUoMTM0Ljg5MSkgc2NhbGUoMzAuNzA5OCAzMi4yOTIyKSI+CiAgICAgIDxzdG9wIHN0b3AtY29sb3I9IiNCRkJGQkYiPjwvc3RvcD4KICAgICAgPHN0b3Agb2Zmc2V0PSIxIiBzdG9wLWNvbG9yPSJ3aGl0ZSIgc3RvcC1vcGFjaXR5PSIwIj48L3N0b3A+CiAgICA8L3JhZGlhbEdyYWRpZW50PgogICAgPGxpbmVhckdyYWRpZW50IGlkPSJwYWludDNfbGluZWFyXzMyMF8yIiB4MT0iMTg3LjQxNyIgeTE9IjExLjIwMTEiIHgyPSIxODcuNDE3IiB5Mj0iNDcuNTAyIiBncmFkaWVudFVuaXRzPSJ1c2VyU3BhY2VPblVzZSI+CiAgICAgIDxzdG9wIHN0b3AtY29sb3I9IiMzMERBRkYiPjwvc3RvcD4KICAgICAgPHN0b3Agb2Zmc2V0PSIxIiBzdG9wLWNvbG9yPSIjMDE5NEQ0Ij48L3N0b3A+CiAgICA8L2xpbmVhckdyYWRpZW50PgogICAgPGxpbmVhckdyYWRpZW50IGlkPSJwYWludDRfbGluZWFyXzMyMF8yIiB4MT0iMTI5LjExNSIgeTE9IjYuNTM5MzMiIHgyPSIxMjkuMTE1IiB5Mj0iMTguMDc2OCIgZ3JhZGllbnRVbml0cz0idXNlclNwYWNlT25Vc2UiPgogICAgICA8c3RvcCBzdG9wLWNvbG9yPSIjMUZCOEZEIj48L3N0b3A+CiAgICAgIDxzdG9wIG9mZnNldD0iMSIgc3RvcC1jb2xvcj0iIzA1OTFGMiI+PC9zdG9wPgogICAgPC9saW5lYXJHcmFkaWVudD4KICA8L2RlZnM+Cjwvc3ZnPgo=)](https://apps.microsoft.com/store/detail/9PDNXTB6MG9L?cid=DevShareMCLPCS)

![Settings](docs/settings.png)
![Example notification](docs/notification.png)

## Features

- **Idle muting:** mutes your default playback device after 1 to 1440 minutes without keyboard or mouse input.
- **One-click toggle:** left-click the tray icon to enable or disable. The icon changes colour to show the current state.
- **Native tray menu:** right-click for Enabled, Mute now, Settings and Exit. It uses the standard Windows menu, so it matches the Windows 11 look and follows light/dark mode.
- **Unmute when I come back (optional):** only unmutes if HushLittlePC did the muting, so a mute you set yourself is never undone.
- **Stay unmuted while audio is playing (optional):** handy for watching videos or listening to music without touching the PC.
- **Mute on output change (optional):** mutes the new device when Windows switches the default playback device, for example when Bluetooth headphones connect or you switch to speakers.
- **Notification with an Unmute button (optional):** a native Windows notification when HushLittlePC changes an unmuted device to muted, so you can undo it with one click. Already-muted devices do not trigger a notification.
- **Start with Windows (optional):** per-user startup entry, no admin rights needed.
- **Follows your output device:** switching to headphones, HDMI or Bluetooth just works.

## Requirements

- Windows 10 or 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (not needed if you publish self-contained, see below)

## Install

- **Microsoft Store:** link coming once the first submission is live.
- **Portable:** download `HushLittlePC_<version>_x64-portable.exe` from the [Releases](../../releases) page (needs the .NET 8 Desktop Runtime).

## Build

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

The exe is created at `bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\HushLittlePC.exe`.

To produce an exe that runs without the .NET runtime installed (larger file), use `--self-contained true` instead.

## Pre-commit checks

Install [pre-commit](https://pre-commit.com/) and enable the hooks:

```
python -m pip install pre-commit
pre-commit install
```

Before each commit, the hooks check whitespace, final newlines, merge-conflict markers and YAML syntax. Changes to C# files or the project file also trigger a Release build. Run all checks manually with `pre-commit run --all-files`.

Run the core unit tests directly with `dotnet test tests/HushLittlePC.Tests.csproj -c Release`. They cover idle-threshold muting, manually muted and app-muted devices, return-from-idle unmuting, skipping active playback, output-change gating, notification eligibility, mute transitions, enabled-state toggling, and settings defaults/serialization. Windows-specific audio hardware, toast delivery, startup registration, tray rendering, and single-instance behavior still require Windows integration testing.

## Usage

| Action | Result |
| --- | --- |
| Left-click the tray icon | Enable or disable idle muting |
| Right-click the tray icon | Open the menu (Enabled, Mute now, Settings, Exit) |

### Settings

| Setting | Description |
| --- | --- |
| Mute after *N* minutes | Idle time before muting. Slider goes up to 2 hours; type up to 24 hours. |
| Enable HushLittlePC | Turns all muting on or off. Same as clicking the tray icon. |
| Unmute when I come back | Unmutes on activity, only if HushLittlePC muted it. |
| Stay unmuted while audio is playing | Skips muting while sound is being output. |
| Mute when the audio output changes | Mutes the new default playback device whenever Windows switches outputs. |
| Show a notification when audio is muted | Windows notification with an Unmute button each time audio is muted. |
| Start HushLittlePC with Windows | Adds an entry under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. |

Settings are saved with **Save changes**, which is enabled only after an option changes. Closing with unsaved edits asks for confirmation before discarding them.

Settings are saved to `%AppData%\HushLittlePC\settings.json`.

## How it works

- Idle time comes from the Win32 `GetLastInputInfo` API, checked once per second. Only keyboard and mouse input counts as activity.
- Notifications use the [Windows Community Toolkit notifications library](https://learn.microsoft.com/windows/apps/design/shell/tiles-and-notifications/send-local-toast) (`Microsoft.Toolkit.Uwp.Notifications`).
- Muting uses the Windows Core Audio API (through [NAudio](https://github.com/naudio/NAudio)) on the current default playback device.
- Output changes are detected through the Windows Core Audio device notifications (the default playback device changing).
- "Audio is playing" is detected from the device's master peak meter.
- The tray menu is a native Win32 popup menu. Light/dark theming uses undocumented `uxtheme.dll` calls; on versions of Windows that lack them, the menu simply stays light.

## Project layout

| File | Purpose |
| --- | --- |
| `Program.cs` | Entry point and single-instance check |
| `TrayApp.cs` | Tray icon, idle-check timer, mute logic |
| `NativeMenu.cs` | Native Win32 tray menu |
| `SettingsForm.cs` | Settings window |
| `Settings.cs` | JSON settings and run-on-startup registry entry |
| `Native.cs` | Idle-time and audio-device helpers |

## Uninstall

1. Untick **Start HushLittlePC with Windows** in Settings.
2. Right-click the tray icon and choose **Exit**.
3. Delete the exe and, if you like, the `%AppData%\HushLittlePC` folder.

## Limitations

- "Mute when the audio output changes" reacts to Windows changing the *default device* (Bluetooth, USB, HDMI, or picking another output). Plugging headphones into a wired 3.5 mm jack usually doesn't change the default device, so it won't trigger.
- Only the default playback device is muted, not every audio device.
- Idle detection is system-wide input only. Playing media without touching the keyboard or mouse counts as idle unless "Stay unmuted while audio is playing" is on.

## License

Add a license of your choice (for example MIT) before publishing.
