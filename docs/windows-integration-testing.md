# Windows integration testing

The MSTest suite covers idle/mute policy, settings serialization, and settings file persistence through the shared `HushLittlePC.Core` project. Persistence tests use isolated temporary directories and exercise directory creation, saving, overwriting, reloading, missing-file defaults, and malformed-file recovery.

CI builds the legacy WinForms fallback and the WinUI 3 application. It also publishes and launches the WinUI portable executable, checks single-instance enforcement and restart, and creates a test MSIX package. These are process and packaging checks; they do not emulate physical Windows audio endpoints or prove that every shell interaction works.

## Automated Windows validation

The `validate` job in `.github/workflows/ci.yml` restores and builds `HushLittlePC.sln`, verifies formatting, and runs the MSTest suite.

The `smoke-test` job runs on a Windows GitHub Actions runner and performs the following checks:

1. Publish and launch the legacy WinForms fallback, then verify startup, single-instance enforcement, and restart.
2. Assemble a test MSIX package from the WinUI self-contained publish output.
3. Publish the WinUI app as a self-contained single-file executable.
4. Launch the WinUI executable and verify startup, single-instance enforcement, and restart.

The shared smoke-test script is:

```powershell
./tests/Integration/SmokeTest.ps1 -ExecutablePath artifacts/winui/HushLittlePC.WinUI.exe
```

The runner forcibly terminates each test process during cleanup. This is a process-level smoke test, not a test of the tray's Exit menu or graceful shutdown. It does not prove that audio muting, notifications, startup registration, accessibility, or MSIX activation work.

## Settings persistence tests

The MSTest suite references the production `SettingsStore` in `HushLittlePC.Core` and supplies a temporary file path and a test error callback. It verifies:

- Loading a missing file returns default settings without reporting an error.
- Saving creates the parent directory and writes settings that can be loaded again.
- Saving over an existing settings file replaces the previous values.
- Malformed JSON returns defaults and reports the parsing error through the supplied callback.

These tests exercise the actual file I/O implementation without touching the user's real settings file or writing test errors to the user's application log.

Run the solution build and tests:

```powershell
dotnet restore HushLittlePC.sln
dotnet build HushLittlePC.sln -c Release
dotnet test tests/HushLittlePC.Tests.csproj -c Release
```

## Manual Windows acceptance

Use a disposable Windows account or VM where practical. Record the Windows version, app version/commit, package type (portable or MSIX), and relevant audio-device setup with the results.

| Area | Scenarios | Notes |
| --- | --- | --- |
| Audio | Default playback device available/unavailable; manually muted and app-muted states | Confirm the app only unmutes devices it muted itself. |
| Idle behavior | Threshold reached; activity resumes; audio-playing skip enabled/disabled | Use a short idle threshold in a test profile. |
| Output changes | Change the default output; disconnect and reconnect a device | Verify the new default device is the one affected. |
| Notifications | Notifications enabled/disabled; activate the Unmute action | Confirm the action unmutes the intended device. |
| Startup | Enable and disable startup registration; verify sign-in launch stays in the tray without opening the settings window | Test portable Run-key and packaged StartupTask paths separately. |
| Tray UI | Left-click toggle; right-click menu; close-to-tray; Exit | Check icon state, menu actions, and graceful shutdown. |
| Window and Fluent UI | Mica, light/dark mode, compact navigation, minimum window size, 100%/150%/200% scaling | Verify the window remains usable and content is not clipped. |
| Accessibility | Keyboard-only navigation, focus visibility, Narrator names/states, high-contrast mode | Native controls should expose useful labels and values. |
| Process lifecycle | Launch a second instance; exit while monitoring is active | Confirm the second instance exits and resources are released. |
| Packaging | Install, launch, upgrade, and uninstall MSIX; launch portable build | Verify settings and startup behavior for each package type. |

## Test safety and reporting

- Record the original default audio device, mute state, startup setting, and notification configuration before testing.
- Restore the original audio and startup configuration during cleanup, including after a failed test.
- Close the legacy WinForms tray process before launching the WinUI preview. Both hosts share the same single-instance mutex to prevent two audio engines from running simultaneously.
- Do not test against a user's normal startup configuration or disrupt an active audio session.
- If the environment lacks suitable audio hardware, shell access, or an MSIX identity, mark the scenario **Not run**. A passing unit test or successful build is not a substitute.
- For each scenario, record the steps, expected result, observed result, and pass/fail/not-run status.

## Diagnostic log

The app writes best-effort error diagnostics to `%LocalAppData%\HushLittlePC\app.log`. The log is capped at approximately 512 KiB; when the limit is reached, the next write starts a fresh log. Logging failures are intentionally ignored so diagnostics cannot crash or block the tray app.
