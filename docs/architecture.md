# WinUI 3 architecture

Hush Little PC is a WinUI 3 application.

## Layout

The projects and their responsibilities are listed in the [project layout](development.md#project-layout) table of the development guide.

The WinUI application owns the tray icon and application lifecycle. It uses the shared Windows services for audio muting, default-output changes, notifications, and startup registration.

## Launch behaviour

Hush Little PC is a background utility and normally starts in the notification area. `LaunchWindowPolicy` opens the settings window only:

- on the first run, detected by `settings.json` not existing yet in the data directory (defaults are saved immediately, so this happens once; users upgrading from an older version are not treated as first-run because their settings are copied in first);
- when **Open settings on startup** (`ShowSettingsOnStartup`, off by default) is on;
- as a fallback when the tray icon cannot be created.

A launch from a notification button (`-ToastActivated`) never opens the window. Closing the window hides it to the tray; **Exit** in the tray menu quits.

## Build and release

The solution is `HushLittlePC.sln`. CI restores, formats, builds, and tests the complete solution, then publishes and smoke-tests the WinUI standalone executable under both its build name and its release file name. It also assembles a test MSIX package.

The release workflow publishes the WinUI 3 self-contained standalone executable, renames it to `HushLittlePC_<version>_x64.exe`, and builds the MSIX package for Microsoft Store submission.

## Compatibility decisions

- Target framework: .NET 10 with Windows 10 build 19041 as the declared minimum OS.
- Windows App SDK: a stable release (see the package reference in `HushLittlePC.WinUI.csproj`), deployed self-contained.
- Mica is enabled only on Windows 11; Windows 10 uses the default WinUI window background.
- The app's compiled XAML is in a PRI file named `resources.pri` (`ProjectPriFileName`). Unpackaged apps look for `<exe name>.pri` and fall back to `resources.pri`, and packaged apps require `resources.pri`, so this name works for the renamed standalone exe and for MSIX. The MSIX publish must use `EnableMsixTooling=true`, or the package has no app PRI and the window fails to load.
- Standalone startup uses the per-user Run key. Its command line includes `--background`, which is accepted but does not change launch behaviour.
- Packaged startup uses the manifest StartupTask.
- Unpackaged notifications take their group name and icon from the exe's file description (`AssemblyTitle`) and embedded icon (`ApplicationIcon`).
- Settings and logs always live in `%LocalAppData%\HushLittlePC` for the standalone executable. The MSIX package uses the same logical path, redirected by Windows to package-private storage. On first run after upgrading, files from the former `Data` folder beside the executable and legacy settings from `%AppData%\HushLittlePC` are copied into the new location if missing; originals are preserved.
- Unpackaged notifications copy the toast icon to `%LocalAppData%\HushLittlePC\toast-icon.png`.
- The single-instance mutex is named `HushLittlePC_SingleInstance`.

## Release checks

Passing CI verifies compilation, formatting, unit tests, process startup/restart, single-instance enforcement, and MSIX assembly. It does not verify behaviour on a real Windows desktop.

Before a release that changes the UI, packaging, or Windows integration, manually verify audio muting/unmuting, output-device changes, toast activation, tray interactions, first-run, data-folder, and startup behaviour for standalone and MSIX installs, graceful shutdown, DPI scaling, light/dark themes, keyboard navigation, Narrator, and high-contrast mode.

See [Windows integration testing](windows-integration-testing.md) for the test procedure and safety precautions.
