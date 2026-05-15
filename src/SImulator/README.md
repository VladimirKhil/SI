# SImulator Navigation Guide

`SImulator` is the offline presentation/simulator application. It combines a WPF shell with view model and controller layers for package loading, game presentation, and hardware/input integration.

## Project map

- `SImulator` — WPF application shell and desktop-specific startup/window code
- `SImulator.ViewModel` — main view models, controllers, services, presentation listeners, settings, and UI support

## Primary entry points

### Application startup

- `SImulator/App.xaml.cs`

This is the desktop startup entry point. It:

- loads settings
- creates the host
- configures services
- resolves `MainViewModel`
- creates and shows `CommandWindow`

### Main shell view model

- `SImulator.ViewModel/ViewModel/MainViewModel.cs`

Use this as the starting point for:

- startup flow
- package selection
- application mode transitions
- screen and device configuration

### Main game presentation view model

- `SImulator.ViewModel/ViewModel/GameViewModel.cs`

Use this as the starting point for:

- presentation/game runtime in the simulator
- screen output behavior
- displayed game state

### Main controllers

- `SImulator.ViewModel/Controllers/GameEngineController.cs`
- `SImulator.ViewModel/Controllers/GameController.cs`
- `SImulator.ViewModel/Controllers/WebPresentationController.cs`

## Folder map

### `SImulator`

Primary WPF shell files:

- `App.xaml.cs`
- `CommandWindow.xaml`
- `CommandWindow.xaml.cs`
- `Implementation/*`
- `Helpers/*`

### `SImulator.ViewModel`

- `ButtonManagers` — player button/device handling
- `Contracts` — interfaces and abstractions
- `Controllers` — main presentation/game controllers
- `Core` — core enums and abstractions
- `Listeners` — listeners for presentation events
- `Model` — settings and domain models
- `PlatformSpecific` — platform integration
- `Properties` — generated resources and metadata
- `Services` — app services and action helpers
- `SIUI` — simulator-specific UI support
- `ViewModel` — main application and game view models

## Where to change X

### Startup and service composition

Start with:

- `SImulator/App.xaml.cs`
- `SImulator.ViewModel/ViewModel/MainViewModel.cs`

### Package selection or startup configuration

Start with:

- `SImulator.ViewModel/ViewModel/MainViewModel.cs`
- `SImulator.ViewModel/Model/AppSettings.cs`

### Presentation/game screen behavior

Start with:

- `SImulator.ViewModel/ViewModel/GameViewModel.cs`
- `SImulator.ViewModel/Controllers/WebPresentationController.cs`
- `SImulator.ViewModel/Controllers/GameController.cs`

### Engine/game control coordination

Start with:

- `SImulator.ViewModel/Controllers/GameEngineController.cs`
- `SImulator.ViewModel/Controllers/GameController.cs`
- `SImulator.ViewModel/Services/GameActions.cs`

### Button hardware or input mapping

Start with:

- `SImulator.ViewModel/ButtonManagers/*`
- `SImulator.ViewModel/ViewModel/MainViewModel.cs`

### Window behavior and desktop shell

Start with:

- `SImulator/CommandWindow.xaml.cs`
- `SImulator/CommandWindow.xaml`

## Large-file hotspots

Important navigation hotspots in this area include:

- `SImulator.ViewModel/ViewModel/GameViewModel.cs`
- `SImulator.ViewModel/ViewModel/MainViewModel.cs`
- `SImulator.ViewModel/Controllers/WebPresentationController.cs`
- `SImulator.ViewModel/Model/AppSettings.cs`

## Practical navigation advice

- Start at `App.xaml.cs` for startup and registration questions.
- Start at `MainViewModel.cs` for user-driven setup and mode changes.
- Start at `GameViewModel.cs` for presentation/runtime behavior.
- Search `Controllers/*` first when the issue is coordination rather than pure UI state.
- Search `ButtonManagers/*` for hardware/input issues.

## Companion docs

- `../SICore/SICore/README.md` — runtime/session core guide
- `../Common/SIEngine/DOCUMENTATION.md` — package-level engine guide
- `../Common/SIEngine.Core/DOCUMENTATION.md` — question-level engine guide

## Files usually safe to ignore first

- `Properties/Resources.Designer.cs`
- generated files under `obj/`
- `bin/`
