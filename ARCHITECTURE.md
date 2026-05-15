# Architecture and Navigation Guide

This document is a repository-wide map intended to help contributors find the right project, folder, and file before making changes.

## High-level mental model

The repository has three main desktop applications:

- **SIGame** — game client and host UI
- **SIQuester** — package editor
- **SImulator** — presentation/offline simulator

These applications are built on shared libraries:

- **SICore** — high-level game runtime, orchestration, and messaging
- **SIEngine** — package-level game engine
- **SIEngine.Core** — question-level state machine
- **SIPackages** — package model and SIQ persistence
- **SIUI** and related UI libraries — reusable UI pieces

## Top-level layout

- `src/Common` — shared libraries
- `src/SICore` — runtime game/session core and networking
- `src/SIGame` — desktop game application and view models
- `src/SIQuester` — editor application and view models
- `src/SImulator` — simulator application and view models
- `test` — test projects
- `assets` — schemas and package-format assets
- `tools` — build/publish scripts
- `web` — web-related assets and projects

## Core runtime stack

The runtime stack is layered roughly like this:

1. **SIPackages** reads and writes SIQ packages.
2. **SIEngine.Core** plays a single question as a state machine.
3. **SIEngine** plays a full package/round/question sequence.
4. **SICore** wraps engine behavior with players, showman, viewers, network messaging, timers, and session rules.
5. **SIGame** and **SImulator** provide application/UI layers.

## Main entry points by area

### SIGame

Application startup:

- `src/SIGame/SIGame/App.xaml.cs`

Desktop application composition:

- host and service configuration are created in `App.xaml.cs`
- `services.AddSIGame()` is called from `src/SIGame/SIGame.ViewModel/ServiceCollectionExtensions.cs`
- the desktop shell uses `src/SIGame/SIGame.ViewModel/ViewModel/MainViewModel.cs`

Typical UI/view model hotspots:

- `src/SIGame/SIGame.ViewModel/ViewModel/MainViewModel.cs`
- `src/SIGame/SIGame.ViewModel/ViewModel/GameViewModel.cs`
- `src/SIGame/SIGame.ViewModel/ViewModel/GameSettingsViewModel.cs`
- `src/SIGame/SIGame.ViewModel/ViewModel/Logic/ViewerHumanLogic.cs`

### SIQuester

Application startup:

- `src/SIQuester/SIQuester/App.xaml.cs`

Editor shell and workspaces:

- `src/SIQuester/SIQuester.ViewModel/Workspaces/MainViewModel.cs`
- `src/SIQuester/SIQuester.ViewModel/Workspaces/QDocument.cs`

Typical editor hotspots:

- document editing and package manipulation in `QDocument.cs`
- import workflows in `Workspaces/Import*.cs`
- item-level editors in `Items/*`

### SImulator

Application startup:

- `src/SImulator/SImulator/App.xaml.cs`

Shell and main orchestration:

- `src/SImulator/SImulator/View/CommandWindow.xaml.cs` is not present; the window code-behind is `src/SImulator/SImulator/CommandWindow.xaml.cs`
- `src/SImulator/SImulator.ViewModel/ViewModel/MainViewModel.cs`
- `src/SImulator/SImulator.ViewModel/ViewModel/GameViewModel.cs`

Controller layer:

- `src/SImulator/SImulator.ViewModel/Controllers/GameEngineController.cs`
- `src/SImulator/SImulator.ViewModel/Controllers/GameController.cs`
- `src/SImulator/SImulator.ViewModel/Controllers/WebPresentationController.cs`

### SICore

New game creation and composition:

- `src/SICore/SICore/GameRunner.cs`

Main runtime hotspots:

- `src/SICore/SICore/Clients/Game/GameController.cs`
- `src/SICore/SICore/Clients/Game/Game.cs`
- `src/SICore/SICore/Clients/Viewer/Viewer.cs`
- `src/SICore/SICore/Services/Intelligence.cs`

Question-specific runtime state and handlers:

- `src/SICore/SICore/Clients/Game/QuestionPlayHandler.cs`
- `src/SICore/SICore/Clients/Game/QuestionPlayState.cs`
- `src/SICore/SICore/Clients/Game/PlayHandler.cs`

### Engines and package model

Package-level engine:

- `src/Common/SIEngine/GameEngine.cs`
- see `src/Common/SIEngine/DOCUMENTATION.md`

Question-level engine:

- `src/Common/SIEngine.Core/QuestionEngine.cs`
- see `src/Common/SIEngine.Core/DOCUMENTATION.md`

Package model and file format:

- `src/Common/SIPackages/README.md`

## Folder maps

### `src/Common`

Shared libraries. Common reasons to look here:

