# WinUI 3 architecture

Hush Little PC is a WinUI 3 application. The original WinForms app has been removed.

## Layout

- `src/WinUI` contains the WinUI 3 application: tray icon and menu, application lifecycle, the settings window (Settings and About pages), and launch behaviour.
- `src/Core` contains settings persistence, application logging, and UI-independent policy (idle muting, mute ownership, the idle-timeout slider scale, and whether the settings window opens at launch).
- `src/Platform/Windows` contains Windows-specific audio, idle-input, output-device, notification, and startup integrations.
- `tests` contains policy, persistence, startup-registration, and integration smoke tests.

The WinUI application owns the tray icon and application lifecycle. It uses the shared Windows services for audio muting, default-output changes, notifications, and startup registration.

## Launch behaviour

Hush Little PC is a background utility and normally starts in the notification area. `LaunchWindowPolicy` opens the settings window only:

- on the first run, detected by `settings.json` not existing yet (defaults are saved immediately, so this happens once; existing users are not treated as first-run);
- when **Open settings on startup** (`ShowSettingsOnStartup`, off by default) is on;
- as a fallback when the tray icon cannot be created.

A launch from a notification button (`-ToastActivated`) never opens the window. Closing the window hides it to the tray; **Exit** in the tray menu quits.

## Build and release

The solution is `HushLittlePC.sln`. CI restores, formats, builds, and tests the complete solution, then publishes and smoke-tests the WinUI portable executable under both its build name and its release file name. It also assembles a test MSIX package.

The release workflow publishes the WinUI 3 self-contained portable executable, renames it to `HushLittlePC_<version>_x64-portable.exe`, and builds the MSIX package for Microsoft Store submission.

## Compatibility decisions

- Target framework: .NET 10 with Windows 10 build 19041 as the declared minimum OS.
- Windows App SDK: stable 2.6.5.
- Mica is enabled only on Windows 11; Windows 10 uses the default WinUI window background.
- The app's compiled XAML is in a PRI file named `resources.pri` (`ProjectPriFileName`). Unpackaged apps look for `<exe name>.pri` and fall back to `resources.pri`, and packaged apps require `resources.pri`, so this name works for the renamed portable exe and for MSIX. The MSIX publish must use `EnableMsixTooling=true`, or the package has no app PRI and the window fails to load.
- Portable startup uses the per-user Run key. Its command line includes `--background`, which is accepted but does not change launch behaviour.
- Packaged startup uses the manifest StartupTask.
- Unpackaged notifications take their group name and icon from the exe's file description (`AssemblyTitle`) and embedded icon (`ApplicationIcon`).
- Settings (`%AppData%\HushLittlePC\settings.json`) and the single-instance mutex name are unchanged from the WinForms app, so existing settings carry over.

## Release checks

Passing CI verifies compilation, formatting, unit tests, process startup/restart, single-instance enforcement, and MSIX assembly. It does not verify behaviour on a real Windows desktop.

Before a release that changes the UI, packaging, or Windows integration, manually verify audio muting/unmuting, output-device changes, toast activation, tray interactions, first-run and startup behaviour for portable and MSIX installs, graceful shutdown, DPI scaling, light/dark themes, keyboard navigation, Narrator, and high-contrast mode.

See [Windows integration testing](windows-integration-testing.md) for the test procedure and safety precautions.
