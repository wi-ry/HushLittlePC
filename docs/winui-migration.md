# WinUI 3 migration plan

## Objective

Move the Hush Little PC desktop interface to WinUI 3 while preserving its existing audio and Windows integration behaviour.

## Architecture decisions

- Keep the existing WinForms application buildable during migration.
- Introduce WinUI as a separate C# project under `src/WinUI`.
- Retain .NET 8 for the initial migration.
- Use the stable Windows App SDK 2.6.5 package, not preview or experimental builds.
- Extract reusable logic into a UI-independent core before connecting audio, settings, notifications, startup, or tray behaviour.
- Do not make WinUI the default launch target until functional parity and packaging are verified.

## Pull request sequence

1. Foundation: WinUI project, native navigation shell, placeholder Settings/About pages, independent Windows CI.
2. Core extraction: shared settings, persistence, idle-mute policy, and tests.
3. Settings migration: native controls connected to shared settings.
4. About and diagnostics migration.
5. Application lifecycle: single-instance behaviour, tray, notifications, startup, graceful shutdown.
6. Audio-device integration and regression testing.
7. Portable and installer packaging, including Windows App Runtime deployment.
8. Cutover after feature parity, accessibility, DPI, and packaging checks; remove WinForms UI separately.

## Current scope

The foundation does not launch WinUI from the existing tray process and does not duplicate audio or settings behaviour. It is a buildable migration target, not yet a replacement application.
