---
name: dotnet-development
description: 'Use when implementing, reviewing, or troubleshooting C#/.NET changes in HushLittlePC, including .NET 8, Windows Forms, application lifecycle, project configuration, dependencies, and builds.'
---

# .NET Development for HushLittlePC

Use this workflow for general C# and .NET changes in this Windows desktop application.

## Procedure

1. Inspect the affected code and its callers before editing. Follow existing C# conventions, nullable annotations, implicit usings, and the project's .NET 8 Windows Forms target.
2. Keep Windows-specific behavior at the platform boundary and reusable policy/model logic straightforward to test. Prefer existing helpers and types over duplicate implementations.
3. Preserve resource ownership and lifecycle. Dispose timers, forms, icons, COM objects, and other disposable resources according to the existing ownership patterns.
4. For asynchronous work, consider UI-thread affinity, shutdown, and cancellation. Do not block the WinForms message loop or update controls from background callbacks.
5. When changing a dependency or project setting, verify its effect on both the app and the linked test project. Avoid unnecessary packages and project-wide behavior changes.
6. Update focused tests and directly related README documentation when behavior or user-facing settings change. Use the `dotnet-unit-testing` skill for detailed test workflow and `windows-integration-testing` for Windows shell or hardware behavior.
7. Build and test the smallest relevant scope. For production changes, run `dotnet build HushLittlePC.csproj -c Release` and `dotnet test tests/HushLittlePC.Tests.csproj -c Release`.

## Review checklist

- Nullable warnings and build errors introduced by the change are resolved.
- Existing behavior is preserved except for the explicitly requested change.
- Errors are visible and actionable; new failures are not swallowed or replaced with success-shaped defaults.
- Windows-only behavior is not claimed as validated solely because the project compiled or unit tests passed.
