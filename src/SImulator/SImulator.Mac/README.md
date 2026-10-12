# SImulator for macOS (preview)

Native macOS version of SImulator built with [Avalonia](https://avaloniaui.net/).
Version 0.1 is a preview: the main game flow works, some features of the Windows version are not ported yet.

## Architecture

- Game logic is shared with the Windows version: `SImulator.ViewModel` and platform-independent files of `SImulator` (linked in `SImulator.Mac.csproj`).
- The control window (`Views/MainWindow.axaml`) is an Avalonia port of the WPF `CommandWindow` (start, game, players, buttons).
- The game board is the same `webtable` web board as in the Windows version. It is shown in a native window with the system WebKit (`Avalonia.Controls.WebView`) and connected through `RemoteBoardServer` (loopback HTTP + WebSocket bridge, session token). The "Browser" screen opens the same board in an external browser.
- Platform services (screens, file dialogs, sounds via `afplay`, timers, keyboard) are in `Implementation/MacPlatformManager.cs`.

## What works

- Package selection from file, recent files
- Board on the main/secondary screen (borderless, covers the working area), in a window or in a browser
- Game flow: round table, questions, answers, players and scores, stakes
- Package media: images, video, audio; media pause / resume / restart from the control window
- Round and question timers with pause
- Player buttons: keyboard (while a SImulator window is active), web buttons
- Russian and English UI (shared resources)

## Not ported yet

- Package library (SIStorage)
- Design, game rules and sounds settings tabs
- Joystick and COM buttons
- Logs folder selection

## Build

Requires .NET 10 SDK.

```bash
dotnet build src/SImulator/SImulator.Mac/SImulator.Mac.csproj
dotnet run --project src/SImulator/SImulator.Mac/SImulator.Mac.csproj -- path/to/package.siq
```

To build `SImulator.app` and a `.dmg` (ad-hoc signed, Apple Silicon by default):

```bash
src/SImulator/SImulator.Mac/build-dmg.sh            # osx-arm64
src/SImulator/SImulator.Mac/build-dmg.sh osx-x64    # Intel
```

The result is placed into `bin/macos/`. As the app is not signed with an Apple Developer certificate,
on the first launch open it via right click → Open.

Set `SIMULATOR_TRACE=1` to write diagnostic trace to the console.
