# Windows integration testing

The automated MSTest suite covers idle/mute policy, settings serialization, settings file persistence, default-output notification filtering, notification Unmute argument validation, and portable startup registry writes. The startup tests create a unique temporary subkey under the current user's registry hive and remove it during cleanup; they never write to the real Windows Run key. The Windows CI smoke test launches the published portable application and checks process startup, single-instance enforcement, and restart after the first process exits.

These automated checks exercise policy and integration boundaries, not physical Windows audio endpoints or the interactive shell. They do not prove that a real audio endpoint changes mute state, that Windows displays or activates a toast, that the tray menu works, or that MSIX activation and StartupTask registration work.

## Automated Windows smoke test

The \`smoke-test\` job in \`.github/workflows/ci.yml\` runs on a Windows GitHub Actions runner. It publishes the portable application and runs:

\`\`\`powershell
./tests/Integration/SmokeTest.ps1 -ExecutablePath artifacts/smoke/HushLittlePC.exe
\`\`\`

The script verifies that:

1. The first application instance remains running after startup.
2. A second instance exits successfully while the first instance is running.
3. The first instance remains running after the second launch.
4. A new instance can start after the original process has exited.

The runner forcibly terminates each test process during cleanup. This is a process-level smoke test, not a test of the tray's Exit menu or graceful shutdown.

## Automated integration-boundary tests

Run the complete suite on Windows:

\`\`\`powershell
dotnet test tests/HushLittlePC.Tests.csproj -c Release
\`\`\`

The additional tests cover:

- Default-output callback filtering: only render/multimedia notifications with a device ID are accepted.
- Duplicate suppression: repeated notifications for the same device within two seconds are ignored, while a different device or a notification at the two-second boundary is accepted.
- Toast activation validation: only the \`unmute\` action with a non-empty device ID is dispatched.
- Portable startup registry behavior: the exact quoted executable command is written as a string value and removed when startup is disabled or the executable path is unavailable. Tests use a unique temporary HKCU subkey and clean it up; they do not touch the production Run key.

The output-change and toast tests exercise the same pure policy helpers used by the Windows callbacks. They do not emulate Core Audio COM notifications or activate a real Windows toast. Portable startup tests exercise the registry-writing helper with a real registry key, while leaving the production Run key untouched. Packaged StartupTask behavior is not covered.

The settings persistence tests link the production \`SettingsStore\` implementation and use an isolated temporary file path and test error callback. They verify missing-file defaults, directory creation, save/reload, overwrite, and malformed-file recovery without touching the user's real settings file or application log.

Build the Windows application:

\`\`\`powershell
dotnet build HushLittlePC.csproj -c Release
\`\`\`

## Remaining manual scenarios

These scenarios still require a real desktop session, physical or virtual audio endpoint, or MSIX package identity:

| Area | Scenarios | Why manual |
| --- | --- | --- |
| Audio | Verify actual mute/unmute against manually muted and app-muted devices | Requires a real Core Audio endpoint and observable device state |
| Idle behavior | Confirm activity and playback behavior end-to-end | Requires real system idle time and audio playback |
| Output changes | Switch default output; disconnect/reconnect Bluetooth, USB, or HDMI | Requires Windows endpoint changes |
| Notifications | Display a toast and click Unmute | Requires the Windows notification platform and interactive user session |
| Startup | Verify startup after sign-in for portable and MSIX packages | Requires Windows logon and, for MSIX, package identity and StartupTask |
| Tray UI | Left-click, right-click menu, settings window, themes, and Exit | Requires an interactive desktop and shell tray |
| Graceful lifecycle | Exit through the tray while monitoring is active; verify resources are released | Current smoke test terminates processes forcibly |
| Packaging | Install, launch, upgrade, and uninstall MSIX | Requires a valid package and signing/identity setup |

For manual integration testing, use a disposable Windows account or VM where practical. Record the Windows version, app version/commit, package type (portable or MSIX), and relevant audio-device setup with the results.

## Manual validation record

Run the scenarios on an actual Windows environment. Do not mark a scenario as passed based only on a build, unit test, or process smoke test. Use **Not run** when the required Windows feature or hardware is unavailable; do not infer a pass.

Copy this record for each validation session and retain it with the release or pull request being validated:

| Field | Value |
| --- | --- |
| Date and tester | |
| Windows edition and version | |
| App version and commit | |
| Package type | Portable / MSIX |
| Audio devices and connection type | |
| Default playback device at start | |
| Original mute state | |
| Original startup configuration | |
| Notification configuration | |

Record each scenario separately. Include enough detail to reproduce a failure, but do not include personal notification content or other private data.

| Scenario | Preconditions / action | Expected result | Status (Pass / Fail / Not run) | Observed result and evidence |
| --- | --- | --- | --- | --- |
| Audio ownership | Manually mute a device, then let the app mute a different device through its normal policy | The app only unmutes the device it muted; a manually muted device remains muted | | |
| Idle and activity | Use a short idle threshold; allow it to elapse, then resume input | The configured idle action occurs, and activity restores only app-managed mute state | | |
| Playback exception | Repeat the idle scenario with skip-while-playing enabled and disabled | Playback prevents muting only when the setting is enabled | | |
| Default output change | Switch the default render/multimedia output while monitoring | The app responds to the new default and does not act on the previous device by mistake | | |
| Device reconnect | Disconnect and reconnect the active output | Monitoring recovers without acting on an unrelated device or leaving stale state | | |
| Toast action | Trigger a mute notification and activate its Unmute action | The device associated with that notification is unmuted, even if the default output has since changed | | |
| Toast disabled | Disable notifications and trigger the same condition | No toast is shown; mute policy still behaves normally | | |
| Portable startup | Enable startup for a portable build, sign out/restart, then disable it | The app starts when enabled and no longer starts after it is disabled | | |
| MSIX startup | Repeat startup checks with the packaged build | The packaged StartupTask state is handled correctly; any Windows-managed disabled state is reported accurately | | |
| Tray interaction | Exercise left-click, menu actions, theme changes, and Exit | Tray state and menu state stay consistent; Exit closes the app cleanly | | |
| Graceful lifecycle | Exit while monitoring is active, then relaunch | The process exits without a lingering instance and can be started again | | |
| Settings persistence | Change settings, close the app, and relaunch | Settings remain as configured for that package type | | |
| Packaging lifecycle | Install, launch, upgrade, and uninstall the MSIX build; separately launch portable build | Each package launches and follows its expected settings/startup behavior | | |

After testing, restore the original default audio device, mute state, startup configuration, and notification configuration. Note any settings that could not be restored and why.

## Test safety and reporting

- Use a disposable Windows account or VM where practical; do not disrupt an active audio session or a user's normal startup configuration.
- If the environment lacks suitable audio hardware, shell access, or an MSIX identity, mark the scenario **Not run**.
- For each failure, record the steps, expected result, observed result, and relevant non-sensitive diagnostic evidence.

## Diagnostic log

The app writes best-effort error diagnostics to \`%LocalAppData%\\HushLittlePC\\app.log\`. The log is capped at approximately 512 KiB; when the limit is reached, the next write starts a fresh log. Logging failures are intentionally ignored so diagnostics cannot crash or block the tray app.
