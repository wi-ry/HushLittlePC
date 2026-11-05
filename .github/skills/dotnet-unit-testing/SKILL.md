---
name: dotnet-unit-testing
description: 'Use when adding, changing, or troubleshooting HushLittlePC .NET unit tests, MSTest cases, idle-mute policy, mute transitions, settings defaults, limits, or JSON serialization.'
---

# .NET Unit Testing

Use the existing MSTest project to verify HushLittlePC's hardware-independent behavior.

## Procedure

1. Read `tests/HushLittlePC.Tests.csproj` to see which production files are linked into the test assembly. The current suite tests `MuteTransition.cs`, `IdleMutePolicy.cs`, and `SettingsModel.cs`; Windows UI, `SettingsStore`, audio devices, and startup registration are not covered by this project.
2. Put new tests under `tests/` and follow the existing MSTest style. Prefer testing decisions and state transitions with simple inputs over introducing audio hardware or UI dependencies.
3. Cover relevant boundary cases, including already-muted devices, user-owned versus app-owned mute state, threshold boundaries, disabled settings, invalid idle-minute values, and missing or malformed serialized values where applicable.
4. If the behavior is difficult to test without Windows, isolate a small pure policy/helper where that improves the design. Avoid broad refactors solely to make a test pass.
5. Run `dotnet test tests/HushLittlePC.Tests.csproj -c Release`. If production code or the project file changed, also run `dotnet build HushLittlePC.csproj -c Release`.
6. Report test results and clearly distinguish automated unit coverage from Windows-only integration checks.

## Conventions

- Use descriptive test names in `Method_Condition_ExpectedResult` form.
- Assert the observable result and relevant side effects, such as whether a mute setter was called.
- Keep each test focused on one behavior and avoid dependence on execution order or real devices.
