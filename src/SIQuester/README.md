# SIQuester Navigation Guide

`SIQuester` is the package editor area. It includes the WPF editor shell, the view model workspace system, import/export flows, and package editing logic.

## Project map

- `SIQuester` — WPF application shell, views, controls, resources, services, and desktop-specific implementation
- `SIQuester.ViewModel` — workspace system, document editing logic, item view models, serializers, and services
- `QTxtConverter` — text-to-package conversion support

## Primary entry points

### Application startup

- `SIQuester/App.xaml.cs`

This is the desktop startup entry point. It:

- loads settings
- creates the host
- configures services
- constructs the main view model
- creates and shows the main window

### Main workspace shell

- `SIQuester.ViewModel/Workspaces/MainViewModel.cs`

Use this as the starting point for:

- document/workspace lifetime
- open/save/import commands
- top-level editor actions

### Main document editor

- `SIQuester.ViewModel/Workspaces/QDocument.cs`

This is the main hotspot for package editing behavior and document manipulation.

## Folder map

### `SIQuester`

- `Behaviors` — UI behaviors
- `Contracts` — UI-related interfaces
- `Controls` — custom WPF controls
- `Converters` — XAML converters
- `Helpers` — app/UI helpers
- `Implementation` — desktop-specific implementations
- `Resources` — resource dictionaries and assets
- `Selectors` — data template selectors
- `Services` — service implementations
- `templates` — UI templates
- `View` — XAML views and code-behind
- `wwwroot` — web/static assets used by the app

### `SIQuester.ViewModel`

- `Changes` — change tracking objects
- `Configuration` — editor configuration types
- `Contracts` — interfaces and abstractions
- `Helpers` — helper logic
- `Items` — item-level view models such as package/round/theme/question
- `Model` — editor-side models and settings
- `PlatformSpecific` — platform integration
- `Serializers` — serialization helpers
- `Services` — editor services
- `Workspaces` — main workspace/document logic

## Where to change X

### Open/save/import/editor command behavior

Start with:

- `SIQuester.ViewModel/Workspaces/MainViewModel.cs`
- `SIQuester/App.xaml.cs`

### Document editing logic

Start with:

- `SIQuester.ViewModel/Workspaces/QDocument.cs`
- `SIQuester.ViewModel/Items/*`

### Package, round, theme, or question editors

Start with:

- `SIQuester.ViewModel/Items/PackageViewModel.cs`
- `SIQuester.ViewModel/Items/ThemeViewModel.cs`
- `SIQuester.ViewModel/Items/QuestionViewModel.cs`
- related workspace and view files

### Text/XML/YAML/import flows

Start with:

- `SIQuester.ViewModel/Workspaces/ImportTextViewModel.cs`
- `SIQuester.ViewModel/Workspaces/ImportDBStorageViewModel.cs`
- other `Workspaces/Import*.cs` files
- `QTxtConverter/*` for text conversion support

### Media/sidebar/storage behavior

Start with:

- `SIQuester.ViewModel/Workspaces/Sidebar/MediaStorageViewModel.cs`
- storage-related services and views

### Editor settings and configuration

Start with:

- `SIQuester.ViewModel/Model/AppSettings.cs`
- `SIQuester.Helpers.SettingsHelper`
- `SIQuester/App.xaml.cs`

## Large-file hotspots

Important navigation hotspots in this area include:

- `SIQuester.ViewModel/Workspaces/QDocument.cs`
- `SIQuester.ViewModel/Workspaces/ImportTextViewModel.cs`
- `SIQuester.ViewModel/Workspaces/MainViewModel.cs`
- `SIQuester/View/FlatDocView.xaml`
- `SIQuester/View/DocumentView.xaml`

## Practical navigation advice

- Start at `App.xaml.cs` for startup/composition questions.
- Start at `Workspaces/MainViewModel.cs` for top-level editor actions.
- Start at `QDocument.cs` for document editing behavior.
- Search `Items/*` for changes to specific editable entities.
- Search `Workspaces/*` for document workflows and dialogs.

## Companion docs

- `../Common/SIPackages/README.md` — SIQ package model and file format
- `../../ARCHITECTURE.md` — repository-wide architecture map

## Files usually safe to ignore first

- `Properties/Resources.Designer.cs`
- generated files under `obj/`
- `bin/`
- large static icon dictionaries unless the task is about editor resources
