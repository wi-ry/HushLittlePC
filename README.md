# HushLittlePC

Mutes your default speaker after N minutes without keyboard/mouse input.

## Build
Requires the .NET 8 SDK (https://dotnet.microsoft.com/download).

    dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

The exe ends up in `bin\Release\net8.0-windows\win-x64\publish\HushLittlePC.exe`.
Add `--self-contained true` if you want an exe that runs without the .NET runtime installed (larger file).

## Use
- Left-click the tray icon: enable / disable
- Right-click: Mute now, Settings, Exit
- "Run when Windows starts" adds an entry under HKCU\...\Run (no admin needed)
