# SI

Desktop applications and shared libraries for the SI family of projects:

- **SIGame** — desktop game client and host UI
- **SIQuester** — package editor
- **SImulator** — offline presentation/simulator app
- **SICore** — game/session orchestration, messaging, and runtime logic
- **Common** — reusable engines, package APIs, UI libraries, and helpers

## Requirements

- **.NET 10 SDK** is required to compile the main solutions.
- **Visual Studio 2026** is required to publish the SImulator ClickOnce application.
- **WiX Toolset** is required to build app installers.

## Start here

- [`ARCHITECTURE.md`](ARCHITECTURE.md) — repository-wide map, entry points, and common workflows
- [`NAVIGABILITY_REPORT.md`](NAVIGABILITY_REPORT.md) — assessment of code structure and documentation gaps

## Area guides

- [`src/SICore/SICore/README.md`](src/SICore/SICore/README.md) — core game/session runtime guide
- [`src/SIGame/README.md`](src/SIGame/README.md) — SIGame app and view model guide
- [`src/SIQuester/README.md`](src/SIQuester/README.md) — SIQuester editor guide
- [`src/SImulator/README.md`](src/SImulator/README.md) — SImulator guide
- [`src/Common/SIEngine/DOCUMENTATION.md`](src/Common/SIEngine/DOCUMENTATION.md) — package-level engine behavior
- [`src/Common/SIEngine.Core/DOCUMENTATION.md`](src/Common/SIEngine.Core/DOCUMENTATION.md) — question-level engine behavior
- [`src/SICore/SICore/GAME_AGENT_DOCUMENTATION.md`](src/SICore/SICore/GAME_AGENT_DOCUMENTATION.md) — game agent messaging protocol
- [`src/Common/SIPackages/README.md`](src/Common/SIPackages/README.md) — SIQ package format and APIs

## Repository layout

- `src/Common` — shared libraries used by multiple apps
- `src/SICore` — game/session runtime and network infrastructure
- `src/SIGame` — main desktop game application
- `src/SIQuester` — package editing tools
- `src/SImulator` — simulator/presentation app
- `test` — tests, generally grouped by product area
- `assets` — schemas and supporting assets
- `tools`, `deploy`, `web` — supporting infrastructure and packaging

## Solutions

- **SImulator** — SIGame offline app for hosting a game with a single computer and projector
- **SIQuester** — SIGame questions editor
- **SIGame** — SIGame desktop app

## Key projects

- `src/Common/Notions` — helper methods for working with strings
- `src/Common/SI.GameServer.Client` — client for the SIGame server
- `src/Common/SI.GameServer.Contract` — SIGame server contract
- `src/Common/SIEngine` — package-level SIGame engine
- `src/Common/SIEngine.Core` — question-level engine
- `src/Common/SIPackages` — SIQ package model and persistence APIs
- `src/Common/SIUI` — shared SIGame table UI
- `src/Common/Utils` — auxiliary shared classes
- `src/SIQuester/QTxtConverter` — text-to-package conversion
- `src/SICore/SICore` — high-level SIGame runtime
- `src/SICore/SICore.Connections` — low-level network connection abstractions
- `src/SICore/SICore.Network` — network nodes and client abstractions
- `src/SICore/SIData` — shared models for client and server

## Practical navigation tips

### If you need to change gameplay runtime behavior
Start with:

- `src/SICore/SICore/README.md`
- `src/SICore/SICore/GameRunner.cs`
- `src/SICore/SICore/Clients/Game/GameController.cs`
- `src/SICore/SICore/Clients/Game/Game.cs`

### If you need to change question or package engine behavior
Start with:

- `src/Common/SIEngine/DOCUMENTATION.md`
- `src/Common/SIEngine.Core/DOCUMENTATION.md`
- `src/Common/SIEngine/GameEngine.cs`
- `src/Common/SIEngine.Core/QuestionEngine.cs`

### If you need to change SIGame desktop UI behavior
Start with:

- `src/SIGame/README.md`
- `src/SIGame/SIGame/App.xaml.cs`
- `src/SIGame/SIGame.ViewModel/ViewModel/MainViewModel.cs`

### If you need to change package editing behavior
Start with:

- `src/SIQuester/README.md`
- `src/SIQuester/SIQuester/App.xaml.cs`
- `src/SIQuester/SIQuester.ViewModel/Workspaces/MainViewModel.cs`
- `src/SIQuester/SIQuester.ViewModel/Workspaces/QDocument.cs`

### If you need to change simulator behavior
Start with:

- `src/SImulator/README.md`
- `src/SImulator/SImulator/App.xaml.cs`
- `src/SImulator/SImulator.ViewModel/ViewModel/MainViewModel.cs`
- `src/SImulator/SImulator.ViewModel/ViewModel/GameViewModel.cs`

## Files to usually ignore during feature work

Unless the task is specifically about build output, localization generation, or resource generation, contributors can usually ignore:

- `bin/`
- `obj/`
- `Properties/Resources.Designer.cs`
- generated `*.g.cs` and `*.g.i.cs` files
- very large static resource dictionaries unless the task is about theme/resource definitions

## Build

- `dotnet build src\SIGame\SIGame\SIGame.csproj`
- `dotnet build src\SIQuester\SIQuester\SIQuester.csproj`
- `dotnet build src\SImulator\SImulator\SImulator.csproj`

## Scripts

- `tools/sigame-build.ps1` — builds a SIGame project in Release configuration and sets the provided version number
- `tools/sigame-build-msi.ps1` — builds a SIGame MSI for the provided platform
- `tools/sigame-build-setup.ps1` — builds a SIGame installer
- `tools/sigame-publish.ps1` — publishes SIGame and sets the provided version number
- `tools/siquester-build.ps1` — builds SIQuester in Release configuration
- `tools/siquester-build-msi.ps1` — builds a SIQuester MSI for win-x86 and win-x64
- `tools/siquester-publish.ps1` — publishes SIQuester
- `tools/simulator-build.ps1` — builds SImulator in Release configuration
- `tools/simulator-publish.ps1` — publishes SImulator in Release configuration for win-x86 as a compressed single file
