# Windows integration testing

The automated MSTest suite covers idle/mute policy, settings serialization, and settings file persistence. Persistence tests use isolated temporary directories and exercise directory creation, saving, overwriting, reloading, missing-file defaults, and malformed-file recovery. The Windows CI smoke test additionally launches the published portable application and checks process startup, single-instance enforcement, and restart after the first process exits. None of these tests emulates Windows audio endpoints, the notification system, the registry, the shell tray, or MSIX activation.

## Automated Windows smoke test

The `smoke-test` job in `.github/workflows/ci.yml` runs on a Windows GitHub Actions runner. It publishes the portable application and runs:

```powershell
./tests/Integration/SmokeTest.ps1 -ExecutablePath artifacts/smoke/HushLittlePC.exe
```

The script verifies that:

1. The first application instance remains running after startup.
2. A second instance exits successfully while the first instance is running.
3. The first instance remains running after the second launch.
4. A new instance can start after the original process has exited.

The runner forcibly terminates each test process during cleanup. This is a process-level smoke test, not a test of the tray's Exit menu or graceful shutdown. It does not prove that audio muting, notifications, startup registration, settings UI, or MSIX activation work.

## Settings persistence tests

The MSTest suite links the production `SettingsStore` implementation and supplies a temporary file path and a test error callback. It verifies:

- Loading a missing file returns default settings without reporting an error.
- Saving creates the parent directory and writes settings that can be loaded again.
- Saving over an existing settings file replaces the previous values.
- Malformed JSON returns defaults and reports the parsing error through the supplied callback.

These tests exercise the actual file I/O code without touching the user's real settings file or writing test errors to the user's application log.

Run the core unit and persistence tests:

```powershell
dotnet test tests/HushLittlePC.Tests.csproj -c Release
```

Build the Windows application:

```powershell
dotnet build HushLittlePC.csproj -c Release
```

For manual integration testing, use a disposable Windows account or VM where practical. Record the Windows version, app version/commit, package type (portable or MSIX), and relevant audio-device setup with the results.

## Scenario checklist

| Area | Scenarios | Notes |
| --- | --- | --- |
| Audio | Default playback device available and unavailable; manually muted and app-muted states | Confirm the app only unmutes devices it muted itself. |
| Idle behavior | Threshold reached; activity resumes; audio-playing skip enabled/disabled | Use a short idle threshold in a test profile. |
| Output changes | Change the default output; disconnect and reconnect a device | Verify the new default device is the one affected. |
| Notifications | Notifications enabled/disabled; activate the Unmute action | Confirm the action unmutes the intended device. |
| Startup | Enable and disable startup registration | Test the portable Run-key path and packaged StartupTask path separately. |
| Tray UI | Left-click toggle; right-click menu; light and dark themes | Check icon state, menu actions, and clean exit. |
| Process lifecycle | Launch a second instance; exit while monitoring is active | Confirm the second instance exits and resources are released. |
| Packaging | Install, launch, upgrade, and uninstall MSIX; launch portable build | Verify settings behavior for each package type. |

## Test safety and reporting

- Record the original default audio device, mute state, startup setting, and notification configuration before testing.
- Restore the original audio and startup configuration during cleanup, including after a failed test.
- Do not test against a user's normal startup configuration or disrupt an active audio session.
- If the environment lacks suitable audio hardware, shell access, or an MSIX identity, mark the scenario **Not run**. A passing unit test or successful build is not a substitute.
- For each scenario, record the steps, expected result, observed result, and pass/fail/not-run status.

## Diagnostic log

The app writes best-effort error diagnostics to `%LocalAppData%\HushLittlePC\app.log`. The log is capped at approximately 512 KiB; when the limit is reached, the next write starts a fresh log. Logging failures are intentionally ignored so diagnostics cannot crash or block the tray app.
