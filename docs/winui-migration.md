# WinUI 3 migration plan

## Objective

Move Hush Little PC's primary desktop experience to WinUI 3 so the application uses native Windows UI controls and Fluent design while preserving the existing audio and Windows integration behaviour.

## Architecture decisions

- Keep the application in C# and target supported .NET 10 LTS for this migration.
- Use the stable Windows App SDK 2.6.5 package.
- Keep domain logic and settings persistence in the UI-independent `HushLittlePC.Core` project.
- Keep audio, idle monitoring, device watching, notifications, startup, and tray hosting in `HushLittlePC.Windows`.
- Use WinUI 3 for the Settings/About window and application lifecycle.
- Retain the WinForms project as a buildable fallback until manual Windows acceptance is complete.
- Publish the WinUI application for portable and Microsoft Store releases.

## Migration PR sequence

1. **#22 – Foundation:** WinUI 3 project, native navigation shell, placeholder pages, and Windows build.
2. **#23 – Shared core:** move the existing settings model and policy logic into a reusable core project.
3. **#24 – Shared persistence:** move settings storage and diagnostics into Core.
4. **#25 – Settings page:** native WinUI controls backed by the shared settings model.
5. **#26 – About and diagnostics:** version/runtime details and copyable diagnostics.
6. **#27 – Windows services:** shared audio, idle, device, startup, notification, and tray services.
7. **#28 – Application lifecycle:** connect WinUI to the shared services, tray actions, close-to-tray, and single-instance guard.
8. **#29 – Distribution:** self-contained portable single-file output and MSIX packaging.
9. **#30 – Fluent finishing:** Mica, adaptive navigation, native window sizing, and accessible control names.
10. **#31 – Cutover and documentation:** solution/CI consolidation, release target, and updated user/developer documentation.

## Cutover state

The WinUI application is the intended release target. The legacy WinForms project remains in the solution and continues to build as a fallback, but the release workflow publishes the WinUI application.

The migration is not considered fully accepted until manual Windows testing covers physical audio endpoints, tray interaction, close-to-tray and graceful exit, notifications, startup registration, MSIX activation, Mica/theme behavior, DPI scaling, keyboard navigation, and Narrator. Do not remove the legacy WinForms UI until those checks have been completed successfully.
