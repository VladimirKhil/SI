# SIGame Navigation Guide

`SIGame` contains the main desktop application, its WPF UI, and the related view model layers used to host or join games.

## Project map

- `SIGame` — WPF application shell, views, controls, converters, themes, and desktop-specific implementation
- `SIGame.ViewModel` — application view models, services, settings, package sources, and networking helpers
- `SIGame.ViewModel.Web` — web-oriented view model project

## Primary entry points

### Application startup

- `SIGame/App.xaml.cs`

This is the desktop startup entry point. It:

- loads settings
- creates the host
- registers services
- resolves `MainViewModel`
- creates and shows the main window

### Service registration

- `SIGame.ViewModel/ServiceCollectionExtensions.cs`

This is where `AddSIGame()` registers the main application services and the root `MainViewModel`.

### Main shell view model

- `SIGame.ViewModel/ViewModel/MainViewModel.cs`

Use this as the starting point for:

- main menu behavior
- top-level navigation
- switching between major application views

## Folder map

### `SIGame`

- `Behaviors` — attached behaviors and UI helpers
- `Contracts` — UI-facing interfaces
- `Controls` — reusable WPF controls
- `Converters` — value converters used in XAML
- `Data` — UI data types
- `Helpers` — UI/application helpers
- `Implementation` — desktop-specific implementation details
- `Resources` — resource dictionaries and assets
- `Selectors` — data template selectors
- `Theme` — theme assets and styling
- `View` — XAML views and code-behind
- `WinAPI` — Windows-specific interop helpers

### `SIGame.ViewModel`

- `Contracts` — interfaces for services and abstractions
- `Helpers` — small helper logic
- `Implementation` — concrete implementations
- `Managers` — management/state helper classes
- `Models` — small data and state models
- `Network` — node and network-related pieces
- `PackageSources` — package selection/loading sources
- `PlatformSpecific` — platform abstractions and implementations
- `Services` — main application services
- `Settings` — persisted settings and configuration models
- `ViewModel` — main application view models

## Where to change X

### Main menu or top-level navigation

Start with:

- `SIGame.ViewModel/ViewModel/MainViewModel.cs`
- `SIGame/View/MainMenuView.xaml.cs`
- `SIGame/View/StartMenuView.xaml.cs`

### Game screen or main game interaction

Start with:

- `SIGame.ViewModel/ViewModel/GameViewModel.cs`
- `SIGame/View/MainView.xaml.cs`
- `SIGame/View/Studia.xaml.cs`
- `SIGame/View/ShowmanView.xaml.cs`
- `SIGame/View/PersonView.xaml.cs`

### Online/network game behavior

Start with:

- `SIGame.ViewModel/ViewModel/SIOnlineViewModel.cs`
- `SIGame.ViewModel/ViewModel/SINetworkViewModel.cs`
- `SIGame/View/SIOnlineView.xaml.cs`
- `SIGame/View/SINetworkView.xaml.cs`

### Viewer/player interaction logic

Start with:

- `SIGame.ViewModel/ViewModel/Logic/ViewerHumanLogic.cs`
- `SIGame.ViewModel/ViewModel/PlayerViewModel.cs`
- `SIGame.ViewModel/ViewModel/ShowmanViewModel.cs`

### Game settings and package selection

Start with:

- `SIGame.ViewModel/ViewModel/GameSettingsViewModel.cs`
- `SIGame.ViewModel/Services/GameSettingsViewModelFactory.cs`
- `SIGame.ViewModel/PackageSources/*`
- `SIGame/View/*SettingsPage.xaml.cs`

### App settings or persisted user options

Start with:

- `SIGame.ViewModel/Settings/AppSettings.cs`
- `SIGame.ViewModel/Settings/UserSettings.cs`
- `SIGame/Helpers/SettingsManager.cs`
- `SIGame/View/AppSettingsPage.xaml.cs`

### UI theming, visuals, or bindings

Start with:

- `SIGame/Theme/*`
- `SIGame/Resources/*`
- `SIGame/Converters/*`
- `SIGame/Behaviors/*`

## Large-file hotspots

Important navigation hotspots in this area include:

- `SIGame.ViewModel/ViewModel/Logic/ViewerHumanLogic.cs`
- `SIGame.ViewModel/ViewModel/GameViewModel.cs`
- `SIGame.ViewModel/ViewModel/GameSettingsViewModel.cs`
- `SIGame.ViewModel/ViewModel/SIOnlineViewModel.cs`
- `SIGame/App.xaml`

## Practical navigation advice

- Start at `App.xaml.cs` for application startup questions.
- Start at `MainViewModel.cs` for top-level navigation questions.
- Prefer `ViewModel/*` for behavior and `View/*` for layout and bindings.
- Search `PackageSources` first for package-loading changes.
- Search `Settings` before editing startup or persistence logic.

## Companion docs

- `../SICore/SICore/README.md` — runtime/session core guide
- `../Common/SIEngine/DOCUMENTATION.md` — package-level engine guide
- `../Common/SIEngine.Core/DOCUMENTATION.md` — question-level engine guide

## Files usually safe to ignore first

- `Properties/Resources.Designer.cs`
- generated files under `obj/`
- `bin/`
