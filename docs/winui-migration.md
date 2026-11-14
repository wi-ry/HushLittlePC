# WinUI 3 application migration

## Current architecture

Hush Little PC now uses the WinUI 3 application as its primary desktop and release target.

- `src/WinUI` contains the native WinUI 3 application, settings and About pages, window lifecycle, and notification-area integration.
- `src/Core` contains shared settings persistence, application logging, and UI-independent mute policy.
- `src/Platform/Windows` contains Windows-specific audio, idle-input, output-device, notification, and startup integrations.
- The root `HushLittlePC.csproj` remains the WinForms fallback while the WinUI application completes Windows acceptance.
- `tests` contains policy, persistence, startup-registration, and integration smoke tests.

The WinUI application owns the tray icon and application lifecycle. It uses the shared Windows services for audio muting, default-output changes, notifications, and startup registration. Settings are shared between the two application hosts.

## Build and release

The solution is `HushLittlePC.sln`. CI restores, formats, builds, and tests the complete solution, then publishes and smoke-tests both application hosts. It also assembles a test MSIX package.

The release workflow publishes the WinUI 3 self-contained portable executable and builds the MSIX package for Microsoft Store submission. The legacy WinForms project is retained as a buildable compatibility fallback; it is not the release target.

## Compatibility decisions

- Target framework: .NET 10 with Windows 10 build 19041 as the declared minimum OS.
- Windows App SDK: stable 2.6.5.
- Mica is enabled only on Windows 11; Windows 10 uses the default WinUI window background.
- Portable startup uses the per-user Run key and launches with `--background`.
- Packaged startup uses the manifest StartupTask.
- Both application hosts use the same single-instance mutex.

## Acceptance gate

Passing CI verifies compilation, formatting, unit tests, process startup/restart, single-instance enforcement, and MSIX assembly. It does not establish functional parity on a real Windows desktop.

Before the WinUI cutover is considered complete, manually verify audio muting/unmuting, output-device changes, toast activation, tray interactions, startup behaviour for portable and MSIX installs, graceful shutdown, DPI scaling, light/dark themes, keyboard navigation, Narrator, and high-contrast mode.

See [Windows integration testing](windows-integration-testing.md) for the test procedure and safety precautions.
