---
name: windows-integration-testing
description: 'Use when validating HushLittlePC behavior that depends on Windows, including real audio devices, device switching, toast activation, startup registration, tray rendering, single-instance behavior, or MSIX installation.'
---

# Windows Desktop Integration Testing

Use this workflow for behavior that the hardware-independent MSTest suite cannot verify.

## Procedure

1. Identify the changed Windows boundary: Core Audio, device notifications, WinForms/tray, toast activation, startup registration, single-instance handling, or MSIX install/launch.
2. Run the unit suite first with `dotnet test tests/HushLittlePC.Tests.csproj -c Release`. Treat this as policy coverage only; it does not exercise a live Windows audio endpoint or shell integration.
3. Build the application in Release. Use a disposable test account or VM where practical, and record Windows version, package type, and relevant audio-device setup.
4. Exercise only the relevant scenarios. For audio tests, check already-muted and unmuted states and, when applicable, disconnect/reconnect or change the default output. For toast actions, verify activation targets the intended device. For startup, verify both enable and disable paths for the package type under test.
5. Avoid disrupting a user's audio or startup configuration. Record the original state before testing and restore it during cleanup, even when a test fails.
6. Capture reproducible steps and the observed result. If the environment has no suitable audio hardware, shell access, or MSIX identity, mark that scenario as not run rather than treating a unit test or successful build as equivalent coverage.

## Suggested scenario matrix

| Area | Scenarios |
| --- | --- |
| Audio | Default output available/unavailable; manually muted/app-muted; default device changes |
| Toasts | Notification enabled/disabled; Unmute action reaches the intended device |
| Startup | Portable Run key and packaged StartupTask enable/disable behavior |
| Tray | Left-click toggle; right-click menu; light/dark theme rendering |
| Process | Second launch is handled; exit cleans up tray and device notifications |
| Packaging | Install, launch, upgrade, and uninstall the MSIX; launch the portable build |
