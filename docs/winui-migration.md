# WinUI 3 migration plan

## Objective

Move the Hush Little PC desktop interface to WinUI 3 so the application uses native Windows UI controls and the Fluent design system, while preserving the existing audio and system integration behaviour.

## Architecture decisions

- Keep the existing WinForms application buildable during the migration.
- Introduce the WinUI application as a separate project under `src/WinUI`.
- Keep the application in C# and retain .NET 8 for the initial migration.
- Use the stable Windows App SDK 2.6.5 package; do not use preview or experimental packages.
- Migrate reusable application logic into a UI-independent core before connecting the new UI to audio, settings, notifications, startup, or tray behaviour.
- Do not make the WinUI shell the default launch target until functional parity and packaging have been verified.

## Pull request sequence

1. **Foundation:** add the WinUI 3 project, native navigation shell, placeholder Settings/About pages, and an independent Windows CI build.
2. **Core extraction:** move settings, persistence, idle-mute policy, and other UI-independent logic into a shared project with existing tests retained.
3. **Settings migration:** connect the WinUI Settings page to the shared settings model and replace placeholders with native controls.
4. **About and diagnostics:** migrate About content and expose useful application/version diagnostics.
5. **Application lifecycle:** integrate single-instance behaviour, system tray, notifications, startup registration, and graceful shutdown.
6. **Windows integration:** verify audio-device monitoring and mute behaviour against the shared core and existing Windows APIs.
7. **Packaging:** produce portable and installer builds, include the Windows App Runtime deployment strategy, and test upgrade/uninstall behaviour.
8. **Cutover:** switch the primary application entry point only after parity, accessibility, DPI, and packaging checks pass; remove the legacy UI in a separate cleanup change.

## Current scope

The foundation PR deliberately does not launch the WinUI app from the existing tray process and does not duplicate audio or settings behaviour. The new shell is a buildable migration target, not yet a replacement application.