- package model and SIQ IO → `SIPackages`
- question/package playback engines → `SIEngine`, `SIEngine.Core`
- shared UI bits → `SIUI`, `SIUI.Model`, `SIUI.ViewModel`
- shared helpers → `Utils`, `Notions`
- server/service contracts and clients → `SI.GameServer.*`

### `src/SICore`

Core runtime and networking. Typical responsibilities:

- runtime game orchestration → `SICore`
- higher-level nodes and clients → `SICore.Network`
- lower-level connection primitives → `SICore.Connections`
- shared runtime models → `SIData`, `SI.Contracts`

### `src/SIGame`

Desktop application and presentation logic:

- WPF app shell → `SIGame`
- desktop view models and services → `SIGame.ViewModel`
- web-oriented view model project → `SIGame.ViewModel.Web`

### `src/SIQuester`

Editor-related projects:

- WPF app shell → `SIQuester`
- editor view model and workspaces → `SIQuester.ViewModel`
- text converter → `QTxtConverter`

### `src/SImulator`

Simulator-related projects:

- WPF app shell → `SImulator`
- simulator view models/controllers → `SImulator.ViewModel`

## Where to change X

### Question playback behavior

Start with:

- `src/Common/SIEngine.Core/QuestionEngine.cs`
- `src/Common/SIEngine.Core/DOCUMENTATION.md`
- `src/SICore/SICore/Clients/Game/QuestionPlayHandler.cs`
- `src/SICore/SICore/Clients/Game/QuestionPlayState.cs`

### Package or round flow

Start with:

- `src/Common/SIEngine/GameEngine.cs`
- `src/Common/SIEngine/DOCUMENTATION.md`
- `src/SICore/SICore/Clients/Game/GameController.cs`
- `src/SICore/SICore/Clients/Game/Game.cs`

### Game session creation and wiring

Start with:

- `src/SICore/SICore/GameRunner.cs`
- `src/SIGame/SIGame/App.xaml.cs`
- `src/SImulator/SImulator/App.xaml.cs`

### Network or message flow

Start with:

- `src/SICore/SICore/GAME_AGENT_DOCUMENTATION.md`
- `src/SICore/SICore/Messages.cs`
- `src/SICore/SICore.Network/Clients/Client.cs`
- `src/SICore/SICore.Network/Nodes/*`
- `src/SICore/SICore.Connections/*`

### SIGame main menu or navigation

Start with:

- `src/SIGame/SIGame.ViewModel/ViewModel/MainViewModel.cs`
- `src/SIGame/SIGame/View/*`
- `src/SIGame/SIGame/Converters/*`

### SIQuester document editing

Start with:

- `src/SIQuester/SIQuester.ViewModel/Workspaces/QDocument.cs`
- `src/SIQuester/SIQuester.ViewModel/Items/*`
- `src/SIQuester/SIQuester/View/*`

### SImulator startup and presentation flow

Start with:

- `src/SImulator/SImulator/App.xaml.cs`
- `src/SImulator/SImulator.ViewModel/ViewModel/MainViewModel.cs`
- `src/SImulator/SImulator.ViewModel/ViewModel/GameViewModel.cs`
- `src/SImulator/SImulator.ViewModel/Controllers/*`

## Large-file hotspots

These files are important, but they are also major navigation hotspots:

- `src/SICore/SICore/Clients/Game/GameController.cs`
- `src/SICore/SICore/Clients/Game/Game.cs`
- `src/SICore/SICore/Clients/Viewer/Viewer.cs`
- `src/SICore/SICore/Services/Intelligence.cs`
- `src/SIQuester/SIQuester.ViewModel/Workspaces/QDocument.cs`
- `src/SImulator/SImulator.ViewModel/ViewModel/GameViewModel.cs`
- `src/SIGame/SIGame.ViewModel/ViewModel/Logic/ViewerHumanLogic.cs`

When working in these files, search for narrow domain terms first instead of reading top-to-bottom.

## Folders with broader names

Some folders are useful but broad:

- `Helpers`
- `Utils`
- `Special`
- `Other`

Search these only after checking more domain-specific folders.

## Generated or noisy files

For most feature work, ignore:

- `bin/`
- `obj/`
- `Properties/Resources.Designer.cs`
- generated `*.g.cs` and `*.g.i.cs`
- very large static resource XAML dictionaries unless the task is specifically about resources or theme assets

## Navigation strategy

Recommended search order:

1. pick the right top-level area (`SIGame`, `SIQuester`, `SImulator`, `SICore`, `Common`)
2. identify the right project
3. identify the right workflow folder
4. only then inspect the largest coordinator files

For best results, search with domain terms such as:

- question flow
- round flow
- selection
- validation
- viewer
- showman
- storage
- package
- import
- presentation
- network
