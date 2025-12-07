# AGENTS.md

This file provides guidance for AI agents working with the SI (SIGame) codebase.

## Project Overview

SI is a suite of applications for playing trivia/quiz games. The project consists of several main components:

- **SIGame** - The main game client (WPF application) for online play
- **SIQuester** - Question package editor
- **SImulator** - Offline version of SIGame for local play without server

## Solution Structure

The workspace contains three main solution files:
- `SIGame.sln` - Main game solution
- `SIQuester.sln` - Question editor solution
- `SImulator.sln` - Simulator solution

### Key Directories

- `src/` - Source code
  - `src/Common/` - Shared libraries (SIPackages, SIEngine, Utils, etc.)
  - `src/SICore/` - Core game logic
  - `src/SIGame/` - Game client
  - `src/SIQuester/` - Question editor
  - `src/SImulator/` - Simulator
- `test/` - Unit tests mirroring src structure
- `tools/` - Build and deployment scripts
- `deploy/` - Deployment configurations
- `assets/` - Schema files (XSD)

## Technology Stack

- **Language**: C# (.NET)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Target Framework**: .NET 6.0+
- **Build System**: MSBuild / dotnet CLI

## Building the Project

```powershell
# Build SIGame
dotnet build SIGame.sln

# Build SIQuester
dotnet build SIQuester.sln

# Build SImulator
dotnet build SImulator.sln
```

## Running Tests

```powershell
# Run all tests
dotnet test

# Run specific test project
dotnet test test/Common/SIEngine.Tests/SIEngine.Tests.csproj
```

## Code Style Guidelines

- Follow standard C# naming conventions
- Use meaningful variable and method names
- Keep methods focused and single-purpose
- Add XML documentation for public APIs
- Use async/await for I/O operations

## Package Format

The SI package format (`.siq`) is a ZIP archive containing:
- `content.xml` - Package structure and metadata
- Media files (images, audio, video)

Schema definitions are in `assets/` directory.

## Common Tasks

### Adding a New Feature
1. Identify the appropriate project/layer
2. Create necessary models in SIData if needed
3. Implement logic in SICore/SIEngine
4. Add UI in the appropriate client (SIGame/SIQuester)
5. Write unit tests

### Modifying Game Logic
- Core game state and rules are in `src/SICore/`
- Engine mechanics are in `src/Common/SIEngine/`

### Working with Packages
- Package handling is in `src/Common/SIPackages/`
- Package format schemas in `assets/`

## Important Notes

- WPF applications are Windows-only
- Some projects may have platform-specific configurations
