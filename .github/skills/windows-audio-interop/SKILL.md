---
name: windows-audio-interop
description: 'Use when changing or debugging HushLittlePC audio behavior, NAudio/Core Audio, default playback-device changes, Win32 P/Invoke, idle detection, or native tray integration.'
---

# Windows Audio and Native Interop

Use this workflow for Windows-specific audio and native API changes in HushLittlePC.

## Procedure

1. Trace the behavior from `TrayApp.cs` to the relevant helper in `Native.cs`, `Notifier.cs`, or `NativeMenu.cs` before editing. Keep policy decisions in `IdleMutePolicy.cs` when they can be expressed without Windows dependencies.
2. Check NAudio and Windows API signatures, ownership rules, threading requirements, and supported OS versions. Match the existing `IDisposable` and `using` patterns for COM-backed audio objects and native handles.
3. Keep default-device lookups fresh so output changes are handled. For device notifications, filter to render / multimedia events and marshal callbacks to the UI thread as the existing watcher does.
4. Preserve mute ownership: only undo a mute HushLittlePC made, and do not claim ownership of a device that was already muted. Keep policy testable without audio hardware.
5. Handle expected device-disconnect or unavailable-device conditions at the integration boundary. Do not silently hide failures in new code; use the repository's existing behavior where it is intentional and report actionable errors for new failure paths.
6. Add or update unit tests for pure decision logic. For COM, P/Invoke, or tray changes, describe the Windows-specific validation needed rather than implying a hardware-dependent test ran.
7. Run `dotnet test tests/HushLittlePC.Tests.csproj -c Release` and build `HushLittlePC.csproj` in Release.

## Review checklist

- Audio and native resources are disposed on all paths.
- COM callbacks do not directly update WinForms controls from a background thread.
- Switching or losing an output device cannot cause an unrelated device to be unmuted.
- P/Invoke declarations use correct character sets, layouts, and handle lifetimes.
- Platform-specific behavior is separated from policy that can be unit tested.
