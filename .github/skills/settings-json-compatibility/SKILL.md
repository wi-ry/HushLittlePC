---
name: settings-json-compatibility
description: 'Use when adding or changing HushLittlePC settings, settings.json persistence, defaults, validation, serialization, or compatibility between portable and Microsoft Store installs.'
---

# Settings and JSON Compatibility

Use this workflow to evolve settings without unexpectedly changing existing users' preferences.

## Procedure

1. Read `SettingsModel.cs`, `Settings.cs`, `SettingsForm.cs`, and settings-related tests before editing. `SettingsModel.cs` contains the serializable model and JSON behavior; `Settings.cs` stores it and handles startup registration.
2. For each new setting, define its default, valid range or allowed values, and behavior when the JSON property is absent or invalid. Prefer compatibility with existing settings files.
3. Keep serialization/model behavior independent of Windows APIs where practical so it remains covered by the existing unit-test project.
4. Wire the setting through every relevant surface: model, UI load/edit/save flow, runtime behavior, tests, and README settings documentation. Preserve the unsaved-changes behavior in the settings form.
5. Preserve atomic settings writes and the current per-user application-data location. Do not change portable versus MSIX storage behavior without explicitly tracing package redirection implications.
6. Add tests for default values, valid round trips, boundary clamping or validation, and backward compatibility with JSON that omits newly introduced properties.
7. Run `dotnet test tests/HushLittlePC.Tests.csproj -c Release` and `dotnet build HushLittlePC.csproj -c Release`.

## Review checklist

- Existing JSON continues to deserialize to the intended values.
- Defaults are explicit and tested.
- UI edits are persisted only through the established save flow.
- Invalid values cannot put runtime behavior into an unsafe or nonsensical state.
- README descriptions and settings tables match actual behavior.
